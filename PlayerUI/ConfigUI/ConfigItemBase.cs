using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.ConfigUI
{
	public class ConfigItemBase<T> : PropertyChangedBase, IConfigItemBase
	{
		protected T _value;
		public T Value
		{
			get { return this._value; }
			set { this._value = value; NotifyOfPropertyChange(() => Value); }
		}

		protected Action<T> _saveCallback = null;
		protected Func<T> _loadCallback = null;
		protected SettingsPropertyAttribute _attr = null;

		public ConfigItemBase(SettingsPropertyAttribute attr, Func<T> loadCallback, Action<T> saveCallback)
		{
			_attr = attr;
			_saveCallback = saveCallback;
			_loadCallback = loadCallback;

			Value = _loadCallback();
		}

		public string DisplayName { get { return _attr.DisplayName; } }

		public virtual void Save()
		{
			_saveCallback(Value);
		}

		public virtual void Reload()
		{
			Value = _loadCallback();
		}

	}

	public interface IConfigItemBase
	{
		void Save();
		void Reload();

	}
}
