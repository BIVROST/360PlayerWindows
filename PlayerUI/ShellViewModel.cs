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
using Bivrost;
using System.Text.RegularExpressions;
using SharpDX.Direct3D11;
using SharpDX.XInput;

namespace PlayerUI
{
	public partial class ShellViewModel : Screen
	{
		public static ShellViewModel Instance;

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
		public HeadsetMode HeadsetUsage { get; set; }
        
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
				if (!string.IsNullOrWhiteSpace(SelectedFileTitle)) return SelectedFileTitle;
				if(!string.IsNullOrWhiteSpace(SelectedFileName))
					if (SelectedFileName.ToLower().StartsWith("http")) return "web stream";
				return Path.GetFileNameWithoutExtension(SelectedFileName);
			}
		}
		public string SelectedFileName { get { return _selectedFileName; } set { this._selectedFileName = value; NotifyOfPropertyChange(() => SelectedFileName); } }
		public bool IsFileSelected { get; set; }

		public string SelectedFileTitle { get; set; } = "";
		public string SelectedFileDescription { get; set; } = "";

		public DPFCanvas DXCanvas;
		public ShellView shellView;

		private bool ended = false;
		private bool lockSlider = false;

		private bool autoplay = false;
		private MediaDecoder _mediaDecoder;
		private bool _ready = false;

		Window playerWindow;
		Nancy.Hosting.Self.NancyHost remoteControl;
		float remoteTime = 0;

		private AutoResetEvent waitForPlaybackReady = new AutoResetEvent(false);
		private ManualResetEvent waitForPlaybackStop = new ManualResetEvent(false);

		private const string DisplayString = "Bivrost 360Player ™ BETA {0} - FOR NON-COMMERCIAL USE";

		public VolumeControlViewModel VolumeRocker { get; set; }
		public HeadsetMenuViewModel HeadsetMenu { get; set; }

		public static string FileFromArgs = "";
		public static string FileFromProtocol = "";

        private Controller xpad;
        private static TimeoutBool urlLoadLock = false;

		public NotificationCenterViewModel NotificationCenter { get; set; }

		public ShellViewModel()
		{
			ShellViewModel.Instance = this;

			var currentParser = Parser.CreateTrigger;
			Parser.CreateTrigger = (target, triggerText) => ShortcutParser.CanParse(triggerText)
																? ShortcutParser.CreateTrigger(triggerText)
																: currentParser(target, triggerText);

			DisplayName = string.Format(DisplayString, "");
			CurrentPosition = "00:00:00";
			VideoLength = "00:00:00";

			NotificationCenter = new NotificationCenterViewModel();

			_mediaDecoder = new MediaDecoder();
			_mediaDecoder.Loop = Loop;


			_mediaDecoder.OnReady += (duration) =>
			{
				_ready = true;
                SendEvent("movieLoaded", Path.GetFileName(SelectedFileName));
				if (autoplay)
				{
					autoplay = false;
                    urlLoadLock = false;
					Play();					
				}
			};

			_mediaDecoder.OnEnded += () =>
			{
                SendEvent("movieEnded", Path.GetFileName(SelectedFileName));
                Task.Factory.StartNew(() =>	Execute.OnUIThread(() =>
				{
					Stop();
					ShowStartupUI();
				}));
			};

			_mediaDecoder.OnStop += () =>
			{
				Task.Factory.StartNew(() => waitForPlaybackStop.Set());
                SendEvent("movieStopped", Path.GetFileName(SelectedFileName));
            };

			_mediaDecoder.OnTimeUpdate += (time) =>
			{

				if (!_mediaDecoder.IsPlaying) return;

				Execute.OnUIThreadAsync(() =>
				{
					if (!lockSlider)
					{
						OculusPlayback.UpdateTime((float)time);
                        OSVRKit.OSVRPlayback.UpdateTime((float)time);
                        CurrentPosition = (new TimeSpan(0, 0, (int)Math.Floor(time))).ToString();
						_timeValue = time;
						NotifyOfPropertyChange(() => TimeValue);
					}
					UpdateTimeLabel();
				});
			};

			_mediaDecoder.OnError += (error) =>
			{
                urlLoadLock = false;
                Execute.OnUIThreadAsync(() =>
				{
					NotificationCenter.PushNotification(MediaDecoderHelper.GetNotification(error));
					SelectedFileName = null;
					ShowStartupUI();
				});
                SendEvent("playbackError", error);
            };

            _mediaDecoder.OnBufferingStarted += () =>
            {
                Execute.OnUIThreadAsync(() =>
                {
                    shellView.BufferingStatus.Visibility = Visibility.Visible;
                });
            };

            _mediaDecoder.OnBufferingEnded += () =>
            {
                Execute.OnUIThreadAsync(() =>
                {
                    shellView.BufferingStatus.Visibility = Visibility.Collapsed;
                });                
            };

            _mediaDecoder.OnProgress += (progress) =>
            {
                Execute.OnUIThreadAsync(() =>
                {
                    shellView.BufferingStatus.Text = $"Buffering... {progress}";
                });
            };


            UpdateTimeLabel();

			VolumeRocker = new VolumeControlViewModel();
			VolumeRocker.Volume = 0.5;
			VolumeRocker.OnVolumeChange += (volume) =>
			{
				_mediaDecoder.SetVolume(volume);
                ShellViewModel.SendEvent("volumeChanged", volume);
            };

			HeadsetMenu = new HeadsetMenuViewModel();
			HeadsetMenu.OnAuto += () => Task.Factory.StartNew(() =>
			{
				Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Automatic headset detection selected.")));
				this.HeadsetUsage = HeadsetMode.Auto;
			});
			HeadsetMenu.OnRift += () => Task.Factory.StartNew(() =>
			{
				Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Oculus Rift playback selected.")));
				this.HeadsetUsage = HeadsetMode.Oculus;
			});
			HeadsetMenu.OnOSVR += () => Task.Factory.StartNew(() =>
			{
				Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("OSVR playback selected.")));
				this.HeadsetUsage = HeadsetMode.OSVR;
			});
			HeadsetMenu.OnDisable += () => Task.Factory.StartNew(() =>
			{
				Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Headset playback disabled.")));
				this.HeadsetUsage = HeadsetMode.Disable;
			});



			this.HeadsetUsage = Logic.Instance.settings.HeadsetUsage;
			

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
							Task.Factory.StartNew(() => Updater.InstallUpdate());													
                        },
						"install now"
						)
					);
			});

            Logic.Instance.ValidateSettings();

        }


		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{

			if (msg == NativeMethods.WM_SHOWBIVROSTPLAYER)
			{
				BringToFront();
				handled = true;
			}
			if(msg == NativeMethods.WM_COPYDATA)
			{				
				NativeMethods.COPYDATASTRUCT cps = (NativeMethods.COPYDATASTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(NativeMethods.COPYDATASTRUCT));
				string data = cps.lpData;				

				BringToFront(data);
				handled = true;
			}
			return IntPtr.Zero;
		}

		public void BringToFront(string wmText = "")
		{
			Execute.OnUIThreadAsync(() => playerWindow.Activate());

			//string clipboardText = Clipboard.GetText();
			string clipboardText = wmText;

			Console.WriteLine(clipboardText);

			if (clipboardText.StartsWith("bivrost:"))
			{
				try
				{
					var protocol = Protocol.Parse(clipboardText);
					switch (protocol.stereoscopy)
					{
						case Protocol.Stereoscopy.autodetect: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.Autodetect; break;
						case Protocol.Stereoscopy.mono: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.Mono; break;
						case Protocol.Stereoscopy.side_by_side: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.SideBySide; break;
						case Protocol.Stereoscopy.top_and_bottom: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.TopBottom; break;
						case Protocol.Stereoscopy.top_and_bottom_reversed: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.TopBottomReversed; break;
					}
					Loop = protocol.loop.HasValue ? protocol.loop.Value : false;
					string videoUrl = protocol.urls.FirstOrDefault((u) => {
                        var b1 = Regex.IsMatch(u, @"(\b|_).mp4(\b|_)");
                        var b2 = Regex.IsMatch(u, @"(\b|_).avi(\b|_)");
                        return b1 || b2;
                    });
					if (string.IsNullOrWhiteSpace(videoUrl))
						videoUrl = protocol.urls[0];
					OpenUrlFrom(videoUrl);
				}
				catch (Exception) { }
				
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

            shellView.BufferingStatus.Visibility = Visibility.Collapsed;

            UpdateRecents();
            ShowStartupUI();

            xpad = new Controller(SharpDX.XInput.UserIndex.One);

            if (File.Exists(FileFromArgs))
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

			//FileFromProtocol = @"bivrost:https://www.youtube.com/watch?v=edcJ_JNeyhg";

			Task.Factory.StartNew(() => {
                if (!string.IsNullOrWhiteSpace(FileFromProtocol))
                {
                    try
                    {
                        var protocol = Protocol.Parse(FileFromProtocol);

						switch (protocol.stereoscopy)
                        {
                            case Protocol.Stereoscopy.autodetect: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.Autodetect; break;
                            case Protocol.Stereoscopy.mono: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.Mono; break;
                            case Protocol.Stereoscopy.side_by_side: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.SideBySide; break;
                            case Protocol.Stereoscopy.top_and_bottom: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.TopBottom; break;
                            case Protocol.Stereoscopy.top_and_bottom_reversed: _mediaDecoder.StereoMode = MediaDecoder.VideoMode.TopBottomReversed; break;
                        }
                        Loop = protocol.loop.HasValue ? protocol.loop.Value : false;
                        string videoUrl = protocol.urls.FirstOrDefault((u) => {
                            var b1 = Regex.IsMatch(u, @"(\b|_).mp4(\b|_)");
                            var b2 = Regex.IsMatch(u, @"(\b|_).avi(\b|_)");
                            return b1 || b2;
                        });
                        if (string.IsNullOrWhiteSpace(videoUrl))
                            videoUrl = protocol.urls[0];

						OpenUrlFrom(videoUrl);
                    }
                    catch (Exception) { }
                }
            });

            OSVRKit.OSVRPlayback.OnGotFocus += () => Task.Factory.StartNew(() => {
                Execute.OnUIThreadAsync(() =>
                {
                    shellView.Activate();
                });
            }); 

			

			//Task.Factory.StartNew(() =>
			//{
			//	var connected = OculusPlayback.IsOculusPresent();
			//	if(!connected)
			//	{
			//		NotificationCenter.PushNotification(new NotificationViewModel("Oculus Rift not connected"));
			//	}
			//});

			Logic.Instance.CheckForUpdate();
            Logic.Instance.CheckForBrowsers();
            Logic.Instance.stats.TrackScreen("Start screen");
			Logic.Instance.stats.TrackEvent("Application events", "Init", "Player launched");

			//LegacyTest();


			if (Logic.Instance.settings.EnableRemoteControl)
				EnableRemoteControl();
			if (Logic.Instance.settings.UseBlackBackground)
				shellView.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
			if (Logic.Instance.settings.DisableUI)
			{
				shellView.controlBar.Visibility = Visibility.Collapsed;
				shellView.TopBar.Visibility = Visibility.Collapsed;
				shellView._OpenUrl.Visibility = Visibility.Collapsed;
				shellView._OpenFile.Visibility = Visibility.Collapsed;
			}

#if DEBUG
			Task.Factory.StartNew(async () =>
			{
				try
				{
					long seconds = await Bivrost.LicenseNinja.Verify(Logic.Instance.settings.ProductCode, Logic.Instance.settings.LicenseCode, Logic.Instance.settings.InstallId.ToString());
				}
				catch (Bivrost.LicenseNinja.LicenseException err)
				{
					Logic.Instance.settings.LicenseCode = "";
					Logic.Instance.settings.Save();

					Execute.OnUIThread(() =>
					{
						OpenLicenseManagement();
					});
				}
			});
#endif

		}

		

		private void Log(string message, ConsoleColor color)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(message);
			Console.ForegroundColor = oldColor;
		}


		public void LoadMedia(bool autoplay = true)
		{
			_ready = false;
			if (!IsFileSelected) return;
			this.autoplay = autoplay;
			//if (!string.IsNullOrWhiteSpace(SelectedFileName))
			//{
			//	if (Path.GetFileNameWithoutExtension(SelectedFileName).ToLower().Contains("fbcube"))
			//		_mediaDecoder.Projection = MediaDecoder.ProjectionMode.CubeFacebook;
			//	else
			//		_mediaDecoder.Projection = MediaDecoder.ProjectionMode.Sphere;
			//}

			string mediaFile = SelectedFileName;
			Task.Factory.StartNew(() => _mediaDecoder.LoadMedia(mediaFile));
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			shellView = view as ShellView;
			DXCanvas = shellView.Canvas1;
			playerWindow = (view as Window);

#if !DEBUG
			shellView.BetaActivationMenu.Visibility = Visibility.Collapsed;
#endif

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
                bool xpadRestart = false;

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

                    if(xpad!=null)
                        if(xpad.IsConnected)
                        {
                            if (this.DXCanvas.Scene == null)
                            {
                                if (IsFileSelected)
                                {
                                    if (xpad.GetState().Gamepad.Buttons == GamepadButtonFlags.Y && !xpadRestart)
                                    {
                                        xpadRestart = true;
                                        if (!_mediaDecoder.IsPlaying)
                                            PlayPause();
                                        else
                                            Rewind();
                                    }
                                    else xpadRestart = false;
                                }
                                else xpadRestart = false;
                            }
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
                {
                    _mediaDecoder.Seek(value);
                }
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

			if (!_ready) return;

				Execute.OnUIThreadAsync(() =>
				{
					ShowPlaybackUI();
					TimeValue = 0;
					MaxTime = _mediaDecoder.Duration;
					VideoLength = (new TimeSpan(0, 0, (int)Math.Floor(_mediaDecoder.Duration))).ToString();
					UpdateTimeLabel();
					//DisplayName = DisplayString + " - " + SelectedFileNameLabel;
					DisplayName = string.Format(DisplayString, " - now playing: " + SelectedFileNameLabel);

					_mediaDecoder.SetVolume(VolumeRocker.Volume);
					//_mediaDecoder.Play();

					//STATS
					Logic.Instance.stats.TrackEvent("Application events", "Play", "");

					Execute.OnUIThread(() =>
					{
						shellView.TopBar.Visibility = Visibility.Visible;
						this.DXCanvas.Visibility = Visibility.Visible;
					});

                    this.DXCanvas.Scene = new Scene(_mediaDecoder.TextureL, _mediaDecoder.Projection) { xpad = this.xpad };
					this.DXCanvas.StartRendering();

					

					Task.Factory.StartNew(() =>
					{
                        while(OculusPlayback.Lock || OSVRKit.OSVRPlayback.Lock)
                        {
                            Thread.Sleep(50);
                        }

						OculusPlayback.Reset();
						OSVRKit.OSVRPlayback.Reset();

						bool detected = false;

						if (this.HeadsetUsage == HeadsetMode.Auto || this.HeadsetUsage == HeadsetMode.Oculus)
						{
							if (OculusPlayback.IsOculusPresent())
							{
								detected = true;
								Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Oculus Rift detected. Starting VR playback...")));
								OculusPlayback.textureL = _mediaDecoder.TextureL;
								OculusPlayback.textureR = _mediaDecoder.TextureR;
								OculusPlayback._stereoVideo = _mediaDecoder.IsStereoRendered;
								OculusPlayback._projection = _mediaDecoder.Projection;
								OculusPlayback.Configure(SelectedFileNameLabel, (float)_mediaDecoder.Duration);
								OculusPlayback.Start();
                                ShellViewModel.SendEvent("headsetConnected", "oculus");
                            }
							else
							{
								Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("Oculus Rift not detected.")));
								Console.WriteLine("No Oculus connected");
                                ShellViewModel.SendEvent("headsetError", "oculus");
                            }
						}

						if(!detected)
						if (this.HeadsetUsage == HeadsetMode.Auto || this.HeadsetUsage == HeadsetMode.OSVR)
						{
							if (OSVRKit.OSVRPlayback.IsOculusPresent())
							{
								Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("OSVR detected. Starting VR playback...")));
								OSVRKit.OSVRPlayback.textureL = _mediaDecoder.TextureL;
								OSVRKit.OSVRPlayback.textureR = _mediaDecoder.TextureR;
								OSVRKit.OSVRPlayback._stereoVideo = _mediaDecoder.IsStereoRendered;
								OSVRKit.OSVRPlayback._projection = _mediaDecoder.Projection;
								OSVRKit.OSVRPlayback.Configure(SelectedFileNameLabel, (float)_mediaDecoder.Duration);
								OSVRKit.OSVRPlayback.Start();
                                    ShellViewModel.SendEvent("headsetConnected", "osvr");
                                } else
							{
								Execute.OnUIThreadAsync(() => NotificationCenter.PushNotification(new NotificationViewModel("OSVR not detected.")));
								Console.WriteLine("No OSVR connected");
                                    ShellViewModel.SendEvent("headsetError", "osvr");
                                }
						}
					});

					_mediaDecoder.Play();

					shellView.PlayPause.Visibility = Visibility.Collapsed;
					shellView.Pause.Visibility = Visibility.Visible;
					NotifyOfPropertyChange(null);
					
					playerWindow.Focus();
					AnimateIndicator(shellView.PlayIndicator);
				});
				
			//});			
        }

		private void OpenUrlFrom(string url)
		{
            if (urlLoadLock)
            {
                return;
            }                

            urlLoadLock = true;


            Execute.OnUIThreadAsync(() =>
            {
				SelectedFileTitle = "";

				NotificationCenter.PushNotification(new NotificationViewModel("Checking url..."));
                OpenUrlViewModel ouvm = new OpenUrlViewModel();
                ouvm.Url = url;
                Task.Factory.StartNew(() => {
                    ouvm.Open();

                    Execute.OnUIThreadAsync(() =>
                    {
                        if (ouvm.Valid)
                        {
                            NotificationCenter.PushNotification(new NotificationViewModel("Loading..."));
                            if (!string.IsNullOrWhiteSpace(ouvm.VideoUrl))
                            {
                                SelectedFileName = ouvm.VideoUrl;
                                IsFileSelected = true;
								if (ouvm.ServiceResult != null)
								{
									_mediaDecoder.Projection = ouvm.ServiceResult.projection;
									_mediaDecoder.StereoMode = ouvm.ServiceResult.stereoscopy;
									SelectedFileTitle = ouvm.ServiceResult.title;
								}
								else {
									_mediaDecoder.Projection = StreamingServices.GetServiceProjection(ouvm.Uri);
								}
								
								Execute.OnUIThreadAsync(() =>
                                {
                                    LoadMedia();
                                });
                            }
                        }
                        else
                        {
                            NotificationCenter.PushNotification(new NotificationViewModel("Url is not valid video or streaming service address."));
                            urlLoadLock = false;
                        }
                    });
                });
                
            });
		}
		

		public void OpenUrl()
		{
			SelectedFileTitle = "";

			OpenUrlViewModel ouvm = DialogHelper.ShowDialogOut<OpenUrlViewModel>();
			if (ouvm.Valid)
			{
				NotificationCenter.PushNotification(new NotificationViewModel("Loading..."));
				if(!string.IsNullOrWhiteSpace(ouvm.VideoUrl))
				{
					SelectedFileName = ouvm.VideoUrl;
					IsFileSelected = true;

					if (ouvm.ServiceResult != null)
					{
						_mediaDecoder.Projection = ouvm.ServiceResult.projection;
						_mediaDecoder.StereoMode = ouvm.ServiceResult.stereoscopy;
						SelectedFileTitle = ouvm.ServiceResult.title;
					}
					else {
						_mediaDecoder.Projection = StreamingServices.GetServiceProjection(ouvm.Uri);
					}

					Execute.OnUIThreadAsync(() =>
					{
						LoadMedia();
					});					
				}
			} else
			{
				NotificationCenter.PushNotification(new NotificationViewModel("Url is not valid video or streaming service address."));
			}
		}

		public void PlayPause()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			if (!_ready)
				return;

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
                ShellViewModel.SendEvent("moviePaused", Path.GetFileName(SelectedFileName));
            });
            Execute.OnUIThreadAsync(() =>
            {
                shellView.PlayPause.Visibility = Visibility.Visible;
                shellView.Pause.Visibility = Visibility.Collapsed;
                NotifyOfPropertyChange(() => CanPlay);
                AnimateIndicator(shellView.PauseIndicator);
            });
            			
			OculusPlayback.Pause();
            OSVRKit.OSVRPlayback.Pause();
		}

		public void UnPause()
		{
            Execute.OnUIThreadAsync(() => {
                IsPaused = false;
                Task.Factory.StartNew(() => {
                    _mediaDecoder.Unpause();
                    ShellViewModel.SendEvent("movieUnpaused", Path.GetFileName(SelectedFileName));
                });
                shellView.PlayPause.Visibility = Visibility.Collapsed;
                shellView.Pause.Visibility = Visibility.Visible;
                NotifyOfPropertyChange(() => CanPlay);
                AnimateIndicator(shellView.PlayIndicator);
                OculusPlayback.UnPause();
                OSVRKit.OSVRPlayback.UnPause();
            });			
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
					_mediaDecoder.Projection = MediaDecoder.ProjectionMode.Sphere;
					IsFileSelected = true;
					SelectedFileTitle = "";
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
			if (Fullscreen) if(!Logic.Instance.settings.DoNotExitFullscreenOnStop) ToggleFullscreen(true);
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());
			ShowBars();
            ShowStartupUI();

			Console.WriteLine("STOP STOP STOP");

			this.DXCanvas.Scene = null;

			OculusPlayback.Stop();
			OSVRKit.OSVRPlayback.Stop();

            Execute.OnUIThread(() =>
			{
				shellView.TopBar.Visibility = Visibility.Hidden;
				this.DXCanvas.Visibility = Visibility.Hidden;

				DisplayName = string.Format(DisplayString, "");
			});			
						
			Task.Factory.StartNew(() =>
			{
				if (IsPlaying || _mediaDecoder.IsEnded)
				{
					_mediaDecoder.Stop();
                    ShellViewModel.SendEvent("movieStopped", Path.GetFileName(SelectedFileName));
					//STATS
					Logic.Instance.stats.TrackEvent("Application events", "Stop", "");

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
			Task.Factory.StartNew(async () =>
			{
				await Logic.Instance.stats.TrackQuit();
			});

			base.TryClose(dialogResult);
			
			if(_mediaDecoder != null)
				_mediaDecoder.Shutdown();
			//nancy.Stop();

		}

		public void Rewind()
		{
            if(_mediaDecoder != null)
            {
                _mediaDecoder.Seek(0);
                if (_mediaDecoder.IsEnded || _mediaDecoder.IsPaused)
                        PlayPause();
            }
			//BivrostPlayerPrototype.PlayerPrototype.Rewind();
		}

		public void Quit()
		{
            if (remoteControl != null)
            {
                remoteControl.Stop();
            }
            if (IsRemoteControlEnabled)
            {
                ShellViewModel.SendEvent("quit");
            }

			//STATS
			TryClose();
		}

		public void OpenSettings()
		{
			//space press hack
			Execute.OnUIThread(() => shellView.VideoProgressBar.Focus());

			DialogHelper.ShowDialog<ConfigurationViewModel>();
            Logic.Instance.ValidateSettings();
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

            ShellViewModel.SendEvent("fullscreenChanged", Fullscreen);
        }

		public void EscapeFullscreen()
		{
			if (Fullscreen) ToggleFullscreen(true);
		}

		public void OnLostFocus()
		{
            if (IsPlaying)
            {
                if(this.DXCanvas.Scene != null)
                    ((Scene)this.DXCanvas.Scene).HasFocus = false;
            }
		}

		public void OnGotFocus()
		{
            if (IsPlaying)
            {
                if (this.DXCanvas.Scene != null)
                    ((Scene)this.DXCanvas.Scene).HasFocus = true;
            }
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
		

        public void LegacyTest()
        {
            SharpDX.Direct3D.FeatureLevel[] _levels = new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 };
            Device _device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, _levels);
            
            SharpDX.Direct3D11.Texture2DDescription frameTextureDescription = new SharpDX.Direct3D11.Texture2DDescription()
            {
                Width = 1920,
                Height = 1080,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                BindFlags = BindFlags.RenderTarget | SharpDX.Direct3D11.BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.Shared
            };


            Texture2D textureL = new SharpDX.Direct3D11.Texture2D(_device, frameTextureDescription);
            SharpDX.DXGI.Surface surface = textureL.QueryInterface<SharpDX.DXGI.Surface>();

            LegacyPlayer.MediaDecoderLegacy md = new LegacyPlayer.MediaDecoderLegacy(surface.NativePointer);
            md.OpenUrl(@"D:\TestVideos\maroon.mp4");

            this.DXCanvas.Scene = new Scene(textureL, MediaDecoder.ProjectionMode.Sphere);
            this.DXCanvas.StartRendering();
        }
	}
}
