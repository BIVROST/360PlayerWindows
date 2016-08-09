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
	public partial class LogWindow : Window, LogWriter
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


		protected override void OnInitialized(EventArgs e)
		{
			Instance = this;
			base.OnInitialized(e);
			Logger.RegisterWriter(this);
		}


		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.UnregisterWriter(this);
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
			string f = (Logger.LogWriters.First(lw => lw is TextFileLogWriter) as TextFileLogWriter).LogFile;
			System.Diagnostics.Process.Start(f);
		}
	}
}
