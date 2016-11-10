using Bivrost.Log;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class Updater
	{
		static UpdateCheckInfo info = null;
		public static Action OnUpdateSuccess = null;
		public static Action OnUpdateFail = null;

		public static bool CheckForUpdate()
		{
			if (!System.Diagnostics.Debugger.IsAttached && ApplicationDeployment.IsNetworkDeployed)
			{
				ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

				try
				{
					info = ad.CheckForDetailedUpdate();

				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Update check failed");
					return false;
				}

				if (info.UpdateAvailable)
					return true;
			}
			return false;
		}

		public static void InstallUpdate()
		{
			if (ApplicationDeployment.IsNetworkDeployed)
			{
				ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
				if (info.UpdateAvailable)
				{
					try
					{
						ad.Update();
						if (OnUpdateSuccess != null)
						{
							OnUpdateSuccess();
							OnUpdateFail = null;
							OnUpdateSuccess = null;
						}
					}
					catch (DeploymentDownloadException dde)
					{
						Logger.Error(dde, "Update deployement failed");
						if (OnUpdateFail != null) {
							OnUpdateFail();
							OnUpdateFail = null;
							OnUpdateSuccess = null;
                        }						
					}
				}
			}
			
		}
	}
}
