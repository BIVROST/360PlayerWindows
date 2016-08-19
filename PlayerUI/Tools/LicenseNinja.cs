using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Logger = Bivrost.Log.Logger;

namespace Bivrost
{
	public class LicenseNinja
	{
		/**
		 * protocol details:
		 * 
		 * [APP]    [SRV]
		 *   |        |
		 *   | -----> | token=random:int, udid:string, product:string, hash=sha1(token+product+udid+key), lang:string
		 *   |        |   (format: http post args)
		 *   |        |
		 *   | <- - - | hash=sha1(time+token+product+key), time:int 
		 *   |        |   (format: http return: "OK|hash|time" or "DENY|human readable message" or "ERROR|technical message")
		 *   |        x
		 *   |
		 *   x          [time end]
		 */


		// Address of your web server containing the license server
		const string licenseURI = "https://tools.bivrost360.com/license-ninja/";


		/// <summary>
		/// Verifies the license
		/// </summary>
		/// <param name="product">The name of your product, not displayed anywhere.</param>
		/// <param name="key">Your secret key, the same key must be used on the server.</param>
		/// <param name="udid">Unique installation id</param>
		/// <param name="lang">Optional language for logs</param>
		/// <returns>how much seconds until license expires?</returns>
		public static async Task<long> Verify(string product, string key, string udid, string lang = "?")
		{
			string token = Guid.NewGuid().ToString();
			string hash = SHA1(token + product + udid + key);

			Log("requesting license");
			string www;
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri(licenseURI);
				var result = client.PostAsync(
					licenseURI, 
					new FormUrlEncodedContent(new[] {
						 new KeyValuePair<string, string>("token", token),
						 new KeyValuePair<string, string>("product", product),
						 new KeyValuePair<string, string>("hash", hash),
						 new KeyValuePair<string, string>("udid", udid),
						 new KeyValuePair<string, string>("lang", lang),

					})).Result;
				www = await result.Content.ReadAsStringAsync();
				if(!result.IsSuccessStatusCode)
					throw new NoLicenseServerConnectionException("network error");
				Log(www);
			}

			try
			{
				var split = www.Split(new char[] { '|' }, 3);
				string response = split[0];
				switch (response)
				{
					case "OK":  // OK|hash|time
						string hashReceived = split[1];
						int time = int.Parse(split[2]);
						string hashSource = time.ToString() + token.ToString() + product + key;
						string hashLocal = SHA1(hashSource);

						if (hashReceived != hashLocal)
							throw new ProtocolErrorException("hash fail: " + hashReceived + " vs " + hashLocal);

						if (time > 0)
							return time;

						throw new TimeEndedException();

					case "DENY":   // DENY|human readable denial message
						throw new LicenseDeniedException(split[1]);

					case "ERROR":  // ERROR|technical message
						throw new ProtocolErrorException(split[1]);

					default:
						throw new ProtocolErrorException("unknown response: " + response);
				}
			}
			catch (LicenseException e) 
			{
				throw;
			}
			catch(Exception e)
			{
				throw new ProtocolErrorException(e.Message);
			}
		}


		/// <summary>
		/// Equivalent of the PHP sha1 function
		/// http://stackoverflow.com/a/5103871/785171
		/// </summary>
		/// <returns>sha1 hash of the string</returns>
		/// <param name="str">string to be hashed</param>
		static string SHA1(string dataString)
		{
			SHA1 hash = SHA1CryptoServiceProvider.Create();
			byte[] plainTextBytes = Encoding.ASCII.GetBytes(dataString);
			byte[] hashBytes = hash.ComputeHash(plainTextBytes);
			string localChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
			return localChecksum;
		}


		static protected void Log(string msg)
		{
			Bivrost.Log.Logger.Info("[License] " + msg);
		}


		#region exceptions

		public abstract class LicenseException : Exception
		{
			public LicenseException(string message) : base(message) { }
		}

		private class NoLicenseServerConnectionException : LicenseException
		{
			public NoLicenseServerConnectionException(string message) : base(message) { }
		}
		private class ProtocolErrorException : LicenseException
		{
			public ProtocolErrorException(string message) : base(message) { }
		}
		private class LicenseDeniedException : LicenseException
		{
			public LicenseDeniedException(string message) : base(message) { }
		}
		private class TimeEndedException : LicenseDeniedException
		{
			public TimeEndedException() : base("license time ended") { }
		}

		#endregion

	}

}
