using Bivrost.AnalyticsForVR;
using SharpDX.Direct3D11;
using System;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using Bivrost.Bivrost360Player.Streaming;
using Bivrost.Log;
using Bivrost.Bivrost360Player.Tools;

namespace Bivrost.Bivrost360Player
{

	public class HeadsetError : Exception
	{
		public HeadsetError(string msg) : base(msg) { }
		public HeadsetError(Exception inner, string msg) : base(msg, inner) { }
	}


	public abstract class Headset : ILookProvider, IUpdatableSceneSettings
    {

		private static ServiceResult nothingIsPlaying = new ServiceResult(null, "(none)", "nothing")
		{
			description = "",
			stereoscopy = MediaDecoder.VideoMode.Autodetect,
			projection = MediaDecoder.ProjectionMode.Sphere,
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
				UpdateSceneSettings(Media.projection, Media.stereoscopy);
			}
		}
		public bool _stereoVideo => Array.IndexOf(new[] { MediaDecoder.VideoMode.Mono, MediaDecoder.VideoMode.Autodetect }, Media.stereoscopy) < 0;
		public MediaDecoder.ProjectionMode Projection => Media.projection;
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
					Render();
				}
				catch (Exception exc)
				{
					log.Error(exc.Message);
					Logic.Notify("An error was encountered in VR playback. See log for details.");
				}
				finally
				{
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
			SetDefaultScene();
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
					var fullResourceName = "Bivrost.Bivrost360Player.Resources.default-background-requirectangular.png";
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


		public void ResizeTexture(Texture2D textureL, Texture2D textureR)
		{
			if(textureL == null && textureR == null)
			{
				log.Info("ResizeTexture got null textures, loading defaults...");

				SetDefaultScene();
				return;
			}

			log.Info($"ResizeTexture {textureL}, {textureR} enqueued");

			updateSettingsActionQueue.Enqueue(() => 
			{
				if (MediaDecoder.Instance.TextureReleased) {
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


					using (var resourceR = textureR.QueryInterface<SharpDX.DXGI.Resource>())
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
			});
		}


		protected void BindToMediadecoder()
		{
			//_stereoVideo ? MediaDecoder.Instance.TextureR : MediaDecoder.Instance.TextureL);
			ResizeTexture(MediaDecoder.Instance.TextureL, MediaDecoder.Instance.TextureR);
			MediaDecoder.Instance.OnFormatChanged += ResizeTexture;
		}


		void TextureCleanup()
		{
			var disposableL = customEffectL.Parameters["UserTex"]?.GetResource<IDisposable>();
			var disposableR = customEffectR.Parameters["UserTex"]?.GetResource<IDisposable>();

			if (disposableL != null && disposableL != _defaultBackgroundTexture)
				disposableL.Dispose();

			if (disposableR != null && disposableR != _defaultBackgroundTexture)
				disposableR.Dispose();
		}


		public void SetDefaultScene()
		{
			updateSettingsActionQueue.Enqueue(() => {
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
				}

				vrui?.EnqueueUIRedraw();

			});

			// also enqueued
			UpdateSceneSettings(MediaDecoder.ProjectionMode.Sphere, MediaDecoder.VideoMode.Mono);
		}

		abstract public bool IsPresent();


		protected Bivrost.ActionQueue updateSettingsActionQueue = new Bivrost.ActionQueue();
		protected SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;
		public void UpdateSceneSettings(MediaDecoder.ProjectionMode projectionMode, MediaDecoder.VideoMode stereoscopy)
		{
			updateSettingsActionQueue.Enqueue(() =>
			{
				primitive?.Dispose();
				primitive = GraphicTools.CreateGeometry(projectionMode, _gd, false);
			});
		}
	}
}
