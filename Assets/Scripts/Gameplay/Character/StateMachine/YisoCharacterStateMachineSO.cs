using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    [CreateAssetMenu(fileName = "SO_FSM_StateMachine_", menuName = "Yiso/State Machine/State Machine")]
    public class YisoCharacterStateMachineSO: ScriptableObject {
        [Header("State Machine Configuration")]
        [Tooltip("이 상태 머신이 처음 시작될 때의 상태.")]
        public YisoCharacterStateSO initialState;

        [Tooltip("이 상태 머신이 사용할 수 있는 모든 상태 SO들의 목록.")]
        public List<YisoCharacterStateSO> allAvailableStates;
    }
}