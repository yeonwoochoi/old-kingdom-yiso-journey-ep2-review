using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public interface IYisoCharacterDecision {
        
    }
    
    [CreateAssetMenu(fileName = "NewCharacterDecision", menuName = "Yiso/Gameplay/Character/State Machine/Character Decision")]
    public class YisoCharacterDecisionSO: ScriptableObject, IYisoCharacterDecision {
        
    }
}