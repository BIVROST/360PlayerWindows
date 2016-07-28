using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.ConfigUI
{
	public class EnumConfigItemViewModel : ConfigItemBase<int>
	{
		private Type _enumType;
		private Dictionary<string, int> enumMap;

		public EnumConfigItemViewModel(SettingsPropertyAttribute attr, Func<int> loadCallback, Action<int> saveCallback, Type enumType) : base(attr, loadCallback, saveCallback) {
			_enumType = enumType;
			var values = Enum.GetValues(enumType);
            EnumList = new List<string>();
			enumMap = new Dictionary<string, int>();
			foreach (object v in values)
			{
				enumMap.Add(GetDescription(v), (int)v);
				EnumList.Add(GetDescription(v));
			}
		}

		string GetDescription(object enumObject)
		{
			var type = enumObject.GetType();
			var memberInfo = type.GetMember(enumObject.ToString());
			var attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
			if (attributes.Length > 0)
			{
				var attr = attributes?.First() as DescriptionAttribute;
				return attr.Description;
			}
			else
			{
				return enumObject.ToString();
			}
		}

		public bool ReadOnly { get; set; } = false;

		public List<string> EnumList { get; set; }

		public string Value
		{
			get
			{
				return enumMap.First(e => ((int)e.Value) == base.Value).Key;
			}

			set
			{
				base.Value = (int)enumMap[value];
            }
		}

	}
}
