using System;
using System.Collections.Generic;


namespace ChanibaL
{

    /// <summary>
    /// Utility class to implement a state machine, example implementation:
    /// 
    /// 1. initiate a object property sm=new StateMachine(StateExample) in Start()
    /// 
    /// 2. run sm.Update() in Update()
    /// 
    /// 3. states as methods in the main class, example:
    /// 
    /// 	void StateExample() {
    /// 		// EnterState should to be the first part of logic in a state method
    /// 		// it is run if this is the first time this state is run after a SwitchState
    ///         // or initialisation of the state machine
    /// 		if(sm.EnterState)
    /// 			init();
    /// 
    /// 		SomeAction();
    /// 
    /// 		// state machine stores deltaTime, so on state 
    /// 		// switch it doesn't add time in the same frame
    /// 		someValue+=sm.DeltaTime*3;	
    /// 
    /// 		// inheritance works as it should
    /// 		base.StateExample();
    /// 		
    /// 		// the state machine keeps time and other properties
    /// 		if(sm.TimeInState > 4 || sm.FramesInState > 1000)
    /// 			sm.SwitchState(StateTimeoutExample);
    /// 
    /// 		if(someFlag)
    /// 			sm.SwitchState(StateAnotherExample);
    /// 			// I suggest not adding any additional logic here, just a state switch
    ///             // add the logic to the next state or add an intermediary state - it makes the code much cleaner
    /// 
    /// 		// ExitState has to be after every SwitchState to work
    ///         // it is run if a state has been switched
    /// 		if(sm.ExitState)
    /// 			smth();
    /// 	}
    /// 
    /// 4. optionaly, closures are supported:
    /// 
    /// 	StateMachine.State StateClosureExample(int i) {
    /// 		int someExpensiveToComputeValue=43-1;
    /// 		return () => {
    /// 			if(sm.EnterState)
    /// 				Debug.Log(i);
    /// 			Debug.Log(someExpensiveToComputeValue);
    /// 		};
    /// 	}
    /// 
    /// </summary>
    public class StateMachine
    {

        /// <summary>
        /// A state is a void delegate, it should keep a reference to it's state machine by other means.
        /// </summary>
        public delegate void State();


        static int maxId = 0;
        int id;


		/// <summary>
		/// Initializes a new instance of the <see cref="StateMachine"/> class.
		/// </summary>
		/// <param name='initState'>
		/// Initial state
		/// </param>
		/// <param name='log'>
		/// Should the state changes of this state machine be logged to the console?
		/// </param>
		/// <param name="logDelegate">
		/// Enable logging of this SM to a custom logging output, null for none
		/// </summary>

		/// </param>
		public StateMachine(State initState, LogDelegate logDelegate = null)
        {
			this.logDelegate = logDelegate;
            id = ++maxId;
            CurrentState = initState;
            FramesInState = 0;
            TimeInState = 0;
            EnterState = true;
            ExitState = false;
            Ended = false;
            IsPushedState = false;
            Ticks = 0;
			LogDebug(this + "init");
        }


        /// <summary>
        /// Current machine state
        /// </summary>
        public State CurrentState { get; protected set; }


        /// <summary>
        /// Is this the first time a state is run after a SwitchState or initial run?
        /// </summary>
        public bool EnterState { get; protected set; }


        /// <summary>
        /// Is this the last time this state is run (ie. was SwitchState used)?
        /// </summary>
        public bool ExitState { get; protected set; }


        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Vizao.StateMachine"/> has ended.
        /// </summary>
        /// <value><c>true</c> if ended; otherwise, <c>false</c>.</value>
        public bool Ended { get; protected set; }


        /// <summary>
        /// Check if one of the states of this SM is currently executing
        /// </summary>
        /// <value><c>true</c> if one of the states is executing, <c>false</c> otherwise.</value>
        public bool CurrentlyExecuting { get; protected set; }


		/// <summary>
		/// An object that may will be locked when the SM updates. Can be used for external synchronization.
		/// </summary>
		public readonly object CurrentlyExecutingSyncRoot = new object();


        /// <summary>
        /// State machine stores deltaTime, so on state switch it doesn't add time in the same frame
        /// </summary>
        /// <value>
        /// Value in seconds, same as Time.deltaTime when given on normal state run, 0 if another state is run in this frame.
        /// </value>
        public float DeltaTime { get; protected set; }


        /// <summary>
        /// Number of frames state machine has run the current state
        /// </summary>
        /// <value>
        /// Number of frames
        /// </value>
        public int FramesInState { get; protected set; }


        /// <summary>
        /// Time the state machine has run in the current state
        /// </summary>
        /// <value>
        /// Time in seconds
        /// </value>
        public float TimeInState { get; protected set; }


        /// <summary>
        /// Returns only once per state run when the time passes the provided value.
        /// </summary>
        /// <returns><c>true</c>, if time passed the provided value, <c>false</c> otherwise.</returns>
        /// <param name="timeInState">Time value, seconds.</param>
        public bool TimeInStatePassed(float timeInState)
        {
            return timeInState >= TimeInState && timeInState < TimeInState + DeltaTime;
        }


