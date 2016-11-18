using System;


namespace PlayerUI.ConfigUI
{
	public class SettingsPropertyAttribute : Attribute
	{
		public string DisplayName = "";
		public string Caption = "";
		public ConfigItemType Type = ConfigItemType.String;
		public bool ReadOnly = false;
		public FeaturesEnum requiredFeatures = FeaturesEnum.none;

		public SettingsPropertyAttribute(string displayName, ConfigItemType type)
		{
			this.DisplayName = displayName;
			this.Type = type;
		}
	}

	public class SettingsAdvancedPropertyAttribute : SettingsPropertyAttribute
	{
		public SettingsAdvancedPropertyAttribute(string displayName, ConfigItemType type) : base(displayName, type) { }
	}

	public enum ConfigItemType
	{
		Bool,
		String,
		Path,
		Int,
		Enum,
		Action
	}
}
