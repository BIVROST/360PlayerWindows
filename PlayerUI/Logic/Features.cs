using PlayerUI.ConfigUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bivrost;
using Bivrost.Log;

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
		locallyStoredSessions = 8,
		requireLicense = 16,
		remote = 32,
		commercialUse = 64,
		isDebugOrCanary = 128
	}


	[AttributeUsage(AttributeTargets.Field)]
	internal class FeatureGrantedFromLicenseAttribute : Attribute
	{
		public readonly string name;

		public FeatureGrantedFromLicenseAttribute(string name) { this.name = name.Trim().ToLowerInvariant(); }
	}


	/// <summary>
	/// List of features that are granted to this build.
	/// The list of features may change at runtime (for example when a license key is changed),
	/// this will trigger the ListUpdated event.
	/// </summary>
	public static class Features
	{
		internal static FeaturesEnum AsEnum
		{
			get
			{
				FeaturesEnum fe = new FeaturesEnum();
				if (IsDebug)
				{
					fe |= FeaturesEnum.isDebug;
					fe |= FeaturesEnum.isDebugOrCanary;
				}
				if (IsCanary)
				{
					fe |= FeaturesEnum.isCanary;
					fe |= FeaturesEnum.isDebugOrCanary;
				}
				if (GhostVR)
					fe |= FeaturesEnum.ghostVR;
				if (LocallyStoredSessions)
					fe |= FeaturesEnum.locallyStoredSessions;
				if (RequireLicense)
					fe |= FeaturesEnum.requireLicense;
				if (RemoteEnabled)
					fe |= FeaturesEnum.remote;

				return fe;
			}
		}


		public static event Action ListUpdated;


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
		/// The build requires an active license from LicenseNinja
		/// </summary>
		public static bool RequireLicense = IsCanary;

		/// <summary>
		/// Online heatmap analytics gathering and sending is enabled
		/// </summary>
		[FeatureGrantedFromLicense("ghostvr")]
		public static bool GhostVR = false;

		/// <summary>
		/// Local video analytics session gathering is enabled
		/// </summary>
		[FeatureGrantedFromLicense("locally-stored-sessions")]
		public static bool LocallyStoredSessions = false;

		/// <summary>
		/// Is the API for the remote enabled
		/// </summary>
		[FeatureGrantedFromLicense("remote")]
		public static bool RemoteEnabled = false;

		/// <summary>
		/// Is commercial use allowed?
		/// </summary>
		[FeatureGrantedFromLicense("commercial")]
		public static bool Commercial = false;

		/// <summary>
		/// Sets the features at the defaults for non-commercial.
		/// Executed when a license is not available.
		/// </summary>
		internal static void SetBasicFeatures()
		{
			GhostVR = false;
			LocallyStoredSessions = false;
			RemoteEnabled = false;
			Commercial = false;
		}

		internal static void SetFromLicense(LicenseNinja.License license)
		{
			Dictionary<string, string> grant = license.GrantAsDictionary;

			foreach (var field in typeof(Features).GetFields())
			{
				var fieldval = field.GetValue(null);
				foreach (var attr in field.GetCustomAttributes(true))
				{
					if (attr is FeatureGrantedFromLicenseAttribute)
					{
						string name = ((FeatureGrantedFromLicenseAttribute)attr).name;
						if (grant.ContainsKey(name))
						{
							string val = grant[name];
							if (fieldval is bool)
								field.SetValue(null, val == "true" || string.IsNullOrEmpty(val));
							else if (fieldval is int)
								field.SetValue(null, int.Parse(val));
							else if (fieldval is string)
								field.SetValue(null, val);
							else
								LoggerManager.Error($"Unsupported feature field type: {field.GetType()} on key {name}");
							grant.Remove(name);
						}
					}
				}
			}

			foreach (var kvp in grant)
			{
				LoggerManager.Error(kvp.Value != null ? $"Unknown feature granted: {kvp.Key} = {kvp.Value}" : $"Unknown feature granted: {kvp.Key} (no value)");
			}
		}

		internal static void TriggerListUpdated()
		{
			ListUpdated?.Invoke();
		}
	}

}
