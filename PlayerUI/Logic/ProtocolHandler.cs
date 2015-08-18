using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class ProtocolHandler
	{
		private const string PROTOCOL_PREFIX = "bivrost";

		public static void RegisterProtocol()
		{
			Task.Factory.StartNew(() => {
				CreateClickOnceShortcut();
				//string Timestamp = DateTime.Now.ToString("dd-MM-yyyy");

				//string key = @"HKEY_CURRENT_USER\Software\Classes\bivrost";
    //            string valueName = "Trial Period";

				//RegistryKey subkey = Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{PROTOCOL_PREFIX}\\shell\\open\\command", true);
				//if(subkey == null)
				//{
				//	subkey = Registry.CurrentUser.CreateSubKey($"Software\\Classes\\{PROTOCOL_PREFIX}\\shell\\open\\command");
				//	//subkey.SetValue("", )

				//} else
				//{
				//	var value = subkey.GetValue("");
				//}
				
				

				//HKEY_CURRENT_USER\Software\Classes
				//Microsoft.Win32.Registry.SetValue(key, valueName, Timestamp, Microsoft.Win32.RegistryValueKind.String);


			});
		}

		public static void CreateClickOnceShortcut()
		{
			byte[] token = System.Reflection.Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken();
			byte[] key = System.Reflection.Assembly.GetExecutingAssembly().GetName().GetPublicKey();
            token = (new SHA1CryptoServiceProvider()).ComputeHash(new byte[0]).Take(8).ToArray();
            File.WriteAllText("publictoken", ByteArrayToString(token));
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
