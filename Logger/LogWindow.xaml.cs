using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace Bivrost.Log
{
	/// <summary>
	/// Interaction logic for LogWindow.xaml
	/// </summary>
	public partial class LogWindow : Window, LogListener
	{
		private bool follow;
		static LogWindow Instance;

		public LogWindow()
		{
			InitializeComponent();
		}


		public static bool IsDisplaying
		{
			get { return Instance != null; }
		}


		public static void CloseLogWindowIfOpened()
		{
			if (Instance != null)
				Instance.Close();
		}


		protected override void OnInitialized(EventArgs e)
		{
			Instance = this;
			base.OnInitialized(e);
			Logger.RegisterListener(this);
		}


		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.UnregisterListener(this);
			base.OnClosing(e);
			Instance = null;
		}

		public void Write(string time, LogType type, string msg, string path)
		{
			Contents.Dispatcher.Invoke( () =>
			{
				Contents.Text += string.Format("[{1}] {0}\r\n{2}\r\n\r\n{3}\r\n\r\n", time, type, msg, path);
				if (follow)
					ScrollViewer.ScrollToEnd();
			});
		}

		private void UpdateFollow(object sender, RoutedEventArgs e)
		{
			follow = Follow.IsChecked.GetValueOrDefault();
		}

		private void Clear(object sender, RoutedEventArgs e)
		{
			Contents.Clear();
		}

		private void OpenTxt(object sender, RoutedEventArgs e)
		{
			string f = (Logger.listeners.First(lw => lw is TextFileLogListener) as TextFileLogListener).LogFile;
			System.Diagnostics.Process.Start(f);
		}
	}
}
