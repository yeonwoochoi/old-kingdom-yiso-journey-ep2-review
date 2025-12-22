using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions.Operator {
    /// <summary>
    /// 단일 Decision의 결과를 반전(Not)시켜 반환하는 Decision.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_Not", menuName = "Yiso/State Machine/Decision/Not")]
    public class YisoCharacterDecisionNotSO: YisoCharacterDecisionSO {
        [Tooltip("이 Decision의 결과를 반전하여 최종 반환값으로 사용합니다.")]
        [SerializeField] private YisoCharacterDecisionSO _decision;
        
        public override bool Decide(IYisoCharacterContext context) {
            // 반전할 Decision이 없으면 False 반환
            if (_decision == null) return false;

            // 기존 Decision 결과를 반전하여 반환
            return !_decision.Decide(context);
        }
    }
}