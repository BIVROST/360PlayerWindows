using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace PlayerUI
{
	public class Recents
	{
		public static List<string> recentFiles = new List<string>();

		public static void Save()
		{
			try
			{
				string dataFoler = Logic.LocalDataDirectory;//Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BivrostPlayer";
				if (!Directory.Exists(dataFoler))
					Directory.CreateDirectory(dataFoler);
				string recentConfig = dataFoler + "recents";
				File.WriteAllText(recentConfig, JsonConvert.SerializeObject(recentFiles), Encoding.UTF8);
			}
			catch (Exception exc) {
				Console.WriteLine("[EXC] " + exc.Message);
			}
		}

		public static void Remove(string file)
		{
			if (recentFiles.Contains(file))
				recentFiles.Remove(file);
		}

		public static void Load()
		{
				try
				{
				string dataFoler = Logic.LocalDataDirectory;//Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BivrostPlayer";
					if (!Directory.Exists(dataFoler))
						Directory.CreateDirectory(dataFoler);
					string recentConfig = dataFoler + "recents";



					if (File.Exists(recentConfig))
					{

						List<string> tempRecents = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(recentConfig, Encoding.UTF8));
						if (tempRecents != null)
						{
							recentFiles.Clear();
							recentFiles.AddRange(tempRecents);
						}
					}
				}
				catch (Exception exc) {
					Console.WriteLine("[EXC] " + exc.Message);
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

			int index = 1;
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
				  //newItem.InputGestureText = "Ctrl+" + index++ % 10;
				  menuItem.Items.Add(newItem);
			  });
		}

	}
}
