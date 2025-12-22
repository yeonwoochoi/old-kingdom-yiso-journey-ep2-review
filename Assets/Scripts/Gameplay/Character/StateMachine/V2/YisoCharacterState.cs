using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.V2 {
    [Serializable]
    public class YisoCharacterState {
        [Title("Settings")]
        [SerializeField] private string stateName;
        
        [Title("Transitions")]
        [SerializeField] private List<YisoCharacterTransition> transitions;
        
        [Title("Actions")]
        [SerializeField] private List<YisoCharacterAction> onEnterActions;
        [SerializeField] private List<YisoCharacterAction> onUpdateActions;
        [SerializeField] private List<YisoCharacterAction> onExitActions;
        
        public string StateName => stateName;
        
        /// <summary>
        /// 트랜지션을 체크하여 전환해야 할 상태가 있다면 해당 상태의 이름을 반환
        /// </summary>
        public string CheckTransitions() {
            if (transitions == null) return null;
            foreach (var transition in transitions) {
                if (transition.CanTransition()) {
                    return transition.NextState;
                }
            }
            return null;
        }

        public void PlayEnterActions() {
            NotifyStateChange(true); 
            ExecuteActions(onEnterActions);
        }

        public void PlayUpdateActions() {
            ExecuteActions(onUpdateActions);
        }

        public void PlayExitActions() {
            NotifyStateChange(false); 
            ExecuteActions(onExitActions);
        }

        private void NotifyStateChange(bool isEnter) {
            // 모든 타입의 Action 리스트에게 전파
            NotifyActions(onEnterActions, isEnter);
            NotifyActions(onUpdateActions, isEnter);
            NotifyActions(onExitActions, isEnter);
            
            // Transition 내의 Decision들에게 전파
            if (transitions == null) return;
            foreach (var transition in transitions) {
                if (transition == null) continue;
                if (isEnter) transition.OnEnterState();
                else transition.OnExitState();
            }
        }

        private void NotifyActions(List<YisoCharacterAction> actions, bool isEnter) {
            if (actions == null) return;
            foreach (var action in actions) {
                if (action == null) continue;
                if (isEnter) action.OnEnterState();
                else action.OnExitState();
            }
        }
        
        private void ExecuteActions(List<YisoCharacterAction> actions) {
            if (actions == null) return;
            // GC Free를 위해 for문
            foreach (var action in actions) {
                if (action != null && action.gameObject.activeSelf) 
                    action.PerformAction();
            }
        }
    }
}