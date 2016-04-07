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

        public bool Next(State stateTo) {
            Dictionary<State, Transition> state2transition;
            if (!_state2state.TryGetValue (_state, out state2transition))
                return false;

            Transition tr;
            if (!state2transition.TryGetValue(stateTo, out tr))
                return false;

            if (!tr.Next (_state))
                return false;

            _state = stateTo;
            return true;
        }
        public Transition Tr(State stateFrom, State stateTo) {
            Dictionary<State, Transition> state2transition;
            if (!_state2state.TryGetValue (stateFrom, out state2transition))
                _state2state [stateFrom] = state2transition = new Dictionary<State, Transition> ();

            if (state2transition.ContainsKey (stateTo))
                Debug.LogFormat ("Trigger Replaced : ({0})-->({1})", stateFrom, stateTo);

            var transition = new Transition (stateTo);
            state2transition [stateTo] = transition;
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
