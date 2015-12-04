using Caliburn.Micro;
using PlayerUI.Tools;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class OpenUrlViewModel : Screen
	{
		private OpenUrlView view;

		public OpenUrlViewModel()
		{
			DisplayName = "Open video URL";
		}

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);
			this.view = view as OpenUrlView;
		}

		private string _url = "";
		public string Url { get { return _url; } set {
				_url = value;
				NotifyOfPropertyChange(() => Url);
			} }

		public Uri Uri { get; set; }

		public string VideoUrl { get; set; }

		public bool Valid { get; set; } = false;

		public void Open()
		{
			Url = Url.Trim();
			if (string.IsNullOrWhiteSpace(Url)) TryClose();

			//Uri uri;
			//string correctedUrl;
			//Valid = StreamingServices.CheckUrlValid(Url, out correctedUrl, out uri);

			//{
			//	RestClient client = new RestClient(uri);
			//	IRestRequest request = new RestRequest(Method.HEAD);
			//	//request.AddHeader("Accept", "text/html");
			//	IRestResponse response = client.Execute(request);
			//	if (response.StatusCode != System.Net.HttpStatusCode.OK)
			//	{
			//		Valid = false;
			//		TryClose();
			//	}
			//}

			//Uri = uri;

			//if (Valid)
			//{
			//	Url = correctedUrl;
			//	//Console.WriteLine(StreamingServices.DetectService(Uri));
			//	string videoUrl;
			//	if (StreamingServices.TryParseVideoFile(Uri, out videoUrl))
			//	{
			//		VideoUrl = videoUrl;
			//	}
			//}

			//TryClose();

			if(view == null)
            {
                Process();
                return;
            }

			Task.Factory.StartNew(() =>
			{
				Process();
			});

			view.Open.IsEnabled = false;
			view.Url.IsEnabled = false;
			view.progressBar.Visibility = System.Windows.Visibility.Visible;
        }

		private void Process()
		{
			Uri uri;
			string correctedUrl;
			Valid = StreamingServices.CheckUrlValid(Url, out correctedUrl, out uri);

			{
				RestClient client = new RestClient(uri);
				IRestRequest request = new RestRequest(Method.HEAD);
				request.AddHeader("Accept", "text/html");
				IRestResponse response = client.Execute(request);
				if (response.StatusCode != System.Net.HttpStatusCode.OK)
				{
                    if(view != null)
					    Execute.OnUIThreadAsync(() =>
					    {
						    Valid = false;
						    TryClose();
					    });
                    return;

				}
			}

			Uri = uri;

			if (Valid)
			{
				Url = correctedUrl;
				//Console.WriteLine(StreamingServices.DetectService(Uri));
				string videoUrl;
				if (StreamingServices.TryParseVideoFile(Uri, out videoUrl))
				{
					VideoUrl = videoUrl;
				}
			}
            if (view != null)
                Execute.OnUIThreadAsync(() =>
			    {
				    TryClose();
			    });
		}

	}
}
