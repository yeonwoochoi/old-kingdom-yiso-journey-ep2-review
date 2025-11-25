using System.Collections.Generic;
using System.Linq;
using Gameplay.Character.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    /// <summary>
    /// 하나의 상태에서 다른 상태로 전환되는 규칙과 대상을 정의하는 ScriptableObject.
    /// 조건에 따라 단일 상태 또는 목록 중 하나를 랜덤으로 선택하여 전환할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterTransition", menuName = "Yiso/Gameplay/Character/State Machine/Transition")]
    public class YisoCharacterTransitionSO : ScriptableObject {
        
        [Header("Condition")]
        [SerializeField]
        private List<YisoCharacterDecisionSO> _decisions;

        [Header("IF TRUE")]
        [Tooltip("True일 때, 목록에서 랜덤으로 상태를 고를지 여부입니다.")]
        [SerializeField]
        private bool _isTrueStateRandom = false;
        
        [Tooltip("랜덤이 아닐 경우 전환될 단일 상태입니다.")]
        [SerializeField, ShowIf("@!_isTrueStateRandom")]
        private YisoCharacterStateSO _trueState;
        
        [Tooltip("랜덤일 경우, 선택될 상태들의 목록입니다.")]
        [SerializeField, ShowIf("_isTrueStateRandom")]
        private List<YisoCharacterStateSO> _trueStates;
        
        [Header("IF FALSE")]
        [Tooltip("False일 때, 목록에서 랜덤으로 상태를 고를지 여부입니다.")]
        [SerializeField]
        private bool _isFalseStateRandom = false;
        
        [Tooltip("랜덤이 아닐 경우 전환될 단일 상태입니다. 비워두면 현재 상태를 유지합니다.")]
        [SerializeField, ShowIf("@!_isFalseStateRandom")]
        private YisoCharacterStateSO _falseState;
        
        [Tooltip("랜덤일 경우, 선택될 상태들의 목록입니다.")]
        [SerializeField, ShowIf("_isFalseStateRandom")]
        private List<YisoCharacterStateSO> _falseStates;
        
        
        /// <summary>
        /// 이 전환 규칙을 평가하여 다음 상태를 결정합니다.
        /// </summary>
        /// <param name="context">캐릭터의 런타임 정보에 접근하기 위한 컨텍스트.</param>
        /// <param name="nextState">전환이 결정될 경우, 다음 상태가 담길 out 파라미터.</param>
        /// <returns>상태 전환이 필요한 경우 true를 반환합니다.</returns>
        public bool CheckTransition(IYisoCharacterContext context, out YisoCharacterStateSO nextState) {
            // Decision의 평가 결과를 먼저 가져옴
            var decisionResult = _decisions.All(decision => decision.Decide(context));

            // 평가 결과에 따라 다음 상태를 결정
            if (decisionResult) {
                // TRUE 경로
                nextState = _isTrueStateRandom ? GetRandomStateFrom(_trueStates) : _trueState;
            }
            else {
                // FALSE 경로
                nextState = _isFalseStateRandom ? GetRandomStateFrom(_falseStates) : _falseState;
            }
            
            // 다음 상태가 유효할 경우(null이 아닐 경우)에만 전환이 필요하다고 판단
            return nextState != null;
        }

        /// <summary>
        /// 제공된 목록에서 무작위로 상태 하나를 선택하여 반환
        /// </summary>
        private YisoCharacterStateSO GetRandomStateFrom(IReadOnlyList<YisoCharacterStateSO> stateList) {
            if (stateList == null || stateList.Count == 0) {
                // 목록이 비어있으면 전환할 수 없으므로 null을 반환
                return null;
            }
            
            // 목록에서 랜덤 인덱스를 뽑아 해당 상태를 반환
            return stateList[Random.Range(0, stateList.Count)];
        }
    }
}