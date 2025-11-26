using System.Collections.Generic;
using System.Linq;
using Gameplay.Character.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public enum YisoStateRole {
        Idle,           // 기본 대기
        Move,           // 이동
        Chase,          // 추격
        Attack,         // 일반 공격
        SkillAttack,    // 스킬 공격
        Hit,            // 피격
        Died,           // 사망
        Spawn,          // 스폰
        Custom          // 그 외의 모든 커스텀 상태
    }

    [CreateAssetMenu(fileName = "SO_FSM_State_", menuName = "Yiso/State Machine/State")]
    public class YisoCharacterStateSO: ScriptableObject {
        [Header("State Identity")]
        [Tooltip("이 상태가 맡은 기본 역할입니다.")]
        [SerializeField] private YisoStateRole role = YisoStateRole.Idle;

        [Tooltip("Role이 'Custom'일 경우, 이 상태를 식별할 고유한 이름입니다.")]
        [ShowIf("role", YisoStateRole.Custom)]
        [SerializeField] private string customStateName;
        
        [Header("Settings")]
        [Tooltip("단일 상태로 볼 것인지 여러 상태가 합쳐져 있는 다중 상태로 볼건지 여부")]
        [SerializeField] private bool hasChildState = false;
        
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

        [Header("Child States"), ShowIf("hasChildState")]
        [Tooltip("자식 상태 목록이다.")]
        [SerializeField, ShowIf("hasChildState")] private List<YisoCharacterStateSO> childStates;
        
        [Header("Behavior Permissions")]
        [Tooltip("이 상태에 있는 동안 이동(Movement)이 허용되는지 여부.")]
        [SerializeField] private bool canMove = true; // Movement Ability만 따로 중단

        [Tooltip("이 상태에 있는 동안 어빌리티 사용(Ability Cast)이 허용되는지 여부.")]
        [SerializeField] private bool canCastAbility = true; // false이면 Ability의 Update 로직 싹다 중단

        public string StateKey => (role == YisoStateRole.Custom) ? customStateName : role.ToString();
        public YisoStateRole Role => role;
        public bool CanMove => canMove;
        public bool CanCastAbility => canCastAbility;
        
        public virtual void OnEnter(IYisoCharacterContext context) {
            ExecuteActions(onEnterActions, context);
            if (hasChildState) {
                foreach (var childState in childStates) {
                    if (childState == this) continue;
                    childState.OnEnter(context);
                }
            }
        }

        public virtual void OnExit(IYisoCharacterContext context) {
            ExecuteActions(onExitActions, context);
            if (hasChildState) {
                foreach (var childState in childStates) {
                    if (childState == this) continue;
                    childState.OnExit(context);
                }
            }
        }

        public virtual void OnUpdate(IYisoCharacterContext context) {
            ExecuteActions(onUpdateActions, context);
            if (hasChildState) {
                foreach (var childState in childStates) {
                    if (childState == this) continue;
                    childState.OnUpdate(context);
                }
            }
        }
        
        public bool CheckTransitions(IYisoCharacterContext context, out YisoCharacterStateSO nextState) {
            var availableStates = new HashSet<YisoCharacterStateSO>();
            var canTransition = false;
            
            foreach (var transition in transitions) {
                // 각 Transition 규칙이 충족되는지 확인
                if (transition.CheckTransition(context, out var candidateState)) {
                    availableStates.Add(candidateState);
                    canTransition = true;
                }
            }

            if (!hasChildState) {
                nextState = canTransition ? availableStates.FirstOrDefault() : null;
                return canTransition;
            }

            var childStateCandidates = new Dictionary<YisoCharacterStateSO, int>();
            
            foreach (var childState in childStates) {
                if (childState.CheckTransitions(context, out var childCandidate)) {
                    if (childCandidate != null) {
                        childStateCandidates.TryAdd(childCandidate, 0);
                        childStateCandidates[childCandidate]++;
                    }
                }
            }

            foreach (var (candidateState, count) in childStateCandidates) {
                if (count == childStates.Count && availableStates.Contains(candidateState)) {
                    nextState = candidateState;
                    return true;
                }
            }

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