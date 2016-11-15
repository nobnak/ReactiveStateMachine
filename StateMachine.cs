using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveStateMachine {
	public class StateMachine<State> {
        Wire<State> _state;
		Wire<Transition<State>> _transition;

		Dictionary<State, StateHandler> _stateMap;
		Dictionary<State, Dictionary<State, TransitionHandler>> _transitionMap;

		public StateMachine(State init) {
			this._state = new Wire<State> (init);
			this._transition = new Wire<Transition<State>> ();
			this._stateMap = new Dictionary<State, StateHandler> ();
			this._transitionMap = new Dictionary<State, Dictionary<State, TransitionHandler>> ();
		}
		public StateMachine() : this(default(State)) {}

		#region Wire
		public StateHandler St(State state) {
			StateHandler st;
			if (!TryGetStateHandler (state, out st))
				st = _stateMap [state] = new StateHandler (state);
			return st;
		}
		public Output<State> St() {
			return _state;
		}
		public TransitionHandler Tr(State stateFrom, State stateTo) {
			Dictionary<State, TransitionHandler> state2transition;
			if (!_transitionMap.TryGetValue (stateFrom, out state2transition))
				state2transition = _transitionMap [stateFrom] = new Dictionary<State, TransitionHandler> ();
			TransitionHandler tr;
			if (!state2transition.TryGetValue (stateTo, out tr))
				tr = state2transition [stateTo] = new TransitionHandler (stateFrom, stateTo);
			return tr;
		}
		public Output<Transition<State>> Tr() {
			return _transition;
		}
		#endregion
		#region Drive
		public State Current { get { return _state.Value; } }
		public bool Is (State state) {
			return _state.Value.Equals(state);
		}
		public bool Next(State stateTo) {
			TransitionHandler tr;
			if (TryGetTransitionHandler(_state.Value, stateTo, out tr) && tr.Transit()) {
				_transition.Value = tr.transition;
				_state.Value = stateTo;
				return true;
			}
			return false;
		}
		public void Update() {
			StateHandler st;
			if (TryGetStateHandler (_state.Value, out st))
				st.Notify ();
			_state.Notify ();
		}
		#endregion

		bool TryGetStateHandler(State state, out StateHandler st) {
			return _stateMap.TryGetValue (state, out st);
		}
		bool TryGetTransitionHandler(State stateFrom, State stateTo, out TransitionHandler tr) {
			tr = default(TransitionHandler);
			Dictionary<State, TransitionHandler> state2transition;
			return _transitionMap.TryGetValue (stateFrom, out state2transition)
			&& state2transition.TryGetValue (stateTo, out tr);
		}

		public class StateHandler : Output<State> {
			public readonly State state;
			event System.Action<State> _onUpdate;

			public StateHandler(State state) {
				this.state = state;
			}
			public void Notify() {
				if (_onUpdate != null)
					_onUpdate (state);
			}
			#region Output implementation
			public Cutter Connect (System.Action<State> input) {
				_onUpdate += input;
				return new Cutter (() => _onUpdate -= input);
			}
			#endregion
		}
		public class TransitionHandler : Output<Transition<State>> {
			public readonly Transition<State> transition;
			System.Func<State, State, bool> _condition;
			event System.Action<Transition<State>> _onChange;

			public TransitionHandler(Transition<State> transition) {
				this.transition = transition;
			}
			public TransitionHandler(State stateFrom, State stateTo) 
				: this(new Transition<State>(stateFrom, stateTo)) {}
			public bool Transit() {
				var canTransit = Check ();
				if (canTransit)
					Notify ();
				return canTransit;
			}
			public TransitionHandler Cond(System.Func<State, State, bool> cond) {
				_condition = cond;
				return this;
			}
			bool Check() {
				return _condition == null || _condition (transition.stateFrom, transition.stateTo);
			}
			void Notify() {
				if (_onChange != null)
					_onChange (transition);
			}
			#region Output implementation
			public Cutter Connect (System.Action<Transition<State>> input) {
				_onChange += input;
				return new Cutter (() => _onChange -= input);
			}
			#endregion
		}
	}

    public struct Transition<T> {
        public readonly T stateFrom;
        public readonly T stateTo;
        public Transition(T stateFrom, T stateTo) {
            this.stateFrom = stateFrom;
            this.stateTo = stateTo;
        }
    }
	public class Cutter : System.IDisposable {
		System.Action _cut;
		public Cutter(System.Action cut) {
			this._cut = cut;
		}
		#region IDisposable implementation
		public void Dispose () {
			if (_cut != null) {
				_cut ();
				_cut = null;
			}
		}
		#endregion
	}
	public interface Output<T> {
		Cutter Connect (System.Action<T> input);
	}
	public class Wire<T> : Output<T> {
		event System.Action<T> _onChange;
		T _value;

		public Wire(T initial) {
			this.Value = initial;
		}
		public Wire() : this(default(T)) {}

		public T Value {
			get {
				return _value;
			}
			set {
				if (!_value.Equals(value))
					Set (value);
			}
		}
		public void Set(T t) {
			_value = t;
			Notify ();
		}
		public void Notify() {
			if (_onChange != null)
				_onChange (_value);
		}
		#region Output implementation
		public Cutter Connect (System.Action<T> input) {
			_onChange += input;
			return new Cutter (() => _onChange -= input);
		}
		#endregion
	}
}
