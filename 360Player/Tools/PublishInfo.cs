using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.Tools
{
	/// <summary>
	/// Helper class to extract identity information based on domain identity
	/// </summary>
	public static class PublishInfo
	{

		/// <summary>
		/// Types of identities
		/// </summary>
		public enum IdentityType
		{
			/// <summary>
			/// Deployment identity
			/// </summary>
			Deployment = 1,
			/// <summary>
			/// Application identity
			/// </summary>
			Application = 2
		}

		// Regular expressions
		private static System.Text.RegularExpressions.Regex _versionRegex =
		  new System.Text.RegularExpressions.Regex(@"Version=(?<Major>\d*)." +
		  @"(?<Minor>\d*).(?<Build>\d*).(?<Revision>\d*)",
		  System.Text.RegularExpressions.RegexOptions.Compiled);
		private static System.Text.RegularExpressions.Regex _cultureRegex =
		  new System.Text.RegularExpressions.Regex(@", Culture=(?<Culture>[^,]*),",
		  System.Text.RegularExpressions.RegexOptions.Compiled);
		private static System.Text.RegularExpressions.Regex _publicKeyTokenRegex =
		  new System.Text.RegularExpressions.Regex(@", PublicKeyToken=(?<PublicKeyToken>[^,]*),",
		  System.Text.RegularExpressions.RegexOptions.Compiled);
		private static System.Text.RegularExpressions.Regex _processorArchitectureRegex =
		  new System.Text.RegularExpressions.Regex(@", processorArchitecture=(?<ProcessorArchitecture>[^,]*)",
		  System.Text.RegularExpressions.RegexOptions.Compiled);

		/// <summary>
		/// Main uri
		/// </summary>
		public static System.Uri Uri { get; private set; }

		/// <summary>
		/// Deployment identity
		/// </summary>
		public static Identity DeploymentIdentity { get; private set; }

		/// <summary>
		/// Application identity
		/// </summary>
		public static Identity ApplicationIdentity { get; private set; }

		/// <summary>
		/// Class that holds the identity information
		/// </summary>
		public class Identity
		{

			/// <summary>
			/// Type of the identity
			/// </summary>
			public PublishInfo.IdentityType IdentityType { get; private set; }

			/// <summary>
			/// Version information
			/// </summary>
			public System.Version Version { get; private set; }

			/// <summary>
			/// Name of the application
			/// </summary>
			public string ApplicationName { get; private set; }

			/// <summary>
			/// Public key token
			/// </summary>
			public string PublicKeyToken { get; private set; }

			/// <summary>
			/// Processor architecture
			/// </summary>
			public System.Reflection.ProcessorArchitecture ProcessorArchitecture { get; private set; }

			/// <summary>
			/// Default constructor
			/// </summary>
			private Identity() { }

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="identity">Identity string to parse</param>
			/// <param name="identityType">Type of the identity</param>
			internal Identity(string identity, PublishInfo.IdentityType identityType)
			{
				System.Text.RegularExpressions.Match regexMatch;
				System.Reflection.ProcessorArchitecture architecture;

				this.IdentityType = identityType;

				try
				{
					// Parse application name
					this.ApplicationName = identity.Substring(0, identity.IndexOf(','));

					// Parse version
					regexMatch = _versionRegex.Match(identity);
					this.Version = new System.Version(int.Parse(regexMatch.Groups["Major"].ToString()),
													 int.Parse(regexMatch.Groups["Minor"].ToString()),
													 int.Parse(regexMatch.Groups["Build"].ToString()),
													 int.Parse(regexMatch.Groups["Revision"].ToString()));

					// Parse public key token
					regexMatch = _publicKeyTokenRegex.Match(identity);
					this.PublicKeyToken = regexMatch.Groups["PublicKeyToken"].ToString();

					// Parse processor architecture
					regexMatch = _processorArchitectureRegex.Match(identity);
					if (!System.Enum.TryParse<System.Reflection.ProcessorArchitecture>(
							regexMatch.Groups["ProcessorArchitecture"].ToString(), true, out architecture))
					{
						architecture = System.Reflection.ProcessorArchitecture.None;
					}
					this.ProcessorArchitecture = architecture;
				}
				catch { }
			}
		}

		/// <summary>
		/// Constructor for the PublishInfo class
		/// </summary>
		static PublishInfo()
		{
			string identities;
			string[] identity;

			try
			{
				// Get the full name of the identity
				identities = System.AppDomain.CurrentDomain.ApplicationIdentity.FullName;

				// Parse uri
				PublishInfo.Uri = new System.Uri(identities.Substring(0, identities.IndexOf('#')));
				identities = identities.Substring(identities.IndexOf('#') + 1);

				//Split the separate identities
				if (identities.IndexOf("\\") > -1)
				{
					identity = identities.Split('\\');
				}
				else {
					identity = identities.Split('/');
				}

				//Create the identity information
				PublishInfo.DeploymentIdentity = new Identity(identity[0], IdentityType.Deployment);
				PublishInfo.ApplicationIdentity = new Identity(identity[1], IdentityType.Application);
			}
			catch { }
		}
	}
}
