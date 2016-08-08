using SharpDX.Direct3D11;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerUI
{

	public class HeadsetError : Exception
	{
		public HeadsetError(string msg) : base(msg) { }
		public HeadsetError(Exception inner, string msg) : base(msg, inner) { }
	}


	public abstract class Headset
	{
		public Texture2D textureL;
		public Texture2D textureR;
		public bool _stereoVideo = false;
		public MediaDecoder.ProjectionMode _projection = MediaDecoder.ProjectionMode.Sphere;



		public void Start()
		{
			abort = false;
			pause = false;
			waitForRendererStop.Reset();
			if (Lock)
				return;
			Task.Factory.StartNew(() =>
			{
				try
				{
					Render();
				}
#if !DEBUG
				catch(Exception exc)
				{
					Console.WriteLine("[EXC] " + exc.Message);
				}
#endif
				finally
				{
					Lock = false;
				}
			});
		}


		protected abstract void Render();



		bool _playbackLock = false;
		public bool Lock { get { return _playbackLock; } protected set { this._playbackLock = value; } }

		protected object localCritical = new object();


		protected ManualResetEvent waitForRendererStop = new ManualResetEvent(false);
		protected bool abort = false;
		protected bool pause = false;

		protected string movieTitle = "";
		protected float duration = 0;
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


		public void Pause()
		{
			vrui?.EnqueueUIRedraw();
			pause = true;
		}
		public void UnPause() { pause = false; }

		public void UpdateTime(float time)
		{
			vrui?.EnqueueUIRedraw();
			currentTime = time;
		}

		public void Configure(string title, float movieDuration)
		{
			movieTitle = title;
			duration = movieDuration;
		}

		public void Stop()
		{
			abort = true;
		}

		public void Reset()
		{
			abort = false;
		}


		protected SharpDX.Toolkit.Graphics.GraphicsDevice _gd;
		protected Device _device;

		abstract protected float Gamma { get; }

		protected void ResizeTexture(Texture2D tL, Texture2D tR)
		{
			if (MediaDecoder.Instance.TextureReleased) return;

			var tempL = textureL;
			var tempR = textureR;

			lock (localCritical)
			{
				(customEffectL.Parameters["UserTex"]?.GetResource<IDisposable>())?.Dispose();
				(customEffectR.Parameters["UserTex"]?.GetResource<IDisposable>())?.Dispose();
				textureL = tL;
				textureR = tR;

				var resourceL = textureL.QueryInterface<SharpDX.DXGI.Resource>();
				var sharedTexL = _device.OpenSharedResource<Texture2D>(resourceL.SharedHandle);


				//basicEffectL.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL);
				customEffectL.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL));
				customEffectL.Parameters["gammaFactor"].SetValue(Gamma);
				customEffectL.CurrentTechnique = customEffectL.Techniques["ColorTechnique"];
				customEffectL.CurrentTechnique.Passes[0].Apply();

				resourceL?.Dispose();
				sharedTexL?.Dispose();

				if (_stereoVideo)
				{
					var resourceR = textureR.QueryInterface<SharpDX.DXGI.Resource>();
					var sharedTexR = _device.OpenSharedResource<Texture2D>(resourceR.SharedHandle);

					//basicEffectR.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR);
					customEffectR.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR));
					customEffectR.Parameters["gammaFactor"].SetValue(Gamma);
					customEffectR.CurrentTechnique = customEffectR.Techniques["ColorTechnique"];
					customEffectR.CurrentTechnique.Passes[0].Apply();

					resourceR?.Dispose();
					sharedTexR?.Dispose();
				}
				//_device.ImmediateContext.Flush();
			}

		}


		abstract public bool IsPresent();

	}
}
