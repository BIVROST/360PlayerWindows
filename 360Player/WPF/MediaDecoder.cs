using Bivrost.Bivrost360Player.Streaming;
using Bivrost.Log;
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

namespace Bivrost.Bivrost360Player
{

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
		Sphere, // Equirectangular 360x180
		CubeFacebook,
		Dome    // Equirectangular 180x180
	}


	public class MediaDecoder
	{

		private Logger log = new Logger("MediaDecoder");

		public class Error
		{
			public long major;
			public int minor;
		}

		public Error LastError;

		private MediaEngine _mediaEngine;
		private MediaEngineEx _mediaEngineEx;
		private object criticalSection = new object();
		private bool waitForFormatChange = false;
		private bool formatChangePending = false;

		private long ts;
		private bool manualRender = false;

		private Texture2D _textureL;
		private Texture2D _textureR;
		public Texture2D TextureL
		{
			get { return _textureL; }
			private set { _textureL = value; }
		}
		public Texture2D TextureR
		{
			get { return _textureR; }
			private set { _textureR = value; }
		}

		//private bool _stereoVideo = false;
		//public bool IsStereo { get { return isPlaying ? _stereoVideo : false; } }
		public bool IsStereoRendered { get
			{
				switch(CurrentMode)
				{
					case VideoMode.Mono: return false;
					case VideoMode.Autodetect: throw new Exception();
					default: return true;
				}
			}
		}

		public bool IsPlaying => isPlaying || IsDisplayingStaticContent;
		private bool isPlaying;
		public bool IsPaused { get {
				/////lock(criticalSection)
				//{
				if (IsDisplayingStaticContent) return true;

				return (_initialized ? (bool)_mediaEngineEx.IsPaused : false);
				//}
			} }
		public bool IsDisplayingStaticContent => staticContentSource != null;


		/// <summary>
		/// Used instead of mediaEngine to show non-animated content.
		/// Should contain the bytes of a png or jpeg (stored, so the textures can be recreated with other stereoscopy values)
		/// </summary>
		byte[] staticContentSource = null;


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

		public double Duration { get; protected set; } = -1;  //  _mediaEngineEx.Duration; 

		public double CurrentPosition { get; protected set; } = 0; // _mediaEngineEx.CurrentTime; 
		public bool Initialized { get { return _initialized; } }
		private VideoMode LoadedStereoMode { get; set; } = VideoMode.Autodetect;

		private VideoMode _currentMode;
		public VideoMode CurrentMode
		{
			get { return _currentMode; }
			private set
			{
				if (_currentMode == value) return;

				_currentMode = value;
				log.Info($"Stereoscopy = {value}");
			}
		}
		public ProjectionMode Projection { get; private set; } = ProjectionMode.Sphere;

		private bool _initialized = false;
		private bool _rendering = false;
		private ManualResetEvent waitForRenderingEnd = new ManualResetEvent(false);

		private static MediaDecoder _instance = null;
		public static MediaDecoder Instance { get { return MediaDecoder._instance; } }
        public static event Action<MediaDecoder> OnInstantiated;

		private SharpDX.Direct3D11.Device _device;
		private Factory _factory;
		private FeatureLevel[] _levels = new FeatureLevel[] { FeatureLevel.Level_10_0 };
		private DXGIDeviceManager _dxgiManager;

		public event Action OnPlay = delegate { };
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


		public MediaDecoder()
		{
			isPlaying = false;
			Ready = false;
			MediaDecoder._instance = this;
            if (MediaDecoder.OnInstantiated != null)
                MediaDecoder.OnInstantiated(this);
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
			//var category = SharpDX.MediaFoundation.TransformCategoryGuids.VideoDecoder;
			//var flags = SharpDX.MediaFoundation.TransformEnumFlag.Hardware | TransformEnumFlag.Localmft | TransformEnumFlag.SortAndFilter;
			//var typeInfo = new SharpDX.MediaFoundation.TRegisterTypeInformation();
			//typeInfo.GuidMajorType = MediaTypeGuids.Video;
			//typeInfo.GuidSubtype = VideoFormatGuids.H264;
		
			//Guid[] guids = new Guid[50];
			//int costamref;
			//MediaFactory.TEnum(category, (int)flags, null, null, null, guids, out costamref);
			//;			
        }

		public static bool CheckExtension(string extension)
		{
			switch(extension.ToLower())
			{
				case ".mp4":
				case ".wmv":
				case ".avi":
				case ".m4v":
				case ".mov":
				case ".png":
				case ".jpg":
				case ".jpeg":	// TODO: tiff?
					return true;

				default:
					return false;
			}
		}

		public static string ExtensionsFilter()
		{
			return
				"All supported formats|*.mp4; *.m4v; *.mov; *.avi; *.wmv; *.png; *.jpg; *.jpeg"
				+"|Video (*.mp4; *.m4v; *.mov; *.avi; *.wmv)|*.mp4; *.m4v; *.mov; *.avi; *.wmv"
				+"|Panorama (*.png; *.jpg; *.jpeg)|*.png; *.jpg; *.jpeg";
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
							Duration = _mediaEngineEx.Duration;
							OnReady(_mediaEngineEx.Duration);
							break;

						case MediaEngineEvent.TimeUpdate:
							CurrentPosition = _mediaEngineEx.CurrentTime;
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
							break;

                        case MediaEngineEvent.Playing:
                            OnPlay?.Invoke();
                            break;

                    }
                };

				_mediaEngineEx = _mediaEngine.QueryInterface<MediaEngineEx>();
				_mediaEngineEx.EnableWindowlessSwapchainMode(true);


				mediaEngineFactory.Dispose();
				_initialized = true;
			}
		}

		//private MediaSource CreateMediaSource(string sURL)
		//{
		//	SourceResolver sourceResolver = new SourceResolver();
		//	ComObject comObject;
		//	comObject = sourceResolver.CreateObjectFromURL(sURL, SourceResolverFlags.MediaSource | SourceResolverFlags.ContentDoesNotHaveToMatchExtensionOrMimeType);
		//	return comObject.QueryInterface<MediaSource>();
		//}

		//public static Guid ToGuid(long value)
		//{
		//	byte[] guidData = new byte[16];
		//	Array.Copy(BitConverter.GetBytes(value), guidData, 8);
		//	return new Guid(guidData);
		//}


		public Texture2D CreateTexture(SharpDX.Direct3D11.Device _device, int width, int height, DataRectangle? dataRectangle = null)
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

			var tex = dataRectangle.HasValue
				?new Texture2D(_device, frameTextureDescription, dataRectangle.Value)
				:new Texture2D(_device, frameTextureDescription);

			tex.Disposing += (s, e) =>
			{
				log.Info("Disposing");
			};

			tex.Disposed += (s, e) =>
			{
				log.Info("Disposed");
			};

			return tex;
		}


		private class ClipCoords {

			public float w;
			public float h;
			public float l;
			public float t;

			public VideoNormalizedRect NormalizedSrcRect
			{
				get
				{
					return new VideoNormalizedRect()
					{
						Left = l,
						Top = t,
						Bottom = t + h,
						Right = l + w
					};
				}
			}

			internal Rectangle DstRect(int pxw, int pxh)
			{
				return new Rectangle(0, 0, Width(pxw), Height(pxh));
			}

			internal int Width(int pxw)
			{
				return (int)(w * pxw);
			}

			internal int Height(int pxh)
			{
				return (int)(h * pxh);
			}

			internal Rectangle SrcRect(int pxw, int pxh)
			{
				return new Rectangle((int)(l * pxw), (int)(t * pxh), (int)(w * pxw), (int)(h * pxh));
			}
		}
		private void GetTextureClipCoordinates(out ClipCoords texL, out ClipCoords texR)
		{
			switch (CurrentMode)
			{
				case VideoMode.Autodetect:
					throw new ArgumentException();
				case VideoMode.Mono:
					texL = new ClipCoords() { w = 1, h = 1f, l = 0, t = 0 };
					texR = null;
					break;
				case VideoMode.SideBySide:
					texL = new ClipCoords() { w = 0.5f, h = 1, l = 0, t = 0 };
					texR = new ClipCoords() { w = 0.5f, h = 1, l = 0.5f, t = 0 };
					break;
				case VideoMode.SideBySideReversed:
					texL = new ClipCoords() { w = 0.5f, h = 1, l = 0.5f, t = 0 };
					texR = new ClipCoords() { w = 0.5f, h = 1, l = 0, t = 0 };
					break;
				case VideoMode.TopBottom:
					texL = new ClipCoords() { w = 1f, h = 0.5f, l = 0, t = 0 };
					texR = new ClipCoords() { w = 1f, h = 0.5f, l = 0, t = 0.5f };
					break;
				case VideoMode.TopBottomReversed:
					texL = new ClipCoords() { w = 1f, h = 0.5f, l = 0, t = 0.5f };
					texR = new ClipCoords() { w = 1f, h = 0.5f, l = 0, t = 0 };
					break;
				default:
					throw new Exception();
			}
		}


		public void Play()
		{
			lock(criticalSection)
			{
				if (isPlaying) return;
				if (IsDisplayingStaticContent) return;


				if (!_initialized) return;
				if (!Ready) return;

				var hasVideo = _mediaEngine.HasVideo();
				var hasAudio = _mediaEngine.HasAudio();

				if (hasVideo)
				{
					//SharpDX.Win32.Variant variant;
					//_mediaEngineEx.GetStreamAttribute(1, MediaTypeAttributeKeys.FrameSize.Guid, out variant);

					int w, h;
					_mediaEngineEx.GetNativeVideoSize(out w, out h);
					CurrentMode = ParseStereoMode(LoadedStereoMode, w, h);

					int cx, cy;
					_mediaEngineEx.GetVideoAspectRatio(out cx, out cy);
					var s3d = _mediaEngineEx.IsStereo3D;
					var sns = _mediaEngineEx.NumberOfStreams;
				}

				_mediaEngineEx.Play();
                ShellViewModel.SendEvent("moviePlaybackStarted");
                //_mediaEngineEx.Volume = 0.2;
                isPlaying = true;
			}

			Task t = Task.Factory.StartNew(() =>
			{
				_rendering = true;
				int w = -1, h = -1;
				while (_mediaEngine != null && isPlaying)
				{
					lock (criticalSection)
					{
						if(w < 0 || h < 0 || formatChangePending)
							_mediaEngineEx.GetNativeVideoSize(out w, out h);

						if(formatChangePending)
						{
							formatChangePending = false;
							ChangeFormat(w, h);
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
								if (!result || ts <= 0) Thread.Sleep(1);
							if(ts > 0)
							if (result && ts != lastTs)
							{
								//Duration = _mediaEngineEx.Duration;
								//CurrentPosition = _mediaEngineEx.CurrentTime; 

								try {
									ClipCoords texL;
									ClipCoords texR;
									GetTextureClipCoordinates(out texL, out texR);

									_mediaEngine.TransferVideoFrame(TextureL, texL.NormalizedSrcRect, texL.DstRect(w,h), null);
									if(texR != null)
										_mediaEngine.TransferVideoFrame(TextureR, texR.NormalizedSrcRect, texR.DstRect(w,h), null);
								}
								catch (Exception exc)
								{
									Console.WriteLine("Playback exception " + exc.Message);
								}
							}
						} else Thread.Sleep(1);
					}
				}

				waitForRenderingEnd.Set();
				_rendering = false;
			});
			
		}

		private VideoMode ParseStereoMode(VideoMode setStereoMode, float w, float h)
		{
			if (setStereoMode == VideoMode.Autodetect)
			{
				float videoAspect = w/h;
				var mode = (videoAspect < 1.3) ? VideoMode.TopBottom : VideoMode.Mono;
				log.Info($"Autodetected stereoscopy={mode} ({w}x{h}, aspect={videoAspect})");
				return mode;
				//h = _stereoVideo ? h / 2 : h;
			}
			else
			{
				return setStereoMode;
			}
		}

		/// <summary>
		/// Should be executed in locked context
		/// </summary>
		/// <param name="w"></param>
		/// <param name="h"></param>
		private void ChangeFormat(int w, int h)
		{
			Texture2D tempL = TextureL;
			Texture2D tempR = TextureR;

			textureReleased = true;

			ClipCoords texL, texR;
			GetTextureClipCoordinates(out texL, out texR);
			TextureL = CreateTexture(_device, texL.Width(w), texL.Height(h));
			TextureR = CreateTexture(_device, texL.Width(w), texL.Height(h));	//< even if mono, this texture should be created

			textureReleased = false;

			//OnReleaseTexture();
			OnFormatChanged(TextureL, TextureR);
			if (waitForFormatChange) waitForFormatChange = false;

			tempL?.Dispose();
			tempR?.Dispose();
		}


		public void Pause()
		{
			if (IsDisplayingStaticContent) return;
			if(isPlaying)
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
			if (IsDisplayingStaticContent) return;
			if (isPlaying)
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
			if (IsDisplayingStaticContent) return;
			if (isPlaying)
			{
				if (IsPaused) Unpause();
				else Pause();
			}
		}

		public void SetVolume(double volume)
		{
            //if(IsPlaying)
            //{
            if (_mediaEngine != null)
            {
                try
                {
                    if(!_mediaEngine.IsDisposed)
                        _mediaEngine.Volume = volume;
                }
                catch (Exception) { }
            }
			//}
		}

		public void Seek(double time)
		{
			if (IsDisplayingStaticContent) return;
			if (isPlaying)
			{
				lock (criticalSection)
				{	
					if (!_mediaEngineEx.IsSeeking)
					{
						_mediaEngineEx.CurrentTime = time;
                        ShellViewModel.SendEvent("movieSeek", time);
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

			if (!force && !IsDisplayingStaticContent)
			{
				if (!_initialized) return;
				if (!isPlaying) return;
			}

			waitForRenderingEnd.Reset();
			isPlaying = false;
			
			if (_rendering) {

				waitForRenderingEnd.WaitOne(1000);
			}

			Ready = false;

			lock (criticalSection)
			{
				if (_mediaEngineEx != null)
				{
					try
					{
						_mediaEngineEx.Shutdown();
						_mediaEngineEx.Dispose();
						_mediaEngine.Dispose();
					}
					catch (Exception e)
					{
						;
					}
				}

				staticContentSource = null;

				TextureL?.Dispose();
				TextureR?.Dispose();

				TextureL = null;
				TextureR = null;

				_initialized = false;	
			}

            OnStop?.Invoke();
        }

		private string _fileName;
        public string FileName { get { return _fileName; } }

		public void LoadMedia(ServiceResult serviceResult)
		{
			Projection = serviceResult.projection;
			LoadedStereoMode = serviceResult.stereoscopy;


			string fileName = serviceResult.BestSupportedStream;

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

			_fileName = fileName;

			staticContentSource = null;

			switch (serviceResult.contentType)
			{
				case ServiceResult.ContentType.video:
					Init();


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
					break;

				case ServiceResult.ContentType.image:

					//SharpDX.Toolkit.Graphics.GraphicsDevice gd;
					//_factory.Adapters.
					// gd.copy?

					staticContentSource = File.ReadAllBytes(_fileName);

					using (var stream = new MemoryStream(staticContentSource))	// stream, so it won't lock the file
					using (var imagingFactory = new SharpDX.WIC.ImagingFactory2())
					using (var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(imagingFactory, stream, SharpDX.WIC.DecodeOptions.CacheOnDemand))
					using (var frame = bitmapDecoder.GetFrame(0))
					using (var formatConverter = new SharpDX.WIC.FormatConverter(imagingFactory))
					{
						formatConverter.Initialize(
							frame,
							SharpDX.WIC.PixelFormat.Format32bppBGRA,       // CreateTexture uses B8G8R8X8_UNorm
							SharpDX.WIC.BitmapDitherType.None,
							null,
							0.0,
							SharpDX.WIC.BitmapPaletteType.Custom
						);

						// to be later cleaned up
						var tempL = TextureL;
						var tempR = TextureR;

						var w = formatConverter.Size.Width;
						var h = formatConverter.Size.Height;
						log.Info($"Loaded image of size {w}x{h} from {_fileName}");
						CurrentMode = ParseStereoMode(LoadedStereoMode, w, h);

						const int bpp = 4;


						ClipCoords texL, texR;
						GetTextureClipCoordinates(out texL, out texR);


						lock (criticalSection)
						{
							textureReleased = true;

							int stride = texL.Width(w) * bpp;
							using (var buffer = new SharpDX.DataStream(texL.Height(h) * stride, true, true))
							{
								formatConverter.CopyPixels(texL.SrcRect(w, h), stride, buffer);
								var dataRect = new SharpDX.DataRectangle(buffer.DataPointer, stride);
								TextureL = CreateTexture(_device, texL.Width(w), texL.Height(h), dataRect);
							}

							if (texR != null)
							{
								stride = texR.Width(w) * bpp;
								using (var buffer = new SharpDX.DataStream(texR.Height(h) * stride, true, true))
								{
									formatConverter.CopyPixels(texR.SrcRect(w, h), stride, buffer);
									var dataRect = new SharpDX.DataRectangle(buffer.DataPointer, stride);
									TextureR = CreateTexture(_device, texR.Width(w), texR.Height(h), dataRect);
								}
							}
							else
							{
								//TextureR = CreateTexture(_device, texL.Width(w), texL.Height(h));   // an empty one still needs to be created
							}

							textureReleased = false;
						}


						OnFormatChanged(TextureL, TextureR);
						tempL?.Dispose();
						tempR?.Dispose();

						// TODO: stereoscopy

						OnReady?.Invoke(-1);
					}

					break;
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

