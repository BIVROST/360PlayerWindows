using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayerUI.Statistics;
using static PlayerUI.Statistics.AsyncStateMachine;
using System.Threading;

namespace PlayerUI.Test
{

    public class WaitingStateMachine
    {
        public bool isDone;
        private AsyncStateMachine sm;
        public WaitTrigger waitTrigger;
        public Trigger standardTrigger;

        public enum EndState { not_ended, ended_standard, ended_wait }
        public EndState endState = EndState.not_ended;

        public WaitingStateMachine()
        {
            sm = new AsyncStateMachine(StateMachine);
            waitTrigger = sm.CreateWaitTrigger();
            standardTrigger = sm.CreateTrigger();
        }

        private IEnumerable<object> StateMachine()
        {
            goto waiting;

            waiting:
            while(true)
            {
                if (waitTrigger.Use())
                    goto wait_done;
                yield return null;
            }

            wait_done:
            isDone = true;
            while(true)
            {
                if (standardTrigger.Use())
                    goto standard_used;
                if (waitTrigger.Use())
                    goto wait_used;
                yield return null;
            }

            standard_used:
            endState = EndState.ended_standard;
            goto end;

            wait_used:
            endState = EndState.ended_wait;
            goto end;

            end:
            ;
        }
    }


    public class WaitingStateMachine2
    {
        public int timesTriggered;
        private AsyncStateMachine sm;
        public WaitTrigger waitTrigger0;
        public WaitTrigger waitTrigger1;
        public WaitTrigger waitTrigger2;
        public WaitTrigger waitTrigger3;
        public WaitTrigger waitTrigger4;
        public WaitTrigger waitTrigger5;
        public WaitTrigger waitTrigger6;


        public WaitingStateMachine2()
        {
            sm = new AsyncStateMachine(StateMachine);
            waitTrigger0 = sm.CreateWaitTrigger();
            waitTrigger1 = sm.CreateWaitTrigger();
            waitTrigger2 = sm.CreateWaitTrigger();
            waitTrigger3 = sm.CreateWaitTrigger();
            waitTrigger4 = sm.CreateWaitTrigger();
            waitTrigger5 = sm.CreateWaitTrigger();
            waitTrigger6 = sm.CreateWaitTrigger();
        }

        private IEnumerable<object> StateMachine()
        {
            goto waiting;

            waiting:
            while (true)
            {
                // warning: OR operator must not short cicruit
                if (waitTrigger0.Use()
                    | waitTrigger1.Use()
                    | waitTrigger2.Use()
                    | waitTrigger3.Use()
                    | waitTrigger4.Use()
                    | waitTrigger5.Use()
                    | waitTrigger6.Use()
                )
                    goto waited;
                yield return null;
            }

            waited:
            timesTriggered++;
            goto waiting;

        }
    }


    public class TestableStateMachine
    {

        public int beeps;
        public bool isOn;

        public Trigger onOffTrigger;
        public Trigger beepTrigger;
        private AsyncStateMachine sm;

        public TestableStateMachine()
        {
            sm = new AsyncStateMachine(StateMachine);
            beepTrigger = sm.CreateTrigger();
            onOffTrigger = sm.CreateTrigger();
        }

        protected IEnumerable<object> StateMachine()
        {
            isOn = false;
            beeps = 0;
            goto off;

            off:
            while(true)
            {
                if (onOffTrigger.Use())
                    goto activate;
                yield return null;
            }

            activate:
            isOn = true;
            goto on;

            beep:
            beeps++;
            goto on;

            on:
            while (true)
            {
                if (onOffTrigger.Use())
                    goto deactivate;
                if (beepTrigger.Use())
                    goto beep;
                yield return null;
            }

            deactivate:
            isOn = false;
            goto off;
        }
    }

