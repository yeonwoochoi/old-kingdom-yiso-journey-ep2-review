using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// Blackboard의 Target이 null인지 확인하는 Decision.
    /// true: Target이 null임 (타겟 없음)
    /// false: Target이 존재함
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_TargetIsNull", menuName = "Yiso/State Machine/Decision/TargetIsNull")]
    public class YisoCharacterDecisionTargetIsNullSO: YisoCharacterDecisionSO {
        [SerializeField] private YisoBlackboardKeySO targetKey;

        public override bool Decide(IYisoCharacterContext context) {
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            
            // 1. Blackboard가 없으면 타겟 관리가 안 되므로 "타겟 없음(True)" 처리
            if (bb == null) return true;

            var target = bb.GetObject<Transform>(targetKey);
            if (target == null) {
                var targetGameObject = bb.GetObject<GameObject>(targetKey);
                if (targetGameObject != null) {
                    target = targetGameObject.transform;
                }
            }

            // 2. 물리적으로 타겟 오브젝트가 없으면 -> True (IsNull 맞음)
            if (target == null) return true;
            
            // 3. 타겟은 있는데, 죽었으면 -> True (사실상 없는 셈)
            var targetContext = target.GetComponent<IYisoCharacterContext>();
            if (targetContext != null && targetContext.IsDead()) {
                // 죽은 타겟은 Blackboard에서 비워주는 게 깔끔할 수 있음
                bb.SetObject(targetKey, null); 
                return true;
            }

            // 타겟이 있고 살아있음 -> False (IsNull 아님)
            return false;
        }
    }
}