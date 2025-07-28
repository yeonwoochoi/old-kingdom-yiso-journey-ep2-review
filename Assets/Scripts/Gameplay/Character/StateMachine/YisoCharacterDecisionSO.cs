using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public abstract class YisoCharacterDecisionSO: ScriptableObject {
        public abstract bool Decide(IYisoCharacterContext context);
    }
}