using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Statistics
{


    public sealed class AsyncStateMachine
    {

        public class Trigger
        {
            private AsyncStateMachine asm;
            private int iteration = -1;

            internal Trigger(AsyncStateMachine asm)
            {
                this.asm = asm;
            }

            public bool IsActive {
                get { return asm.currentIteration == iteration; }
            }

            public bool Use() {
                if (!IsActive)
                    return false;
                Clear();
                return true;
            }

            public void Clear()
            {
                iteration = -1;
            }

            public void Activate()
            {
                iteration = asm.currentIteration;
                asm.Iterate();
            }
        }

        private int currentIteration = 0;
        private IEnumerator<object> sm;

        public AsyncStateMachine(Func<IEnumerable<object>> stateMachine)
        {
            sm = stateMachine().GetEnumerator();
        }


        public AsyncStateMachine(IEnumerator<object> stateMachineEnumerator)
        {
            sm = stateMachineEnumerator;
        }

        public Trigger CreateTrigger()
        {
            return new Trigger(this);
        }

        private void Iterate()
        {
            sm.MoveNext();
            currentIteration++;
        }




    }


    public class GhostVRConnector
    {
        private AsyncStateMachine sm;
        private AsyncStateMachine.Trigger uiConnectTrigger;
        private AsyncStateMachine.Trigger uiCancelTrigger;
        private AsyncStateMachine.Trigger uiDisconnectTrigger;

        public enum Status { pending, connected, disconnected };

        protected Status status { get; set; } = Status.disconnected;
        protected Guid? Token { get; set; } = null;
        protected string Name { get; set; } = null;


        public void Disconnect() { uiDisconnectTrigger.Activate(); }
        public void Connect() { uiConnectTrigger.Activate(); }
        public void Cancel() { uiCancelTrigger.Activate();  }


        public class PlayerDetails
        {

            public readonly string name = "BIVROST 360Player";
            //public enum LicenseType { debug, unregistered, commercial, canary };
            public string version;
            //public LicenseType licenseType;

            static public PlayerDetails Current {
                get {
                    //**@license_type@:enum(debug, commercial, canary, unregistered) - grupa licencji z której pochodzi player.@debug@ - build developerski, @canary@ - build developerski rozpowszechniony do testów, @commercial@ - build z prawem
                    //LicenseType licenseType;
                    //if (Features.IsDebug)
                    //    licenseType = LicenseType.debug;
                    //else if (Features.IsCanary)
                    //    licenseType = LicenseType.canary;
                    //else if (Features.Commercial)
                    //    licenseType = LicenseType.commercial;
                    //else
                    //    licenseType = LicenseType.unregistered;
                    return new PlayerDetails()
                    {
                        version = Tools.PublishInfo.ApplicationIdentity?.Version?.ToString()
                        //licenseType = licenseType
                    };
                }
            }

            public string AsQsFormat
            {
                get
                {
                    // https://www.npmjs.com/package/qs

                    //                     player%5Bname%5D=Bivrost%20360%20Player&player%5Bversion%5D=1.2.3'
                    //Edit
                    //player_details ={ name: "Bivrost 360 Player", version: "1.2.3" }
                    //                    Zostanie zakodowany jako:

                    //                    player % 5Bname % 5D = Bivrost % 20360 % 20Player & player % 5Bversion % 5D = 1.2.3
                    //Przykład w środowisku node:

                    //> require('qs').stringify({ player: { name: "Bivrost 360 Player", version: "1.2.3" } })
                    //'player%5Bname%5D=Bivrost%20360%20Player&player%5Bversion%5D=1.2.3'
                    return string.Join("&",
                        Uri.EscapeDataString("player[name]") + "=" + Uri.EscapeDataString(name),
                        Uri.EscapeDataString("player[version]") + "=" + Uri.EscapeDataString(version ?? "")
                        //Uri.EscapeDataString("player[license_type]") + "=" + Uri.EscapeDataString(licenseType.ToString())
                    );


                }
            }
        }

        
        public void authorizeToken()
        {

        }


        public GhostVRConnector()
        {
            sm = new AsyncStateMachine(StateMachine);
            uiConnectTrigger = sm.CreateTrigger();
            uiCancelTrigger = sm.CreateTrigger();
            uiDisconnectTrigger = sm.CreateTrigger();
        }

        protected IEnumerable<object> StateMachine()
        {
            goto init;

            init:
            switch (status)
            {
                case Status.connected: goto verify;
                case Status.disconnected: goto disconnected;
                case Status.pending: goto pending;
            }


            disconnected:
            Token = null;
            Name = null;
            status = Status.disconnected;
            while(true)
            {
                if (uiConnectTrigger.Use())
                    goto connecting;
            }

            connecting:
            Token = Guid.NewGuid();
            OpenConnectionPageInBrowser();
            goto pending;

            pending:
            status = Status.pending;
            VerifyToken(() => goto pending_wait;);

            pending_wait:

            cancel_pending:

            connecting_failed:

            verified:

            verify:

            verifying_failed:

            connected:

            disconnect:

            yield return null;
        }
    }
}
