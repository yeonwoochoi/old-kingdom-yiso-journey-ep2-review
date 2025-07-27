using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public interface IYisoCharacterTransition {
        
    }
    
    [CreateAssetMenu(fileName = "NewCharacterTransition", menuName = "Yiso/Gameplay/Character/State Machine/Character Transition")]
    public class YisoCharacterTransitionSO: ScriptableObject, IYisoCharacterTransition {
        
    }
}