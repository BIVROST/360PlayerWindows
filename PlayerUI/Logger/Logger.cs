using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace PlayerUI
{
	/// <summary>
	/// Logger.For(this).Warn();
	/// </summary>


	public static class Logger
	{
		private enum Type
		{
			info,
			error,
			notification,
			fatal
		}


		static EventLogEntryType ToEventLogEntryType(this Type t)
		{
			switch(t)
			{
				default:
				case Type.info:
				case Type.notification:
					return EventLogEntryType.Information;
				case Type.error:
					return EventLogEntryType.Warning;
				case Type.fatal:
					return EventLogEntryType.Error;
			}
		}

		private static string logFile = null;

		// TODO: async
		private static void WriteLogEntry(Type type, string msg)
		{
			string now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
			////// windows event log
			//var sSource = "360Player";
			//var sLog = "Application";

			//if (!EventLog.SourceExists(sSource))
			//	EventLog.CreateEventSource(sSource, sLog);

			//EventLog.WriteEntry(sSource, type + ": " + msg, type.ToEventLogEntryType());



			// trace
			Trace.WriteLine(msg, type.ToString());
			Trace.Indent();
			Trace.WriteLine(now);
			Trace.WriteLine(msg);
			Trace.Unindent();

			
			// text log
			lock (logFile)
			{
				try
				{
					File.AppendAllText(logFile, string.Format("[{0}] {1}\r\n\t{2}\r\n\r\n", type, now, msg.Replace("\n", "\n\t")));
				}
				catch(Exception e)
				{
					Console.Error.WriteLine("Error writing to text log: " + e);
				}
			}
		}

		internal static void RegisterLogDirectory(string logDirectory)
		{
			if (logFile != null)
				return;

			string now = DateTime.Now.ToString("yyyy-MM-ddTHHmmss");
			#if DEBUG
			string version = "DEBUG";
			#else
			string version = "v" + Assembly.GetEntryAssembly().GetName().Version.ToString();
			#endif
			logFile = logDirectory + string.Format("log-360Player-{0}-{1}.txt", version, now);

			Console.WriteLine("LOG FILE: " + logFile);

			Info("Registered log directory: " + logDirectory);
		}

		// informacje do debugu, niewyświetlane
		public static void Info(string fmt, params object[] args)
		{
			WriteLogEntry(Type.info, string.Format(fmt, args));
		}

		// informacje do debugu, niewyświetlane
		public static void Error(string fmt, params object[] args)
		{
			WriteLogEntry(Type.error, string.Format(fmt, args));
		}

		public static void Error(Exception e, string additionalMsg="Exception")
		{
			WriteLogEntry(Type.error, additionalMsg + "\n" + e);
		}

		// informacje dla użytkownika, wyświetlane jako chmurka w rogu (samo zarządza ExecuteOnUIThread)
		public static void Notification(string fmt, params object[] args)
		{
			WriteLogEntry(Type.notification, string.Format(fmt, args));
			// TODO
		}

		// informacje dla użytkownika, wyświetlane jako MessageBox, aplikacja umrze po tym
		public static void Fatal(string fmt, params object[] args)
		{
			string msg = string.Format(fmt, args);
			WriteLogEntry(Type.fatal, msg);
			System.Windows.MessageBox.Show(msg, "Fatal error");
		}


		public static void Fatal(Exception e)
		{
			WriteLogEntry(Type.fatal, e.ToString());
			System.Windows.MessageBox.Show(e.ToString(), "Fatal error");
		}


		public static void Fatal(Exception e, string additionalMsg)
		{
			WriteLogEntry(Type.fatal, additionalMsg + "\r\n" + e.ToString());
			System.Windows.MessageBox.Show(e.ToString(), additionalMsg);
		}
	}
}
