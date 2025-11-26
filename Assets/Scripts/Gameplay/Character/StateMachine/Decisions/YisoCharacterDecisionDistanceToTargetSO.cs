using Gameplay.Character.Core;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_DistanceToTarget", menuName = "Yiso/State Machine/Decision/DistanceToTarget")]
    public class YisoCharacterDecisionDistanceToTargetSO: YisoCharacterDecisionSO {
        [SerializeField] private YisoBlackboardKeySO targetKey;
        
        public override bool Decide(IYisoCharacterContext context) {
            return true;
        }
    }
}