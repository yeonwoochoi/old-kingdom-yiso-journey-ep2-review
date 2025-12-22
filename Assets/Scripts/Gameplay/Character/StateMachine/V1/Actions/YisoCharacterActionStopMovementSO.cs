using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    [CreateAssetMenu(fileName = "SO_FSM_Action_Common_StopMovement", menuName = "Yiso/State Machine/Action/StopMovement")]
    public class YisoCharacterActionStopMovementSO: YisoCharacterActionSO {
        public override void PerformAction(IYisoCharacterContext context) {
            // AI의 이동을 멈춤
            var aiModule = context.GetModule<YisoCharacterAIModule>();
            aiModule?.StopMovement();
        }
    }
}