using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Input;
using System.Collections.Specialized;
using System.Collections;
using System.Windows.Threading;

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
	/// Wrapper for any IEnumerable collection providing INotifyCollectionChanged for MVVM
	/// Usage: wrap an collection in this object and when it has been modified, fire the NotifyCollectionChanged method
	/// that notifies the view of the change. It will trigger a NotifyCollectionChangedEventArgs of Reset type
	/// </summary>
	public class ObservableCollectionWrapper : INotifyCollectionChanged, IEnumerable
	{
		private IEnumerable collection;
		private Dispatcher dispatcher;

		public ObservableCollectionWrapper(IEnumerable collection)
		{
			this.collection = collection;
		}

		public ObservableCollectionWrapper(IEnumerable collection, Dispatcher dispatcher) : this(collection)
		{
			this.dispatcher = dispatcher;
		}

		public void NotifyCollectionChanged()
		{
			var ev = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null);
			if (dispatcher == null)
				CollectionChanged?.Invoke(this, ev);
			else
				dispatcher.InvokeAsync(() => CollectionChanged?.Invoke(this, ev));
		}

		public IEnumerator GetEnumerator()
		{
			return collection.GetEnumerator();
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;
	}


	/// <summary>
	/// Interaction logic for LogWindow.xaml
	/// </summary>
	partial class LogWindow : Window, LogListener
	{

		internal static Logger log = new Logger("Log Window");


		public LogWindow()
		{
			Published = new ObservableCollectionWrapper(LoggerManager.published, Dispatcher);
			LoggerManager.PublishedListUpdated += d => Published.NotifyCollectionChanged();

			Initialized += (s, e) =>
			{
				Instance = this;
				LoggerManager.RegisterListener(this);
			};

			Closing += (s, e) =>
			{
				LoggerManager.UnregisterListener(this);
				Instance = null;
			};

			InitializeComponent();

#if DEBUG
			// debug published property
			SizeChanged += (s, e) => LoggerManager.Publish("log window size", e.NewSize);
#endif

		}


		private static LogWindow Instance;


		public static void CloseIfOpened()
		{
			if (Instance != null)
				Instance.Close();
		}


		public static void OpenIfClosed()
		{
			if (Instance != null)
			{
				log.Info("Refused to open a second log viewer.");
			}
			else
			{
				Window lv = new LogWindow();
				lv.Show();
			}
		}


		public void Write(LoggerManager.LogElement entry)
		{
			List_Log.Dispatcher.InvokeAsync(() =>
		    {
				lock (entriesSyncLock)
				   Entries.Add(entry);
				if (FollowLog)
					ScrollViewer.ScrollToEnd();
		    });
		}


		public ObservableCollectionWrapper Published { get; }


		public ObservableCollection<LoggerManager.LogElement> Entries { get; private set; } = new ObservableCollection<LoggerManager.LogElement>();
		private object entriesSyncLock = new object();


		public bool FollowLog { get; set; } = true;


		private ICommand _clearLogCommand;
		public ICommand ClearLogCommand
		{
			get
			{
				if (_clearLogCommand == null)
					_clearLogCommand = new RelayCommand(
						(p) =>
						{
							lock (entriesSyncLock)
								Entries.Clear();
							log.Info("Log has been cleared");
						}
					);
				return _clearLogCommand;
			}
		}
		

		private ICommand _openTxtCommand;
		public ICommand OpenTxtCommand
		{
			get
			{
				if (_openTxtCommand == null)
				{
					Func<TextFileLogListener> getTextFileLogListener = () => LoggerManager.listeners.FirstOrDefault(lw => lw is TextFileLogListener) as TextFileLogListener;

					_openTxtCommand = new RelayCommand(
						(p) =>
						{
							var tfll = getTextFileLogListener();
							if (tfll == null)
							{
								log.Error("No TextFileLogListener available");
							}
							else
							{
								System.Diagnostics.Process.Start(tfll.LogFile);
							}
						},
						(p) => getTextFileLogListener() != null
					);
				}
				return _openTxtCommand;
			}
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
