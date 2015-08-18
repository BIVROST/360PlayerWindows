using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BivrostInstaller
{
	public class CustomInstaller
	{
		InPlaceHostingManager _iphm;

		/// <summary>
		/// Installs a ClickOnce application
		/// </summary>
		/// <param name="deployManifestUriStr"></param>
		public void InstallApplication(string deployManifestUriStr)
		{
			try
			{
				var deploymentUri = new Uri(deployManifestUriStr);
				_iphm = new InPlaceHostingManager(deploymentUri, false);
			}
			catch (UriFormatException uriEx)
			{
				MessageBox.Show("Cannot install the application: " + "The deployment manifest URL supplied is not a valid URL. " + "Error: " + uriEx.Message);
				return;
			}
			catch (PlatformNotSupportedException platformEx)
			{
				MessageBox.Show("Cannot install the application: " + "This program requires Windows XP or higher. " + "Error: " + platformEx.Message);
				return;
			}
			catch (ArgumentException argumentEx)
			{
				MessageBox.Show("Cannot install the application: " + "The deployment manifest URL supplied is not a valid URL. " + "Error: " + argumentEx.Message);
				return;
			}

			_iphm.GetManifestCompleted += iphm_GetManifestCompleted;
			_iphm.GetManifestAsync();
		}


		/// <summary>
		/// Occurs when the deployment manifest has been downloaded to the local computer.
		/// </summary>
		void iphm_GetManifestCompleted(object sender, GetManifestCompletedEventArgs e)
		{
			// Check for an error.
			if (e.Error != null)
			{
				// Cancel download and install.
				MessageBox.Show("Could not download manifest. Error: " + e.Error.Message);
				return;
			}

			// Verify this application can be installed.
			try
			{
				// The true parameter allows InPlaceHostingManager to grant the permissions requested in the applicaiton manifest.
				_iphm.AssertApplicationRequirements(true);
			}
			catch (Exception ex)
			{
				MessageBox.Show("An error occurred while verifying the application. " + "Error: " + ex.Message);
				return;
			}

			// Present application information to the user.
			//((Form1)Application.OpenForms[0]).label1.Text = String.Format("Application Name:    {0}\nApplication Version: {1}", e.ProductName, e.Version);

			// Check if application already installed
			if (e.ActivationContext.Form == ActivationContext.ContextForm.StoreBounded)
			{
				//((Form1)Application.OpenForms[0]).label1.Text += "\n\nApplication is already installed in this machine.";
				return;
			}

			// Download the deployment manifest. 
			_iphm.DownloadProgressChanged += iphm_DownloadProgressChanged;
			_iphm.DownloadApplicationCompleted += iphm_DownloadApplicationCompleted;

			try
			{
				// Usually this shouldn't throw an exception unless AssertApplicationRequirements() failed, or you did not call that method before calling this one.
				_iphm.DownloadApplicationAsync();
			}
			catch (Exception downloadEx)
			{
				MessageBox.Show("Cannot initiate download of application. Error: " + downloadEx.Message);
				return;
			}
		}


		/// <summary>
		/// Occurs when the application has finished downloading to the local computer.
		/// </summary>
		void iphm_DownloadApplicationCompleted(object sender, DownloadApplicationCompletedEventArgs e)
		{
			// Check for an error.
			if (e.Error != null)
			{
				// Cancel download and install.
				MessageBox.Show("Could not download and install application. Error: " + e.Error.Message);
				return;
			}

			// Inform the user that the application has been installed. 
			//((Form1)Application.OpenForms[0]).label1.Text += "\n\nApplication installed!\nYou may now run it from the Start menu.";
		}


		/// <summary>
		/// Occurs when there is a change in the status of an application or manifest download.
		/// </summary>
		void iphm_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			// Show % of task completed 
			//((Form1)Application.OpenForms[0]).progressBar1.Value = e.ProgressPercentage;
		}


	}
}
