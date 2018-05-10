using Bivrost.AnalyticsForVR;
using SharpDX.Direct3D11;
using System;
using System.Threading;
using SharpDX;
using Bivrost.Bivrost360Player.Streaming;
using Bivrost.Log;
using Bivrost.Bivrost360Player.Tools;
using System.Drawing;
using System.Drawing.Imaging;

namespace Bivrost.Bivrost360Player
{

	public class HeadsetError : Exception
	{
		public HeadsetError(string msg) : base(msg) { }
		public HeadsetError(Exception inner, string msg) : base(msg, inner) { }
	}


	public abstract class Headset : ILookProvider, IContentUpdatableFromMediaEngine
	{

		private static ServiceResult nothingIsPlaying = new ServiceResult(null, "(none)", "nothing")
		{
			description = "",
			stereoscopy = VideoMode.Autodetect,
			projection = ProjectionMode.Sphere,
			title = "",
			contentType = ServiceResult.ContentType.none
		};

		private ServiceResult _media;
		public ServiceResult Media
		{
			get => _media ?? nothingIsPlaying;
			set
			{
				pause = false;
				_media = value;
				vrui?.EnqueueUIRedraw();
				
				//UpdateSceneSettings(Media.projection, Media.stereoscopy);
			}
		}
		//public bool _stereoVideo => Array.IndexOf(new[] { VideoMode.Mono, VideoMode.Autodetect }, Media.stereoscopy) < 0;
		protected float Duration => (float)MediaDecoder.Instance.Duration;


		protected Logger log;


		public void Start()
		{
			abort = false;
			pause = false;
			waitForRendererStop.Reset();
			if (Lock)
				return;

			var thread = new Thread(() =>
			{
				try
				{
					MediaDecoder.Instance.OnContentChanged += ContentChanged;
					Render();
				}
				catch (Exception exc)
				{
					log.Error(exc.Message);
					Logic.Notify("An error was encountered in VR playback. See log for details.");
				}
				finally
				{
					MediaDecoder.Instance.OnContentChanged -= ContentChanged;
					Lock = false;
					_defaultBackgroundTexture?.Dispose();
					_defaultBackgroundTexture = null;
				}
			})
			{
				Name = $"Headset: {DescribeType}",
				IsBackground = true
			};
			thread.Start();
		}


		protected abstract void Render();



		bool _playbackLock = false;
		public bool Lock { get { return _playbackLock; } protected set { this._playbackLock = value; } }

		protected object localCritical = new object();


		protected ManualResetEvent waitForRendererStop = new ManualResetEvent(false);
		protected bool abort = false;
		protected bool pause = false;

		protected float currentTime = 0;

		protected SharpDX.Toolkit.Graphics.Effect customEffectL;
		protected SharpDX.Toolkit.Graphics.Effect customEffectR;


		private static SharpDX.Toolkit.Graphics.EffectCompilerResult gammaShader = null;
		private static SharpDX.Toolkit.Graphics.EffectCompilerResult GammaShader
		{
			get
			{
				if (gammaShader == null)
				{
					string shaderSource = Properties.Resources.GammaShader;
					SharpDX.Toolkit.Graphics.EffectCompiler compiler = new SharpDX.Toolkit.Graphics.EffectCompiler();
					var shaderCode = compiler.Compile(shaderSource, "gamma shader", SharpDX.Toolkit.Graphics.EffectCompilerFlags.Debug | SharpDX.Toolkit.Graphics.EffectCompilerFlags.EnableBackwardsCompatibility | SharpDX.Toolkit.Graphics.EffectCompilerFlags.SkipOptimization);

					if (shaderCode.HasErrors)
						throw new HeadsetError("Shader compile error:\n" + string.Join("\n", shaderCode.Logger.Messages));
					gammaShader = shaderCode;
				}
				return gammaShader;
			}
		}


