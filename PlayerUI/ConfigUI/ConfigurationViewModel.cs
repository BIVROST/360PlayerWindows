using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.ConfigUI
{
	public class ConfigurationViewModel : Screen
	{
		private Settings settings;

		public BindableCollection<PropertyChangedBase> ConfigItems { get; set; } = new BindableCollection<PropertyChangedBase>();
		public BindableCollection<PropertyChangedBase> ConfigAdvancedItems { get; set; } = new BindableCollection<PropertyChangedBase>();

		public ConfigurationViewModel()
		{
			DisplayName = "Settings";

			settings = Logic.Instance.settings;
			var props = settings.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SettingsPropertyAttribute)));
			props.ToList().ForEach(p =>
			{
				SettingsPropertyAttribute attr = p.GetAttributes<SettingsPropertyAttribute>(false).First();
				PropertyInfo property = p;

				var TargetCollection = attr is SettingsAdvancedPropertyAttribute ? ConfigAdvancedItems : ConfigItems;

				switch (attr.Type)
				{
					case ConfigItemType.Bool:
						TargetCollection.Add(new BoolConfigItemViewModel(attr, () => (bool)property.GetValue(settings, null), (value) => property.SetValue(settings, value, null)));
						break;
					case ConfigItemType.Path:
						TargetCollection.Add(new PathConfigItemViewModel(attr, () => (string)property.GetValue(settings, null), (value) => property.SetValue(settings, value, null)));
						break;
					case ConfigItemType.String:
						TargetCollection.Add(new StringConfigItemViewModel(attr, () => (string)property.GetValue(settings, null), (value) => property.SetValue(settings, value, null)));
						break;
				}
			});

		}

		public void Save()
		{
			ConfigItems.ToList().ForEach(ci => ((IConfigItemBase)ci).Save());
			Logic.Instance.settings.Save();
			TryClose();
		}

		public void Cancel()
		{
			TryClose();
		}


	}
}
