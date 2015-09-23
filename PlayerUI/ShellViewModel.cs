using Caliburn.Micro;
using PlayerUI.ConfigUI;
using PlayerUI;
using PlayerUI.Oculus;
using PlayerUI.Tools;
using PlayerUI.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;


namespace PlayerUI
{
	public partial class ShellViewModel : Screen
	{
		public enum PlayerState
		{
			Idle,
			FileSelected,
			MediaLoaded,
			Playing,
			Ended
		}

		public string VideoLength { get; set; }
		public string CurrentPosition { get; set; }
		public string VideoTime { get; set; }
        
		public bool IsPlaying { get { return _mediaDecoder.IsPlaying; }	}
		public bool IsPaused { get; set; }
		public bool Loop {
			get
			{
				return _mediaDecoder.Loop;
			}
			set
			{
				Console.WriteLine("Writing Loop: " + value);
				_mediaDecoder.Loop = value;
				NotifyOfPropertyChange(() => Loop);
			}
		}

		private string _selectedFileName = "";
		public string SelectedFileNameLabel {
			get {
				if (SelectedFileName.ToLower().StartsWith("http")) return "web stream";
				return Path.GetFileNameWithoutExtension(SelectedFileName);
			}
		}
		public string SelectedFileName { get { return _selectedFileName; } set { this._selectedFileName = value; NotifyOfPropertyChange(() => SelectedFileName); } }
		public bool IsFileSelected { get; set; }

		public DPFCanvas DXCanvas;
		public ShellView shellView;

		private bool ended = false;
		private bool lockSlider = false;

		private bool autoplay = false;
		private MediaDecoder _mediaDecoder;

		Window playerWindow;
		Nancy.Hosting.Self.NancyHost nancy;

		private AutoResetEvent waitForPlaybackReady = new AutoResetEvent(false);
		private ManualResetEvent waitForPlaybackStop = new ManualResetEvent(false);

		private const string DisplayString = "Bivrost 360Player ™ BETA";

		public VolumeControlViewModel VolumeRocker { get; set; }
		public HeadsetMenuViewModel HeadsetMenu { get; set; }

		public static string FileFromArgs = "";

		public NotificationCenterViewModel NotificationCenter { get; set; }

		public ShellViewModel()
		{
			var currentParser = Parser.CreateTrigger;
			Parser.CreateTrigger = (target, triggerText) => ShortcutParser.CanParse(triggerText)
																? ShortcutParser.CreateTrigger(triggerText)
																: currentParser(target, triggerText);

			DisplayName = DisplayString;
			CurrentPosition = "00:00:00";
			VideoLength = "00:00:00";

			NotificationCenter = new NotificationCenterViewModel();

			_mediaDecoder = new MediaDecoder();
			_mediaDecoder.Loop = Loop;

			_mediaDecoder.OnReady += (duration) =>
			{
				if (autoplay)
				{
					autoplay = false;
					Play();					
				}
				//Task.Factory.StartNew(() => Execute.OnUIThread(() => waitForPlaybackReady.Set()));
			};

			_mediaDecoder.OnEnded += () =>
			{
				Task.Factory.StartNew(() =>	Execute.OnUIThread(() =>
				{
					Stop();
					ShowStartupUI();
				}));
			};

			_mediaDecoder.OnStop += () =>
			{
				Task.Factory.StartNew(() => waitForPlaybackStop.Set());
			};

			_mediaDecoder.OnTimeUpdate += (time) =>
			{

				if (!_mediaDecoder.IsPlaying) return;

				//Task.Factory.StartNew(() =>
				//{
				//	Execute.OnUIThread(() =>
				//	{
				//		if (!lockSlider)
				//		{
				//			CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(time))).ToString();
				//			_timeValue = time;
				//			NotifyOfPropertyChange(() => TimeValue);
				//		}
				//		UpdateTimeLabel();
				//	});
				//});

				Execute.OnUIThreadAsync(() =>
				{
					if (!lockSlider)
					{
						OculusPlayback.UpdateTime((float)time);
						CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(time))).ToString();
						_timeValue = time;
						NotifyOfPropertyChange(() => TimeValue);
					}
					UpdateTimeLabel();
				});
			};

