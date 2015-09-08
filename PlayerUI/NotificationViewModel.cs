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
	public class NotificationViewModel : Screen
	{
		public event Action<NotificationViewModel> OnClose = delegate { };
		private float _timeout = 5f;
		private float _currentTime = 5f;
		private BackgroundWorker _closeOnTimeoutWorker;
		private NotificationView _view;
		private bool _animatedClosing = false;
		private System.Action _userAction = null;
		private bool _isAction = false;

		public NotificationViewModel(string message, string url = "", float timeout = 5f)
		{
			Message = message;
			Url = url;
			ActionLabel = "read more";
			this._timeout = timeout;
			this._currentTime = _timeout;

			_closeOnTimeoutWorker = new BackgroundWorker();
			_closeOnTimeoutWorker.DoWork += (s,e) =>
			{
				while(_currentTime > 0)
				{
					_currentTime -= 0.1f;
					Thread.Sleep(100);
				}
			};
			_closeOnTimeoutWorker.RunWorkerCompleted += (s, e) => AnimateClose();
			_closeOnTimeoutWorker.RunWorkerAsync();
		}

		public NotificationViewModel(string message, System.Action userAction, string actionLabel = "", float timeout = 10f)
		{
			_isAction = true;
			Message = message;
			Url = "";
			ActionLabel = actionLabel;
			this._timeout = timeout;
			this._currentTime = _timeout;
			this._userAction = userAction;

			_closeOnTimeoutWorker = new BackgroundWorker();
			_closeOnTimeoutWorker.DoWork += (s, e) =>
			{
				while (_currentTime > 0)
				{
					_currentTime -= 0.1f;
					Thread.Sleep(100);
				}
			};
			_closeOnTimeoutWorker.RunWorkerCompleted += (s, e) => AnimateClose();
			_closeOnTimeoutWorker.RunWorkerAsync();
		}

		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);
			_view = view as NotificationView;
		}

		public string Message { get; set; }
		public string Url { get; set; }
		public string ActionLabel { get; set; }

		public Visibility MoreVisible { get {
				if (_isAction)
				{
					if (!string.IsNullOrWhiteSpace(ActionLabel)) return Visibility.Visible;
				}
				
				return string.IsNullOrWhiteSpace(Url) ? Visibility.Collapsed : Visibility.Visible;
			} }

		public void OpenUrl(RoutedEventArgs e)
		{
			e.Handled = true;
			if(!string.IsNullOrWhiteSpace(Url))
			{
				System.Diagnostics.Process.Start(Url);
			}
			if (_userAction != null)
			{	
				_userAction();
				Close();
			}
		}

		public void Close()
		{
			if (_animatedClosing) return;
			OnClose(this);
		}

		public void AnimateClose()
		{
			Execute.OnUIThread(() =>
			{
				Storyboard storyboard = new Storyboard();
				DoubleAnimation opacityAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.5f)));
				opacityAnimation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
				Storyboard.SetTarget(opacityAnimation, _view);
				Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
				storyboard.Children.Add(opacityAnimation);
				storyboard.Completed += (s, e) => Execute.OnUIThread(() => OnClose(this));
				storyboard.Begin();
			});
		}

		public void ResetTimeout(MouseEventArgs e)
		{
			_currentTime = _timeout;
			e.Handled = true;
		}


	}
}
