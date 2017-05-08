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

		public void Write(Logger.LogElement entry)
		{
			if (messageOnly)
			{
				Trace.WriteLine(entry.msg);
			}
			else
			{
				Trace.WriteLine($"{entry.time} at {entry.path} {entry.type.ToString()}");
				Trace.Indent();
				Trace.WriteLine(entry.msg);
				Trace.Unindent();
				Trace.WriteLine("");
			}
			Trace.Flush();
		}
	}
}
