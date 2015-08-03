using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace PlayerUI
{
	public class MediaDecoder
	{
		public class Error
		{
			public long major;
			public int minor;
		}

		public Error LastError;

		private MediaEngine _mediaEngine;
		private MediaEngineEx _mediaEngineEx;
		private object criticalSection = new object();

		private long ts;
		private bool _stereoVideo = false;
		private int w, h;

		private Texture2D textureL;
		private Texture2D textureR;
		public Texture2D TextureL { get { return this.textureL; } }
		public Texture2D TextureR { get { return this.textureR; } }

		public bool IsStereo { get { return IsPlaying ? _stereoVideo : false; } }
		public bool IsPlaying { get; private set; }
		public bool IsPaused { get {
				lock(criticalSection)
				{
					return _initialized ? (bool)_mediaEngineEx.IsPaused : false;
				}
			} }

		public bool IsEnded
		{
			get
			{
				lock (criticalSection)
				{
					return _initialized? (bool)_mediaEngineEx.IsEnded : false;
				}
			}
		}
		public bool Ready { get; private set; }

		public double Duration { get { return _mediaEngineEx.Duration; } }

		private bool _initialized = false;
		private bool _rendering = false;
		private ManualResetEvent waitForRenderingEnd = new ManualResetEvent(false);

		private static MediaDecoder _instance = null;
		public static MediaDecoder Instance { get { return MediaDecoder._instance; } }

		private SharpDX.Direct3D11.Device _device;
		private Factory _factory;
		private FeatureLevel[] _levels = new FeatureLevel[] { FeatureLevel.Level_11_0 };
		private DXGIDeviceManager _dxgiManager;

		public event Action<bool> OnPlay = delegate { };
		public event Action<double> OnReady = delegate { };
		public event Action OnStop = delegate { };
		public event Action OnEnded = delegate { };
		public event Action OnError = delegate { };
		public event Action OnAbort = delegate { };
		public event Action<double> OnTimeUpdate = delegate { };

		VideoNormalizedRect topRect = new VideoNormalizedRect()
		{
			Left = 0,
			Top = 0,
			Right = 1,
			Bottom = 0.5f
		};

		VideoNormalizedRect bottomRect = new VideoNormalizedRect()
		{
			Left = 0,
			Top = 0.5f,
			Right = 1,
			Bottom = 1f
		};

		public MediaDecoder()
		{
			IsPlaying = false;
			Ready = false;
			MediaDecoder._instance = this;
			_initialized = false;

			_factory = new SharpDX.DXGI.Factory();
			_device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, _levels);

			DeviceMultithread mt = _device.QueryInterface<DeviceMultithread>();
			mt.SetMultithreadProtected(true);

			using (SharpDX.DXGI.Device1 dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device1>())
			{
				dxgiDevice.MaximumFrameLatency = 1;
			}

			_dxgiManager = new DXGIDeviceManager();
		}


		public void Init()
		{
			criticalSection = new object();
			lock(criticalSection)
			{
				if (_initialized) return;

				MediaManager.Startup();
				var mediaEngineFactory = new MediaEngineClassFactory();

				_dxgiManager.ResetDevice(_device);

				MediaEngineAttributes attr = new MediaEngineAttributes();
				attr.VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm;
				attr.DxgiManager = _dxgiManager;

				_mediaEngine = new MediaEngine(mediaEngineFactory, attr, MediaEngineCreateFlags.None);

				_mediaEngine.PlaybackEvent += (playEvent, param1, param2) =>
				{
					switch (playEvent)
					{
						case MediaEngineEvent.CanPlay:
							Console.WriteLine(string.Format("CAN PLAY {0}, {1}", param1, param2));
							Ready = true;
							OnReady(_mediaEngineEx.Duration);
							break;

						case MediaEngineEvent.TimeUpdate:
							OnTimeUpdate(_mediaEngineEx.CurrentTime);
							break;

						case MediaEngineEvent.Error:
							Console.WriteLine(string.Format("ERROR {0}, {1}", param1, param2));
							Console.WriteLine(((MediaEngineErr)param1).ToString());
							LastError = new Error() { major = param1, minor = param2 };

							OnError();
							Stop();
							break;

						case MediaEngineEvent.Abort:
							Console.WriteLine(string.Format("ABORT {0}, {1}", param1, param2));
							OnAbort();
							Stop();
							break;

						case MediaEngineEvent.Ended:
							Console.WriteLine(string.Format("ENDED {0}, {1}", param1, param2));
							OnEnded();
							break;
					}
				};

				_mediaEngineEx = _mediaEngine.QueryInterface<MediaEngineEx>();
				_mediaEngineEx.EnableWindowlessSwapchainMode(true);

				mediaEngineFactory.Dispose();
				_initialized = true;
			}
		}

		

		public void Play()
		{
			lock(criticalSection)
			{
				if (IsPlaying) return;

				if (!_initialized) return;
				if (!Ready) return;


				var hasVideo = _mediaEngine.HasVideo();
				var hasAudio = _mediaEngine.HasAudio();

				if (hasVideo)
				{
					_mediaEngine.GetNativeVideoSize(out w, out h);


					float videoAspect = w / h;
					_stereoVideo = videoAspect < 1.5;
					h = _stereoVideo ? h / 2 : h;

					Texture2DDescription frameTextureDescription = new Texture2DDescription()
					{
						Width = w,
						Height = h,
						MipLevels = 1,
						ArraySize = 1,
						Format = Format.B8G8R8A8_UNorm,
						Usage = ResourceUsage.Default,
						SampleDescription = new SampleDescription(1, 0),
						BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
						CpuAccessFlags = CpuAccessFlags.None,
						OptionFlags = ResourceOptionFlags.Shared
					};


					textureL = new SharpDX.Direct3D11.Texture2D(_device, frameTextureDescription);
					textureR = new SharpDX.Direct3D11.Texture2D(_device, frameTextureDescription);
				}

				_mediaEngineEx.Play();
				_mediaEngineEx.Volume = 0.2;
				IsPlaying = true;
			}

			Task t = Task.Factory.StartNew(() =>
			{
				_rendering = true;
				while (_mediaEngine != null && IsPlaying)
				{
					lock (criticalSection)
					{
						if (!_mediaEngine.IsPaused)
						{
							long lastTs = ts;
							bool result = _mediaEngine.OnVideoStreamTick(out ts);

							if (result && ts != lastTs)
							{
								if (_stereoVideo)
								{
									_mediaEngine.TransferVideoFrame(textureL, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
									_mediaEngine.TransferVideoFrame(textureR, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);
								}
								else
								{
									_mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
								}
							}
						}
					}
				}
				waitForRenderingEnd.Set();
				_rendering = false;
			});
			
		}

		public void Pause()
		{
			if(IsPlaying)
			{
				lock (criticalSection)
				{
					if (!_mediaEngine.IsPaused)
						_mediaEngine.Pause();
				}
			}
		}

		public void Unpause()
		{
			if (IsPlaying)
			{
				lock (criticalSection)
				{
					if (_mediaEngine.IsPaused)
						_mediaEngine.Play();
				}
			}
		}

		public void TogglePause()
		{
			if(IsPlaying)
			{
				if (IsPaused) Unpause();
				else Pause();
			}
		}

		public void SetVolume(double volume)
		{
			if(IsPlaying)
			{
				_mediaEngine.Volume = volume;
			}
		}

		public void Seek(double time)
		{
			if(IsPlaying)
			{
				lock (criticalSection)
				{
					if (!_mediaEngineEx.IsSeeking)
					{
						_mediaEngineEx.CurrentTime = time;
					}
				}
			}
		}

		
		public void Stop(bool force = false)
		{
			if (!force)
			{
				if (!_initialized) return;
				if (!IsPlaying) return;
			}

			waitForRenderingEnd.Reset();
			IsPlaying = false;
			
			if (_rendering) {

				waitForRenderingEnd.WaitOne(1000);
			}
			
			Ready = false;

			lock (criticalSection)
			{
				_mediaEngineEx.Shutdown();
				_mediaEngineEx.Dispose();
				_mediaEngine.Dispose();


				fileStream?.Close();
				fileStream?.Dispose();
				webStream?.Close();
				webStream?.Dispose();
				stream?.Close();
				stream?.Dispose();

				textureL?.Dispose();
				textureR?.Dispose();

				_initialized = false;
	
			}
		}

		private FileStream fileStream;
		private Stream webStream;
		private ByteStream stream;
		private Uri url;

		public void LoadMedia(string fileName)
		{
			Stop();
			Init();

			if (fileName.Contains("http://") || fileName.Contains("https://"))
			{
				webStream = new System.Net.WebClient().OpenRead(fileName);
				stream = new ByteStream(webStream);
				url = new Uri(fileName, UriKind.Absolute);

				_mediaEngineEx.SetSourceFromByteStream(stream, url.AbsoluteUri);
				_mediaEngineEx.Load();
			}
			else
			{
				fileStream = File.OpenRead(fileName);
				stream = new ByteStream(fileStream);
				url = new Uri(fileStream.Name, UriKind.RelativeOrAbsolute);
				_mediaEngineEx.SetSourceFromByteStream(stream, url.AbsoluteUri);
			}
		}

		public MediaEngine Engine { get { return this._mediaEngine; } }

		public void Shutdown()
		{
			MediaManager.Shutdown();
			_dxgiManager.Dispose();
			_factory.Dispose();
			_device.Dispose();
		}

	}
}
