using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    /// <summary>
    /// 타겟을 추격(Chase)하는 이동 액션.
    /// Blackboard의 Target 키에 저장된 Transform을 추적합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Action_Common_MoveChase", menuName = "Yiso/State Machine/Action/MoveChase")]
    public class YisoCharacterActionMoveChaseSO: YisoCharacterActionSO {
        [Header("Blackboard Keys")]
        [SerializeField] private YisoBlackboardKeySO targetKey; // 추적할 타겟 (Transform 또는 GameObject)

        public override void PerformAction(IYisoCharacterContext context) {
            var aiModule = context.GetModule<YisoCharacterAIModule>();
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (aiModule == null || bb == null) return;

            // Target 가져오기 (Transform 또는 GameObject)
            var target = bb.GetObject<Transform>(targetKey);
            if (target == null) {
                // Transform이 없으면 GameObject로 시도
                var targetGameObject = bb.GetObject<GameObject>(targetKey);
                if (targetGameObject != null) {
                    target = targetGameObject.transform;
                }
            }

            // 타겟이 없으면 이동하지 않음
            if (target == null) {
                aiModule.StopMovement();
                Debug.Log($"[MoveChase] No target found. Movement stopped.");
                return;
            }

            // 타겟 위치로 이동
            var targetPosition = (Vector2)target.position;
            aiModule.SetDestination(targetPosition);
            Debug.Log($"[MoveChase] Chasing target at {targetPosition}. Current: {context.Transform.position}");
        }
    }
}