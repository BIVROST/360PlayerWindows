using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.ConfigUI
{
	public class EnumConfigItemViewModel : ConfigItemBase<int>
	{
		private Type _enumType;

		public EnumConfigItemViewModel(SettingsPropertyAttribute attr, Func<int> loadCallback, Action<int> saveCallback, Type enumType) : base(attr, loadCallback, saveCallback) {
			_enumType = enumType;
			var values = Enum.GetValues(enumType);
            EnumList = new List<string>();
			foreach(object v in values)
				EnumList.Add(v.ToString());
		}

		public bool ReadOnly { get; set; } = false;

		public List<string> EnumList { get; set; }

		public string Value
		{
			get
			{
				return Enum.Parse(_enumType, base.Value.ToString()).ToString();
			}

			set
			{
				base.Value = (int)Enum.Parse(_enumType, value);
            }
		}

	}
}
