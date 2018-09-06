using Caliburn.Micro;
using Bivrost.Bivrost360Player.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using LoggerManager = Bivrost.Log.LoggerManager;


namespace Bivrost.Bivrost360Player
{
	public partial class ShellViewModel
	{

		public void HideUI()
		{
			var shell = playerWindow as ShellView;
			shell.topMenuPanel.Visibility = Visibility.Collapsed;
			//shell.controlBar.Visibility = Visibility.Collapsed;
			//shell.logoImage.Visibility = Visibility.Collapsed;
			shell.menuRow.Height = new GridLength(0);
			//shell.SelectedFileNameLabel.Visibility = Visibility.Collapsed;
			//shell.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
			shell.OpenSettings.Visibility = Visibility.Collapsed;
			NotifyOfPropertyChange(null);
		}

		public void ShowUI()
		{
			var shell = playerWindow as ShellView;
			shell.topMenuPanel.Visibility = Visibility.Visible;
			//shell.controlBar.Visibility = Visibility.Visible;
			//shell.logoImage.Visibility = Visibility.Visible;
			shell.menuRow.Height = new GridLength(22);
			//shell.SelectedFileNameLabel.Visibility = Visibility.Visible;
			//shell.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
			shell.OpenSettings.Visibility = Visibility.Visible;
			NotifyOfPropertyChange(null);
		}

