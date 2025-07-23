using System;
using System.Collections.Generic;
using Gameplay.Tools.Event;
using UnityEngine;

namespace Gameplay.Tools.StateMachine {
    // T : Enum
    // State Change시 Event 발생시키고 싶으면 
    public struct YisoStateChangeEvent<T> where T : struct, IComparable, IConvertible, IFormattable {
        public GameObject target;
        public YisoStateMachine<T> targetStateMachine;
        public T previousState;
        public T newState;

        public YisoStateChangeEvent(YisoStateMachine<T> stateMachine) {
            target = stateMachine.Target;
            targetStateMachine = stateMachine;
            previousState = stateMachine.PreviousState;
            newState = stateMachine.CurrentState;
        }
    }
    
    public interface IYisoStateMachine {
        bool TriggerEvents { get; set; }
        GameObject Target { get; set; }
    }

    /// <summary>
    /// Represents the state type used in the generic state machine class.
    /// </summary>
    /// <typeparam name="T">Specifies the enum type that defines the states.</typeparam>
    public class YisoStateMachine<T>: IYisoStateMachine where T : struct, IComparable, IConvertible, IFormattable {
        public bool TriggerEvents { get; set; }
        public GameObject Target { get; set; }
        public T PreviousState { get; set; }
        public T CurrentState { get; set; }

        public delegate void OnStateChangeDelegate(T previousState, T currentState);
        public OnStateChangeDelegate OnStateChange;

        public YisoStateMachine(GameObject target, bool triggerEvents) {
            Target = target;
            TriggerEvents = triggerEvents;
        }

        public virtual void ChangeState(T newState) {
            // 같은 상태면 return
            if (EqualityComparer<T>.Default.Equals(newState, CurrentState)) {
                return;
            }
            
            PreviousState = CurrentState;
            CurrentState = newState;

            OnStateChange?.Invoke(PreviousState, CurrentState);

            if (TriggerEvents) {
                YisoEventManager.TriggerEvent(new YisoStateChangeEvent<T>(this));
            }
        }
        
        public virtual void RestorePreviousState() {
            CurrentState = PreviousState;

            OnStateChange?.Invoke(PreviousState, CurrentState);

            if (TriggerEvents) {
                YisoEventManager.TriggerEvent(new YisoStateChangeEvent<T>(this));
            }
        }
    }
}