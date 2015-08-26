﻿using Caliburn.Micro;
using PlayerUI.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace PlayerUI
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
			shell.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
			shell.OpenSettings.Visibility = Visibility.Collapsed;
			NotifyOfPropertyChange(null);
		}

		public void ShowUI()
		{
			var shell = playerWindow as ShellView;
			shell.topMenuPanel.Visibility = Visibility.Visible;
			shell.controlBar.Visibility = Visibility.Visible;
			//shell.logoImage.Visibility = Visibility.Visible;
			shell.menuRow.Height = new GridLength(22);
			//shell.SelectedFileNameLabel.Visibility = Visibility.Visible;
			shell.mainGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
			shell.OpenSettings.Visibility = Visibility.Visible;
			NotifyOfPropertyChange(null);
		}

		private void HideBars()
		{
			Task.Factory.StartNew(() => Execute.OnUIThread(() => {

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

			}));
		}

		private void ShowBars()
		{
			Task.Factory.StartNew(() => Execute.OnUIThread(() => {

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

			}));
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

		public void ShowPlaybackUI()
		{
			ShowStartupPanel(false);
			ShowDropFilesPanel(false);
			shellView.VideoTime.Visibility = Visibility.Visible;
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
					Console.WriteLine("Begin storyboard");
				}
				else
				{
					if (dropFilesPanelDashAnimation.GetCurrentState(shellView.DropFilesRect) == ClockState.Stopped)
					{
						Console.WriteLine("Begin storyboard again");
						dropFilesPanelDashAnimation.Begin(shellView.DropFilesRect, true);
					}
				}
			}
			else
			{
				if (dropFilesPanelDashAnimation != null)
				{
					dropFilesPanelDashAnimation.Stop(shellView.DropFilesRect);
					Console.WriteLine("Stop storyboard");
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
			ShowStartupPanel(true);
			ShowDropFilesPanel(false);
			shellView.VideoTime.Visibility = Visibility.Hidden;
		}


		public void OpenEULA()
		{

		}

		public void OpenHomePage()
		{
			if (fullscreen) ToggleFullscreen();
			System.Diagnostics.Process.Start("http://www.bivrost360.com");
		}

		public void OpenSupportPage()
		{
			if (fullscreen) ToggleFullscreen();
			System.Diagnostics.Process.Start("http://www.bivrost360.com");
		}

		public void DisableTabPress(object sender, object e)
		{ 			
			Console.WriteLine("tabstop");
		}
    }
}
