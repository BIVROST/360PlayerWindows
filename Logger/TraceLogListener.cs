using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bivrost.Log
{
	/// <summary>
	/// Writing to the console
	/// </summary>
	public class TraceLogListener : LogListener
	{
		private bool messageOnly;

		public TraceLogListener(bool messageOnly)
		{
			this.messageOnly = messageOnly;
		}

		public void Write(LoggerManager.LogElement entry)
		{
			if (messageOnly)
			{
				Trace.WriteLine(entry.Message);
			}
			else
			{
				Trace.WriteLine($"{entry.Time} at {entry.Path} {entry.Type.ToString()} {entry.Tag}");
				Trace.Indent();
				Trace.WriteLine(entry.Message);
				Trace.Unindent();
				Trace.WriteLine("");
			}
			Trace.Flush();
		}
	}
}
