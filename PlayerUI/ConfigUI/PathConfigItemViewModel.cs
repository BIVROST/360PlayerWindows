using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayerUI.ConfigUI
{
	public class PathConfigItemViewModel : ConfigItemBase<string>
	{
		public PathConfigItemViewModel(SettingsPropertyAttribute attr, Func<string> loadCallback, Action<string> saveCallback) : base(attr, loadCallback, saveCallback) { }

		public void SelectPath()
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();
			DialogResult result = fbd.ShowDialog();
			if (result == DialogResult.OK)
			{
				Value = fbd.SelectedPath;
			}
		}

		public bool ReadOnly { get; set; } = false;

		public bool CanSelectPath { get { return !this.ReadOnly; } }
	}
}
