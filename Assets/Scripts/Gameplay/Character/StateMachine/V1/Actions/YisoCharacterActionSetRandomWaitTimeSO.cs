using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions {
    /// <summary>
    /// Random한 시간동안 머물어야하는 State의 경우 Start Action에 반드시 해당 action 넣어야함.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Action_Common_SetRandomWaitTime", menuName = "Yiso/State Machine/Action/SetRandomWaitTime")]
    public class YisoCharacterActionSetRandomWaitTimeSO: YisoCharacterActionSO {
        [Header("Input Keys")]
        [SerializeField] private YisoBlackboardKeySO minTimeKey;
        [SerializeField] private YisoBlackboardKeySO maxTimeKey;

        [Header("Output Key (Write)")]
        [SerializeField] private YisoBlackboardKeySO resultKey; // 계산된 시간을 저장할 키 (예: CurrentWaitTime)

        public override void PerformAction(IYisoCharacterContext context) {
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (bb == null) return;

            var min = bb.GetFloat(minTimeKey, 1f);
            var max = bb.GetFloat(maxTimeKey, 2f);

            // 여기서 한 번만 계산해서 저장!
            var randomDuration = Random.Range(min, max);
            bb.SetFloat(resultKey, randomDuration);
        }
    }
}