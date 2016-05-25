using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace StateGraphSystem {
    public class StateMachine<State> {
        State _initialState;
        State _state;
        Dictionary<State, Dictionary<State, Transition>> _state2state;

        public State Current { get { return _state; } }

        public StateMachine(State initialState) {
            _state = _initialState = initialState;
            _state2state = new Dictionary<State, Dictionary<State, Transition>> ();
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
        public Transition Tr(State stateFrom, State stateTo) {
            var transition = new Transition (stateTo);
			this [stateFrom, stateTo] = transition;
            return transition;
        }

        public StateMachine<State> Restart() { return Restart (_initialState); }
        public StateMachine<State> Restart(State initialState) { 
            _initialState = _state;
            return this;
        }

        public class Transition {
            public readonly State NextState;

            System.Action<State, State> _onTrigger;
            System.Func<State, State, bool> _condition;

            public Transition(State nextState) {
                this.NextState = nextState;
            }

            public Transition On(System.Action onTrigger) { return On ((f, t) => onTrigger()); }
            public Transition On(System.Action<State, State> onTrigger) {
                this._onTrigger = onTrigger;
                return this;
            }
            public Transition Cond(System.Func<bool> condition) { return Cond ((f, t) => condition()); }
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
                if (_onTrigger != null)
                    _onTrigger (stateFrom, NextState);                
            }
        }
    }
}
