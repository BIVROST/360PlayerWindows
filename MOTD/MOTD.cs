using Bivrost.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace Bivrost.MOTD
{

	public interface IMOTDBridge
	{
		string InstallId { get; }
		string Version { get; }
		string Product { get; }
	}


	public class MOTDClient
	{

		// TODO: register callbacks
		// TODO: html popup
		// TODO: register notification from system

		internal static Logger logger = new Logger("MOTD");
		private readonly Uri serverUri;
		private readonly IMOTDBridge app;

		public MOTDClient(Uri serverUri, IMOTDBridge app)
		{
			this.serverUri = serverUri;
			this.app = app;
		}


		public void RequestMOTD()
		{
			
		}


		protected void DisplayNotification(string text) { }
		protected void DisplayNotification(string text, string link, string url) { }
		protected void DisplayPopup(string title, string url, int width = 600, int height = 400) { }


	}
}