        /// <summary>
        /// The number of states that this state machine has gone through,
        /// incremented on each switchState
        /// </summary>
        public ulong Ticks { get; protected set; }


		public Action ValidOnlyInThisState(Action wrapped, bool withAutoUpdate = false)
		{
			var tick = Ticks;
			return () =>
			{
				if (tick != Ticks)
					return;
				wrapped();
				if (withAutoUpdate)
					Update(0);
			};
		}

		public Action<T> ValidOnlyInThisState<T>(Action<T> wrapped, bool withAutoUpdate = false)
		{
			var tick = Ticks;
			return (arg1) =>
			{
				if (tick != Ticks)
					return;
				wrapped(arg1);
				if (withAutoUpdate)
					Update(0);
			};
		}

		public Action StateSwitcherValidOnlyInThisState(State nextState, bool withAutoUpdate = true)
        {
            return ValidOnlyInThisState(() => {
                if (withAutoUpdate)
                    SwitchStateExternalImmidiate(nextState);
                else
                    SwitchStateExternalDelayed(nextState);
            });
        }



        #region events
        /// <summary>
        /// Event that occurs before the final run of a state.
        /// </summary>
        public event Action OnExitState;


        /// <summary>
        /// Occurs before the final run of a state.
        /// </summary>
        protected virtual void RunOnExitState()
        {
            if (OnExitState != null)
                OnExitState();
        }


        /// <summary>
        /// Event that occurs when ending the SM
        /// </summary>
        public event Action OnEndSM;



        /// <summary>
        /// Occurs when ending the SM
        /// </summary>
        protected virtual void RunOnEndSM()
        {
            if (OnEndSM != null)
                OnEndSM();
        }
        #endregion



        #region state machine logic
        protected State nextState = null;


        protected State forceNextChange = null;


        protected bool ignoreEndUpdate = false;


        protected void _SwitchState(State nextState)
        {
            if (Ended)
                LogWarning(this + " switch on an ended SM");

            if (!CurrentlyExecuting)
            {
                forceNextChange = nextState;
            }

            LogDebug(this + " -> " + DelegateToString(nextState));

            this.nextState = nextState;
            ExitState = true;
            RunOnExitState();
            OnExitState = () => { };
            FramesInState = 0;
            TimeInState = 0;
        }


		#region logging
		public delegate void LogDelegate(string message, bool isWarning);


		readonly LogDelegate logDelegate;


        private void LogWarning(string v)
        {
			if(logDelegate != null)
				logDelegate(v, true);
        }

        private void LogDebug(string v)
        {
			if (logDelegate != null)
				logDelegate(v, false);
		}

#if UNITY_5 || UNITY_6
		public static void LogDelegateUnityDebug(LogSeverity severity, string message) 
		{
			if (isWarning)
				UnityEngine.Debug.LogWarning(message);
			else
				UnityEngine.Debug.Log(message);
		}
#endif

		public static void LogDelegateDiagnosticsTrace(string message, bool isWarning)
		{
			if (isWarning)
				System.Diagnostics.Trace.WriteLine(message, "warning");
			else
				System.Diagnostics.Trace.WriteLine(message, "info");
		}

		#endregion


		/// <summary>
		/// Switches the current state. The next state will be run just after this one, without any delay.
		/// The exitstate/enterstate flags will be set.
		/// Use only from other states of this SM.
		/// </summary>
		/// <param name='nextState'>
		/// The next state.
		/// </param>
		public void SwitchState(State nextState)
        {
            if (!CurrentlyExecuting)
                LogWarning(this + " switched state from outside of another state (use SwitchStateExternal* if this is not a bug)");
            IsPushedState = false;
            _SwitchState(nextState);
        }


        /// <summary>
        /// Switches the current state. The next state will be run just after this one, without any delay.
        /// The exitstate/enterstate flags will be set.
        /// Use only outside of other states of this SM.
        /// </summary>
        /// <param name='nextState'>
        /// The next state.
        /// </param>
        public void SwitchStateExternalImmidiate(State nextState)
        {
            if (CurrentlyExecuting)
                LogWarning(this + " used external switch from inside of a state (use SwitchState if this is not a bug)");
            IsPushedState = false;
            _SwitchState(nextState);
            Update(0f);
        }


        /// <summary>
        /// Switches the current state. The next state will be run on the next update.
        /// The exitstate/enterstate flags will be set.
        /// Use only outside of other states of this SM.
        /// </summary>
        /// <param name='nextState'>
        /// The next state.
        /// </param>
        public void SwitchStateExternalDelayed(State nextState)
        {
            if (CurrentlyExecuting)
                LogWarning(this + " used external switch from inside of a state (use SwitchState if this is not a bug)");
            IsPushedState = false;
            _SwitchState(nextState);
        }


        public virtual void SwitchStateWithoutExitStateDoNotUseUnlessDesperate(State replaceWithState, bool immidiate)
        {
            FramesInState = 0;
            TimeInState = 0;
            ExitState = false;
            EnterState = true;
            forceNextChange = null;
            nextState = null;
            CurrentState = null;
            _SwitchState(replaceWithState);

            if (immidiate)
                Update(0);
        }


