using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class OpenUrlViewModel : Screen
	{
		public OpenUrlViewModel()
		{
			DisplayName = "Open video URL";
		}

		public string Url { get; set; }

		public void Open()
		{
			if (string.IsNullOrWhiteSpace(Url))
			{
				System.Windows.MessageBox.Show("Incorrect URL");
				TryClose();
				return;
			}

			//var step1 = Url.Split('?');
			//if (step1.Length == 2)
			//{
			//	var step2 = step1[1].Split('&');
			//	if (step2.Length > 0)
			//	{
			//		var list = step2.Where(s => s.StartsWith("v=")).ToList();
			//		if (list.Count == 1)
			//		{
			//			YoutubeId = list[0].Split('=')[1];
			//		}
			//		else System.Windows.MessageBox.Show("Incorrect URL");
			//	}
			//	else System.Windows.MessageBox.Show("Incorrect URL");
			//}
			//else System.Windows.MessageBox.Show("Incorrect URL");

			TryClose();
		}

	}
}
