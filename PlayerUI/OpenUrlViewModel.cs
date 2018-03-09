using Bivrost.Log;
using Caliburn.Micro;
using Bivrost.Bivrost360Player.Tools;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player
{
	public class OpenUrlViewModel : Screen
	{
		private OpenUrlView view;

		public OpenUrlViewModel(string url = "")
		{
			Url = url;
			DisplayName = "Open video URL";
		}

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);
			this.view = view as OpenUrlView;
		}

		private string _url = "";
		public string Url {
			get { return _url; }
			set
			{
				_url = value;
				NotifyOfPropertyChange(() => Url);
			}
		}


		/// <summary>
		/// On button press
		/// </summary>
        public void Open()
		{
			Url = Url.Trim();
			
			view.Open.IsEnabled = false;
			view.Url.IsEnabled = false;
			view.progressBar.Visibility = System.Windows.Visibility.Visible;

			TryClose(true);
		}
		

		/// <summary>
		/// Executes OpenUrlViewModel window and returns an URL that the user passed, or null if the user cancelled.
		/// Doesn't check validity
		/// </summary>
		public static string GetURI(string defaultUrl = "")
		{
			OpenUrlViewModel ouvm = new OpenUrlViewModel(defaultUrl);
			WindowManager windowManager = new WindowManager();
			bool? success = windowManager.ShowDialog(ouvm);
			// return null if user cancelled
			if (!success.GetValueOrDefault(false))
				return null;
			// return a string is open was pressed - even the url is in valid or is empty
			return ouvm._url ?? "";
		}
	}
}
