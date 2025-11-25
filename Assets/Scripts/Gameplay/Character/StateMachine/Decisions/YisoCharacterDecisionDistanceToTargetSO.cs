using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_DistanceToTarget", menuName = "Yiso/State Machine/Decision/DistanceToTarget")]
    public class YisoCharacterDecisionDistanceToTargetSO: YisoCharacterDecisionSO {
        public override bool Decide(IYisoCharacterContext context) {
            return true;
        }
    }
}