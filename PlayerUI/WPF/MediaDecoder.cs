using SharpDX.DXGI;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

		public bool IsPlaying { get; private set; }

		private static MediaDecoder _instance = null;
		public static MediaDecoder Instance { get { return MediaDecoder._instance; } }

		public MediaDecoder(DPFCanvas canvas)
		{
			IsPlaying = false;
			MediaDecoder._instance = this;
			this._canvas = canvas;
			Init();
		}


		private void Init()
		{
			System.Diagnostics.Debug.WriteLine("Media Init: " + System.Threading.Thread.CurrentThread.ManagedThreadId + " " + System.Threading.Thread.CurrentThread.GetApartmentState());

			MediaManager.Startup();
			var mediaEngineFactory = new MediaEngineClassFactory();
			var dxgiManager = new DXGIDeviceManager();
			var device = _canvas.GetDevice();
			dxgiManager.ResetDevice(device);
			MediaEngineAttributes attr = new MediaEngineAttributes();
			attr.VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm;
			attr.DxgiManager = dxgiManager;
			_mediaEngine = new MediaEngine(mediaEngineFactory, attr, MediaEngineCreateFlags.None);
			_mediaEngine.PlaybackEvent += (playEvent, param1, param2) =>
			{
				switch (playEvent)
				{
					case MediaEngineEvent.CanPlay:
						eventReadyToPlay.Set();
						//VideoLoaded(mediaEngine.Duration);
						break;
					case MediaEngineEvent.TimeUpdate:
						//TimeUpdate(mediaEngine.CurrentTime);
						break;
					case MediaEngineEvent.Error:
					case MediaEngineEvent.Abort:
					case MediaEngineEvent.Ended:
						System.Diagnostics.Debug.WriteLine(playEvent.ToString());
						break;
				}
			};

			_mediaEngineEx = _mediaEngine.QueryInterface<MediaEngineEx>();
		}

		public void Play(string fileName)
		{
			
		}

		public MediaEngine Engine { get { return this._mediaEngine; } }

		public void Shutdown()
		{
			_cancelTransferFrame.Cancel();
			MediaManager.Shutdown();
		}

	}
}
