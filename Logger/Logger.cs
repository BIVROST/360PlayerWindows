using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Bivrost.Log
{

	public interface LogListener
	{
		void Write(string time, LogType type, string msg, string path);
	}

	#region writers
	/// <summary>
	/// Windows Event Log Writer.
	/// </summary>
	public class WindowsEventLogListener : LogListener
	{

		string sSource;

		static EventLogEntryType ToEventLogEntryType(LogType t)
		{
			switch (t)
			{
				default:
				case LogType.info:
				case LogType.notification:
					return EventLogEntryType.Information;
				case LogType.error:
					return EventLogEntryType.Warning;
				case LogType.fatal:
					return EventLogEntryType.Error;
			}
		}


		/// <summary>
		/// Logger with custom application name. This requires admin rights.
		/// </summary>
		/// <param name="appname"></param>
		/// <param name="logname"></param>
		public WindowsEventLogListener(string appname, string logname)
		{
			if (!EventLog.SourceExists(sSource))
				EventLog.CreateEventSource(sSource, logname);
			sSource = appname;
		}


		/// <summary>
		/// Logger with generic "Application" application name.
		/// </summary>
		public WindowsEventLogListener()
		{
			sSource = "Application";
		}


		public void Write(string time, LogType type, string msg, string path)
		{
			EventLog.WriteEntry(sSource, type + ": " + msg, ToEventLogEntryType(type));
		}

	}


    /// <summary>
    /// Writing to the console
    /// </summary>
    public class TraceLogListener : LogListener
    {
        public void Write(string time, LogType type, string msg, string path)
        {
            Trace.WriteLine(time + " at " + path, type.ToString());
            Trace.Indent();
            Trace.WriteLine(msg);
            Trace.Unindent();
            Trace.WriteLine("");
            Trace.Flush();
        }
    }

    /// <summary>
    /// Writing to the console
    /// </summary>
    public class TraceLogMsgOnlyListener : LogListener
    {
        public void Write(string time, LogType type, string msg, string path)
        {
            Trace.WriteLine(msg);
        }
    }


    /// <summary>
    /// Writing to a text file
    /// </summary>
    public class TextFileLogListener : LogListener
	{
        private FileStream fp;
        private UTF8Encoding encoding;

        public string LogFile { get; protected set; }


		public TextFileLogListener(string logDirectory, string logPrefix = "log", string version = null)
		{
			string now = DateTime.Now.ToString("yyyy-MM-ddTHHmmss");
			if(version == null)
#if DEBUG
				version = "DEBUG";
#else
				version = "v" + Assembly.GetEntryAssembly().GetName().Version.ToString();
#endif

			LogFile = logDirectory + string.Format("{2}-{0}-{1}.txt", version, now, logPrefix);

			Logger.Info("Log file: " + LogFile);
            fp = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            encoding = new System.Text.UTF8Encoding(false);
        }


		public void Write(string time, LogType type, string msg, string path)
		{
            byte[] buf = encoding.GetBytes($"[{type}] {time} at {path}\r\n\r\n{msg.Trim().Replace("\r\n", "\r\n\t")}\r\n");
            lock (LogFile)
            {
                fp.Write(buf, 0, buf.Length);
                fp.Flush();
            }
        }
    }

	#endregion

	public enum LogType
	{
		info,
		error,
		notification,
		fatal
	}


	public static class Logger
	{
		/// <summary>
		/// Returns a normalized path relative to the logger (or provided sourceFilePath)
		/// </summary>
		/// <param name="path"></param>
		/// <param name="sourceFilePath"></param>
		/// <returns></returns>
		static string NormalizePath(string path, [CallerFilePath] string sourceFilePath = "")
		{
			int l = Math.Min(path.Length, sourceFilePath.Length);
			for (int i = 0; i < l; i++)
				if (path[i] != sourceFilePath[i])
					return path.Substring(i);
			return "(Logger)"; // normalization failed
		}


		static string PathUtil(string sourceFilePath, int sourceLineNumber, string memberName)
		{
			return string.Format("{0}#{1} ({2})", NormalizePath(sourceFilePath), sourceLineNumber, memberName);
		}


		struct LogElement { public string now; public LogType type; public string msg; public string path; }
        static BlockingCollection<LogElement> logElementQueue = new BlockingCollection<LogElement>();


        static void WriteLogEntry(LogType type, string msg, string path)
		{
			string now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

			// normalize newlines to windows format
            if(msg != null)
			    msg = msg.Replace("\r\n", "\n").Replace("\n", "\r\n");

			logElementQueue.Add(new LogElement() { now = now, type = type, msg = msg, path = path });
		}


		static void WriteLogThread()
		{
			while (true)
			{
                LogElement e = logElementQueue.Take();				
                lock(listeners)
				    foreach (var l in listeners)
					    l.Write(e.now, e.type, e.msg, e.path);
			}
		}


		public static HashSet<LogListener> listeners = new HashSet<LogListener>();


		static Thread thread;


		public static void RegisterListener(LogListener lw)
		{
			lock(listeners)
			{
				if (thread == null)
				{
					thread = new Thread(new ThreadStart(WriteLogThread))
					{
						IsBackground = true,
						Name = "log listener thread"
					};
					thread.Start();
				}
			}
			listeners.Add(lw);
			Info("Registered log writer: " + lw);
		}


        public static void UnregisterListener(LogListener lw)
        {
            lock (listeners)
                listeners.Remove(lw);
            Info("Unregistered log writer: " + lw);
        }


        public static void UnregisterListener(Predicate<LogListener> predicate)
        {
            lock (listeners)
                Info("Unregistered " + listeners.RemoveWhere(predicate) + " log writes");
        }


        /// <summary>
        /// Use for registering degug information.
        /// Not displayed on screen.
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="memberName">(automatically added) source code trace information</param>
        /// <param name="sourceFilePath">(automatically added) source code trace information</param>
        /// <param name="sourceLineNumber">(automatically added) source code trace information</param>
        public static void Info(
			string msg, 
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0
		)
		{
			WriteLogEntry(LogType.info, msg, PathUtil(sourceFilePath, sourceLineNumber, memberName));
		}


		/// <summary>
		/// Use for registering non fatal errors.
		/// Not displayed on screen.
		/// </summary>
		/// <param name="msg">the message</param>
		/// <param name="memberName">(automatically added) source code trace information</param>
		/// <param name="sourceFilePath">(automatically added) source code trace information</param>
		/// <param name="sourceLineNumber">(automatically added) source code trace information</param>
		public static void Error(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0
		)
		{
			WriteLogEntry(LogType.error, msg, PathUtil(sourceFilePath, sourceLineNumber, memberName));
		}


		/// <summary>
		/// Use for registering non fatal errors in form of exceptions.
		/// Not displayed on screen.
		/// </summary>
		/// <param name="e">The exception that signalled the error</param>
		/// <param name="additionalMsg">an optional message</param>
		/// <param name="memberName">(automatically added) source code trace information</param>
		/// <param name="sourceFilePath">(automatically added) source code trace information</param>
		/// <param name="sourceLineNumber">(automatically added) source code trace information</param>
		public static void Error(
			Exception e, 
			string additionalMsg="Exception", 
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0
		)
		{
			Error(additionalMsg + "\n" + e, memberName, sourceFilePath, sourceLineNumber);
		}


		/// <summary>
		/// Use for displaying fatal errors. 
		/// The application is supposed to quit soon after this is called.
		/// This message will be displayed on screen.
		/// </summary>
		/// <param name="msg">the message</param>
		/// <param name="memberName">(automatically added) source code trace information</param>
		/// <param name="sourceFilePath">(automatically added) source code trace information</param>
		/// <param name="sourceLineNumber">(automatically added) source code trace information</param>
		public static void Fatal(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0
		)
		{
			WriteLogEntry(LogType.error, msg, PathUtil(sourceFilePath, sourceLineNumber, memberName));
			// System.Windows.MessageBox.Show(msg, "Fatal error");
		}


		/// <summary>
		/// Use for displaying fatal errors in form of exceptions. 
		/// The application is supposed to quit soon after this is called.
		/// This message will be displayed on screen.
		/// </summary>
		/// <param name="e">The exception that signalled the error</param>
		/// <param name="additionalMsg">an additional message</param>
		/// <param name="memberName">(automatically added) source code trace information</param>
		/// <param name="sourceFilePath">(automatically added) source code trace information</param>
		/// <param name="sourceLineNumber">(automatically added) source code trace information</param>
		public static void Fatal(
			Exception e,
			string additionalMsg = "",
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0
		)
		{
			Fatal((additionalMsg + "\n").Trim() + e, memberName, sourceFilePath, sourceLineNumber);
		}


		internal static ConcurrentDictionary<string, object> published = new ConcurrentDictionary<string, object>();
		public static void Publish(string key, object value) {
			published[key] = value;
		}


		static Logger()
		{
			RegisterListener(new TraceLogListener());
		}

	}
}
