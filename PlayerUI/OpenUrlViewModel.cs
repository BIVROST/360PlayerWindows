using Caliburn.Micro;
using PlayerUI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class OpenUrlViewModel : Screen
	{
		public OpenUrlViewModel()
		{
			DisplayName = "Open video URL";
		}

		public string Url { get; set; }

		public Uri Uri { get; set; }

		public string VideoUrl { get; set; }

		public bool Valid { get; set; } = false;

		public void Open()
		{
			Uri uri;
			string correctedUrl;
			Valid = StreamingServices.CheckUrlValid(Url, out correctedUrl, out uri);
			Uri = uri;

			if (Valid) Url = correctedUrl;
			Console.WriteLine(StreamingServices.DetectService(Uri));
			string videoUrl;
			if(StreamingServices.TryParseVideoFile(Uri, out videoUrl))
			{
				VideoUrl = videoUrl;
			}			

			TryClose();
		}

	}
}
