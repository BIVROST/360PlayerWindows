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
		private DPFCanvas _canvas;
		private MediaEngine _mediaEngine;
		private MediaEngineEx _mediaEngineEx;
		private ManualResetEvent eventReadyToPlay = new ManualResetEvent(false);
		private long ts;
		private CancellationTokenSource _cancelTransferFrame;
		private Surface _surface;
		private bool _stereoVideo = false;

		private Texture2D textureL;
		private Texture2D textureR;
		private Surface surfaceL;
		private Surface surfaceR;

		private DXGIDeviceManager dxgiManager;

		public Texture2D TextureL { get { return this.textureL; } }
		public Texture2D TextureR { get { return this.textureR; } }

		public bool IsPlaying { get; private set; }

		private static MediaDecoder _instance = null;
		public static MediaDecoder Instance { get { return MediaDecoder._instance; } }

		private SharpDX.Direct3D11.Device _device;
		public SharpDX.Direct3D11.Device Device { get { return _device; } set { _device = value; } }

		private Thread frameGrabber;
		private Window _currentWindow;

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

		public MediaDecoder(DPFCanvas canvas, Window currentWindow)
		{
			IsPlaying = false;
			MediaDecoder._instance = this;
			this._canvas = canvas;
			this._currentWindow = currentWindow;
		}

		private SwapChain _swapChain;
		private Texture2D _backBufferTexture;
		private RenderTargetView _backBufferRenderTargetView;
		private Texture2D _swapChainTexture;

		public void Init()
		{
			System.Diagnostics.Debug.WriteLine("Media Init: " + System.Threading.Thread.CurrentThread.ManagedThreadId + " " + System.Threading.Thread.CurrentThread.GetApartmentState());

			SharpDX.DXGI.Factory factory = new SharpDX.DXGI.Factory();
			FeatureLevel[] levels = new FeatureLevel[] { FeatureLevel.Level_11_0};


			_device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, levels);
			//SharpDX.Direct3D11.DeviceDebug _deviceDebug = _device.QueryInterface<DeviceDebug>();
			//InfoQueue _deviceInfoQueue = _deviceDebug.QueryInterface<InfoQueue>();
			//_deviceInfoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
			//_deviceInfoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
			//_deviceInfoQueue.SetBreakOnSeverity(MessageSeverity.Warning, true);
			//_device = _canvas.GetDevice();

			DeviceMultithread mt = _device.QueryInterface<DeviceMultithread>();
			mt.SetMultithreadProtected(true);
			
			

			//Texture2DDescription frameTextureDescription = new Texture2DDescription()
			//{
			//	Width = 4096,
			//	Height = 4096,
			//	MipLevels = 1,
			//	ArraySize = 1,
			//	Format = Format.B8G8R8A8_UNorm,
			//	Usage = ResourceUsage.Default,
			//	SampleDescription = new SampleDescription(1, 0),
			//	BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
			//	CpuAccessFlags = CpuAccessFlags.None,
			//	OptionFlags = ResourceOptionFlags.Shared
			//};

			//_swapChainTexture = new Texture2D(_device, frameTextureDescription);

			//SwapChainDescription swapChainDescription = new SwapChainDescription();
			//swapChainDescription.BufferCount = 1;
			//swapChainDescription.IsWindowed = true;
			//swapChainDescription.OutputHandle = IntPtr.Zero;
			//swapChainDescription.SampleDescription = new SampleDescription(1, 0);
			//swapChainDescription.Usage = Usage.RenderTargetOutput | Usage.ShaderInput;
			//swapChainDescription.SwapEffect = SwapEffect.Sequential;
			//swapChainDescription.Flags = SwapChainFlags.AllowModeSwitch;
			//swapChainDescription.ModeDescription.Width = 4096;
			//swapChainDescription.ModeDescription.Height = 4096;
			//swapChainDescription.ModeDescription.Format = Format.R8G8B8A8_UNorm;
			//swapChainDescription.ModeDescription.RefreshRate.Numerator = 0;
			//swapChainDescription.ModeDescription.RefreshRate.Denominator = 1;

			//// Create the swap chain.
			
			//_swapChain = new SwapChain(factory, _device, swapChainDescription);

			// Retrieve the back buffer of the swap chain.
			//_backBufferTexture = _swapChain.GetBackBuffer<Texture2D>(0); // = BackBuffer
			//_backBufferRenderTargetView = new RenderTargetView(_device, _backBufferTexture);      // = BackBufferRT



			using (SharpDX.DXGI.Device1 dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device1>())
			{
				dxgiDevice.MaximumFrameLatency = 1;
			}




			MediaManager.Startup();
			var mediaEngineFactory = new MediaEngineClassFactory();
			dxgiManager = new DXGIDeviceManager();
			

			dxgiManager.ResetDevice(_device);

			MediaEngineAttributes attr = new MediaEngineAttributes();
			attr.VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm;
			attr.DxgiManager = dxgiManager;

			_mediaEngine = new MediaEngine(mediaEngineFactory, attr, MediaEngineCreateFlags.None);
			//_mediaEngine = new MediaEngine(mediaEngineFactory, null, MediaEngineCreateFlags.AudioOnly);

			_mediaEngine.PlaybackEvent += (playEvent, param1, param2) =>
			{
				switch (playEvent)
				{
					case MediaEngineEvent.CanPlay:
						Console.WriteLine(string.Format("CAN PLAY {0}, {1}", param1, param2));
						break;

					case MediaEngineEvent.TimeUpdate:
						//Console.WriteLine(string.Format("Time Update {0}, {1}", param1, param2));
						//Console.WriteLine(_mediaEngine.CurrentTime);
						break;

					case MediaEngineEvent.Error:
						Console.WriteLine(string.Format("ERROR {0}, {1}", param1, param2));
						Console.WriteLine(((MediaEngineErr)param1).ToString());

						break;

					case MediaEngineEvent.Abort:
						Console.WriteLine(string.Format("ABORT {0}, {1}", param1, param2));
						break;

					case MediaEngineEvent.Ended:
						Console.WriteLine(string.Format("ENDED {0}, {1}", param1, param2));
						break;
				}
			};

			_mediaEngineEx = _mediaEngine.QueryInterface<MediaEngineEx>();
			_mediaEngineEx.EnableWindowlessSwapchainMode(true);
			
		}

		int w, h;

		public void Play()
		{	
			
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

				BivrostPlayerPrototype.PlayerPrototype.videoTextureL = textureL;
				BivrostPlayerPrototype.PlayerPrototype.videoTextureR = textureR;


				//TextureCreated(textureL);

				surfaceL = textureL.QueryInterface<SharpDX.DXGI.Surface>();
				surfaceR = textureR.QueryInterface<SharpDX.DXGI.Surface>();

				//bool surfaceFirst = true;

				// Play the music
				//_mediaEngineEx.Loop = Loop;

				//readyToPlayLoadedVideo = true;
			}
			
			_mediaEngineEx.Play();
			_mediaEngineEx.Volume = 0.2;
			IsPlaying = true;


			Task.Factory.StartNew(() =>
			{
				while (IsPlaying)
				{
					if (!_mediaEngine.IsPaused)
					{
						long lastTs = ts;
						bool result = _mediaEngine.OnVideoStreamTick(out ts);
						
						if(result == false)
						{
							Console.WriteLine("FALSE!!");
						}

						if (result && ts != lastTs)
						{
							//		if (_stereoVideo)
							//		{
							_mediaEngine.TransferVideoFrame(surfaceL, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
							//			//Thread.Sleep(1);
							_mediaEngine.TransferVideoFrame(surfaceR, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);
						
						//_swapChain.Present(1, 0);
						//			Console.WriteLine("Tick stereo" + ts);
						//		}
						//		else
						//		{
						//			_mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
						//			Console.WriteLine("Tick mono" + ts);
						//		}

						//		Thread.Sleep(10);
						}
					}
				}
			});

			//frameGrabber = new Thread(new ThreadStart(() =>
			//{
			//	while (IsPlaying)
			//	{
			//		if (!_mediaEngine.IsPaused)
			//		{
			//			long lastTs = ts;
			//			bool result = _mediaEngine.OnVideoStreamTick(out ts);
			//		//	if (result && ts != lastTs)
			//		//	{

			//		//		if (_stereoVideo)
			//		//		{
			//					_mediaEngine.TransferVideoFrame(surfaceL, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
			//		//			//Thread.Sleep(1);
			//					_mediaEngine.TransferVideoFrame(textureR, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);

			//		//			Console.WriteLine("Tick stereo" + ts);
			//		//		}
			//		//		else
			//		//		{
			//		//			_mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
			//		//			Console.WriteLine("Tick mono" + ts);
			//		//		}
							
			//		//		Thread.Sleep(10);
			//		//	}
			//		}
			//	}

			//}));

			//frameGrabber.Start();
		}

		void Trim()
		{
			
			using (var Direct3DDevice = _device.QueryInterface<SharpDX.Direct3D11.Device1>())
				using (var DxgiDevice3 = Direct3DDevice.QueryInterface<SharpDX.DXGI.Device3>())
					DxgiDevice3.Trim();
		}

		public void Stop()
		{
			IsPlaying = false;

			_mediaEngineEx.Shutdown();

			fileStream?.Close();
			fileStream?.Dispose();
			webStream?.Close();
			webStream?.Dispose();
			stream?.Close();
			stream?.Dispose();

			textureL?.Dispose();
			textureR?.Dispose();
			surfaceL?.Dispose();
			surfaceR?.Dispose();
		}

		private FileStream fileStream;
		private Stream webStream;
		private ByteStream stream;
		private Uri url;

		public void LoadMedia(string fileName)
		{
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
			_cancelTransferFrame.Cancel();
			MediaManager.Shutdown();
		}

	}
}
