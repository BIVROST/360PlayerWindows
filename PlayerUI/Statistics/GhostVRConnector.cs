using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerUI.Statistics
{


    public sealed class AsyncStateMachine
    {

        public class Trigger
        {
            protected AsyncStateMachine asm;
            protected int iteration = -1;

            internal Trigger(AsyncStateMachine asm)
            {
                this.asm = asm;
            }

            public bool IsActive {
                get {
                    return asm.currentIteration == iteration;
                }
            }

            public bool Use() {
                Console.WriteLine("USE ACTIVE: " + asm.currentIteration + ((asm.currentIteration == iteration) ? "==" : "!=") + iteration);
                if (!IsActive)
                    return false;
                Clear();
                return true;
            }

            public void Clear()
            {
                iteration = -1;
            }

            virtual public void Activate()
            {
                iteration = asm.currentIteration;
                asm.Iterate();
            }
        }

        public class WaitTrigger : Trigger
        {
            private Thread waitThread;

            public WaitTrigger(AsyncStateMachine asm) : base(asm)
            {
            }

            // TODO: manual reset event 
            // http://stackoverflow.com/questions/5793177/how-to-abort-a-thread-when-it-is-sleeping

            public void Reset(float seconds)
            {
                Cancel();
                iteration = asm.currentIteration;
                waitThread = new Thread(() =>
                {
                    Console.WriteLine("SLEEP START");
                    Thread.Sleep(TimeSpan.FromSeconds(seconds));
                    Console.WriteLine($"SLEEP END iter={iteration}, asm.iter={asm.currentIteration}");
                    if (asm.currentIteration == iteration)
                        asm.Iterate();
                })
                { IsBackground = true, Name = "WaitTrigger thread" };
                waitThread.Start();
            }

            public override void Activate()
            {
                throw new NotImplementedException("Use reset");
            }

            public void Cancel()
            {
                if (waitThread != null && waitThread.IsAlive)
                    waitThread.Abort();
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
        public WaitTrigger CreateWaitTrigger()
        {
            return new WaitTrigger(this);
        }

        private object syncRoot = new object();
        private void Iterate()
        {
            lock (syncRoot)
            {
                sm.MoveNext();
                Console.WriteLine("ASM:" + currentIteration + "->" + (currentIteration + 1));
                currentIteration++;
            }
        }

    }


    public class GhostVRConnector
    {
        private AsyncStateMachine sm;
        private AsyncStateMachine.Trigger uiConnectTrigger;
        private AsyncStateMachine.Trigger uiCancelTrigger;
        private AsyncStateMachine.Trigger uiDisconnectTrigger;
        private AsyncStateMachine.Trigger tokenVerifiedTrigger;
        private AsyncStateMachine.Trigger tokenVerificationFailedTrigger;
        private AsyncStateMachine.Trigger tokenVerificationPendingOrErrorTrigger;
        private AsyncStateMachine.WaitTrigger waitTrigger;

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
            tokenVerifiedTrigger = sm.CreateTrigger();
            tokenVerificationFailedTrigger = sm.CreateTrigger();
            tokenVerificationPendingOrErrorTrigger = sm.CreateTrigger();
            waitTrigger = sm.CreateWaitTrigger();
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
                yield return null;
            }

            connecting:
            Token = Guid.NewGuid();
            OpenConnectionPageInBrowser();
            goto pending;

            pending:
            status = Status.pending;
            VerifyToken();
            while (true)
            {
                if (tokenVerifiedTrigger.Use())
                    goto verified;
                if (tokenVerificationFailedTrigger.Use())
                    goto connecting_failed;
                if (tokenVerificationPendingOrErrorTrigger.Use())
                    goto pending_wait;
                if (uiCancelTrigger.Use())
                    goto cancel_pending;
                yield return null;
            }


            pending_wait:
            waitTrigger.Reset(20.0f);
            while(true)
            {
                if (waitTrigger.Use())
                    goto pending;
                if (uiCancelTrigger.Use())
                    goto cancel_pending;
                yield return null;
            }

            cancel_pending:

            connecting_failed:

            verified:

            verify:

            verifying_failed:

            connected:

            disconnect:

            yield return null;
        }

        private void VerifyToken()
        {
            throw new NotImplementedException();
            // should trigger tokenVerifiedTrigger or tokenVerificationFailedTrigger
        }

        private void OpenConnectionPageInBrowser()
        {
            throw new NotImplementedException();
        }
    }
}
