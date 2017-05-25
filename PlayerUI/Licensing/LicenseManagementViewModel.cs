using Bivrost;
using Bivrost.Log;
using Caliburn.Micro;
using System.Threading.Tasks;
using System;
using PlayerUI.Licensing;
using System.Windows;

namespace PlayerUI
{

	/// <summary>
	/// Window for verifying and changing the license code.
	/// Look at /docs/360player-license.dia for details on how this works
	/// </summary>
	public class LicenseManagementViewModel : Screen
	{


		private Logger log = new Logger("LicenseNinja");

		public LicenseManagementViewModel(bool changeKey, System.Action onCommit)
		{
			if (inLicenseVerification)
			{
				log.Info("Currently waiting for license verification, ignored duplicate request.");
				return;
			}

			DisplayName = "Enter valid license key";
			string currentLicense = Logic.Instance.settings.LicenseCode;
			this.onCommit = onCommit;
			if (changeKey)
			{
				allowClose = true;
				OpenLicenseChange(LicenseChangeReason.explicitChange, currentLicense);  // opens dialog to change license
			}
			else
				LicenseVerify(currentLicense);	// tries to verify the license, in the background unless an error occurs
		}

		private System.Action onCommit;




		#region window helpers

		bool dialogIsOpen = false;

		bool dialogWasOrIsOpen = false;

		void DialogOpenIfNotOpenedYet()
		{
			if (dialogIsOpen)
				return;
			Execute.OnUIThreadAsync(() =>
			{
				try { new WindowManager().ShowDialog(this); }
				catch (NullReferenceException) { ; }	// sometimes when application closes, this fires
			});
			dialogIsOpen = true;
			dialogWasOrIsOpen = true;
		}

		bool disableCloseLock = false;

		bool allowClose = false;

		void DialogCloseIfOpen()
		{
			if (!dialogIsOpen)
				return;
			disableCloseLock = true;
			TryClose(true);
			dialogIsOpen = false;
			disableCloseLock = false;
		}

		private PropertyChangedBase _windowContent;
		public PropertyChangedBase WindowContent
		{
			get { return _windowContent; }
			set { _windowContent = value; NotifyOfPropertyChange(() => WindowContent); }
		}
		#endregion


		public enum LicenseChangeReason
		{
			licenseUnknown,
			licenseEnded,
			explicitChange,
			licenseRequired
		}
		void OpenLicenseChange(LicenseChangeReason reason, string oldLicense)
		{
			WindowContent = new LicenseChangeViewModel(reason, oldLicense, OpenLicenseVerify, LicenseClear);
			DialogOpenIfNotOpenedYet();
		}


		void OpenLicenseVerify(string license)
		{
			WindowContent = new LicenseVerificationViewModel();

			LicenseVerify(license);
			DialogOpenIfNotOpenedYet();
		}


		void OpenLicenseServerUnreachable(string license)
		{
			WindowContent = new LicenseServerUnreachableViewModel(
				() =>
				{
					if (ConfirmUseBasicFeatures())
						LicenseSetBasicFeatures();
				},
				() => LicenseVerify(license)
			);
			DialogOpenIfNotOpenedYet();
		}


		/// <summary>
		/// Clear license information
		/// Continues to LicenseSetBasicFeatures
		/// </summary>
		void LicenseClear()
		{
			Logic.Instance.settings.LicenseCode = null;
			Logic.Instance.settings.Save();

			log.Info("License information cleared");

			LicenseSetBasicFeatures();
		}


		/// <summary>
		/// Sets basic license features
		/// Continues to LicenseCommit
		/// </summary>
		void LicenseSetBasicFeatures()
		{
			LicenseCommit(null);
		}


		void LicenseStore(string licenseCode, LicenseNinja.License license)
		{
			// store new license code
			if (Logic.Instance.settings.LicenseCode != licenseCode)
			{
				log.Info($"New license code: {licenseCode}, license: {license}");
				Logic.Instance.settings.LicenseCode = licenseCode;
				Logic.Instance.settings.Save();
			}

			// continue to LicenseCommit
			LicenseCommit(license);
		}


		/// <summary>
		/// Saves the license key (if not saved already), sets the features
		/// and closes the license manager.
		/// </summary>
		/// <param name="license">object with features to be granted from licensing server</param>
		void LicenseCommit(LicenseNinja.License license)
		{
			if (license == null && Features.RequireLicense)
			{
				throw new Exception("Set basic features cannot work with a required license");
			}
			else if (license == null || license.grant == null)
			{
				Features.SetBasicFeatures();
			}
			else
			{
				Features.SetFromLicense(license);
			}

			DialogCloseIfOpen();

			onCommit?.Invoke();

			Features.TriggerListUpdated();
		}


