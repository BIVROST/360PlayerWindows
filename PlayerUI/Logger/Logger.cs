using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bivrost.Log
{

	public interface LogWriter
	{
		void Write(string time, LogType type, string msg, string path);
	}

	#region writers
	/// <summary>
	/// Windows Event Log Writer. This requires admin rights.
	/// </summary>
	public class WindowsEventLogWriter : LogWriter
	{

		string sSource;
		string sLog;

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


		public WindowsEventLogWriter(string appname, string logname)
		{
			sSource = appname;
			sLog = logname;
		}


		public void Write(string time, LogType type, string msg, string path)
		{
			if (!EventLog.SourceExists(sSource))
				EventLog.CreateEventSource(sSource, sLog);

			EventLog.WriteEntry(sSource, type + ": " + msg, ToEventLogEntryType(type));
		}

	}


	/// <summary>
	/// Writing to the console
	/// </summary>
	public class TraceLogWriter : LogWriter
	{
		public void Write(string time, LogType type, string msg, string path)
		{
			Trace.WriteLine(time, type.ToString());
			Trace.Indent();
			Trace.WriteLine(msg);
			Trace.WriteLine("");
			Trace.WriteLine("at " + path);
			Trace.Unindent();
			Trace.Flush();
		}
	}


	/// <summary>
	/// Writing to a text file
	/// </summary>
	public class TextFileLogWriter : LogWriter
	{

		public string LogFile { get; protected set; }


		public TextFileLogWriter(string logDirectory, string logPrefix = "log", string version = null)
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
		}


		public void Write(string time, LogType type, string msg, string path)
		{
			lock (LogFile)
			{
				try
				{
					File.AppendAllText(
						LogFile,
						string.Format(
							"[{0}] {1}\r\n\t{2}\r\n\r\nat {3}\r\n",
							type,
							time,
							msg.Trim().Replace("\r\n", "\r\n\t"),
							path
						)
					);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Error writing to text log: " + e);
				}
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


		static void WriteLogEntry(LogType type, string msg, string path)
		{
			string now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

			// normalize newlines to windows format
			msg = msg.Replace("\r\n", "\n").Replace("\n", "\r\n");

			foreach (var lw in logWriters)
				lw.Write(now, type, msg, path);
		}


		static HashSet<LogWriter> logWriters = new HashSet<LogWriter>();
		

		public static LogWriter[] LogWriters
		{
			get {
				var r = new LogWriter[logWriters.Count];
				logWriters.CopyTo(r);
				return r;
			}
		}

		public static void RegisterWriter(LogWriter lw)
		{
			logWriters.Add(lw);
			Info("Registered log writer: " + lw);
		}


		public static void UnregisterWriter(LogWriter lw)
		{
			logWriters.Remove(lw);
			Info("Unregistered log writer: " + lw);
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
			System.Windows.MessageBox.Show(msg, "Fatal error");
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


		static Logger()
		{
			RegisterWriter(new TraceLogWriter());
		}

	}
}
