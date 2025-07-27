using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public interface IYisoCharacterState {
        
    }
    
    [CreateAssetMenu(fileName = "NewCharacterState", menuName = "Yiso/Gameplay/Character/State Machine/Character State")]
    public class YisoCharacterStateSO: ScriptableObject, IYisoCharacterState {
        [SerializeField] private string stateName;
    }
}