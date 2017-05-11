using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bivrost.Log
{

	public interface LogListener
	{
		void Write(Logger.LogElement entry);
	}


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


		public struct LogElement { public string time; public LogType type; public string msg; public string path; }
        static BlockingCollection<LogElement> logElementQueue = new BlockingCollection<LogElement>();
		static List<LogElement> history = new List<LogElement>();


        static void WriteLogEntry(LogType type, string msg, string path)
		{
			string now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

			// normalize newlines to windows format
            if(msg != null)
			    msg = msg.Replace("\r\n", "\n").Replace("\n", "\r\n");

			var logElement = new LogElement() { time = now, type = type, msg = msg, path = path };
			logElementQueue.Add(logElement);
			lock (history)
			{
				history.Add(logElement);
				while (history.Count > 50)
					history.RemoveAt(0);
			}
		}


		static void WriteLogThread()
		{
			while (true)
			{
                LogElement e = logElementQueue.Take();				
                lock(listeners)
				    foreach (var l in listeners)
					    l.Write(e);
			}
		}


		internal static HashSet<LogListener> listeners = new HashSet<LogListener>();


		private static Thread thread;


		public static void RegisterListener(LogListener lw)
		{
			lock(listeners)
				listeners.Add(lw);

			if (thread == null)
			{
				thread = new Thread(new ThreadStart(WriteLogThread))
				{
					IsBackground = true,
					Name = "log listener thread"
				};
				thread.Start();
			}

			foreach (var entry in history)
				lw.Write(entry);

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
                Info("Unregistered " + listeners.RemoveWhere(predicate) + " log writer");
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


		public static void Unpublish(string key)
		{
			object _unused;
			published.TryRemove(key, out _unused);
		}

	}
}
