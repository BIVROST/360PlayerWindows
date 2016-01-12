using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ChangePublishConfig
{
	class Program
	{
		/// <summary>
		/// This function will set the minimum operating system version for a ClickOnce application.
		/// </summary>
		/// <param name="args">
		/// Command Line Arguments:
		/// 0 - Path to application manifest (.exe.manifest).
		/// 1 - Version of OS
		///</param>
		static void Main(string[] args)
		{
			string applicationManifestPath = args[0];
			Console.WriteLine("Application Manifest Path: " + applicationManifestPath);

			// Get version name.
			Version osVersion = null;
			if (args.Length >= 2)
			{
				osVersion = new Version(args[1]);
			}
			else {
				throw new ArgumentException("OS Version not specified.");
			}
			Console.WriteLine("Desired OS Version: " + osVersion.ToString());

			XmlDocument document;
			XmlNamespaceManager namespaceManager;
			namespaceManager = new XmlNamespaceManager(new NameTable());
			namespaceManager.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
			namespaceManager.AddNamespace("asmv2", "urn:schemas-microsoft-com:asm.v2");

			document = new XmlDocument();
			document.Load(applicationManifestPath);

			string baseXPath;
			baseXPath = "/asmv1:assembly/asmv2:dependency/asmv2:dependentOS/asmv2:osVersionInfo/asmv2:os";

			// Change minimum required operating system version.
			XmlNode node;
			node = document.SelectSingleNode(baseXPath, namespaceManager);
			node.Attributes["majorVersion"].Value = osVersion.Major.ToString();
			node.Attributes["minorVersion"].Value = osVersion.Minor.ToString();
			node.Attributes["buildNumber"].Value = osVersion.Build.ToString();
			node.Attributes["servicePackMajor"].Value = osVersion.Revision.ToString();

			document.Save(applicationManifestPath);
		}
	}
}
