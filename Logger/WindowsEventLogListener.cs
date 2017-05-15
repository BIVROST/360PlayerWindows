using System;
using System.Diagnostics;

namespace Bivrost.Log
{
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


		public void Write(LoggerManager.LogElement entry)
		{
			EventLog.WriteEntry(sSource, $"{entry.Type} {entry.Tag}: {entry.Message}", ToEventLogEntryType(entry.Type));
		}

	}

}
