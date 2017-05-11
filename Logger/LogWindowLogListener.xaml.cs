using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Threading;
using System.Collections.Generic;

namespace Bivrost.Log
{
	/// <summary>
	/// Interaction logic for LogWindow.xaml
	/// </summary>
	public partial class LogWindowLogListener : Window, LogListener
	{
		private bool follow;
		static LogWindowLogListener Instance;
		private DispatcherTimer dispatcherTimer;

		public LogWindowLogListener()
		{
			InitializeComponent();
			Tabs.SelectionChanged += (s, e) => {
				var isPublished = e.AddedItems.Contains(Tab_Published);
				if (isPublished)
					dispatcherTimer.Start();
				else
					dispatcherTimer.Stop();
			};

			dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
			dispatcherTimer.Tick += new EventHandler(UpdatePublishedValues);
			dispatcherTimer.Interval = TimeSpan.FromSeconds(1 / 10f);

			Closing += (s,e) => dispatcherTimer.Stop();

			UpdatePublishedValues(null, null);
			List_Published.ItemsSource = Logger.published;
		}


		private void UpdatePublishedValues(object sender, EventArgs e)
		{
			var list = Logger.published.ToList();
			list.Sort((a, b) => a.Key.CompareTo(b.Key));
			List_Published.ItemsSource = list;
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

		public void Write(Logger.LogElement entry)
		{
			Contents.Dispatcher.Invoke( () =>
			{
				Contents.Text += $"[{entry.type}] {entry.time}\r\n{entry.msg}\r\n\r\n{entry.path}\r\n\r\n";
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
