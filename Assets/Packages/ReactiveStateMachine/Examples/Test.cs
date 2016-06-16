using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

namespace ReactiveStateMachine {

    public class Test : MonoBehaviour {
        public const string FORMAT_TRIGGER = "Triggered ({0})-->({1})";

        enum VitalState { Dead = 0, Alive }
        enum TransitionState { None = 0, Spawning, Dying }

     	void Start () {
            var smVital = new StateMachine<VitalState> (VitalState.Dead);
            var smTransition = new StateMachine<TransitionState> (TransitionState.None);

            smTransition.Tr (TransitionState.None, TransitionState.Spawning).Cond (
                (a, b) => smVital.Is(VitalState.Dead));
            smTransition.Tr (TransitionState.None, TransitionState.Dying).Cond (
                (a, b) => smVital.Is(VitalState.Alive));
            smTransition.Tr (TransitionState.Spawning, TransitionState.None).Cond (
                (a, b) => smVital.Is(VitalState.Dead));
            smTransition.Tr (TransitionState.Dying, TransitionState.None).Cond (
                (a, b) => smVital.Is(VitalState.Alive));

            smVital.Tr (VitalState.Dead, VitalState.Alive).Cond (
                (a, b) => smTransition.Is(TransitionState.Spawning) && smTransition.Next (TransitionState.None));
            smVital.Tr (VitalState.Alive, VitalState.Dead).Cond (
                (a, b) => smTransition.Is(TransitionState.Dying) && smTransition.Next (TransitionState.None));

            var updateCounter = 0;
            smVital.Hr (VitalState.Alive).OnUpdate ((a) => {
                Assert.AreEqual(a, VitalState.Alive);
                updateCounter++;
            });
            smVital.Hr (VitalState.Dead).OnUpdate ((a) => {
                Assert.AreEqual(a, VitalState.Dead);
                updateCounter++;
            });
            Assert.AreEqual (smVital.Hr (VitalState.Alive).state, VitalState.Alive);
            Assert.AreEqual (smVital.Hr (VitalState.Dead).state, VitalState.Dead);

            var counter = 0;
            smTransition.Tr (TransitionState.None, TransitionState.Spawning).On ((a, b) => {
                counter++;
            }).On ((a, b) => {
                counter++;
            });

            Assert.IsTrue (smTransition.Next (TransitionState.Spawning));
            Assert.AreEqual (smTransition.Current, TransitionState.Spawning);
            Assert.AreEqual (smVital.Current, VitalState.Dead);

            Assert.IsTrue (smVital.Next (VitalState.Alive));
            Assert.AreEqual (smVital.Current, VitalState.Alive);
            Assert.AreEqual (smTransition.Current, TransitionState.None);
            for (var i = 0; i < 2; i++)
                smVital.Update ();
            Assert.AreEqual (updateCounter, 2);

            Assert.IsTrue (smTransition.Next (TransitionState.Dying));
            Assert.AreEqual (smTransition.Current, TransitionState.Dying);
            Assert.AreEqual (smVital.Current, VitalState.Alive);

            Assert.IsTrue (smVital.Next (VitalState.Dead));
            Assert.AreEqual (smVital.Current, VitalState.Dead);
            Assert.AreEqual (smTransition.Current, TransitionState.None);
            for (var i = 0; i < 2; i++)
                smVital.Update ();
            Assert.AreEqual (updateCounter, 4);

            Assert.AreEqual (counter, 2);
        }            
    }
}