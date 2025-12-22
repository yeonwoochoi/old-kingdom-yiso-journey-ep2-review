using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public abstract class YisoCharacterActionSO: ScriptableObject {
        [Multiline, SerializeField] private string description; // 그냥 설명 쓰라고 추가한 필드
        public abstract void PerformAction(IYisoCharacterContext context);
    }
}