using System.Collections.Generic;
using System.Linq;
using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions.Operator {
    /// <summary>
    /// 여러 Decision 중 하나라도 True이면 True를 반환하는 Decision. (논리합 OR)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_Or", menuName = "Yiso/State Machine/Decision/OR Group")]
    public class YisoCharacterDecisionOrSO : YisoCharacterDecisionSO {
        [Tooltip("이 목록 중 하나라도 True를 반환하면 최종 결과는 True가 됩니다.")]
        [SerializeField]
        private List<YisoCharacterDecisionSO> decisions;

        public override bool Decide(IYisoCharacterContext context) {
            // 리스트가 비어있으면 False 반환 (조건 불충족)
            if (decisions == null || decisions.Count == 0) return false;
            
            // 끝까지 돌았는데 True가 하나도 없으면 False
            return decisions.Where(decision => decision != null).Any(decision => decision.Decide(context));
        }
    }
}