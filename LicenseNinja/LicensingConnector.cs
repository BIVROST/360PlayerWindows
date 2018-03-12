namespace Bivrost.Licensing
{
	/**
	 * Class that provides a public interface to the licensing mechanisms
	 * It requires an context object implementing the IContext interface with
	 * the application specific details.
	 * 
	 * The instance LicensingConnector.Context is used later as the context for view models
	 */
	public class LicensingConnector
    {
		private IContext context;

		public LicensingConnector(IContext context)
		{
			this.context = context;
		}


		/// <summary>
		/// Displays a window allowing for the user to change the current license key.
		/// </summary>
		public void OpenLicenseManagement()
		{
			new LicenseManagementViewModel(context, true);
		}


		/// <summary>
		/// Checks the currently stored license and if needed, requires a new license key from the user.
		/// </summary>
		public void LicenseCheck()
		{
			new LicenseManagementViewModel(context, false);
		}

	
		/// <summary>
		/// Thin wrapper for protected members to be sent as context to the internals of this package.
		/// Is needed so the members aren't exposed to the app side
		/// </summary>
		public interface IContext
		{
			/// <summary>
			/// The license code, a string to be verified by the server.
			/// </summary>
			string LicenseCode { get; set; }


			/// <summary>
			/// Is the license required for the application to run.
			/// If false, the user will be given an option to not provide a license code and use basic features.
			/// </summary>
			bool RequireLicense { get; }


			/// <summary>
			/// A unique identifier of this installation of the application.
			/// </summary>
			string InstallId { get; }


			/// <summary>
			/// The name of this application.
			/// </summary>
			string ProductCode { get; }


			/// <summary>
			/// If the license is required, this is called when the user chooses to
			/// not provide the license key
			/// </summary>
			void QuitApplication();


			/// <summary>
			/// Called when a license has been correctly obtained
			/// OR the license is not required and no license key was provided
			/// OR the license is not required and there has been an error obtaining the license and the user chooses to not continue trying
			/// </summary>
			/// <param name="license">The obtained license details. Will be null if no license was obtained.</param>
			void LicenseUpdated(LicenseNinja.License license);


			/// <summary>
			/// Called as a notification that the currently stored license has been verified to be still good.
			/// </summary>
			void LicenseVerified();
		}

	}
}
