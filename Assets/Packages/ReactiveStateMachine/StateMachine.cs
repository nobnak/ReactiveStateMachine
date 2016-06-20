using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveStateMachine {
	public class StateMachine<State> : Output<State>, Output<Transition<State>> {
        State _initialState;
        State _state;
        Dictionary<State, Dictionary<State, TransitionHandler>> _state2state;
        Dictionary<State, StateHandler> _state2handler;
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
				if (_value != value) {
					_value = value;
				}
			}
		}
		#region Output implementation
		public Cutter<T> Input (System.Action<T> input) {
			_onChange += input;
			return new Cutter<T> (() => _onChange -= input);
		}
		#endregion
	}
}
