using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player
{
	public class ProtocolHandler
	{
		private const string PROTOCOL_PREFIX = "bivrost";

		public static void RegisterProtocol()
		{
			Task.Factory.StartNew(() => {
				CreateClickOnceShortcut();
			});
		}

		public static void CreateClickOnceShortcut()
		{
			byte[] token = System.Reflection.Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken();
			byte[] key = System.Reflection.Assembly.GetExecutingAssembly().GetName().GetPublicKey();
            token = (new SHA1CryptoServiceProvider()).ComputeHash(new byte[0]).Take(8).ToArray();
            File.WriteAllText(Logic.LocalDataDirectory + Path.DirectorySeparatorChar + "publictoken", ByteArrayToString(token));
		}

		public static string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}
	}
}
