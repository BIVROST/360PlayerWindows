using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PlayerUI
{
	public class Logic
	{
		private static Logic _instance = null;
		public static Logic Instance { 
			get { 
				if (_instance == null) 
					_instance = new Logic();
				return _instance;
			}
		}
		
		public Settings settings;
		private WebSocketServer webSocketServer;

		public Logic()
		{
			settings = new Settings();

			Task.Factory.StartNew(() =>
			{
				webSocketServer = new WebSocketServer("ws://127.0.0.1:24876"); // PORT "BIVRO" 24876
				try
				{
					webSocketServer.Start(socket =>
					{
						socket.OnOpen = () => Console.WriteLine("Open!");
						socket.OnClose = () => Console.WriteLine("Close!");
						socket.OnMessage = message =>
						{
							switch (message)
							{
								case "version":
									var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName();
									socket.Send(assembly.Name + ";" + assembly.Version.ToString());
									break;

								default:
									socket.Send(message);
									break;
							};
						};
					});
				}
				catch (Exception) { }
			});

		}

		~Logic()
		{
			try
			{
				webSocketServer.Dispose();
			}
			catch (Exception) { }
		}

		public void ReloadPlayer()
		{
			System.Diagnostics.Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
			Application.Current.Shutdown();
		}
	}
}
