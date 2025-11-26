using Gameplay.Character.Core;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_TargetIsNull", menuName = "Yiso/State Machine/Decision/TargetIsNull")]
    public class YisoCharacterDecisionTargetIsNullSO: YisoCharacterDecisionSO {
        [SerializeField] private YisoBlackboardKeySO targetKey;
        
        public override bool Decide(IYisoCharacterContext context) {
            throw new System.NotImplementedException();
        }
    }
}