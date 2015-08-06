using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PlayerUI
{
	public class EventShellViewModel : Screen
	{
		public EventShellViewModel()
		{
			var currentParser = Parser.CreateTrigger;
			Parser.CreateTrigger = (target, triggerText) => ShortcutParser.CanParse(triggerText)
																? ShortcutParser.CreateTrigger(triggerText)
																: currentParser(target, triggerText);
		}

		Window playerWindow;

		BackgroundWorker _stayFocused;

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);

			playerWindow.Activate();
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			playerWindow = (view as Window);

			if (Logic.Instance.settings.EventModeAutoPlay)
			{
				//BivrostPlayerPrototype.PlayerPrototype.Loop = Logic.Instance.settings.EventModeLoop;
				Play();
			}			

			_stayFocused = new BackgroundWorker();
			_stayFocused.DoWork += (sender, e) =>
			{
				while (true)
				{
					Execute.OnUIThread(() =>
					{
						playerWindow.Activate();
						playerWindow.Focus();
						
					});					
					Thread.Sleep(5000);
				}
			};

			_stayFocused.RunWorkerAsync();
		}

		public void SwitchMode()
		{
			Logic.Instance.settings.EventMode = false;
			Logic.Instance.settings.Save();
			Logic.Instance.ReloadPlayer();
		}

		public void Play()
		{
			Task.Factory.StartNew(() =>
			{


				//BivrostPlayerPrototype.PlayerPrototype.TextureCreated += (tex) =>
				//{

				//};
				//BivrostPlayerPrototype.PlayerPrototype.Play(Logic.Instance.settings.EventModeSingleFile);

				
			});

			Thread.Sleep(2000);				
			playerWindow.Focus();
		}

		public void PlayPause()
		{
			//BivrostPlayerPrototype.PlayerPrototype.PlayPause();
		}

		public void Rewind()
		{
			//BivrostPlayerPrototype.PlayerPrototype.Rewind();
		}

		public void Restart()
		{
			Logic.Instance.ReloadPlayer();
		}
	}
}
