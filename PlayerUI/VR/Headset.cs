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

		protected SharpDX.Toolkit.Graphics.BasicEffect basicEffectL;
		protected SharpDX.Toolkit.Graphics.BasicEffect basicEffectR;


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

		protected virtual void ResizeTexture(Texture2D tL, Texture2D tR)
		{
			if (MediaDecoder.Instance.TextureReleased) return;

			var tempL = textureL;
			var tempR = textureR;

			lock (localCritical)
			{
				basicEffectL.Texture?.Dispose();
				textureL = tL;

				if (_stereoVideo)
				{
					basicEffectR.Texture?.Dispose();
					textureR = tR;
				}



				var resourceL = textureL.QueryInterface<SharpDX.DXGI.Resource>();
				var sharedTexL = _device.OpenSharedResource<Texture2D>(resourceL.SharedHandle);
				basicEffectL.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL);
				resourceL?.Dispose();
				sharedTexL?.Dispose();

				if (_stereoVideo)
				{
					var resourceR = textureR.QueryInterface<SharpDX.DXGI.Resource>();
					var sharedTexR = _device.OpenSharedResource<Texture2D>(resourceR.SharedHandle);
					basicEffectR.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR);
					resourceR?.Dispose();
					sharedTexR?.Dispose();
				}
				//_device.ImmediateContext.Flush();
			}

		}

		abstract public bool IsPresent();

	}
}
