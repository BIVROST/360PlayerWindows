using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayerUI.Statistics;
using static PlayerUI.Statistics.AsyncStateMachine;

namespace PlayerUI.Test
{

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
    }
}
