using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_DetectTargetRadius", menuName = "Yiso/State Machine/Decision/DetectTargetRadius")]
    public class YisoCharacterDecisionDetectTargetRadiusSO: YisoCharacterDecisionSO {
        public override bool Decide(IYisoCharacterContext context) {
            return true;
        }
    }
}