using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    public class YisoCharacterActionSetAnimator: YisoCharacterAction {
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Tooltip("State 종료 시 이전 Controller로 복원")]
        [SerializeField] private bool restoreOnExit = false;

        private RuntimeAnimatorController previousController;

        public override void OnEnterState() {
            if (StateMachine?.Owner?.Animator == null) {
                Debug.LogWarning($"[{name}] Animator를 찾을 수 없습니다.");
                return;
            }

            if (animatorController == null) {
                Debug.LogWarning($"[{name}] AnimatorController가 할당되지 않았습니다.");
                return;
            }

            // 이전 controller 백업
            previousController = StateMachine.Owner.Animator.runtimeAnimatorController;

            // 새 controller 적용
            StateMachine.Owner.Animator.runtimeAnimatorController = animatorController;
        }

        public override void OnExitState() {
            if (restoreOnExit && previousController != null &&
                StateMachine?.Owner?.Animator != null) {
                StateMachine.Owner.Animator.runtimeAnimatorController = previousController;
            }
        }

        public override void PerformAction() {
            // OnEnterState에서 처리하므로 비워둠
        }
    }
}