		private bool inLicenseVerification = false;


		/// <summary>
		/// Starts a background verification procedure for given key and displays a progress bar
		///		if no license && not required -> end licensesetbasicfeatures
		///		check license:
		///		if ok -> end licensestore
		///		if licensedeny -> end openlicensechange(denied/ended)
		///		if connectionerror -> end openlicenseunreachable
		/// </summary>
		/// <param name="newLicense">the license key to be verified</param>
		private void LicenseVerify(string newLicense)
		{
			// no license and it's not required
			if(!Features.RequireLicense && string.IsNullOrEmpty(newLicense))
			{
				log.Info("No license set nor required, using basic feature set.");
				LicenseSetBasicFeatures();
				return;
			}

			// start license verification in background
			Task.Factory.StartNew(async () =>
			{
				await Task.Delay(500);	// fake delay so it looks like more wo

				try
				{
					var settings = Logic.Instance.settings;
					inLicenseVerification = true;
					LicenseNinja.License license = await LicenseNinja.Verify(Logic.productCode, newLicense, settings.InstallId.ToString());
					log.Info("License verified");
					if(dialogWasOrIsOpen)
						Logic.Notify("License verified"); 
					Execute.OnUIThread(() =>
					{
						LicenseStore(newLicense, license);
					});
				}
				catch (LicenseNinja.TimeEndedException ex)
				{
					log.Error(ex, $"License timed out");
					Execute.OnUIThread(() => OpenLicenseChange(LicenseChangeReason.licenseEnded, newLicense));
				}
				catch (LicenseNinja.LicenseDeniedException ex)
				{
					log.Error(ex, $"License denied");
					var reason = string.IsNullOrEmpty(newLicense)
						?LicenseChangeReason.licenseRequired
						:LicenseChangeReason.licenseUnknown;
					Execute.OnUIThread(() => OpenLicenseChange(reason, newLicense));
				}
				catch (LicenseNinja.NoLicenseServerConnectionException ex)
				{
					log.Error(ex, $"No connection to license server: {ex}");
					Execute.OnUIThread(() => OpenLicenseServerUnreachable(newLicense));
				}
				catch (LicenseNinja.LicenseException ex)
				{
					log.Error(ex, "Other error");
					Execute.OnUIThread(() => OpenLicenseServerUnreachable(newLicense));
				}
				finally
				{
					inLicenseVerification = false;
				}
			});

		}


		#region close button support
		/// <summary>
		/// Asks the user if he wants to set the feature set to the basic level.
		/// Does not execute any additional commands.
		/// Blocking
		/// <returns>true on confirmation</returns>
		/// </summary>
		private bool ConfirmUseBasicFeatures()
		{
			MessageBoxResult messageBoxResult = MessageBox.Show(
				"Without a license you will not have access to some features, including the permission for commecial use.\nPressing OK will use basic features.",
				"Use basic features confirmation",
				MessageBoxButton.OKCancel
			);
			return messageBoxResult == MessageBoxResult.OK;
		}


		private void QuitApplication()
		{
			ShellViewModel.Instance.Quit();
		}


		/// <summary>
		/// Asks the user if he wants to quit
		/// Does not execute any additional commands.
		/// Blocking
		/// </summary>
		/// <returns>true on confirmation</returns>
		private bool ConfirmQuitBecauseOfLicense()
		{
			MessageBoxResult messageBoxResult = MessageBox.Show(
				"You cannot continue without a valid license.\nPressing OK will close 360Player.",
				"No valid license found",
				MessageBoxButton.OKCancel
			);
			return messageBoxResult == MessageBoxResult.OK;
		}


		/// <summary>
		/// When trying to close the licensing window, the user is asked if he wants to continue without a license
		/// or, if a license is required by the build, wants to quit
		/// </summary>
		/// <param name="callback"></param>
		public override void CanClose(Action<bool> callback)
		{
			// when closing is forced, allow it
			if (disableCloseLock || allowClose)
			{
				callback(true);
				return;
			}

			if (Features.RequireLicense)
			{
				if (ConfirmQuitBecauseOfLicense())
				{
					QuitApplication();
					callback(true);
					return;
				}
			}
			else
			{
				if(ConfirmUseBasicFeatures())
				{
					LicenseSetBasicFeatures();
					callback(true);
					return;
				}
			}

			// canceled
			callback(false);
		}
		#endregion


	}
}
