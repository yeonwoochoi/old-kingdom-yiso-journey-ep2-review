using System.Collections.Generic;
using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    [CreateAssetMenu(fileName = "NewCharacterState", menuName = "Yiso/Gameplay/Character/State Machine/Character State")]
    public class YisoCharacterStateSO: ScriptableObject {
        [Header("Actions")]
        [Tooltip("이 상태에 진입할 때 한 번 실행될 액션들의 목록이다.")]
        [SerializeField] private List<YisoCharacterActionSO> onEnterActions;
        
        [Tooltip("이 상태가 활성화된 동안 주기적으로(StateModule의 주기에 따라) 실행될 액션들의 목록이다.")]
        [SerializeField] private List<YisoCharacterActionSO> onUpdateActions;

        [Tooltip("이 상태를 빠져나갈 때 한 번 실행될 액션들의 목록이다.")]
        [SerializeField] private List<YisoCharacterActionSO> onExitActions;
        
        [Header("Transitions")]
        [Tooltip("이 상태에서 다른 상태로 전환될 수 있는 모든 규칙(Transition)들의 목록이다. 목록의 순서가 곧 우선순위가 된다.")]
        [SerializeField] private List<YisoCharacterTransitionSO> transitions;

        public virtual void OnEnter(IYisoCharacterContext context) {
            ExecuteActions(onEnterActions, context);
        }

        public virtual void OnExit(IYisoCharacterContext context) {
            ExecuteActions(onExitActions, context);
        }

        public virtual void OnUpdate(IYisoCharacterContext context) {
            ExecuteActions(onUpdateActions, context);
        }

        public bool CheckTransitions(IYisoCharacterContext context, out YisoCharacterStateSO nextState) {
            foreach (var transition in transitions) {
                // 각 Transition 규칙이 충족되는지 확인
                if (transition.CheckTransition(context, out nextState)) {
                    // 하나라도 충족되면 즉시 true를 반환하여 전환을 시작 (우선순위)
                    return true;
                }
            }
            
            // 모든 Transition 규칙을 확인했지만 충족되는 것이 없으면, 전환하지 않음
            nextState = null;
            return false;
        }
        
        private void ExecuteActions(IReadOnlyList<YisoCharacterActionSO> actions, IYisoCharacterContext context) {
            if (actions == null) return;
            foreach (var action in actions) {
                action.PerformAction(context);
            }
        }
    }
}