		public static SharpDX.Toolkit.Graphics.Effect GetCustomEffect(SharpDX.Toolkit.Graphics.GraphicsDevice gd)
		{
			var ce = new SharpDX.Toolkit.Graphics.Effect(gd, GammaShader.EffectData);
			ce.CurrentTechnique = ce.Techniques["ColorTechnique"];
			ce.CurrentTechnique.Passes[0].Apply();
			return ce;
		}


		protected VRUI vrui;
		protected bool ShouldShowVRUI
		{
			get
			{
				if (Media.contentType != ServiceResult.ContentType.video)
					return false;
				return pause;
			}
		}

		public void Pause()
		{
			vrui?.EnqueueUIRedraw();
			pause = true;
		}

		public void UnPause() { pause = false; }

		public void Stop()
		{
			Media = null;
			pause = true;
			vrui?.EnqueueUIRedraw();
		}

		public void UpdateTime(float time)
		{
			vrui?.EnqueueUIRedraw();
			currentTime = time;
		}


		public void Abort()
		{
			abort = true;
		}
		



		protected SharpDX.Toolkit.Graphics.GraphicsDevice _gd;
		protected Device _device;

        public abstract event Action<Vector3, Quaternion, float> ProvideLook;

        abstract protected float Gamma { get; }
        public abstract string DescribeType { get; }


		private SharpDX.Toolkit.Graphics.Texture2D _defaultBackgroundTexture = null;
		public SharpDX.Toolkit.Graphics.Texture2D DefaultBackgroundTexture
		{
			get
			{
				if(_defaultBackgroundTexture == null)
				{
					var assembly = GetType().Assembly;
					var fullResourceName = "Bivrost.Bivrost360Player.Resources.default-background-equirectangular.png";
					using (var stream = assembly.GetManifestResourceStream(fullResourceName))
					{
						_defaultBackgroundTexture = SharpDX.Toolkit.Graphics.Texture2D.Load(_gd, stream);
					}

					_defaultBackgroundTexture.Disposing += (s, e) =>
					{
						log.Info("Default background is being disposed.");
					};
				}


				return _defaultBackgroundTexture;
			}
		}

		
		void TextureCleanup()
		{
			localBitmapTextureL?.Dispose();
			localBitmapTextureR?.Dispose();
			localBitmapTextureL = null;
			localBitmapTextureR = null;
		}


		public void SetDefaultScene()
		{
			lock (localCritical)
			{
				TextureCleanup();

				customEffectL.Parameters["UserTex"].SetResource(DefaultBackgroundTexture);
				customEffectL.Parameters["gammaFactor"].SetValue(Gamma);
				customEffectL.CurrentTechnique = customEffectL.Techniques["ColorTechnique"];
				customEffectL.CurrentTechnique.Passes[0].Apply();

				customEffectR.Parameters["UserTex"].SetResource(DefaultBackgroundTexture);
				customEffectR.Parameters["gammaFactor"].SetValue(Gamma);
				customEffectR.CurrentTechnique = customEffectR.Techniques["ColorTechnique"];
				customEffectR.CurrentTechnique.Passes[0].Apply();

				primitive?.Dispose();
				primitive = GraphicTools.CreateGeometry(ProjectionMode.Sphere, _gd, false);
			}

			vrui?.EnqueueUIRedraw();
		}


		private bool contentUpdateRequested = true;
		protected void UpdateContentIfRequested()
		{
			if (contentUpdateRequested)
			{
				contentUpdateRequested = false;
				lock (localCritical)
					MediaDecoder.Instance.ContentRequested(this);
			}
		}
		private void ContentChanged()
		{
			contentUpdateRequested = true;
		}


		abstract public bool IsPresent();


		protected SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;

		void IContentUpdatableFromMediaEngine.ReceiveTextures(Texture2D textureL, Texture2D textureR)
		{
			if (MediaDecoder.Instance.TextureReleased)
			{
				log.Error("MediaDecoder texture released");
				return;
			}

			lock (localCritical)
			{
				TextureCleanup();

				using (var resourceL = textureL.QueryInterface<SharpDX.DXGI.Resource>())
				using (var sharedTexL = _device.OpenSharedResource<Texture2D>(resourceL.SharedHandle))
				{
					customEffectL.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL));
					customEffectL.Parameters["gammaFactor"].SetValue(Gamma);
					customEffectL.CurrentTechnique = customEffectL.Techniques["ColorTechnique"];
					customEffectL.CurrentTechnique.Passes[0].Apply();
				}

