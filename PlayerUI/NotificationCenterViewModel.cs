using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class NotificationCenterViewModel : Screen
	{
		public NotificationCenterViewModel()
		{

		}
		
		public BindableCollection<NotificationViewModel> Notifications { get; set; } = new BindableCollection<NotificationViewModel>();

		public void PushNotification(NotificationViewModel notification)
		{
			notification.OnClose += (n) =>
			{
				RemoveNotification(n);
			};
			Notifications.Add(notification);
            ShellViewModel.SendEvent("notification", notification.Message + " :: " + notification.ActionLabel);
        }

		public void RemoveNotification(NotificationViewModel notification)
		{
			Notifications.Remove(notification);
		}
	}
}
