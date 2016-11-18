using Bivrost;
using Bivrost.Log;
using Caliburn.Micro;
using System.Threading.Tasks;
using System;
using PlayerUI.Licensing;
using System.Windows;

namespace PlayerUI
{
	public class LicenseManagementViewModel : Screen
	{

		public bool IsValid { get; private set; } = false;


		public LicenseManagementViewModel(bool changeKey)
		{
			DisplayName = "Enter valid license key";
			string currentLicense = Logic.Instance.settings.LicenseCode;
			if (changeKey)
				OpenLicenceChange(LicenceChangeReason.explicitChange, currentLicense);	// opens dialog to change license
			else
				LicenseVerify(currentLicense);	// tries to verify the license, in the background unless an error occurs
		}



		#region window helpers

		bool dialogIsOpen = false;

		void DialogOpenIfNotOpenedYet()
		{
			if (dialogIsOpen)
				return;
			Execute.OnUIThreadAsync(() => new WindowManager().ShowDialog(this));
			dialogIsOpen = true;
		}

		bool disableCloseLock = false;

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


		public enum LicenceChangeReason
		{
			licenseUnknown,
			licenseEnded,
			explicitChange
		}
		void OpenLicenceChange(LicenceChangeReason reason, string oldLicense)
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
			WindowContent = new LicenseServerUnreachableViewModel(() => ConfirmUseBasicFeatures(), () => LicenseVerify(license));
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

			Logger.Info("License information cleared");

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


		void LicenseStore(string license, object grant)
		{
			// store new license code
			if (Logic.Instance.settings.LicenseCode != license)
			{
				Logger.Info($"New license code: {license}");
				Logic.Instance.settings.LicenseCode = license;
				Logic.Instance.settings.Save();
			}

			// continue to LicenseCommit
			LicenseCommit(grant);
		}


		/// <summary>
		/// Saves the license key (if not saved already), sets the features
		/// and closes the license manager.
		/// </summary>
		/// <param name="license">verified license</param>
		/// <param name="grant">object with features to be granted from licensing server</param>
		void LicenseCommit(object grant)
		{
			if (grant == null)
				Features.SetBasicFeatures();

			// TODO: set features

			DialogCloseIfOpen();
		}


		/// <summary>
		/// Starts a background verification procedure for given key and displays a progress bar
		/// </summary>
		/// <param name="newLicense">the license key to be verified</param>
		private void LicenseVerify(string newLicense)
		{
			// if no license && not required -> end licensesetbasicfeatures
			// check license:
			// if ok -> end licensestore
			// if licensedeny -> end openlicensechange(denied/ended)
			// if connectionerror -> end openlicenseunreachable

			// no license and it's not required
			if(!Features.RequireLicense && string.IsNullOrEmpty(newLicense))
			{
				Logger.Info("No license set nor required, using basic feature set.");
				LicenseSetBasicFeatures();
				return;
			}

			// start license verification in background
			Task.Factory.StartNew(async () =>
			{
				await Task.Delay(5000);


				var settings = Logic.Instance.settings;
				try
				{
					long seconds = await LicenseNinja.Verify(settings.ProductCode, newLicense, settings.InstallId.ToString());
					object grantedFeatures = new object();      // TODO: features
					Logger.Info("License: license verified");
					Execute.OnUIThread(() =>
					{
						LicenseStore(newLicense, grantedFeatures);
					});
				}
				catch (LicenseNinja.TimeEndedException ex)
				{
					Logger.Error(ex, $"License: license timed out");
					Execute.OnUIThread(() => OpenLicenceChange(LicenceChangeReason.licenseEnded, newLicense));
				}
				catch (LicenseNinja.LicenseDeniedException ex)
				{
					Logger.Error(ex, $"License: license denied");
					Execute.OnUIThread(() => OpenLicenceChange(LicenceChangeReason.licenseUnknown, newLicense));
				}
				catch (LicenseNinja.NoLicenseServerConnectionException ex)
				{
					Logger.Error(ex, $"License: no connection to license server");
					Execute.OnUIThread(() => OpenLicenseServerUnreachable(newLicense));
				}
				catch (LicenseNinja.LicenseException ex)
				{
					Logger.Error(ex, $"License: other error");
					Execute.OnUIThread(() => OpenLicenseServerUnreachable(newLicense));
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
			if (disableCloseLock)
			{
				callback(true);
				return;
			}

			if (Features.RequireLicense)
			{
				if (ConfirmQuitBecauseOfLicense())
				{
					ShellViewModel.Instance.Quit();
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


		///// <summary>
		///// Verifies the license at start of the player, in the background.
		///// If an error occurs, and the build requires a license the user is forced to provide a valid code and verify it via License Ninja.
		///// If an error occurs, but the build doesn't require a license, the user is notified of a problem and all the extended features are blocked.
		///// License verification is performed only when the license is required or there is an optional license.
		///// </summary>
		//void LicenseCheckInBackground(string licenseCode, Action<bool, string, object> success)
		//{
		//	var settings = Logic.Instance.settings;

		//	// if the license is required or there is a license provided
		//	if (Features.RequireLicense || !string.IsNullOrEmpty(licenseCode))
		//	{
		//		// start a license verification in background
		//		Task.Factory.StartNew(async () =>
		//		{
		//			try
		//			{
		//				long seconds = await LicenseNinja.Verify(settings.ProductCode, licenseCode, settings.InstallId.ToString());
		//				Execute.OnUIThread(() => success(true, licenseCode, new object()));
		//			}
		//			catch (LicenseNinja.LicenseDeniedException err) when (!Features.RequireLicense)
		//			{
		//				Logger.Error(err, $"License: license denied, but the license is not required.");
		//				Logic.Notify("Your license was denied. Standard features will still work.");
		//			}
		//			catch (LicenseNinja.LicenseException err) when (!Features.RequireLicense)
		//			{
		//				Logger.Error(err, $"License: license check failed, but the license is not required.");
		//				Logic.Notify("License server unreachable. Standard features will still work.");
		//			}
		//			catch (LicenseNinja.LicenseException err) when (Features.RequireLicense)
		//			{
		//				Logger.Error(err, $"License: license check failed");

		//				// reset license info only if the license was denied (not when there is an issue with transmission)
		//				if (err is LicenseNinja.LicenseDeniedException)
		//				{
		//					Logic.Notify("Your license was denied.");
		//				}
		//				else
		//				{
		//					Logic.Notify("There was a problem connecting to the license server.");
		//				}
		//			}
		//		});

		//		Execute.OnUIThread(() => success(false, licenseCode, new object()));
		//	}
		//	else
		//	{
		//		// No license and none is required
		//		Execute.OnUIThread(() => success(true, licenseCode, new object()));
		//	}
		//}


		public static void OpenLicenseManagement()
		{
			new LicenseManagementViewModel(true);
		}


		public static void LicenseCheck()
		{
			new LicenseManagementViewModel(false);
		}

	}
}
