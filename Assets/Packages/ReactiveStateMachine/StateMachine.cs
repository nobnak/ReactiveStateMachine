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

		#region Flow
		public Output<State> OnUpdate() {
			return _state;
		}
		public Output<Transition<State>> OnChange() {
			return _transition;
		}
		public Output<State> St(State state) {
			StateHandler st;
			if (!TryGetStateHandler (state, out st))
				st = _stateMap [state] = new StateHandler (state);
			return st;
		}
		public Output<Transition<State>> Tr(State stateFrom, State stateTo) {
			Dictionary<State, TransitionHandler> state2transition;
			if (!_transitionMap.TryGetValue (stateFrom, out state2transition))
				state2transition = _transition [stateFrom] = new Dictionary<State, TransitionHandler> ();
			TransitionHandler tr;
			if (!state2transition.TryGetValue (stateTo, out tr))
				tr = state2transition [stateTo] = new TransitionHandler (stateFrom, stateTo);
			return tr;
		}
		#endregion
		#region Definition
		public bool Next() {
			
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
			readonly State _state;
			event System.Action<State> _onUpdate;

			public StateHandler(State state) {
				this._state = state;
			}
			public void Notiry() {
				if (_onUpdate != null)
					_onUpdate (_state);
			}
			#region Output implementation
			public Cutter Input (System.Action<State> input) {
				_onUpdate += input;
				return new Cutter (_onUpdate -= input);
			}
			#endregion
		}
		public class TransitionHandler : Output<Transition<State>> {
			readonly Transition<State> _transition;
			System.Func<Transition<State>, bool> _condition;
			event System.Action<Transition<State>> _onChange;

			public TransitionHandler(Transition<State> transition) {
				this._transition = transition;
			}
			public TransitionHandler(State stateFrom, State stateTo) 
				: this(new Transition<State>(stateFrom, stateTo)) {}
			public bool Transit() {
				var canTransit = Check ();
				if (canTransit)
					Notify ();
				return canTransit;
			}
			bool Check() {
				return _condition == null || _condition ();
			}
			void Notify() {
				if (_onChange != null)
					_onChange (_transition);
			}
			#region Output implementation
			public Cutter Input (System.Action<Transition<State>> input) {
				_onChange += input;
				return new Cutter (_onChange -= input);
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
		Cutter Input (System.Action<T> input);
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
				if (_value != value)
					Set (value);
			}
		}
		public void Set(T t) {
			_value = t;
			Notify (t);
		}
		void Notify(T t) {
			if (_onChange != null)
				_onChange (t);
		}
		#region Output implementation
		public Cutter Input (System.Action<T> input) {
			_onChange += input;
			return new Cutter (() => _onChange -= input);
		}
		#endregion
	}
}
