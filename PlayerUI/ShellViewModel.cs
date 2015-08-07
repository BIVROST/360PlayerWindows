using Caliburn.Micro;
using PlayerUI.ConfigUI;
using PlayerUI.Oculus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;


namespace PlayerUI
{
	public class ShellViewModel : Screen
	{
		public string VideoLength { get; set; }
		public string CurrentPosition { get; set; }
		public string VideoTime { get; set; }
        
		public bool IsPlaying { get { return _mediaDecoder.IsPlaying; }	}
		public bool IsPaused { get; set; }

		private string _selectedFileName = "";
		public string SelectedFileNameLabel { get { return Path.GetFileNameWithoutExtension(SelectedFileName); } }
		public string SelectedFileName { get { return _selectedFileName; } set { this._selectedFileName = value; NotifyOfPropertyChange(() => SelectedFileName); } }
		public bool IsFileSelected { get; set; }

		public DPFCanvas DXCanvas;
		public ShellView shellView;

		private bool ended = false;
		private bool lockSlider = false;

		private MediaDecoder _mediaDecoder;

		Window playerWindow;
		Nancy.Hosting.Self.NancyHost nancy;

		private AutoResetEvent waitForPlaybackReady = new AutoResetEvent(false);

		private const string DisplayString = "Bivrost Player ™ BETA";

		public ShellViewModel()
		{
			var currentParser = Parser.CreateTrigger;
			Parser.CreateTrigger = (target, triggerText) => ShortcutParser.CanParse(triggerText)
																? ShortcutParser.CreateTrigger(triggerText)
																: currentParser(target, triggerText);

			DisplayName = DisplayString;
			CurrentPosition = "00:00:00";
			VideoLength = "00:00:00";

			_mediaDecoder = new MediaDecoder();

			_mediaDecoder.OnReady += (duration) =>
			{
				waitForPlaybackReady.Set();
			};

			_mediaDecoder.OnEnded += () =>
			{
				Stop();
			};

			_mediaDecoder.OnTimeUpdate += (time) =>
			{
				if (!_mediaDecoder.IsPlaying) return;

				if (!lockSlider)
				{
					CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(time))).ToString();
					_timeValue = time;
					NotifyOfPropertyChange(() => TimeValue);
				}
				UpdateTimeLabel();
			};


			UpdateTimeLabel();