        /// <summary>
        /// Set this state machine as ended.
        /// You will get a warning if you use a ended state machine.
        /// </summary>
        public void End()
        {
            if (!Ended)
            {
                Ended = true;
                RunOnExitState();
                RunOnEndSM();
                ignoreEndUpdate = true;
                EnterState = false;
                Update(0);
                ignoreEndUpdate = false;
            }
        }


        /// <summary>
        /// Perform state machine logic. Run this method on every frame.
        /// </summary>
        /// <param name="deltaTime">
        /// Current deltaTime, <see cref="DeltaTime"/>
        /// </param>
        public void Update(float deltaTime)
        {
			if (CurrentlyExecuting)
				throw new Exception("Cannot execute Update while Update is still running");
			lock (CurrentlyExecutingSyncRoot)
			{
				watchdogRunsThisFrame = 0;
				CurrentlyExecuting = true;
				nextState = forceNextChange;    // null if none

				DeltaTime = deltaTime;
				RunState();
				EnterState = false;

				if (Ended && !ignoreEndUpdate)
					LogWarning(this + " ended, but still updating");

				while (nextState != null)
				{
					CurrentState = nextState;
					nextState = null;
					Ticks++;

					EnterState = true;
					ExitState = false;
					deltaTime = 0f;
					TimeInState = 0;
					RunState();

					EnterState = false;
				}

				CurrentlyExecuting = false;
				forceNextChange = null;

				FramesInState++;
				TimeInState += deltaTime;
			}
        }


#if UNITY_5 || UNITY_6
        /// <summary>
        /// Perform state machine logic. Run this method on every frame. 
        /// DeltaTime is taken from UnityEngine.Time
        /// </summary>
        public void Update()
        {
            Update(Time.deltaTime);
        }
#endif


        protected virtual void RunState()
        {
            Watchdog();
            CurrentState();
        }
#endregion



#region watchdog
        public class WatchDogException:Exception
        {
            public WatchDogException():base("StateMachine Watchdog has been triggered, possibly an infinite loop has been detected") { }
        }


        public int watchdogLimit = 100;


        public bool watchdogEnabled = true;


        int watchdogRunsThisFrame = 0;


        protected virtual void Watchdog()
        {
            if (!watchdogEnabled)
                return;

            if (watchdogLimit > 0 && ++watchdogRunsThisFrame > watchdogLimit)
                throw new WatchDogException();
        }
#endregion



#region substatemachine logic
        protected Stack<State> stack = new Stack<State>();


        /// <summary>
        /// Returns true if the current state was pushed using PushState
        /// </summary>
        public bool IsPushedState { get; protected set; }


        /// <summary>
        /// Utility providing a sanity check for checking if this was a pushed state
        /// </summary>
        public void AssertIsPushedState(bool expected = true)
        {
            if (IsPushedState != expected)
                throw new Exception("assertion failed: expected IsPushedState=" + expected);
        }


        /// <summary>
        /// Pushes the state. Usefull for sub state machines.
        /// Can be used inside or outside of other states (when outside works as delayed)
        /// </summary>
        /// <param name="state">The inner state, this SM is switched to it.</param>
        /// <param name="returnState">The state that will be returned to, default: CurrentState</param>
        public void PushState(State state, State returnState = null)
        {
			LogDebug(this + " push state: " + StateName + " -> " + DelegateToString(state));
            stack.Push(returnState ?? CurrentState);
            IsPushedState = true;
            _SwitchState(state);
        }


        /// <summary>
        /// Pops the state. Usefull for sub state machines.
        /// Can be used inside or outside of other states (when outside works as delayed)
        /// </summary>
        public void PopState()
        {
            LogDebug(this + " pop state: " + StateName + " -> " + DelegateToString(stack.Peek()));
            _SwitchState(stack.Pop());
        }


        public int SubStateMachineDepth
        {
            get { return stack.Count; }
        }
#endregion




#region debug utilities
        public override string ToString()
        {
            return string.Format("[StateMachine#{0} {1} +{2}]", id, DelegateToString(CurrentState), SubStateMachineDepth);
        }


        /// <summary>
        /// Utility used for getting human readable names of states
        /// </summary>
        /// <param name="del">the state</param>
        private static string DelegateToString(State del)
        {
#if UNITY_FLASH && !UNITY_EDITOR
			return "(stripped)";
#else
            string o = "";
            if (del == null)
                return "(null?)";
            var delegates = del.GetInvocationList();
            if (delegates.Length == 0)
                return "(?)";
            string target = (delegates[0].Target == null) ? "?" : delegates[0].Target.GetType().ToString();
            string method = (delegates[0].Method == null) ? "?" : delegates[0].Method.Name;
            o = target + "::" + method;
            if (delegates.Length > 1)
                o += "(+" + (delegates.Length - 1) + ")";
            return o.Trim();
#endif
        }


        /// <summary>
        /// Returns a human readable name of the current state
        /// </summary>
        public string StateName
        {
            get { return DelegateToString(CurrentState); }
        }
		
#endregion



    }


}
