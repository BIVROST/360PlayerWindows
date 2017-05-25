using Bivrost.Log;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.LicenseNinja
{
	public class LicenseNinja
	{
		public static Logger log = new Logger("LicenseNinja");

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
		 *   
		 * version 2:
		 * [APP]    [SRV]
		 *   |        |
		 *   | -----> | token=random:int, udid:string, product:string, hash=sha1(token+product+udid+key), lang:string, version=2
		 *   |        |   (format: http post args)
		 *   |        |
		 *   | <- - - | hash=sha1(time+token+product+grant+key), time:int 
		 *   |        |   (format: http return: "OK|hash|time|grant" or "DENY|human readable message" or "ERROR|technical message")
		 *   |        x
		 *   |
		 *   x          [time end]
		 */


		// Address of your web server containing the license server
		const string licenseURI = "https://tools.bivrost360.com/license-ninja/";


		public class License
		{
			/// <summary>
			/// How much seconds until license expires?
			/// </summary>
			public readonly long time;

			/// <summary>
			/// String with serialized features or message
			/// </summary>
			public readonly string grant;


			private Dictionary<string, string> _grantAsDictionary = null;

			public License(long time, string grant)
			{
				this.time = time;
				this.grant = grant;
			}

			/// <summary>
			/// Retrieves the grant license as a string->string dictionary.
			/// Requires format of grant message:
			///   key[=val][,key[=val]]...
			/// If value is not given (no =val part) it is null, but the key is in the dictionary
			/// </summary>
			public Dictionary<string, string> GrantAsDictionary
			{
				get
				{
					if (_grantAsDictionary == null)
					{
						_grantAsDictionary = new Dictionary<string, string>();
						if (!string.IsNullOrWhiteSpace(grant))
						{
							foreach (string opt in grant.Split(new char[] { ',' }))
							{
								string[] keyval = opt.Split(new char[] { '=' }, 2);
								string key = keyval[0];
								string val = keyval.Length > 1 ? keyval[1] : null;
								_grantAsDictionary.Add(key.Trim().ToLowerInvariant(), val?.Trim()?.ToLowerInvariant());
							}
						}
					}
					return _grantAsDictionary;
				}
			}

		}


		/// <summary>
		/// Verifies the license, supports LicenseNinja v1 and v2
		/// </summary>
		/// <param name="product">The name of your product, not displayed anywhere.</param>
		/// <param name="key">Your secret key, the same key must be used on the server.</param>
		/// <param name="udid">Unique installation id</param>
		/// <param name="lang">Optional language for logs</param>
		/// <returns>License details</returns>
		public static async Task<License> Verify(string product, string key, string udid, string lang = "?")
		{
			string token = Guid.NewGuid().ToString();
			string hash = SHA1(token + product + udid + key);

			log.Info("Requesting license");
			string www;
			try
			{
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
							 new KeyValuePair<string, string>("version", "2")
						})).Result;
					www = await result.Content.ReadAsStringAsync();
					if (!result.IsSuccessStatusCode)
						throw new NoLicenseServerConnectionException("bad status code");
					log.Info($"Received {www}");
				}
			}
			catch(Exception e)
			{
				throw new NoLicenseServerConnectionException($"network error: {e.Message}");
			}

			try
			{
				var split = www.Split(new char[] { '|' }, 4);
				string response = split[0];
				switch (response)
				{
					// version 1: OK|hash|time
					// version 2: OK|hash|time|grant
					case "OK":  
						int version = 1;

						// version 1 or 2:
						string hashReceived = split[1];
						long time = long.Parse(split[2]);
						string hashSource = $"{time}{token}{product}{key}";

						// version 2, with grant object:
						string grant = null;
						if (www.Split(new char[] { '|' }).Length == 4)
						{
							version = 2;
							grant = www.Split(new char[] { '|' }, 4)[3];
							hashSource = $"{time}{token}{product}{grant}{key}";
						}

						log.Info($"Version {version}");

						string hashLocal = SHA1(hashSource);
						if (hashReceived != hashLocal)
							throw new ProtocolErrorException("hash fail: " + hashReceived + " vs " + hashLocal);

						if (time > 0)
							return new License(time, grant);

						throw new TimeEndedException();

					case "DENY":   // DENY|human readable denial message
						throw new LicenseDeniedException(split[1]);

					case "ERROR":  // ERROR|technical message
						throw new ProtocolErrorException(split[1]);

					default:
						throw new ProtocolErrorException("unknown response: " + response);
				}
			}
			catch(Exception e) when (!(e is LicenseException))
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


		#region exceptions

		public abstract class LicenseException : Exception
		{
			public LicenseException(string message) : base(message) { }
		}

		public class NoLicenseServerConnectionException : LicenseException
		{
			public NoLicenseServerConnectionException(string message) : base(message) { }
		}
		public class ProtocolErrorException : LicenseException
		{
			public ProtocolErrorException(string message) : base(message) { }
		}
		public class LicenseDeniedException : LicenseException
		{
			public LicenseDeniedException(string message) : base(message) { }
		}
		public class TimeEndedException : LicenseDeniedException
		{
			public TimeEndedException() : base("license time ended") { }
		}

		#endregion

	}

}