			#region NANCY REMOTE CONTROL SERVER
			if (Logic.Instance.settings.EnableRemoteServer) { 

				// NANCY HTTP REMOTE
				nancy = ApiServer.InitNancy(apiServer =>
					  Execute.OnUIThread(() => {
						  Console.WriteLine("got unity init, device_id=", ApiServer.device_id, "movies=[\n", string.Join(",\n", ApiServer.movies), "]");

					  
					  })
				);

				ApiServer.OnBackPressed += ApiServer_OnBackPressed;
				ApiServer.OnStateChange += ApiServer_OnStateChange;
				ApiServer.OnPos += ApiServer_OnPos;
				ApiServer.OnInfo += msg => Console.WriteLine(msg);
			}
			#endregion
		}

		#region REMOTE CONTROL HANDLERS

		private ApiServer.State _serverState;
		private void ApiServer_OnStateChange(ApiServer.State newState)
		{
			_serverState = newState;
			//InfoState.Dispatcher.Invoke(() => InfoState.Content = newState);
			switch(newState)
			{
				case ApiServer.State.init:
					Rewind();
					break;
				case ApiServer.State.off:
					Pause();
					break;
				case ApiServer.State.pause:
					Pause();
					break;
				case ApiServer.State.play:
					//if (BivrostPlayerPrototype.PlayerPrototype.IsPlaying)
					//	UnPause();
					//else
					//	BivrostPlayerPrototype.PlayerPrototype.PlayLoadedFile();
					break;
				case ApiServer.State.stop:
					Rewind();
					break;
			}

			Console.WriteLine("STATE CHANGED: " + _serverState);
		}

		private void ApiServer_OnBackPressed()
		{
			//ApiServer.CommandMessage("back feedback "+ApiServer.status.max_id);
			//ApiServer.CommandReset();

			//Console.WriteLine("BACK PRESSED WITH STATE: " + _serverState);
			//switch(_serverState)
			//{
   //            case ApiServer.State.stop:
			//		//ApiServer.CommandReset();
			//		//ApiServer.CommandUnPause();
			//		Rewind();
			//		if (BivrostPlayerPrototype.PlayerPrototype.IsPlaying)
			//			UnPause();
			//		else
			//			BivrostPlayerPrototype.PlayerPrototype.PlayLoadedFile();
			//		break;
			//	case ApiServer.State.play:
			//		//ApiServer.CommandReset();
			//		Rewind();
			//		break;
			//	case ApiServer.State.pause:
			//		//ApiServer.CommandUnPause();
			//		UnPause();
			//		break;
			//	case ApiServer.State.init:
			//		//ApiServer.CommandReset();
			//		if (BivrostPlayerPrototype.PlayerPrototype.IsPlaying)
			//			UnPause();
			//		else
			//			BivrostPlayerPrototype.PlayerPrototype.PlayLoadedFile();
   //                 break;
			//}
		}

		private void ApiServer_OnPos(System.Tuple<float, float, float> euler, float t01)
		{
			try { 
				Execute.OnUIThread(() => {
					//InfoX.Content = euler.Item1;
					//InfoY.Content = euler.Item2;
					//InfoZ.Content = euler.Item3;
					//InfoT01.Content = t01;
					if(DXCanvas.Scene != null)
					{
						(DXCanvas.Scene as Scene).SetLook(euler);
					}
				});
			} catch(Exception) { };
        }

		#endregion



		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			shellView = view as ShellView;
			DXCanvas = shellView.Canvas1;
			playerWindow = (view as Window);

			shellView.PlayPause.Visibility = Visibility.Visible;
			shellView.Pause.Visibility = Visibility.Collapsed;

			if (Logic.Instance.settings.StartInFullScreen)
				ToggleFullscreen();
			if (Logic.Instance.settings.AutoLoad)
				if(!string.IsNullOrWhiteSpace(Logic.Instance.settings.AutoPlayFile))
				{
					if(File.Exists(Logic.Instance.settings.AutoPlayFile))
					{
						SelectedFileName = Logic.Instance.settings.AutoPlayFile.Trim();
						Play();
					}
				}
		}

		protected override void OnDeactivate(bool close)
		{
			TryClose();
			base.OnDeactivate(close);
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
				if (!_mediaDecoder.IsPlaying) return;

				_timeValue = value;

				if (!lockSlider)
					_mediaDecoder.Seek(value);
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
			set { this._sphereSize = value; /*BivrostPlayerPrototype.PlayerPrototype.SetSphereSize(value);*/ }
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
			if (!IsFileSelected) return;

			_mediaDecoder.LoadMedia(SelectedFileName);
			Task.Factory.StartNew(() =>
			{

				waitForPlaybackReady.WaitOne();

				Execute.OnUIThread(() =>
				{
					TimeValue = 0;
					MaxTime = _mediaDecoder.Duration;
					VideoLength = (new TimeSpan(0, 0, (int)Math.Floor(_mediaDecoder.Duration))).ToString();
					UpdateTimeLabel();
					DisplayName = DisplayString + " - " + SelectedFileNameLabel;

					_mediaDecoder.Play();
					this.DXCanvas.Scene = new Scene(_mediaDecoder.TextureL);
					if(OculusPlayback.IsOculusPresent()) { 
						OculusPlayback.textureL = _mediaDecoder.TextureL;
						OculusPlayback.textureR = _mediaDecoder.TextureR;
						OculusPlayback._stereoVideo = _mediaDecoder.IsStereo;
						OculusPlayback.Start();
					}
					shellView.PlayPause.Visibility = Visibility.Collapsed;
					shellView.Pause.Visibility = Visibility.Visible;
					NotifyOfPropertyChange(null);
				});
				
			});

			playerWindow.Focus();
			AnimateIndicator(shellView.PlayIndicator);
        }

		private void AnimateIndicator(UIElement uiControl)
		{
			Task.Factory.StartNew(() =>
			{
				Thread.Sleep(100);
				Execute.OnUIThread(() =>
				{
					Storyboard storyboard = new Storyboard();
					double animTime = 0.8;

					DoubleAnimation opacityAnimation = new DoubleAnimation { From = 0.8, To = 0.0, Duration = TimeSpan.FromSeconds(animTime) };
					Storyboard.SetTarget(opacityAnimation, uiControl);
					Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
					storyboard.Children.Add(opacityAnimation);

					DoubleAnimation scaleAnimationX = new DoubleAnimation { From = 0.5, To = 1.5, Duration = TimeSpan.FromSeconds(animTime) };
					Storyboard.SetTarget(scaleAnimationX, uiControl);
					Storyboard.SetTargetProperty(scaleAnimationX, new PropertyPath("RenderTransform.ScaleX"));
					storyboard.Children.Add(scaleAnimationX);

					DoubleAnimation scaleAnimationY = new DoubleAnimation { From = 0.5, To = 1.5, Duration = TimeSpan.FromSeconds(animTime) };
					Storyboard.SetTarget(scaleAnimationY, uiControl);
					Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath("RenderTransform.ScaleY"));
					storyboard.Children.Add(scaleAnimationY);

					storyboard.Begin();
				});
			});			
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
			if (!IsPlaying)
			{
				if(CanPlay)
					Play();
			} else
			{
				if (IsPaused) UnPause();
				else Pause();
			}
			
		}

		public void Pause()
		{
			IsPaused = true;
			Task.Factory.StartNew(() => {
				_mediaDecoder.Pause();
			});			
			shellView.PlayPause.Visibility = Visibility.Visible;
			shellView.Pause.Visibility = Visibility.Collapsed;
			NotifyOfPropertyChange(() => CanPlay);
			AnimateIndicator(shellView.PauseIndicator);
		}

		public void UnPause()
		{
			IsPaused = false;
			Task.Factory.StartNew(() => {
				_mediaDecoder.Unpause();
			});
			shellView.PlayPause.Visibility = Visibility.Collapsed;
			shellView.Pause.Visibility = Visibility.Visible;
			NotifyOfPropertyChange(() => CanPlay);
			AnimateIndicator(shellView.PlayIndicator);
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

		public void OpenAbout()
		{
			DialogHelper.ShowDialog<AboutViewModel>();
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
			_mediaDecoder.Seek(_timeValue);
		}

		public void FilePreviewDragEnter(DragEventArgs e)
		{
			e.Handled = true;
		}

		public bool CanPlay { get { return (!IsPlaying || IsPaused) && IsFileSelected;  } }
		public bool CanStop { get { return IsPlaying; } }

		public bool CanOpenFile { get { return !IsPlaying; } }

		public void Stop()
		{
			OculusPlayback.Stop();
			this.DXCanvas.Scene = null;
			Task.Factory.StartNew(() =>
			{
				if (IsPlaying || _mediaDecoder.IsEnded)
				{
					_mediaDecoder.Stop();
					_timeValue = 0;
					Execute.OnUIThread(() =>
					{
						NotifyOfPropertyChange(() => TimeValue);
						NotifyOfPropertyChange(() => CanPlay);
					});
				}
			});
			shellView.PlayPause.Visibility = Visibility.Visible;
			shellView.Pause.Visibility = Visibility.Collapsed;
		}

		public override void TryClose(bool? dialogResult = null)
		{
			ended = true;
			Stop();

			base.TryClose(dialogResult);
			
			if(_mediaDecoder != null)
				_mediaDecoder.Shutdown();
			//nancy.Stop();

		}

		public void Rewind()
		{
			//BivrostPlayerPrototype.PlayerPrototype.Rewind();
		}

		public void Quit()
		{
			TryClose();
		}

		public void OpenSettings()
		{
			DialogHelper.ShowDialog<ConfigurationViewModel>();
		}

		private Point _mouseDownPoint;
		private bool _drag = false;
		private IInputElement _element;
		private Point _dragLastPosition;

		public void MouseMove(object sender, MouseEventArgs e)
		{
			var current = e.GetPosition(null);
			var delta = current - _dragLastPosition;
			_dragLastPosition = current;
			if(this.DXCanvas.Scene != null) {
				((Scene)this.DXCanvas.Scene).MoveDelta((float)delta.X, (float)delta.Y, (float) (72f/this.DXCanvas.ActualWidth) * 1.5f);
			}
		}

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			_mouseDownPoint = e.GetPosition(null);
			_dragLastPosition = _mouseDownPoint;
			_drag = false;
			_element = (IInputElement)e.Source;
			_element.CaptureMouse();
			_element.MouseMove += MouseMove;
		}

		public void MouseUp(MouseButtonEventArgs e)
		{
			if (_mouseDownPoint == e.GetPosition(null) && !_drag) {
				PlayPause();				
			} else {

			}
			if(_element != null) { 
				_element.ReleaseMouseCapture();
				_element.MouseMove -= MouseMove;
			}
		}

		private bool fullscreen = false;
		public void ToggleFullscreen()
		{
			fullscreen = !fullscreen;
			if(!fullscreen) {
				ShowUI();
				playerWindow.WindowState = WindowState.Normal;
				playerWindow.WindowStyle = WindowStyle.SingleBorderWindow;
				//playerWindow.Topmost = false;
				playerWindow.ResizeMode = ResizeMode.CanResize;
			} else
			{
				HideUI();
				playerWindow.WindowState = WindowState.Normal;
				playerWindow.WindowStyle = WindowStyle.None;
				//playerWindow.Topmost = true;
				playerWindow.ResizeMode = ResizeMode.NoResize;
				playerWindow.Margin = new Thickness(0,0,0,0);
				playerWindow.WindowState = WindowState.Maximized;
			}
		}

		public void EscapeFullscreen()
		{
			if (fullscreen) ToggleFullscreen();
		}

		public void HideUI()
		{
			var shell = playerWindow as ShellView;
			shell.topMenuPanel.Visibility = Visibility.Collapsed;
			//shell.controlBar.Visibility = Visibility.Collapsed;
			//shell.logoImage.Visibility = Visibility.Collapsed;
			shell.menuRow.Height = new GridLength(0);
			shell.SelectedFileNameLabel.Visibility = Visibility.Collapsed;
			shell.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
			NotifyOfPropertyChange(null);
		}

		public void ShowUI()
		{
			var shell = playerWindow as ShellView;
			shell.topMenuPanel.Visibility = Visibility.Visible;
			shell.controlBar.Visibility = Visibility.Visible;
			//shell.logoImage.Visibility = Visibility.Visible;
			shell.menuRow.Height = new GridLength(22);
			shell.SelectedFileNameLabel.Visibility = Visibility.Visible;
			shell.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
			NotifyOfPropertyChange(null);
		}
	}
}
