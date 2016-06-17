using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveStateMachine {
    public class StateMachine<State> {
        State _initialState;
        State _state;
        Dictionary<State, Dictionary<State, Transition>> _state2state;
        Dictionary<State, StateHandler> _state2handler;

        public State Current { get { return _state; } }

        public StateMachine(State initialState) {
            _state = _initialState = initialState;
            _state2state = new Dictionary<State, Dictionary<State, Transition>> ();
            _state2handler = new Dictionary<State, StateHandler> ();
        }

		public Transition this[State stateFrom, State stateTo] {
			get {
				Transition tr;
				if (TryGetTransition(stateFrom, stateTo, out tr))
					return tr;
				return null;
			}
			set {
				if (SetTransition(stateFrom, stateTo, value))
					Debug.LogFormat ("Trigger Replaced : ({0})-->({1})", stateFrom, stateTo);
			}
		}

		public bool TryGetTransition(State stateFrom, State stateTo, out Transition tr) {
            tr = null;
			Dictionary<State, Transition> s2t;
			return _state2state.TryGetValue (stateFrom, out s2t) && s2t.TryGetValue (stateTo, out tr);			
		}
		public bool SetTransition(State stateFrom, State stateTo, Transition tr) {
			var replace = false;
			Dictionary<State, Transition> s2t;
			if (!_state2state.TryGetValue (stateFrom, out s2t))
				_state2state [stateFrom] = s2t = new Dictionary<State, Transition> ();
			else if (replace = s2t.ContainsKey (stateTo))
				Debug.LogFormat ("Trigger Replaced : ({0})-->({1})", stateFrom, stateTo);
			s2t [stateTo] = tr;
			return replace;
		}
        public bool TryGetHandler(State state, out StateHandler hr) {
            return _state2handler.TryGetValue(state, out hr);
        }

        #region Flow
        public bool Is(State state) {
            return _state.Equals(state);
        }
        public bool Next(State stateTo) {
			var tr = this [_state, stateTo];
			if (tr != null && tr.Next (_state)) {
				_state = stateTo;
				return true;
			}
			return false;
        }
        public void Update() {
            StateHandler h;
            if (TryGetHandler (_state, out h))
                h.Update ();
        }
        #endregion

        #region Definition
        public Transition Tr(State stateFrom, State stateTo) {
            Transition t;
            if (!TryGetTransition(stateFrom, stateTo, out t))
                t = this [stateFrom, stateTo] = new Transition (stateTo);
            return t;
        }
        public StateHandler St(State state) {
            StateHandler h;
            if (!TryGetHandler(state, out h))
                h = _state2handler [state] = new StateHandler (state);
            return h;
        }
        #endregion

        public StateMachine<State> Restart() { return Restart (_initialState); }
        public StateMachine<State> Restart(State initialState) { 
            _initialState = _state;
            return this;
        }

		public class StateHandler : IObservable<State> {
            public readonly State state;
			event System.Action<State> _observable;

            public StateHandler(State state) {
                this.state = state;
            }
			public StateHandler Update() {
				if (_observable != null)
					_observable(state);
				return this;
			}
			#region IObservable implementation
			public System.IDisposable Subscribe (System.Action<State> observer) {
				_observable += observer;
				return new Disposer (() => _observable -= observer);
			}
			#endregion
        }
		public class Transition : IObservable<State, State> {
            public readonly State NextState;

            event System.Action<State, State> _observable;
            System.Func<State, State, bool> _condition;

            public Transition(State nextState) {
                this.NextState = nextState;
            }

            public Transition Cond(System.Func<State, State, bool> condition) {
                this._condition = condition;
                return this;
            }
            public bool Next(State stateFrom) {
                if (!Check(stateFrom))
                    return false;

                Notify (stateFrom);
                return true;
            }
            bool Check(State stateFrom) {
                return _condition == null || _condition (stateFrom, NextState);
            }
            void Notify(State stateFrom) {
                if (_observable != null)
                    _observable(stateFrom, NextState);                
            }
			#region IObservable implementation
			public System.IDisposable Subscribe (System.Action<State, State> observer) {
				_observable += observer;
				return new Disposer(() => _observable -= observer);
			}
			#endregion
        }
    }

	public interface IObservable<T> {
		System.IDisposable Subscribe(System.Action<T> observer);
	}
	public interface IObservable<S, T> {
		System.IDisposable Subscribe(System.Action<S, T> observer);
	}
	public class Disposer : System.IDisposable {
		bool _disposed = false;
		readonly System.Action _disposer;

		public Disposer(System.Action disposer) {
			this._disposer = disposer;
		}

		#region IDisposable implementation
		public void Dispose () 	{
			if (!_disposed) {
				_disposed = true;
				_disposer ();
			}
		}
		#endregion
	}

	public static class Extension {
		public static IObservable<T> Where<T>(this IObservable<T> observable, System.Func<T, bool> filter) {
			throw new System.NotImplementedException ();
		}
	}
}
