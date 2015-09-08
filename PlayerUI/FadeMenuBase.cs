using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PlayerUI
{
	public abstract class FadeMenuBase : Screen
	{
		private Control view;
		private DateTime lastMouse;
		private BackgroundWorker visibilityWorker;
		private bool visible = false;

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);
			this.view = view as Control;
			this.view.Opacity = 0;
			this.view.IsHitTestVisible = false;
			this.view.MouseMove += MouseMove;
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
			this.view.MouseMove -= MouseMove;
		}

		public void Show()
		{
			lastMouse = DateTime.Now;

			if (!visible)
			{
				visible = true;
				Execute.OnUIThread(() => view.IsHitTestVisible = true);
				Animate(true);

				visibilityWorker = new BackgroundWorker();
				visibilityWorker.DoWork += (sender, parameters) =>
				{
					while (visible)
					{

						if ((DateTime.Now - lastMouse).TotalSeconds > 2)
						{
							Execute.OnUIThread(() => Hide());
						}
						else
							Thread.Sleep(100);
					}
				};
				visibilityWorker.RunWorkerAsync();
			}
		}

		public void Hide()
		{
			Hide(0.5f);
		}

		public void Hide(float duration)
		{
			if (visible)
			{
				visible = false;
				view.IsHitTestVisible = false;
				Animate(false, duration);
			}
		}

		public void ToggleVisibility()
		{
			if (visible) Hide();
			else Show();
		}

		private void Animate(bool visible, float duration = 0.5f)
		{
			Storyboard storyboard = new Storyboard();
			DoubleAnimation opacityAnimation = new DoubleAnimation() { From = view.Opacity, To = visible ? 1 : 0, Duration = TimeSpan.FromSeconds(duration) };
			Storyboard.SetTarget(opacityAnimation, view);
			Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
			storyboard.Children.Add(opacityAnimation);
			storyboard.Begin();
		}

		public void MouseMove(object sender, MouseEventArgs eventArgs)
		{
			lastMouse = DateTime.Now;
		}

	}
}
