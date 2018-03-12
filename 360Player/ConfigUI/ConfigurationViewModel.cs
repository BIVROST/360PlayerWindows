using Caliburn.Micro;
using Bivrost.Bivrost360Player.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.ConfigUI
{
	public class ConfigurationViewModel : Screen
	{
		private Settings settings;

		public BindableCollection<PropertyChangedBase> ConfigItems { get; set; } = new BindableCollection<PropertyChangedBase>();
		public BindableCollection<PropertyChangedBase> ConfigAdvancedItems { get; set; } = new BindableCollection<PropertyChangedBase>();

		public ConfigurationViewModel()
		{
			DisplayName = "Settings";

			FeaturesEnum features = Features.AsEnum;

			settings = Logic.Instance.settings;
			var props = settings.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SettingsPropertyAttribute)));
			props.ToList().ForEach(p =>
			{
				SettingsPropertyAttribute attr = p.GetAttributes<SettingsPropertyAttribute>(false).First();
				PropertyInfo property = p;

				if (!features.HasFlag(attr.requiredFeatures))
					return;

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
					case ConfigItemType.Enum:
						Type enumType = p.PropertyType;
						TargetCollection.Add(new EnumConfigItemViewModel(attr, () => (int)property.GetValue(settings, null), (value) => property.SetValue(settings, value, null), enumType));
						break;
					case ConfigItemType.Action:
						TargetCollection.Add(new ButtonConfigItemViewModel(attr, () => { ((System.Action)property.GetValue(settings))(); }));
						break;
				}
			});

		}

		protected override void OnViewReady(object view)
		{
			base.OnViewReady(view);
			IconHelper.RemoveIcon(view as System.Windows.Window);
		}

		public void Save()
		{
			ConfigItems.Where(ci => ci is IConfigItemBase).ToList().ForEach(ci => ((IConfigItemBase)ci).Save());
			ConfigAdvancedItems.Where(ci => ci is IConfigItemBase).ToList().ForEach(ci => ((IConfigItemBase)ci).Save());
			Logic.Instance.settings.Save();
			TryClose();
		}

		public void Cancel()
		{
			TryClose();
		}


	}
}
