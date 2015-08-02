using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PlayerUI
{
	public class TestWindowViewModel : Screen
	{
		MediaDecoder mediaDecoder;
		DPFCanvas _canvas;

		public TestWindowViewModel(DPFCanvas canvas)
		{
			DisplayName = "TEST";
			_canvas = canvas;
		}

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);
			mediaDecoder = new MediaDecoder(_canvas, view as Window);
			mediaDecoder.Init();
		}

		public void Load()
		{
			mediaDecoder.LoadMedia(@"d:\Octopus 3D 360 - Panocam3d.com.mp4");
			//mediaDecoder.LoadMedia(@"D:\videoplayback.mp4");
			//mediaDecoder.LoadMedia(@"D:\raindrops.mp3");
			

		}

		public void Play()
		{
			mediaDecoder.Play();
		}

		public void Stop()
		{
			mediaDecoder.Stop();
		}


	}
}