    [TestClass]
    public class StateMachineTest
    {
        [TestMethod]
        public void TestStateMachine()
        {
            TestableStateMachine asm = new TestableStateMachine();
            Assert.AreEqual(0, asm.beeps);
            Assert.AreEqual(false, asm.isOn);

            asm.beepTrigger.Activate();
            Assert.AreEqual(0, asm.beeps);
            Assert.AreEqual(false, asm.isOn);

            asm.onOffTrigger.Activate();
            Assert.AreEqual(0, asm.beeps);
            Assert.AreEqual(true, asm.isOn);

            asm.beepTrigger.Activate();
            Assert.AreEqual(1, asm.beeps);
            Assert.AreEqual(true, asm.isOn);

            asm.beepTrigger.Activate();
            asm.beepTrigger.Activate();
            Assert.AreEqual(3, asm.beeps);
            Assert.AreEqual(true, asm.isOn);

            asm.onOffTrigger.Activate();
            Assert.AreEqual(3, asm.beeps);
            Assert.AreEqual(false, asm.isOn);

            asm.beepTrigger.Activate();
            Assert.AreEqual(3, asm.beeps);
            Assert.AreEqual(false, asm.isOn);

            asm.onOffTrigger.Activate();
            Assert.AreEqual(3, asm.beeps);
            Assert.AreEqual(true, asm.isOn);
        }

        [TestMethod]
        public void TestWaitTriggerWorks()
        {
            WaitingStateMachine wsm = new WaitingStateMachine();
            wsm.standardTrigger.Activate(); // NO-OP

            Assert.AreEqual(false, wsm.isDone);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.AreEqual(false, wsm.isDone);
            wsm.waitTrigger.Reset(1);
            Assert.AreEqual(false, wsm.isDone);
            Thread.Sleep(TimeSpan.FromSeconds(0.5f));
            Assert.AreEqual(false, wsm.isDone);
            Thread.Sleep(TimeSpan.FromSeconds(1f));
            Assert.AreEqual(true, wsm.isDone);

            wsm.waitTrigger.Reset(1);
            wsm.standardTrigger.Activate();

            Assert.AreEqual(true, wsm.isDone);
            Assert.AreEqual(WaitingStateMachine.EndState.ended_standard, wsm.endState);
        }


        [TestMethod]
        public void TestResettingWaitTriggerWorks()
        {
            var wsm = new WaitingStateMachine();
            wsm.waitTrigger.Reset(0.01f);
            Thread.Sleep(TimeSpan.FromSeconds(0.1f));
            Assert.AreEqual(true, wsm.isDone);
            Assert.AreEqual(WaitingStateMachine.EndState.not_ended, wsm.endState);

            wsm.waitTrigger.Reset(1);
            Thread.Sleep(TimeSpan.FromSeconds(0.5f));
            wsm.waitTrigger.Cancel();
            Thread.Sleep(TimeSpan.FromSeconds(1f));
            Assert.AreEqual(WaitingStateMachine.EndState.not_ended, wsm.endState);
            wsm.standardTrigger.Activate();
            Assert.AreEqual(WaitingStateMachine.EndState.ended_standard, wsm.endState);
        }

        [TestMethod]
        public void TestSingleTriggerWaitTriggerIterationWorks()
        {
            var wsm2 = new WaitingStateMachine2();
            Assert.AreEqual(0, wsm2.timesTriggered);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            Assert.AreEqual(0, wsm2.timesTriggered);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1, wsm2.timesTriggered);

            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger0.Reset(0.1f);
            Assert.AreEqual(1, wsm2.timesTriggered);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.AreEqual(2, wsm2.timesTriggered);
        }

        [TestMethod]
        public void TestMultipleTriggerWaitTriggerIterationWorks()
        {
            var wsm2 = new WaitingStateMachine2();
            Assert.AreEqual(0, wsm2.timesTriggered);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger1.Reset(0.2f);
            wsm2.waitTrigger2.Reset(0.3f);
            wsm2.waitTrigger3.Reset(0.4f);
            wsm2.waitTrigger4.Reset(0.5f);
            wsm2.waitTrigger5.Reset(0.6f);
            wsm2.waitTrigger6.Reset(0.7f);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1, wsm2.timesTriggered);
            wsm2.waitTrigger0.Reset(0.1f);
            wsm2.waitTrigger1.Reset(0.1f);
            wsm2.waitTrigger2.Reset(0.1f);
            wsm2.waitTrigger3.Reset(0.1f);
            wsm2.waitTrigger4.Reset(0.1f);
            wsm2.waitTrigger5.Reset(0.1f);
            wsm2.waitTrigger6.Reset(0.1f);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.AreEqual(2, wsm2.timesTriggered);
        }

    }
}
