using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Globalization;
using Bivrost.Log;
using System.Windows.Input;
using System.Collections.Specialized;
using System.Collections;
using System.Collections.Concurrent;

namespace Bivrost.Log
{
	/// <summary>
	/// https://msdn.microsoft.com/en-us/magazine/dd419663.aspx?f=255&MSPPError=-2147217396#id0090030
	/// </summary>
	public class RelayCommand : ICommand
	{
		#region Fields 
		readonly Action<object> _execute;
		readonly Predicate<object> _canExecute;
		#endregion // Fields 
		#region Constructors 
		public RelayCommand(Action<object> execute) : this(execute, null) { }
		public RelayCommand(Action<object> execute, Predicate<object> canExecute)
		{
			if (execute == null)
				throw new ArgumentNullException("execute");
			_execute = execute; _canExecute = canExecute;
		}
		#endregion // Constructors 
		#region ICommand Members 
		public bool CanExecute(object parameter)
		{
			return _canExecute == null ? true : _canExecute(parameter);
		}
		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
		public void Execute(object parameter) { _execute(parameter); }
		#endregion // ICommand Members 
	}


	/// <summary>
	/// Interaction logic for LogWindow.xaml
	/// </summary>
	partial class LogWindow : Window, LogListener
	{
		static LogWindow Instance;
		//private DispatcherTimer dispatcherTimer;


		public LogWindow()
		{
			ClearLogCommand = new RelayCommand(p => ClearLog());
			OpenTxtCommand = new RelayCommand(p => OpenTxt());

			SizeChanged += (s, e) => LoggerManager.Publish("size", e.NewSize);

			Published = new ObservableConcurrentDictionary(LoggerManager.published);
			LoggerManager.PublishedListUpdated += d => Published.OnNotifyCollectionChanged();

			InitializeComponent();
		}


		public class ObservableConcurrentDictionary : INotifyCollectionChanged, IEnumerable
		{
			private ConcurrentDictionary<string, object> dict;

			public ObservableConcurrentDictionary(ConcurrentDictionary<string, object> dict)
			{
				this.dict = dict;
			}

			public void OnNotifyCollectionChanged()
			{
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
			}

			public IEnumerator GetEnumerator()
			{
				return ((IEnumerable)dict).GetEnumerator();
			}

			public event NotifyCollectionChangedEventHandler CollectionChanged;
		}


		public ObservableConcurrentDictionary Published { get; }
		//public ObservableCollection<KeyValuePair<string,object>> Published { get; }
		object publishedSyncLock = new object();

		//private void UpdatePublishedValues(object sender, EventArgs e)
		//{
		//	var list = LoggerManager.published.ToList();
		//	list.Sort((a, b) => a.Key.CompareTo(b.Key));
		//	List_Published.ItemsSource = list;
		//}

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

		public void Write(LoggerManager.LogElement entry)
		{
			List_Log.Dispatcher.Invoke(() =>
		    {
				//Contents.Text += $"[{entry.type} {entry.tag}] {entry.time}\r\n{entry.msg}\r\n\r\n{entry.path}\r\n\r\n";
				lock (entriesSyncLock)
				   Entries.Add(entry);
				if (FollowLog)
					ScrollViewer.ScrollToEnd();
		    });
		}

		public ObservableCollection<LoggerManager.LogElement> Entries { get; private set; } = new ObservableCollection<LoggerManager.LogElement>();
		private object entriesSyncLock = new object();
		public bool FollowLog { get; set; } = true;
		public RelayCommand ClearLogCommand { get; }
		public RelayCommand OpenTxtCommand { get; }


		private void ClearLog()
		{
			lock (entriesSyncLock)
				Entries.Clear();
		}


		private void OpenTxt()
		{
			string f = (LoggerManager.listeners.First(lw => lw is TextFileLogListener) as TextFileLogListener).LogFile;
			if(f != null)
				System.Diagnostics.Process.Start(f);
		}
	}


	public class LogTypeToColorConverter : System.Windows.Markup.MarkupExtension, System.Windows.Data.IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) { return this; }


		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch ((LogType)value)
			{
				case LogType.error:
					return new SolidColorBrush(Color.FromArgb(32, 255, 255, 0));
				case LogType.fatal:
					return new SolidColorBrush(Color.FromArgb(32, 255, 0, 0));
				default:
					return null;
			}
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return null; }
	}


}
