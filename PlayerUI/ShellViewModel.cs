using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;


namespace PlayerUI
{
	public class ShellViewModel : Screen
	{
		public string VideoLength { get; set; }
		public string CurrentPosition { get; set; }
		public string VideoTime { get; set; }
        
		public bool IsPlaying { get; set; }

		private string _selectedFileName = "";
		public string SelectedFileName { get { return _selectedFileName; } set { this._selectedFileName = value; NotifyOfPropertyChange(() => SelectedFileName); } }
		public bool IsFileSelected { get; set; }

		public DPFCanvas Canvas;

		private bool ended = false;
		private bool lockSlider = false;

		private MediaDecoder _mediaDecoder;

		Window playerWindow;

		public ShellViewModel()
		{
			var currentParser = Parser.CreateTrigger;
			Parser.CreateTrigger = (target, triggerText) => ShortcutParser.CanParse(triggerText)
																? ShortcutParser.CreateTrigger(triggerText)
																: currentParser(target, triggerText);

			DisplayName = "Bivrost Player ™";
			CurrentPosition = "00:00:00";
			VideoLength = "00:00:00";

			BivrostPlayerPrototype.PlayerPrototype.TimeUpdate += (time) =>
			{
				if (ended) return;


				if (!lockSlider)
				{
					CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(time))).ToString();
					_timeValue = time;
					NotifyOfPropertyChange(() => TimeValue);
				}
				UpdateTimeLabel();
			};

			BivrostPlayerPrototype.PlayerPrototype.VideoLoaded += (duration) =>
			{
				if (ended) return;

				TimeValue = 0;
				MaxTime = duration;
				CurrentPosition = "00:00:00";
				VideoLength = (new TimeSpan(0, 0, (int)Math.Floor(duration))).ToString();

				UpdateTimeLabel();
			};

			UpdateTimeLabel();
		}

		public void Test()
		{
			Console.Beep();
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			Canvas = (view as ShellView).Canvas1;
			playerWindow = (view as Window);
			
		}


		private double _timeValue = 0;
		public double TimeValue
		{
			get
			{
				return _timeValue;
			}

			set
			{
				_timeValue = value;

				if(!lockSlider)
					BivrostPlayerPrototype.PlayerPrototype.SetTime(_timeValue);
				else
				{
					CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(_timeValue))).ToString();
					NotifyOfPropertyChange(() => CurrentPosition);
				}
			}
		}

		private double _sphereSize = 6f;
		public double SphereSize
		{
			get { return _sphereSize; }
			set { this._sphereSize = value; BivrostPlayerPrototype.PlayerPrototype.SetSphereSize(value); }
		}


		private double _maxTime = double.MaxValue;
		public double MaxTime
		{
			get { return _maxTime; }
			set { _maxTime = value; NotifyOfPropertyChange(() => MaxTime); }
		}

		private void UpdateTimeLabel()
		{
			if (ended) return;
			VideoTime = CurrentPosition + " / " + VideoLength;
			NotifyOfPropertyChange(() => VideoTime);
		}

		public void Play()
		{
			//if (_mediaDecoder == null) _mediaDecoder = new MediaDecoder(Canvas);
			//_mediaDecoder.Play(SelectedFileName);

			Task.Factory.StartNew(() =>
			{
				Execute.OnUIThread(() =>
				{
					IsPlaying = true;
					NotifyOfPropertyChange(null);
				});
				
				BivrostPlayerPrototype.PlayerPrototype.TextureCreated += (tex) =>
				{
					Caliburn.Micro.Execute.OnUIThread(() =>
					{
						this.Canvas.Scene = new Scene();
						this.Canvas.SetVideoTexture(tex);
					});
				};
				BivrostPlayerPrototype.PlayerPrototype.Play(SelectedFileName);

				Thread.Sleep(500);
				Execute.OnUIThread(() =>
				{
					IsPlaying = false;
					NotifyOfPropertyChange(null);
				});
			});

			playerWindow.Focus();
		}

		public void Youtube()
		{
			YoutubeAddressViewModel yavm = DialogHelper.ShowDialogOut<YoutubeAddressViewModel>();
			if (!string.IsNullOrWhiteSpace(yavm.YoutubeId)) { 
				SelectedFileName = YoutubeEngine.GetVideoUrlFromId(yavm.YoutubeId);
				Play();
			}
		}

		public void PlayPause()
		{
			BivrostPlayerPrototype.PlayerPrototype.PlayPause();
		}

		public void OpenFile()
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.Filter = "Video MP4|*.mp4|Video M4V|*.m4v|All|*.*";
			bool? result = ofd.ShowDialog();
			if(result.HasValue)
				if (result.Value == true)
				{
					IsFileSelected = true;
					SelectedFileName = ofd.FileName;
					Play();
				}
		}

		public void FileDropped(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				// Note that you can have more than one file.
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				string ext = Path.GetExtension(files[0]);
				if (Path.GetExtension(files[0]) == ".mp4")
				{
					IsFileSelected = true;
					SelectedFileName = files[0];
					Play();
				}
				
			}
		}

		public void SliderLock()
		{
			lockSlider = true;
		}

		public void SliderUnlock()
		{
			lockSlider = false;
			BivrostPlayerPrototype.PlayerPrototype.SetTime(_timeValue);
		}

		public void FilePreviewDragEnter(DragEventArgs e)
		{
			e.Handled = true;
		}

		public bool CanPlay { get { return !IsPlaying && IsFileSelected; } }
		public bool CanStop { get { return IsPlaying; } }

		public bool CanOpenFile { get { return !IsPlaying; } }

		public void Stop()
		{
			BivrostPlayerPrototype.PlayerPrototype.Close();
		}

		public override void TryClose(bool? dialogResult = null)
		{
			base.TryClose(dialogResult);
			ended = true;
			if(_mediaDecoder != null)
				_mediaDecoder.Shutdown();
		}

		public void Rewind()
		{
			BivrostPlayerPrototype.PlayerPrototype.Rewind();
		}

		public void Quit()
		{
			TryClose();
		}

		public void OpenSettings()
		{
			DialogHelper.ShowDialog<SettingsWindowViewModel>();
		}
	}
}
