using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveStateMachine {
	public class StateMachine<State> : IObservable<State>, IObservable<State, State> {
        State _initialState;
        State _state;
        Dictionary<State, Dictionary<State, Transition>> _state2state;
        Dictionary<State, StateHandler> _state2handler;

		event System.Action<State, State> _onStateChange;
		event System.Action<State> _onStateUpdate;

        public State Current { get { return _state; } }

        public StateMachine(State initialState) {
            _state = _initialState = initialState;
            _state2state = new Dictionary<State, Dictionary<State, Transition>> ();
            _state2handler = new Dictionary<State, StateHandler> ();
        }

        #region Flow
        public bool Is(State state) {
            return _state.Equals(state);
        }
        public bool Next(State stateTo) {
			var tr = this [_state, stateTo];
			if (tr != null && tr.Next ()) {
				Notify (_state, stateTo);
				_state = stateTo;
				return true;
			}
			return false;
        }
        public void Update() {
            StateHandler h;
            if (TryGetHandler (_state, out h))
                h.Update ();
			Notify (_state);
        }
        #endregion

        #region Definition
        public Transition Tr(State stateFrom, State stateTo) {
            Transition t;
            if (!TryGetTransition(stateFrom, stateTo, out t))
                t = this [stateFrom, stateTo] = new Transition (stateFrom, stateTo);
            return t;
        }
        public StateHandler St(State state) {
            StateHandler h;
            if (!TryGetHandler(state, out h))
                h = _state2handler [state] = new StateHandler (state);
            return h;
        }
        #endregion

		#region events
		void Notify(State state) {
			if (_onStateUpdate != null)
				_onStateUpdate (state);
		}
		void Notify(State stateFrom, State stateTo) {
			if (_onStateChange != null)
				_onStateChange (_state, stateTo);
		}
		#region IObservable implementation
		public System.IDisposable Subscribe (System.Action<State> observer) {
			_onStateUpdate += observer;
			return new Disposer (() => _onStateUpdate -= observer);
		}
		public System.IDisposable Subscribe (System.Action<State, State> observer) {
			_onStateChange += observer;
			return new Disposer (() => _onStateChange -= observer);
		}
		#endregion
		#endregion

        public StateMachine<State> Restart() { return Restart (_initialState); }
        public StateMachine<State> Restart(State initialState) { 
            _initialState = _state;
            return this;
        }

		Transition this[State stateFrom, State stateTo] {
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

		bool TryGetTransition(State stateFrom, State stateTo, out Transition tr) {
            tr = null;
			Dictionary<State, Transition> s2t;
			return _state2state.TryGetValue (stateFrom, out s2t) && s2t.TryGetValue (stateTo, out tr);			
		}
		bool SetTransition(State stateFrom, State stateTo, Transition tr) {
			var replace = false;
			Dictionary<State, Transition> s2t;
			if (!_state2state.TryGetValue (stateFrom, out s2t))
				_state2state [stateFrom] = s2t = new Dictionary<State, Transition> ();
			else if (replace = s2t.ContainsKey (stateTo))
				Debug.LogFormat ("Trigger Replaced : ({0})-->({1})", stateFrom, stateTo);
			s2t [stateTo] = tr;
			return replace;
		}
        bool TryGetHandler(State state, out StateHandler hr) {
            return _state2handler.TryGetValue(state, out hr);
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
			public readonly State stateFrom;
            public readonly State stateTo;

            event System.Action<State, State> _observable;
            System.Func<State, State, bool> _condition;

            public Transition(State current, State next) {
				this.stateFrom = current;
                this.stateTo = next;
            }

            public Transition Cond(System.Func<State, State, bool> condition) {
                this._condition = condition;
                return this;
            }
            public bool Next() {
				if (Check ()) {
					Notify ();
					return true;
				}
				return false;
            }
            bool Check() {
                return _condition == null || _condition (stateFrom, stateTo);
            }
            void Notify() {
                if (_observable != null)
                    _observable(stateFrom, stateTo);                
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
	public class Chain<S, T> : System.IDisposable, IObservable<T> {
		protected System.IDisposable _disposer;
		protected event System.Action<T> _observer;

		public Chain(System.IDisposable disposer) {
			this._disposer = disposer;
		}
		protected void Notify(T t) {
			if (_observer != null)
				_observer (t);
		}
		#region IObservable implementation
		public System.IDisposable Subscribe (System.Action<T> observer) {
			_observer += observer;
			return new Disposer (() => _observer -= observer);
		}
		#endregion
		#region IDisposable implementation
		public void Dispose () {
			_disposer.Dispose();
		}
		#endregion
	}
	public class Filter<S> : Chain<S, S> {
		public Filter(IObservable<S> observable, System.Func<S, bool> filter)
			: base(observable.Subscribe ((s) =>	{
				if (filter (s))
					Notify(s);
			})) {}
	}
	public class Select<S, T> : Chain<S, T> {
		public Select(IObservable<S> observable, System.Func<S, T> converter)
			: base(observable.Subscribe ((t) => Notify(converter (t)))) {}
	}

	public static class Extension {
		public static IObservable<T> Where<T>(this IObservable<T> observable, System.Func<T, bool> filter) {
			return new Filter<T>(observable, filter);
		}
		public static IObservable<T> Select<S, T>(this IObservable<S> observable, System.Func<S, T> converter) {
			return new Select<S, T> (observable, converter);
		}
	}
}
