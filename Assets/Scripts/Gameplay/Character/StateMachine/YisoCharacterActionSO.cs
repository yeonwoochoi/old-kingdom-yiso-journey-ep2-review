using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public interface IYisoCharacterAction {
        
    }
    
    [CreateAssetMenu(fileName = "NewCharacterAction", menuName = "Yiso/Gameplay/Character/State Machine/Character Action")]
    public class YisoCharacterActionSO: ScriptableObject, IYisoCharacterAction {
        
    }
}