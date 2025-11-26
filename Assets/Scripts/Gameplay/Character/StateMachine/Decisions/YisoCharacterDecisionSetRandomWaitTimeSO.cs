using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_SetRandomWaitTime", menuName = "Yiso/State Machine/Decision/SetRandomWaitTime")]
    public class YisoCharacterDecisionSetRandomWaitTimeSO: YisoCharacterDecisionSO {
        [SerializeField] private YisoBlackboardKeySO targetTimeKey; // Action이 저장했던 그 키 (예: CurrentWaitTime)

        public override bool Decide(IYisoCharacterContext context) {
            var blackboardModule = context.GetModule<YisoCharacterBlackboardModule>();
            var stateModule = context.GetModule<YisoCharacterStateModule>();
            
            if (blackboardModule == null || stateModule == null) return false;

            // 저장된 목표 시간을 가져옴
            var targetDuration = blackboardModule.GetFloat(targetTimeKey);
            
            // 현재 경과 시간과 비교
            return stateModule.TimeInCurrentState >= targetDuration;
        }
    }
}