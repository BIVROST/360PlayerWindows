using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PlayerUI
{
	public class VolumeControlViewModel : Screen
	{
		private VolumeControlView view;
		private IInputElement _element;
		private bool isDragging = false;
		private DateTime lastMouse;
		private BackgroundWorker visibilityWorker;
		private bool visible = false;
		private bool mute = false;

		public event Action<double> OnVolumeChange = delegate { };

		public VolumeControlViewModel()
		{
			Volume = 1;
			lastMouse = DateTime.Now;
		}

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);
			this.view = view as VolumeControlView;
			this.view.Opacity = 0;
			this.view.IsHitTestVisible = false;
		}

		private double _volume = 0.5;
		public double Volume {
			get
			{
				return mute ? 0 : this._volume;
			}
			set
			{
				this._volume = value;
				NotifyOfPropertyChange(() => Volume);
				OnVolumeChange(Volume);
			}
		}

		public bool IsMuted { get { return this.mute; } }

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
			if (visible)
			{
				visible = false;
				view.IsHitTestVisible = false;
				Animate(false);
			}
		}

		private void Animate(bool visible)
		{
			Storyboard storyboard = new Storyboard();
			DoubleAnimation opacityAnimation = new DoubleAnimation() { From = view.Opacity, To = visible ? 1 : 0, Duration = TimeSpan.FromSeconds(0.5) };
			Storyboard.SetTarget(opacityAnimation, view);
			Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
			storyboard.Children.Add(opacityAnimation);
			storyboard.Begin();
		}

		public void MouseWheel(MouseWheelEventArgs eventArgs)
		{
			double vol = Volume + Math.Sign(eventArgs.Delta) * 0.05f;
			vol = Math.Max(0, Math.Min(1, vol));
			if(!IsMuted)
				Volume = vol;
			lastMouse = DateTime.Now;
		}

		public void MouseDown(object sender, MouseButtonEventArgs eventArgs)
		{
			PositionToVolume(eventArgs.GetPosition(view).Y);

			_element = (IInputElement)eventArgs.Source;
			_element.CaptureMouse();
			_element.MouseMove += MouseMove;

			isDragging = true;
			eventArgs.Handled = true;
		}

		public void ToggleMute()
		{
			mute = !mute;
			OnVolumeChange(Volume);
			NotifyOfPropertyChange(() => Volume);
		}

		private void PositionToVolume(double position)
		{
			//54 / 86
			double ratio = view.ActualHeight / 86;
			double margin = (86 - 54) * ratio * 0.5;
			double vol = ((86 * ratio - position - margin) / (54 * ratio));
			vol = Math.Max(0, Math.Min(1, vol));
			Volume = vol;
		}

		public void MouseMove(object sender, MouseEventArgs eventArgs)
		{
			if (isDragging)
			{
				PositionToVolume(eventArgs.GetPosition(view).Y);
			}
			lastMouse = DateTime.Now;
		}

		public void MouseUp(object sender, MouseButtonEventArgs eventArgs)
		{
			if (isDragging && _element != null)
			{
				_element.ReleaseMouseCapture();
				isDragging = false;
			}
		}

	}
}
