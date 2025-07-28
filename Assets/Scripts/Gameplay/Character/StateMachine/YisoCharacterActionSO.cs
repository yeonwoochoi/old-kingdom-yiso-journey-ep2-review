using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public abstract class YisoCharacterActionSO: ScriptableObject {
        public abstract void PerformAction(IYisoCharacterContext context);
    }
}