using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PlayerUI
{
	public class Recents
	{
		public static List<string> recentFiles = new List<string>();

		public static void Save()
		{
			try
			{
				File.WriteAllText("recents", JsonConvert.SerializeObject(recentFiles), Encoding.UTF8);
			}
			catch (Exception) { }
		}

		public static void Remove(string file)
		{
			if (recentFiles.Contains(file))
				recentFiles.Remove(file);
		}

		public static void Load()
		{
			
			if (File.Exists("recents"))
			{
				try
				{
					List<string> tempRecents = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("recents", Encoding.UTF8));
					if (tempRecents != null)
					{
						recentFiles.Clear();
						recentFiles.AddRange(tempRecents);
					}
				}
				catch (Exception) { }
			}
		}

		public static void AddRecent(string file)
		{
			if(recentFiles.Contains(file))
			{
				int index = recentFiles.IndexOf(file);
				recentFiles.RemoveAt(index);
			}
			recentFiles.Add(file);
			if(recentFiles.Count > 10)
			{
				recentFiles.RemoveAt(0);
			}
			Save();
		}

		public static void UpdateMenu(MenuItem menuItem, Action<string> bindAction)
		{
			List<MenuItem> deleteItems = new List<MenuItem>();
			foreach (object oitem in menuItem.Items)
			{
				MenuItem item = oitem as MenuItem;
				if (item != null)
				{
					if (((string)item.Tag) == "recent")
						deleteItems.Add(item);
				}
			}

			deleteItems.ForEach(di => menuItem.Items.Remove(di));


			recentFiles.Reverse<string>().Take(10).ToList().ForEach(recent =>
			  {
				  string header = Path.GetFileName(recent);
				  string fileName = recent;
				  MenuItem newItem = new MenuItem() { Header = header };
				  newItem.Tag = "recent";
				  newItem.Click += (sender, e) =>
				  {
					  bindAction(fileName);
				  };
				  menuItem.Items.Add(newItem);
			  });
		}

	}
}
