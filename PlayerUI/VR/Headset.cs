using SharpDX.Direct3D11;
using System.Threading;

namespace PlayerUI
{
	public abstract class Headset
	{
		public Texture2D textureL;
		public Texture2D textureR;
		public bool _stereoVideo = false;
		public MediaDecoder.ProjectionMode _projection = MediaDecoder.ProjectionMode.Sphere;

		abstract public void Start();

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

		abstract public bool IsPresent();

	}
}
