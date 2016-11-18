using PlayerUI.ConfigUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PlayerUI
{
	/// <summary>
	/// Features in enum form, required for serialized attributes in SettingsPropertyAttribute,
	/// shouldn't be used anywhere else.
	/// </summary>
	[Flags]
	public enum FeaturesEnum
	{
		none,
		isDebug = 1,
		isCanary = 2,
		ghostVR = 4,
		heatmaps = 8,
		requireLicense = 16,
		remote = 32
	}

	public static class Features
	{


		public static FeaturesEnum AsEnum
		{
			get
			{
				FeaturesEnum fe = new FeaturesEnum();
				if (IsDebug)
					fe |= FeaturesEnum.isDebug;
				if (IsCanary)
					fe |= FeaturesEnum.isCanary;
				if (GhostVR)
					fe |= FeaturesEnum.ghostVR;
				if (Heatmaps)
					fe |= FeaturesEnum.heatmaps;
				if (RequireLicense)
					fe |= FeaturesEnum.requireLicense;
				if (RemoteEnabled)
					fe |= FeaturesEnum.remote;

				return fe;
			}
		}


		public static bool IsDebug =
#if DEBUG
			true;
#else
			false;
#endif


		/// <summary>
		/// This build is a canary build
		/// </summary>
		public static bool IsCanary =
#if CANARY
			true;
#else
			false;
#endif

		/// <summary>
		/// Online heatmap analytics gathering and sending is enabled
		/// Requires Heatmaps
		/// </summary>
		public static bool GhostVR = IsDebug;

		/// <summary>
		/// Local heatmap analytics gathering is enabled
		/// </summary>
		public static bool Heatmaps = IsDebug;

		/// <summary>
		/// The build requires an active license from LicenseNinja
		/// </summary>
		public static bool RequireLicense = IsCanary;

		/// <summary>
		/// Is the API for the remote enabled
		/// </summary>
		public static bool RemoteEnabled = IsDebug;


		/// <summary>
		/// Sets the features at the defaults for non-commercial.
		/// Executed when a license is not available.
		/// </summary>
		internal static void SetBasicFeatures()
		{
			if (RequireLicense)
				throw new Exception("Set basic features cannot work with a required license");
			GhostVR = IsDebug;
			Heatmaps = IsDebug;
			RemoteEnabled = IsDebug;
		}

	}

}