				using (var resourceR = (textureR ?? textureL).QueryInterface<SharpDX.DXGI.Resource>())
				using (var sharedTexR = _device.OpenSharedResource<Texture2D>(resourceR.SharedHandle))
				{
					customEffectR.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR));
					customEffectR.Parameters["gammaFactor"].SetValue(Gamma);
					customEffectR.CurrentTechnique = customEffectR.Techniques["ColorTechnique"];
					customEffectR.CurrentTechnique.Passes[0].Apply();
				}

				//_device.ImmediateContext.Flush();
			}

			vrui?.EnqueueUIRedraw();
		}


		Texture2D localBitmapTextureL;
		Texture2D localBitmapTextureR;



		private Texture2D BitmapAndCoordsToTexture2D(Bitmap bitmap, MediaDecoder.ClipCoords coords)
		{
			var rect = coords.SrcRectSystemDrawing(bitmap.Width, bitmap.Height);
			var data = bitmap.LockBits(
				rect,
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppRgb
			);
			DataRectangle dataRect = new DataRectangle(data.Scan0, data.Stride);
			var tex = new Texture2D(
				_device,
				new Texture2DDescription()
				{
					Width = rect.Width,
					Height = rect.Height,
					MipLevels = 1,
					ArraySize = 1,
					Format = SharpDX.DXGI.Format.B8G8R8X8_UNorm,
					Usage = ResourceUsage.Default,
					SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
					BindFlags = /*BindFlags.RenderTarget |*/ BindFlags.ShaderResource,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.Shared
				},
				dataRect
			);
			bitmap.UnlockBits(data);

			return tex;
		}

		void IContentUpdatableFromMediaEngine.ReceiveBitmap(Bitmap bitmap, MediaDecoder.ClipCoords texL, MediaDecoder.ClipCoords texR)
		{
			log.Info($"Received image of size {bitmap.Width}x{bitmap.Height} from stream");


			lock (localCritical)
			{
				TextureCleanup();

				localBitmapTextureL = BitmapAndCoordsToTexture2D(bitmap, texL);
				localBitmapTextureR = (texR != null) ? BitmapAndCoordsToTexture2D(bitmap, texR) : null;

				using (var resourceL = localBitmapTextureL.QueryInterface<SharpDX.DXGI.Resource>())
				using (var sharedTexL = _device.OpenSharedResource<Texture2D>(resourceL.SharedHandle))
				{
					customEffectL.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL));
					customEffectL.Parameters["gammaFactor"].SetValue(Gamma);
					customEffectL.CurrentTechnique = customEffectL.Techniques["ColorTechnique"];
					customEffectL.CurrentTechnique.Passes[0].Apply();
				}

				using (var resourceR = (localBitmapTextureR ?? localBitmapTextureL).QueryInterface<SharpDX.DXGI.Resource>())
				using (var sharedTexR = _device.OpenSharedResource<Texture2D>(resourceR.SharedHandle))
				{
					customEffectR.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR));
					customEffectR.Parameters["gammaFactor"].SetValue(Gamma);
					customEffectR.CurrentTechnique = customEffectR.Techniques["ColorTechnique"];
					customEffectR.CurrentTechnique.Passes[0].Apply();
				}
			}

			log.Info($"Changed texture");
		}


		void IContentUpdatableFromMediaEngine.ClearContent()
		{
			SetDefaultScene();
		}

		void IContentUpdatableFromMediaEngine.SetProjection(ProjectionMode projection)
		{
			lock (localCritical)
			{
				primitive?.Dispose();
				primitive = GraphicTools.CreateGeometry(projection, _gd, false);
			}
		}
	}
}
