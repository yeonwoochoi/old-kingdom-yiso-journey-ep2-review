using Core.Behaviour;
using UnityEngine;

namespace Gameplay.Character.Core {
    
    [AddComponentMenu("Yiso/Character/Core/Character Ability")]
    public class YisoCharacterAbility: RunIBehaviour {
        public bool AbilityInitialized { get; private set; } = false;

        public virtual void UpdateAnimator() {
            
        }
    }
}