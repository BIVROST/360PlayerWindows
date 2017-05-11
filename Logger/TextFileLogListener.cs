using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Bivrost.Log
{
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
			if (version == null)
#if DEBUG
				version = "DEBUG";
#else
				version = "v" + Assembly.GetEntryAssembly().GetName().Version.ToString();
#endif

			LogFile = logDirectory + string.Format("{2}-{0}-{1}.txt", version, now, logPrefix);

			Logger.Info("Log file: " + LogFile);
			fp = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			encoding = new UTF8Encoding(false);
		}


		public void Write(Logger.LogElement entry)
		{
			byte[] buf = encoding.GetBytes($"[{entry.type}] {entry.time} at {entry.path}\r\n\r\n{entry.msg.Trim().Replace("\r\n", "\r\n\t")}\r\n");
			lock (LogFile)
			{
				fp.Write(buf, 0, buf.Length);
				fp.Flush();
			}
		}

	}
}