			_mediaDecoder.OnError += (error) =>
			{
				Execute.OnUIThreadAsync(() =>
				{
					NotificationCenter.PushNotification(MediaDecoderHelper.GetNotification(error));
					SelectedFileName = null;
					ShowStartupUI();
				});
			};


			UpdateTimeLabel();

			VolumeRocker = new VolumeControlViewModel();
			VolumeRocker.Volume = 0.5;
			VolumeRocker.OnVolumeChange += (volume) =>
			{
				_mediaDecoder.SetVolume(volume);
			};

			HeadsetMenu = new HeadsetMenuViewModel();
			HeadsetMenu.OnRift += () => Task.Factory.StartNew(() =>
			{
				if (OculusPlayback.IsOculusPresent())
					Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Automatic Oculus Rift playback selected.")));
				else
					Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Oculus Rift not detected.")));
			});

			Logic.Instance.OnUpdateAvailable += () => Execute.OnUIThreadAsync(() =>
			{
				NotificationCenter.PushNotification(
					new NotificationViewModel(
						"A new version of Bivrost 360Player is available.",
						() => {
							Updater.OnUpdateFail += 
								() => Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Something went wrong :(")));
							Updater.OnUpdateSuccess +=
								() => Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Update completed successfully.", () => {
									System.Windows.Forms.Application.Restart();
									System.Windows.Application.Current.Shutdown();
								}, "restart", 60f )));
							Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Installing update...")));
							Updater.InstallUpdate();							
                        },
						"install now"
						)
					);
			});


			//Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Installing update...")));

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


		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{

			if (msg == NativeMethods.WM_SHOWBIVROSTPLAYER)
			{
				BringToFront();
				handled = true;
			}
			return IntPtr.Zero;
		}

		public void BringToFront()
		{
			playerWindow.Activate();

			string clipboardText = Clipboard.GetText();
			Console.WriteLine(clipboardText);

			if (clipboardText.StartsWith("bivrost://"))
			{
				Console.WriteLine(clipboardText);
			} else
			{
				OpenFileFrom(clipboardText);
			}
		}

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);

			HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(playerWindow).Handle);
			source.AddHook(new HwndSourceHook(WndProc));

			this.DXCanvas.StopRendering();

			UpdateRecents();
			ShowStartupUI();

			if(File.Exists(FileFromArgs))
			{
				OpenFileFrom(FileFromArgs);
				//IsFileSelected = true;
				//SelectedFileName = FileFromArgs;
				//Play();
				//Task.Factory.StartNew(() => Execute.OnUIThread(() => {
				//	Recents.AddRecent(SelectedFileName);
				//	UpdateRecents();
				//	ShowPlaybackUI();
				//}));
			}

			//Task.Factory.StartNew(() =>
			//{
			//	var connected = OculusPlayback.IsOculusPresent();
			//	if(!connected)
			//	{
			//		NotificationCenter.PushNotification(new NotificationViewModel("Oculus Rift not connected"));
			//	}
			//});

			Logic.Instance.CheckForUpdate();
		}

		public void LoadMedia(bool autoplay = true)
		{
			if (!IsFileSelected) return;
			this.autoplay = autoplay;
			_mediaDecoder.LoadMedia(SelectedFileName);
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			shellView = view as ShellView;
			DXCanvas = shellView.Canvas1;
			playerWindow = (view as Window);

			shellView.PlayPause.Visibility = Visibility.Visible;
			shellView.Pause.Visibility = Visibility.Collapsed;

			if (Logic.Instance.settings.StartInFullScreen)
				ToggleFullscreen(true);
			if (Logic.Instance.settings.AutoLoad)
				if(!string.IsNullOrWhiteSpace(Logic.Instance.settings.AutoPlayFile))
				{
					if(File.Exists(Logic.Instance.settings.AutoPlayFile))
					{
						SelectedFileName = Logic.Instance.settings.AutoPlayFile.Trim();
						LoadMedia();
						//Play();
					}
				}

			shellView.MouseMove += WatchUIVisibility;

			uiVisibilityBackgrundChecker = new BackgroundWorker();
			uiVisibilityBackgrundChecker.WorkerSupportsCancellation = true;
			uiVisibilityBackgrundChecker.DoWork += (sender, parameters) =>
			{
				while(!ended || uiVisibilityBackgrundChecker.CancellationPending)
				{

					if ((DateTime.Now - lastUIMove).TotalSeconds > 2)
					{
						if (IsPlaying)
						{
							if (uiVisible)
							{
								uiVisible = false;
								HideBars();
							}	
						}
					}

					if ((DateTime.Now - lastCursorMove).TotalSeconds > 3)
					{
						if (Fullscreen)
							Execute.OnUIThread(() => Mouse.OverrideCursor = Cursors.None);
					}

					Thread.Sleep(100);
				}
			};

			uiVisibilityBackgrundChecker.RunWorkerAsync();
			
        }

		private DateTime lastUIMove;
		private DateTime lastCursorMove;
		private BackgroundWorker uiVisibilityBackgrundChecker;
		private bool uiVisible = true;

		public void WatchUIVisibility(object sender, MouseEventArgs e)
		{
			Execute.OnUIThread(() => Mouse.OverrideCursor = null);
			lastCursorMove = DateTime.Now;
            if (!IsPlaying || (IsPlaying && IsPaused))
			{
				lastUIMove = DateTime.Now;
				if (!uiVisible)
				{
					uiVisible = true;
					ShowBars();
				}
			} else
			if(IsPlaying && !IsPaused) {
				double height = shellView.ActualHeight;
				double Y = e.GetPosition(null).Y;
				if(!Fullscreen || (height - Y) < 120)
				{
					lastUIMove = DateTime.Now;
					if (!uiVisible)
					{
						uiVisible = true;
						ShowBars();
					}
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
			//if (!IsFileSelected) return;

			//_mediaDecoder.LoadMedia(SelectedFileName);

			//Task.Factory.StartNew(() =>
			//{
				
				//waitForPlaybackReady.WaitOne();

				//if (_mediaDecoder.LastError != null) return;

				Execute.OnUIThreadAsync(() =>
				{
					ShowPlaybackUI();
					TimeValue = 0;
					MaxTime = _mediaDecoder.Duration;
					VideoLength = (new TimeSpan(0, 0, (int)Math.Floor(_mediaDecoder.Duration))).ToString();
					UpdateTimeLabel();
					DisplayName = DisplayString + " - " + SelectedFileNameLabel;

					_mediaDecoder.SetVolume(VolumeRocker.Volume);
					_mediaDecoder.Play();

					Execute.OnUIThread(() =>
					{
						shellView.TopBar.Visibility = Visibility.Visible;
						this.DXCanvas.Visibility = Visibility.Visible;
					});
					this.DXCanvas.Scene = new Scene(_mediaDecoder.TextureL);
					this.DXCanvas.StartRendering();

					Task.Factory.StartNew(() =>
					{
						if (OculusPlayback.IsOculusPresent())
						{

							OculusPlayback.textureL = _mediaDecoder.TextureL;
							OculusPlayback.textureR = _mediaDecoder.TextureR;
							OculusPlayback._stereoVideo = _mediaDecoder.IsStereoRendered;
							OculusPlayback.Configure(SelectedFileNameLabel, (float)_mediaDecoder.Duration);
							OculusPlayback.Start();
						}
						else
						{
							Console.WriteLine("No Oculus connected");
						}
					});				

					shellView.PlayPause.Visibility = Visibility.Collapsed;
					shellView.Pause.Visibility = Visibility.Visible;
					NotifyOfPropertyChange(null);
					
					playerWindow.Focus();
					AnimateIndicator(shellView.PlayIndicator);
				});
				
			//});			
        }

		

		public void Youtube()
		{
			YoutubeAddressViewModel yavm = DialogHelper.ShowDialogOut<YoutubeAddressViewModel>();
			if (!string.IsNullOrWhiteSpace(yavm.YoutubeId)) {
				//SelectedFileName = YoutubeEngine.GetVideoUrlFromId(yavm.YoutubeId);
				
				SelectedFileName = YoutubeEngine.GetVideUrl(yavm.YoutubeId);
				IsFileSelected = true;
				LoadMedia();
				//Play();
			}
		}

		public void PlayPause()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (!IsPlaying)
			{
				if (CanPlay)
					LoadMedia();
					//Play();
			} else
			{
				if (IsPaused) UnPause();
				else Pause();
			}
			
		}

		public void Pause()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			IsPaused = true;
			Task.Factory.StartNew(() => {
				_mediaDecoder.Pause();
			});			
			shellView.PlayPause.Visibility = Visibility.Visible;
			shellView.Pause.Visibility = Visibility.Collapsed;
			NotifyOfPropertyChange(() => CanPlay);
			AnimateIndicator(shellView.PauseIndicator);
			OculusPlayback.Pause();
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
			OculusPlayback.UnPause();
		}

		public void OpenFile()
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			//ofd.Filter = "Video MP4|*.mp4|Video M4V|*.m4v|All|*.*";
			ofd.Filter = MediaDecoder.ExtensionsFilter();
			bool? result = ofd.ShowDialog();
			if(result.HasValue)
				if (result.Value == true)
				{
					OpenFileFrom(ofd.FileName);
				}
		}

		public void UpdateRecents()
		{
			Recents.UpdateMenu(shellView.FileMenuItem, (file) =>
			{
				if (!File.Exists(file))
				{
					Recents.Remove(file);
					UpdateRecents();
				}
				else
					OpenFileFrom(file);
			});
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
				//if (Path.GetExtension(files[0]) == ".mp4")
				if(MediaDecoder.CheckExtension(Path.GetExtension(files[0])))
				{
					OpenFileFrom(files[0]);
				} else
				{
					NotificationCenter.PushNotification(new NotificationViewModel("File format not supported."));
				}
			}
			ShowDropFilesPanel(false);
			if (IsFileSelected == false) ShowStartupPanel(true);
			PlaybackControlUIHitTestVisible(true);
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
			ShowDropFilesPanel(true);
			ShowStartupPanel(false);
			PlaybackControlUIHitTestVisible(false);
            e.Handled = true;
		}

		public void FilePreviewDragLeave(DragEventArgs e)
		{
			ShowDropFilesPanel(false);
			if(IsFileSelected == false) ShowStartupPanel(true);
			PlaybackControlUIHitTestVisible(true);
			e.Handled = true;
        }

		public bool CanPlay { get { return (!IsPlaying || IsPaused) && IsFileSelected;  } }
		public bool CanStop { get { return IsPlaying; } }

		//public bool CanOpenFile { get { return !IsPlaying; } }

		private void OpenFileFrom(string file)
		{			
			if (File.Exists(file))
			{
				Task.Factory.StartNew(() =>
				{
					if (IsPlaying)
					{
						waitForPlaybackStop.Reset();
						Stop();						
						waitForPlaybackStop.WaitOne();						
					} else
					{
						int it = 5;
						while(_mediaDecoder.Initialized && it > 0)
						{
							Thread.Sleep(100);
							it--;
						}
					}
					IsFileSelected = true;
					SelectedFileName = file;
					Execute.OnUIThread(() => LoadMedia());
					Task.Factory.StartNew(() => Execute.OnUIThread(() => {
						Recents.AddRecent(SelectedFileName);
						UpdateRecents();
						ShowPlaybackUI();
					}));
				});
				
			}
        }

		public void Stop()
		{
			Console.WriteLine("FILE ENDED");
			if (Fullscreen) ToggleFullscreen(true);
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());
			ShowBars();

			Console.WriteLine("STOP STOP STOP");
			OculusPlayback.Stop();
			Execute.OnUIThread(() =>
			{
				shellView.TopBar.Visibility = Visibility.Hidden;
				this.DXCanvas.Visibility = Visibility.Hidden;
			});			
			this.DXCanvas.Scene = null;
			
				Task.Factory.StartNew(() =>
				{
					if (IsPlaying || _mediaDecoder.IsEnded)
					{
						_mediaDecoder.Stop();
						_timeValue = 0;
						try
						{
							Execute.OnUIThread(() =>
							{
								NotifyOfPropertyChange(() => TimeValue);
								NotifyOfPropertyChange(() => CanPlay);
								CurrentPosition = (new TimeSpan(0, 0, 0)).ToString();
								UpdateTimeLabel();

								shellView.PlayPause.Visibility = Visibility.Visible;
								shellView.Pause.Visibility = Visibility.Collapsed;

								this.DXCanvas.StopRendering();
							});
						}
						catch (Exception) { }
					}
				});
			
			waitForPlaybackStop.Set();
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
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			DialogHelper.ShowDialog<ConfigurationViewModel>();
		}

		private Point _mouseDownPoint;
		private bool _drag = false;
		private IInputElement _element;
		private Point _dragLastPosition;
		private DateTime _doubleClickFirst;
		private bool _doubleClickDetected = false;
		private bool _waitingForDoubleClickTimeout = false;
		private Point _waitingPoint;

		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (_doubleClickDetected) return;
			var current = e.GetPosition(null);
			var delta = current - _dragLastPosition;
			_dragLastPosition = current;
			if(this.DXCanvas.Scene != null) {
				((Scene)this.DXCanvas.Scene).MoveDelta((float)delta.X, (float)delta.Y, (float) (72f/this.DXCanvas.ActualWidth) * 1.5f, 20f);
			}
		}

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{	
			if (_doubleClickDetected) _doubleClickDetected = false;
			else
			if ((DateTime.Now - _doubleClickFirst).TotalMilliseconds < 250 )
			{
				ToggleFullscreen(true);
				_doubleClickDetected = true;
			}
			_doubleClickFirst = DateTime.Now;

			_mouseDownPoint = e.GetPosition(null);
			_dragLastPosition = _mouseDownPoint;
			_drag = false;
			_element = (IInputElement)e.Source;
			_element.CaptureMouse();
			_element.MouseMove += MouseMove;
		}

		public void MouseUp(MouseButtonEventArgs e)
		{
			if(!_waitingForDoubleClickTimeout)
			{
				_waitingPoint = e.GetPosition(null);
				_waitingForDoubleClickTimeout = true;
				Task.Factory.StartNew(() =>
				{
					Thread.Sleep(280);
					Execute.OnUIThread(() =>
				   {
					   if (!_doubleClickDetected)
					   {
						   if (_mouseDownPoint == _waitingPoint && !_drag)
						   {
							   PlayPause();
						   }
						   else
						   {

						   }
					   }
					   _waitingForDoubleClickTimeout = false;
				   });
				});
			}

			if(_element != null) { 
				_element.ReleaseMouseCapture();
				_element.MouseMove -= MouseMove;
			}
		}

		private bool _fullscreen = false;
		public bool Fullscreen
		{
			get { return this._fullscreen; }
			set { this._fullscreen = value; NotifyOfPropertyChange(() => Fullscreen); }
		}

		//public void ToggleFullscreen() { ToggleFullscreen(false); }

		public void ToggleFullscreen(bool realToggle = false)
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if(realToggle)
				Fullscreen = !Fullscreen;

			if(!Fullscreen) {
				Mouse.OverrideCursor = null;
				ShowUI();
				playerWindow.WindowState = WindowState.Normal;
				playerWindow.WindowStyle = WindowStyle.SingleBorderWindow;
				//playerWindow.Topmost = false;
				playerWindow.ResizeMode = ResizeMode.CanResize;
			} else
			{
				Mouse.OverrideCursor = null;
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
			if (Fullscreen) ToggleFullscreen(true);
		}

		public void OnLostFocus()
		{
			if (IsPlaying) ((Scene)this.DXCanvas.Scene).HasFocus = false;
		}

		public void OnGotFocus()
		{
			if (IsPlaying) ((Scene)this.DXCanvas.Scene).HasFocus = true;
		}

		public void ShowVolumeControl()
		{
			VolumeRocker.Show();
		}

		public void Mute()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			VolumeRocker.ToggleMute();
			NotifyOfPropertyChange(() => VolumeTooltip);
		}

		public string VolumeTooltip
		{
			get { return "Volume: " + (VolumeRocker.IsMuted ? "muted" : (Math.Round(VolumeRocker.Volume * 100) + "%")); }
		}

		public void VolumeMouseWheel(MouseWheelEventArgs e)
		{
			VolumeRocker.MouseWheel(e);
			NotifyOfPropertyChange(() => VolumeTooltip);
		}

		public void FastForward()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (IsPlaying)
			{
				_mediaDecoder.Seek(_mediaDecoder.CurrentPosition + 5f);
			}
		}

		public void FastRewind()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (IsPlaying)
			{
				_mediaDecoder.Seek(_mediaDecoder.CurrentPosition - 5f);
			}
		}

		public void HeadsetSelect()
		{
			HeadsetMenu.ToggleVisibility();
		}
		
	}
}
