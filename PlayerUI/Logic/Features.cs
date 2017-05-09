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
		heatmaps = 8,
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

	public static class Features
	{




		public static FeaturesEnum AsEnum
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
		/// The build requires an active license from LicenseNinja
		/// </summary>
		public static bool RequireLicense = IsCanary;

		/// <summary>
		/// Online heatmap analytics gathering and sending is enabled
		/// Requires Heatmaps
		/// </summary>
		[FeatureGrantedFromLicense("ghostvr")]
		public static bool GhostVR = false;

		/// <summary>
		/// Local heatmap analytics gathering is enabled
		/// </summary>
		[FeatureGrantedFromLicense("heatmap")]
		public static bool Heatmaps = false;

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
			Heatmaps = false;
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
								Logger.Error($"Unsupported feature field type: {field.GetType()} on key {name}");
							grant.Remove(name);
						}
					}
				}
			}

			foreach (var kvp in grant)
			{
				Logger.Error(kvp.Value != null ? $"Unknown feature granted: {kvp.Key} = {kvp.Value}" : $"Unknown feature granted: {kvp.Key} (no value)");
			}
		}
	}

}
