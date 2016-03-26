using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

		public enum VideoMode
		{
			Autodetect,
			Mono,
			SideBySide,
			TopBottom,
			SideBySideReversed,
			TopBottomReversed
		}

		public enum ProjectionMode
		{
			Sphere,	// Equirectangular 360x180
			CubeFacebook,
			Dome	// Equirectangular 180x180
		}

		public Error LastError;

		private MediaEngine _mediaEngine;
		private MediaEngineEx _mediaEngineEx;
		private object criticalSection = new object();
		private bool waitForFormatChange = false;
		private bool formatChangePending = false;

		private long ts;
		private bool _stereoVideo = false;
		private int w, h;
		private bool manualRender = false;

		private Texture2D textureL;
		private Texture2D textureR;
		public Texture2D TextureL { get { return this.textureL; } }
		public Texture2D TextureR { get { return this.textureR; } }

		public bool IsStereo { get { return IsPlaying ? _stereoVideo : false; } }
		public bool IsStereoRendered { get
			{
				switch(StereoMode)
				{
					case VideoMode.Mono: return false;
					case VideoMode.Autodetect: return IsStereo;
					default: return true;
				}
			} }

		public bool IsPlaying { get; private set; }
		public bool IsPaused { get {
				/////lock(criticalSection)
				//{
					return _initialized ? (bool)_mediaEngineEx.IsPaused : false;
				//}
			} }


		private bool _loop = false;
		public bool Loop
		{
			get { return _loop;}
			set {
				_loop = value;
				if (_initialized) _mediaEngineEx.Loop = _loop;
			}
		}

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

		//public int BufferLevel
		//{
		//    get
		//    {
		//        return _mediaEngineEx.
		//    }
		//}

		public int formatCounter = 0;

		public bool Ready { get; private set; }

		public double Duration { get { return _mediaEngineEx.Duration; } }

		public double CurrentPosition { get { return _mediaEngineEx.CurrentTime; } }
		public bool Initialized { get { return _initialized; } }
		public VideoMode StereoMode { get; set; } = VideoMode.Autodetect;
		public VideoMode CurrentMode { get; set; } = VideoMode.Autodetect;
		public ProjectionMode Projection { get; set; } = MediaDecoder.ProjectionMode.Sphere;

		private bool _initialized = false;
		private bool _rendering = false;
		private ManualResetEvent waitForRenderingEnd = new ManualResetEvent(false);

		private static MediaDecoder _instance = null;
		public static MediaDecoder Instance { get { return MediaDecoder._instance; } }

		private SharpDX.Direct3D11.Device _device;
		private Factory _factory;
		private FeatureLevel[] _levels = new FeatureLevel[] { FeatureLevel.Level_10_0 };
		private DXGIDeviceManager _dxgiManager;

		public event Action<bool> OnPlay = delegate { };
		public event Action<double> OnReady = delegate { };
		public event Action OnStop = delegate { };
		public event Action OnEnded = delegate { };
		public event Action<Error> OnError = delegate { };
		public event Action OnAbort = delegate { };
		public event Action<double> OnTimeUpdate = delegate { };
        public event Action OnBufferingStarted = delegate { };
        public event Action OnBufferingEnded = delegate { };
        public event Action<double> OnProgress = delegate { };

		//public event Action OnReleaseTexture = delegate { };
		public event Action<Texture2D, Texture2D> OnFormatChanged = delegate { };
		private bool textureReleased = true;
		public bool TextureReleased { get { return textureReleased; } }

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

		VideoNormalizedRect leftRect = new VideoNormalizedRect()
		{
			Left = 0f,
			Top = 0f,
			Right = 0.5f,
			Bottom = 1f
		};

		VideoNormalizedRect rightRect = new VideoNormalizedRect()
		{
			Left = 0.5f,
			Top = 0f,
			Right = 1f,
			Bottom = 1f
		};

		public MediaDecoder()
		{
			IsPlaying = false;
			Ready = false;
			MediaDecoder._instance = this;
			_initialized = false;

			_factory = new SharpDX.DXGI.Factory();
			_device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, _levels);

            //SharpDX.DXGI.Device1 dxdevice = _device.QueryInterface<SharpDX.DXGI.Device1>();
            //MessageBox.Show(dxdevice.Adapter.Description.Description);
            //dxdevice.Dispose();

            DeviceMultithread mt = _device.QueryInterface<DeviceMultithread>();
			mt.SetMultithreadProtected(true);

			using (SharpDX.DXGI.Device1 dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device1>())
			{
				dxgiDevice.MaximumFrameLatency = 1;
			}

			_dxgiManager = new DXGIDeviceManager();

			//MFTEnumEx 
			var category = SharpDX.MediaFoundation.TransformCategoryGuids.VideoDecoder;
			var flags = SharpDX.MediaFoundation.TransformEnumFlag.Hardware | TransformEnumFlag.Localmft | TransformEnumFlag.SortAndFilter;
			var typeInfo = new SharpDX.MediaFoundation.TRegisterTypeInformation();
			typeInfo.GuidMajorType = MediaTypeGuids.Video;
			typeInfo.GuidSubtype = VideoFormatGuids.H264;
		
			Guid[] guids = new Guid[50];
			int costamref;
			MediaFactory.TEnum(category, (int)flags, null, null, null, guids, out costamref);
			;			
        }

		public static bool CheckExtension(string extension)
		{
			switch(extension)
			{
				case ".mp4": return true;
				case ".wmv": return true;
				case ".avi": return true;
				case ".m4v": return true;
				case ".mov": return true;
				default: return false;
			}
		}

		public static string ExtensionsFilter()
		{
			return "Video MP4|*.mp4|Video M4V|*.m4v|Video MOV|*.mov|Video AVI|*.avi|Video WMV|*.wmv";
		}

		public void Init()
		{
			LastError = null;
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
				//attr.Set(MediaTypeAttributeKeys.TransferFunction.Guid, VideoTransferFunction.Func10);

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

							Stop(true);
							OnError(LastError);
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
                        case MediaEngineEvent.BufferingStarted:
                            OnBufferingStarted();
                            break;
                        case MediaEngineEvent.BufferingEnded:
                            OnBufferingEnded();
                            break;
                        case MediaEngineEvent.FormatChange:
							Console.WriteLine("[!!!] FormatChange " + formatCounter++);
							formatChangePending = true;
							//Task.Factory.StartNew(() =>
							//{
								//if (_mediaEngineEx.IsDisposed) break;
								//if (_mediaEngineEx.IsEnded) break;

								//lock (criticalSection)
								//{
								//	if (_mediaEngineEx.IsDisposed) break;
								//	if (_mediaEngineEx.IsEnded) break;
									
									//Texture2D tempL = textureL;
									//Texture2D tempR = textureR;
									//_mediaEngineEx.GetNativeVideoSize(out w, out h);
									
									//textureReleased = true;
									
									//textureL = CreateTexture(_device, w, h);
									//textureR = CreateTexture(_device, w, h);
									//textureReleased = false;

									////OnReleaseTexture();
									//OnFormatChanged(textureL, textureR);
									//if (waitForFormatChange) waitForFormatChange = false;

									//tempL?.Dispose();
									//tempR?.Dispose();
								//}
							//});
							break;
                    }
				};

				_mediaEngineEx = _mediaEngine.QueryInterface<MediaEngineEx>();
				_mediaEngineEx.EnableWindowlessSwapchainMode(true);


				mediaEngineFactory.Dispose();
				_initialized = true;
			}
		}

		public static VideoMode DetectFromFileName(string fileName)
		{
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				if (Regex.IsMatch(Path.GetFileNameWithoutExtension(fileName), @"(\b|_)(SbS|LR)(\b|_)")) return VideoMode.SideBySide;
				if (Regex.IsMatch(Path.GetFileNameWithoutExtension(fileName), @"(\b|_)(RL)(\b|_)")) return VideoMode.SideBySideReversed;
				if (Regex.IsMatch(Path.GetFileNameWithoutExtension(fileName), @"(\b|_)(TaB|TB)(\b|_)")) return VideoMode.TopBottom;
				if (Regex.IsMatch(Path.GetFileNameWithoutExtension(fileName), @"(\b|_)(BaT|BT)(\b|_)")) return VideoMode.TopBottomReversed;
				if (Regex.IsMatch(Path.GetFileNameWithoutExtension(fileName), @"(\b|_)mono(\b|_)")) return VideoMode.Mono;
			}
			return VideoMode.Autodetect;
		}


		private MediaSource CreateMediaSource(string sURL)
		{
			SourceResolver sourceResolver = new SourceResolver();
			ComObject comObject;
			comObject = sourceResolver.CreateObjectFromURL(sURL, SourceResolverFlags.MediaSource | SourceResolverFlags.ContentDoesNotHaveToMatchExtensionOrMimeType);
			return comObject.QueryInterface<MediaSource>();
		}

		public static Guid ToGuid(long value)
		{
			byte[] guidData = new byte[16];
			Array.Copy(BitConverter.GetBytes(value), guidData, 8);
			return new Guid(guidData);
		}

		public static Texture2D CreateTexture(SharpDX.Direct3D11.Device _device, int width, int height)
		{
			Texture2DDescription frameTextureDescription = new Texture2DDescription()
			{
				Width = width,
				Height = height,
				MipLevels = 1,
				ArraySize = 1,
				Format = Format.B8G8R8X8_UNorm,
				Usage = ResourceUsage.Default,
				SampleDescription = new SampleDescription(1, 0),
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.Shared
			};

			return new SharpDX.Direct3D11.Texture2D(_device, frameTextureDescription);
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
					//SharpDX.Win32.Variant variant;
					//_mediaEngineEx.GetStreamAttribute(1, MediaTypeAttributeKeys.FrameSize.Guid, out variant);


					_mediaEngineEx.GetNativeVideoSize(out w, out h);
					int cx, cy;
					_mediaEngineEx.GetVideoAspectRatio(out cx, out cy);
					var s3d = _mediaEngineEx.IsStereo3D;
					var sns = _mediaEngineEx.NumberOfStreams;


					float videoAspect = ((float)w) / ((float)h);
					_stereoVideo = videoAspect < 1.3;
					h = _stereoVideo ? h / 2 : h;

					CurrentMode = StereoMode;

					if (CurrentMode == VideoMode.Autodetect)
						CurrentMode = DetectFromFileName(_fileName);

					Console.WriteLine("VIDEO STEREO MODE: " + CurrentMode);

					if (CurrentMode != VideoMode.Mono && CurrentMode != VideoMode.Autodetect) _stereoVideo = true;


					//Texture2DDescription frameTextureDescription = new Texture2DDescription()
					//{
					//	Width = w,
					//	Height = h,
					//	MipLevels = 1,
					//	ArraySize = 1,
					//	Format = Format.B8G8R8A8_UNorm,
					//	Usage = ResourceUsage.Default,
					//	SampleDescription = new SampleDescription(1, 0),
					//	BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
					//	CpuAccessFlags = CpuAccessFlags.None,
					//	OptionFlags = ResourceOptionFlags.Shared
					//};


					//textureL = new SharpDX.Direct3D11.Texture2D(_device, frameTextureDescription);
					//textureR = new SharpDX.Direct3D11.Texture2D(_device, frameTextureDescription);
					//textureL = CreateTexture(_device, w, h);
					//textureR = CreateTexture(_device, w, h);
				}

				_mediaEngineEx.Play();
				//_mediaEngineEx.Volume = 0.2;
				IsPlaying = true;
			}

			Task t = Task.Factory.StartNew(() =>
			{
				_rendering = true;
				while (_mediaEngine != null && IsPlaying)
				{
					lock (criticalSection)
					{
						if(formatChangePending)
						{
							formatChangePending = false;
							Texture2D tempL = textureL;
							Texture2D tempR = textureR;
							_mediaEngineEx.GetNativeVideoSize(out w, out h);

							textureReleased = true;

							switch (CurrentMode)
							{
								case VideoMode.Autodetect:
									if (IsStereo)
									{
										textureL = CreateTexture(_device, w, h);
										textureR = CreateTexture(_device, w, h);
										//_mediaEngine.TransferVideoFrame(textureL, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
										//_mediaEngine.TransferVideoFrame(textureR, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);
									}
									else
									{
										textureL = CreateTexture(_device, w, h);
										textureR = CreateTexture(_device, w, h);
										//_mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
									}
									break;
								case VideoMode.Mono:
									textureL = CreateTexture(_device, w, h);
									textureR = CreateTexture(_device, w, h);
									//_mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
									break;

								case VideoMode.SideBySide:
								case VideoMode.SideBySideReversed:
									textureL = CreateTexture(_device, w/2, h);
									textureR = CreateTexture(_device, w/2, h);
									//_mediaEngine.TransferVideoFrame(textureL, rightRect, new SharpDX.Rectangle(0, 0, w, h), null);
									//_mediaEngine.TransferVideoFrame(textureR, leftRect, new SharpDX.Rectangle(0, 0, w, h), null);
									break;
								case VideoMode.TopBottom:
								case VideoMode.TopBottomReversed:
									textureL = CreateTexture(_device, w, h/2);
									textureR = CreateTexture(_device, w, h/2);
									//_mediaEngine.TransferVideoFrame(textureR, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
									//_mediaEngine.TransferVideoFrame(textureL, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);
									break;
							}

							
							textureReleased = false;

							//OnReleaseTexture();
							OnFormatChanged(textureL, textureR);
							if (waitForFormatChange) waitForFormatChange = false;

							tempL?.Dispose();
							tempR?.Dispose();
						}

						if(!textureReleased)
						if (!_mediaEngine.IsPaused || manualRender)
						{
							manualRender = false;
								//waitForResize.WaitOne();
								if(formatCounter == 0)
									Console.WriteLine("[!!!] Render " + formatCounter++);
								long lastTs = ts;
							bool result = _mediaEngine.OnVideoStreamTick(out ts);
							if(ts > 0)
							if (result && ts != lastTs)
							{
										try {
											switch (CurrentMode)
											{
												case VideoMode.Autodetect:
													if (IsStereo)
													{
														_mediaEngine.TransferVideoFrame(textureL, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
														_mediaEngine.TransferVideoFrame(textureR, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);
													}
													else
													{
														_mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
													}
													break;
												case VideoMode.Mono:
													_mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
													break;
												case VideoMode.SideBySide:
													_mediaEngine.TransferVideoFrame(textureL, leftRect, new SharpDX.Rectangle(0, 0, w, h), null);
													_mediaEngine.TransferVideoFrame(textureR, rightRect, new SharpDX.Rectangle(0, 0, w, h), null);
													break;
												case VideoMode.SideBySideReversed:
													_mediaEngine.TransferVideoFrame(textureL, rightRect, new SharpDX.Rectangle(0, 0, w, h), null);
													_mediaEngine.TransferVideoFrame(textureR, leftRect, new SharpDX.Rectangle(0, 0, w, h), null);
													break;
												case VideoMode.TopBottom:
													_mediaEngine.TransferVideoFrame(textureL, topRect, new SharpDX.Rectangle(0, 0, w, h/2), null);
													_mediaEngine.TransferVideoFrame(textureR, bottomRect, new SharpDX.Rectangle(0, 0, w, h/2), null);
													break;
												case VideoMode.TopBottomReversed:
													_mediaEngine.TransferVideoFrame(textureR, topRect, new SharpDX.Rectangle(0, 0, w, h/2), null);
													_mediaEngine.TransferVideoFrame(textureL, bottomRect, new SharpDX.Rectangle(0, 0, w, h/2), null);
													break;
											}
										} catch (Exception exc)
										{
											Console.WriteLine("Playback exception " + exc.Message);
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
			//if(IsPlaying)
			//{
			if(_mediaEngine != null)
				_mediaEngine.Volume = volume;
			//}
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
						if (IsPaused)
						{
							manualRender = true;
						}
					}
				}
			}
		}
		
		public void Stop(bool force = false)
		{
			textureReleased = true;
			//OnReleaseTexture();			

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
                try {
                    _mediaEngineEx.Shutdown();
                    _mediaEngineEx.Dispose();
                    _mediaEngine.Dispose();
                }catch(Exception)
                {

                }


				fileStream?.Close();
				fileStream?.Dispose();
				webStream?.Close();
				webStream?.Dispose();
				stream?.Close();
				stream?.Dispose();

				textureL?.Dispose();
				textureR?.Dispose();

				textureL = null;
				textureR = null;

				_initialized = false;	
			}
			
		}

		private FileStream fileStream;
		private Stream webStream;
		private ByteStream stream;
		private Uri url;
		private string _fileName;

		public void LoadMedia(string fileName)
		{
			Console.WriteLine("Load media: " + fileName);

			_fileName = "";
			textureReleased = true;
			waitForFormatChange = true;
			Stop();
			while (_initialized == true)
			{
				Console.WriteLine("Cannot init when initialized");
				Stop(true);
				Thread.Sleep(5);
			}
			Init();
            			
			_fileName = fileName;

			//Collection collection;
			//MediaFactory.CreateCollection(out collection);

			//SourceResolver sourceResolver = new SourceResolver();
			//var mediaSource1 = sourceResolver.CreateObjectFromURL(@"D:\TestVideos\maroon.m4a", SourceResolverFlags.MediaSource | SourceResolverFlags.ContentDoesNotHaveToMatchExtensionOrMimeType).QueryInterface<MediaSource>();
			//var mediaSource2 = sourceResolver.CreateObjectFromURL(@"D:\TestVideos\maroon-video.mp4", SourceResolverFlags.MediaSource | SourceResolverFlags.ContentDoesNotHaveToMatchExtensionOrMimeType).QueryInterface<MediaSource>();
			//collection.AddElement(mediaSource1);
			//collection.AddElement(mediaSource2);
			//MediaSource aggregateSource;
			//MediaFactory.CreateAggregateSource(collection, out aggregateSource);

			//MediaEngineSrcElementsEx
			formatCounter = 0;
			textureReleased = true;
			waitForFormatChange = true;
			_mediaEngineEx.Source = _fileName;
            _mediaEngineEx.Preload = MediaEnginePreload.Automatic;
			_mediaEngineEx.Load();
			
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

