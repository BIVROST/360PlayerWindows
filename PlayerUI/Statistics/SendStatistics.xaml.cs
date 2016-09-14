using Caliburn.Micro;
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using static Bivrost.Log.Logger;

namespace PlayerUI.Statistics
{
    /// <summary>
    /// Interaction logic for SendStatistics.xaml
    /// </summary>
    public partial class SendStatistics : Window
    {

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        [ComVisible(true)]
        public class ScriptingHelper
        {
            public event System.Action OnCanceled;
            public event System.Action OnCompleted;

            public void ActionCanceled() { OnCanceled?.Invoke(); }
            public void ActionCompleted() { OnCompleted?.Invoke(); }
        }


        protected SendStatistics(Session session)
        {
            InitializeComponent();
            cancel.Click += (s, e) => Close();
            browser.Navigate("http://tools.bivrost360.com/heatmap-viewer/event-test.html");
            ScriptingHelper helper = new ScriptingHelper();
            //browser.ContextMenu = false;
            browser.ObjectForScripting = helper;
            helper.OnCanceled += Helper_OnCanceled;
            helper.OnCompleted += Helper_OnCompleted;
        }

        public static void Send(Session session)
        {
            Execute.OnUIThreadAsync(() =>
            {
                var ss = new SendStatistics(session);
                ss.ShowDialog();
            });
        }


        private void Helper_OnCompleted()
        {
            MessageBox.Show("completed");
            Close();
        }

        private void Helper_OnCanceled()
        {
            MessageBox.Show("canceled");
            Close();
        }
    }
}
