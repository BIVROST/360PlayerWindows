using Bivrost.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PlayerUI
{
	public class Recents
	{
		public class RecentsFormat1 : List<string> { }

		public class RecentsFormat2 : List<RecentsFormat2.RecentElement>
		{
			public class RecentElement
			{
				public string title;
				public string uri;
			}


			public static implicit operator RecentsFormat2(RecentsFormat1 rf1)
			{
				RecentsFormat2 rf2 = new RecentsFormat2();
				rf2.AddRange(rf1.ConvertAll(uri => 
				{
					LoggerManager.Info("Upgrading recents from v1");
					string title = Path.GetFileName(uri);
					if (string.IsNullOrWhiteSpace(title))
						title = uri;
					return new RecentElement() { uri = uri, title = title };
				}));
				return rf2;
			}
		}


		static RecentsFormat2 recentFiles = null;


		public static RecentsFormat2 RecentFiles
		{
			get
			{
				if (recentFiles == null)
					Load();
				return recentFiles;
			}
		}

		static void Save()
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
				LoggerManager.Error("Recents save: " + exc.Message);
			}
		}

		static void Load()
		{
			Debug.Assert(recentFiles == null);
			try
			{
				string dataFoler = Logic.LocalDataDirectory;//Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BivrostPlayer";
				if (!Directory.Exists(dataFoler))
					Directory.CreateDirectory(dataFoler);
				string recentConfig = dataFoler + "recents";

				if (File.Exists(recentConfig))
				{
					string recentsSerialized = File.ReadAllText(recentConfig, Encoding.UTF8);

					try
					{
						recentFiles = JsonConvert.DeserializeObject<RecentsFormat2>(recentsSerialized);
					}
					// this probably isn't in the newest format, try the older one
					catch(JsonSerializationException e)
					{
						recentFiles = JsonConvert.DeserializeObject<RecentsFormat1>(recentsSerialized);
						Save();
					}
				}
				else
				{
					LoggerManager.Info("Creating new recents file");
					recentFiles = new RecentsFormat2();
				}
			}
			catch (Exception exc) {
				LoggerManager.Error(exc, "Recents error");
			}
		}

		public static void AddRecent(Streaming.ServiceResult result)
		{
			// remove duplicate
			recentFiles.RemoveAll(f => f.uri == result.originalURL);

			// trim to at most 9 (so 1 can be added)
			while (recentFiles.Count > 9)
				recentFiles.RemoveAt(0);

			string title = result.title;
			if (string.IsNullOrWhiteSpace(title))
				title = Path.GetFileName(result.videoStreams[0].url);
			recentFiles.Insert(0, new RecentsFormat2.RecentElement() { title = title, uri = result.originalURL });
			Save();
		}

	}
}
