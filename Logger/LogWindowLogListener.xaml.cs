using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

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
			//List_Published.ItemsSource = LoggerManager.published;

			List_Log.ItemsSource = Entries;
			System.Windows.Data.BindingOperations.EnableCollectionSynchronization(Entries, entriesSyncLock);

		}


		private void UpdatePublishedValues(object sender, EventArgs e)
		{
			var list = LoggerManager.published.ToList();
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
			LoggerManager.RegisterListener(this);
		}


		protected override void OnClosing(CancelEventArgs e)
		{
			LoggerManager.UnregisterListener(this);
			base.OnClosing(e);
			Instance = null;
		}

		//public List<LoggerManager.LogElement> Entries { get; private set; } = new List<LoggerManager.LogElement>();
		public ObservableCollection<LoggerManager.LogElement> Entries { get; private set; } = new ObservableCollection<LoggerManager.LogElement>();
		public object entriesSyncLock = new object();

		public void Write(LoggerManager.LogElement entry)
		{
			List_Log.Dispatcher.Invoke( () =>
			{
				//Contents.Text += $"[{entry.type} {entry.tag}] {entry.time}\r\n{entry.msg}\r\n\r\n{entry.path}\r\n\r\n";
				lock(entriesSyncLock)
					Entries.Add(entry);
				//if (follow)
				//	List_Log.scr
				//	ScrollViewer.ScrollToEnd();
			});
		}

		private void UpdateFollow(object sender, RoutedEventArgs e)
		{
			follow = Follow.IsChecked.GetValueOrDefault();
		}

		private void Clear(object sender, RoutedEventArgs e)
		{
			lock(entriesSyncLock)
				Entries.Clear();
		}


		private void OpenTxt(object sender, RoutedEventArgs e)
		{
			string f = (LoggerManager.listeners.First(lw => lw is TextFileLogListener) as TextFileLogListener).LogFile;
			System.Diagnostics.Process.Start(f);
		}
	}


	public class LogTypeToColorConverter : System.Windows.Markup.MarkupExtension, System.Windows.Data.IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			switch ((LogType)value) {
			case LogType.error:
				return new SolidColorBrush(Color.FromArgb(32, 255, 255, 0));
			case LogType.fatal:
				return new SolidColorBrush(Color.FromArgb(32, 255, 0, 0));
			}
			return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}   
	}

}