		private void HideBars()
		{
			//Task.Factory.StartNew(() => Execute.OnUIThread(() => {
			Execute.OnUIThreadAsync(() =>
			{
				Storyboard storyboard = new Storyboard();
				double animTime = 0.8;

				GridLengthAnimation heightAnimation = new GridLengthAnimation() { From = shellView.bottomBarRow.Height, To = new GridLength(0), Duration = TimeSpan.FromSeconds(animTime) };
				heightAnimation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
				Storyboard.SetTarget(heightAnimation, shellView.bottomBarRow);
				Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));
				storyboard.Children.Add(heightAnimation);

				DoubleAnimation topHeightAnimation = new DoubleAnimation() { From = shellView.TopBar.Height, To = 0, Duration = TimeSpan.FromSeconds(animTime) };
				topHeightAnimation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
				Storyboard.SetTarget(topHeightAnimation, shellView.TopBar);
				Storyboard.SetTargetProperty(topHeightAnimation, new PropertyPath("Height"));
				storyboard.Children.Add(topHeightAnimation);

				DoubleAnimation opacityAnimatiion = new DoubleAnimation() { From = shellView.SelectedFileNameLabel.Opacity, To = 0, Duration = TimeSpan.FromSeconds(animTime / 2) };
				Storyboard.SetTarget(opacityAnimatiion, shellView.SelectedFileNameLabel);
				Storyboard.SetTargetProperty(opacityAnimatiion, new PropertyPath("Opacity"));
				storyboard.Children.Add(opacityAnimatiion);

				DoubleAnimation opacityProgressAnimation = new DoubleAnimation() { From = shellView.SelectedFileNameLabel.Opacity, To = 0, Duration = TimeSpan.FromSeconds(animTime) };
				Storyboard.SetTarget(opacityProgressAnimation, shellView.VideoProgressBar);
				Storyboard.SetTargetProperty(opacityProgressAnimation, new PropertyPath("Opacity"));
				storyboard.Children.Add(opacityProgressAnimation);

				storyboard.Begin();

			});
			//}));
		}

		private void ShowBars()
		{
			//Task.Factory.StartNew(() => Execute.OnUIThread(() => {
			Execute.OnUIThreadAsync(() =>
			{

				Storyboard storyboard = new Storyboard();
				double animTime = 0.4;

				GridLengthAnimation heightAnimation = new GridLengthAnimation() { From = shellView.bottomBarRow.Height, To = new GridLength(68), Duration = TimeSpan.FromSeconds(animTime) };
				heightAnimation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
				Storyboard.SetTarget(heightAnimation, shellView.bottomBarRow);
				Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));
				storyboard.Children.Add(heightAnimation);

				DoubleAnimation topHeightAnimation = new DoubleAnimation() { From = shellView.TopBar.Height, To = 32, Duration = TimeSpan.FromSeconds(animTime) };
				topHeightAnimation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
				Storyboard.SetTarget(topHeightAnimation, shellView.TopBar);
				Storyboard.SetTargetProperty(topHeightAnimation, new PropertyPath("Height"));
				storyboard.Children.Add(topHeightAnimation);

				DoubleAnimation opacityAnimatiion = new DoubleAnimation() { From = shellView.SelectedFileNameLabel.Opacity, To = 1, Duration = TimeSpan.FromSeconds(animTime * 2) };
				Storyboard.SetTarget(opacityAnimatiion, shellView.SelectedFileNameLabel);
				Storyboard.SetTargetProperty(opacityAnimatiion, new PropertyPath("Opacity"));
				storyboard.Children.Add(opacityAnimatiion);

				DoubleAnimation opacityProgressAnimation = new DoubleAnimation() { From = shellView.SelectedFileNameLabel.Opacity, To = 1, Duration = TimeSpan.FromSeconds(animTime) };
				Storyboard.SetTarget(opacityProgressAnimation, shellView.VideoProgressBar);
				Storyboard.SetTargetProperty(opacityProgressAnimation, new PropertyPath("Opacity"));
				storyboard.Children.Add(opacityProgressAnimation);

				storyboard.Begin();

			});
			//}));
		}

		private void AnimateIndicator(UIElement uiControl)
		{
			Task.Factory.StartNew(() =>
			{
				Thread.Sleep(100);
				Execute.OnUIThreadAsync(() =>
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

		public void ShowPlaybackUI()
		{
			ShowStartupPanel(false);
			ShowDropFilesPanel(false);
			//shellView.VideoTime.Visibility = Visibility.Visible;
		}


		private Storyboard dropFilesPanelDashAnimation;

		public void ShowDropFilesPanel(bool visibility)
		{
			shellView.DropFilesPanel.Visibility = visibility ? Visibility.Visible : Visibility.Collapsed;

			if (visibility)
			{
				if (dropFilesPanelDashAnimation == null)
				{

					dropFilesPanelDashAnimation = new Storyboard();
					double animTime = 5;

					DoubleAnimation offsetAnimation = new DoubleAnimation { From = 0, To = 50, Duration = TimeSpan.FromSeconds(animTime) };
					Storyboard.SetTarget(offsetAnimation, shellView.DropFilesRect);
					Storyboard.SetTargetProperty(offsetAnimation, new PropertyPath("StrokeDashOffset"));
					dropFilesPanelDashAnimation.Children.Add(offsetAnimation);

					dropFilesPanelDashAnimation.RepeatBehavior = RepeatBehavior.Forever;
					dropFilesPanelDashAnimation.Begin(shellView.DropFilesRect, true);
					LoggerManager.Info("Begin storyboard");
				}
				else
				{
					if (dropFilesPanelDashAnimation.GetCurrentState(shellView.DropFilesRect) == ClockState.Stopped)
					{
						LoggerManager.Info("Begin storyboard again");
						dropFilesPanelDashAnimation.Begin(shellView.DropFilesRect, true);
					}
				}
			}
			else
			{
				if (dropFilesPanelDashAnimation != null)
				{
					dropFilesPanelDashAnimation.Stop(shellView.DropFilesRect);
					LoggerManager.Info("Stop storyboard");
				}
			}
		}

		void PlaybackControlUIHitTestVisible(bool hitTestVisible)
		{
			shellView.controlBar.IsHitTestVisible = hitTestVisible;
        }


		public void ShowStartupPanel(bool visibility)
		{
			shellView.StartupPanel.Visibility = visibility ? Visibility.Visible : Visibility.Collapsed;
		}

		public void ShowStartupUI()
		{
            Execute.OnUIThreadAsync(() =>
            {
                shellView.TopBar.Visibility = Visibility.Hidden;
                this.DXCanvas.Visibility = Visibility.Hidden;
                ShowStartupPanel(true);
                ShowDropFilesPanel(false);
                //shellView.VideoTime.Visibility = Visibility.Hidden;
            });			
		}


		public void OpenEULA()
		{

		}

		public void OpenHomePage()
		{
			if (Fullscreen) ToggleFullscreen(true);
			System.Diagnostics.Process.Start("https://tools.bivrost360.com/landing-pages/360PlayerWindows");
        }

        public void OpenSupportPage()
		{
			if (Fullscreen) ToggleFullscreen(true);
            System.Diagnostics.Process.Start("https://tools.bivrost360.com/landing-pages/360PlayerWindows/support");
        }

        public void OpenBuyPage()
		{
			var confirm = Application.Current.Dispatcher.Invoke(() =>
			{
				var decision = MessageBox.Show(
					Application.Current.MainWindow,
					"To gain full access to the 360Player's features and get a commercial license, " +
					"join the BIVROST R&D Program at http://bivrost360.com." +
					"\n\nIf you have bought a copy of 360Player before and your license is still " +
					"valid, please contact support: support@bivrost360.com.",
					"360Player for Windows licensing",
					MessageBoxButton.OK,
					MessageBoxImage.Asterisk,
					MessageBoxResult.OK
				);
				return decision == MessageBoxResult.OK;
			});
		}
		

		public void OpenLicenseManagement()
		{
#if FEATURE_LICENSE_NINJA
			licensingConnector.OpenLicenseManagement();
#else
			OpenBuyPage();
#endif
		}

        public void DisableTabPress(object sender, object e)
		{ 			
			LoggerManager.Info("tabstop");
		}

		public void ChangeFov(System.Windows.Input.MouseWheelEventArgs wheelEventArgs)
		{
			ChangeFovDeg(Math.Sign(wheelEventArgs.Delta) * -5f);
		}

		// Caliburn/WPF bindings don't support function overload?
		public void ChangeFovDeg(float deg)
		{
			if (IsPlaying)
			{
				if (this.DXCanvas.Scene != null)
				{
					Scene scene = ((Scene)this.DXCanvas.Scene);
					scene.ChangeFov(deg);
				}
			}
		}

		// Using arguments changed -10 to 0?
		public void ZoomIn() { ChangeFovDeg(-10); }
		public void ZoomOut() { ChangeFovDeg(10); }


		public void ResetFov()
		{
			if (IsPlaying)
			{
				if (this.DXCanvas.Scene != null)
				{
					Scene scene = ((Scene)this.DXCanvas.Scene);
					scene.ResetFov();
				}
			}
		}

		public void ChangeFovZoomIn()
		{
			ChangeFov(new System.Windows.Input.MouseWheelEventArgs(null, 0, 120));
		}

		public void ChangeFovZoomOut()
		{
			ChangeFov(new System.Windows.Input.MouseWheelEventArgs(null, 0, -120));
        }

		public void Dummy(RoutedEventArgs e)
		{
			e.Handled = true;
		}

		public Visibility DebugVisible { get; set; } = Visibility.Collapsed;
		public void ShowDebug()
		{
			Execute.OnUIThreadAsync(() =>
			{
				DebugVisible = Visibility.Visible;
				NotifyOfPropertyChange(() => DebugVisible);
			});
		}

		public void HideDebug()
		{
			Execute.OnUIThreadAsync(() =>
			{
				DebugVisible = Visibility.Collapsed;
				NotifyOfPropertyChange(() => DebugVisible);
			});
		}


#region debug text
		public string DebugText { get; set; } = "";

		public void AppendDebugText(string line)
		{
			DebugText += line + "\n";
		}

		public void ClearDebugText()
		{
			DebugText = "";
		}

		public void UpdateDebugText()
		{
			Execute.OnUIThreadAsync(() => NotifyOfPropertyChange(() => DebugText));
		}
#endregion


		public void NormalProjection()
		{
			if (this.DXCanvas.Scene != null)
			{
				((Scene)this.DXCanvas.Scene).RectlinearProjection();
			}
		}

		public void LittlePlanetProjection()
		{
			if(this.DXCanvas.Scene != null)
			{
				((Scene)this.DXCanvas.Scene).StereographicProjection();
			}
        }

        public void SeekRelative(int v)
        {
            if(_mediaDecoder != null)
            {
                if(_mediaDecoder.Initialized && _mediaDecoder.Ready)
                {
                    _mediaDecoder.Seek(_mediaDecoder.CurrentPosition + v);
                }
            }
        }


    }
}
