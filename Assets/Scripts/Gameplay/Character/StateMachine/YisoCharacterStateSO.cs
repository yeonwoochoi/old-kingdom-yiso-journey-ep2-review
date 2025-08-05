using Gameplay.Character.Core;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public interface IYisoCharacterState {
        
    }
    
    [CreateAssetMenu(fileName = "NewCharacterState", menuName = "Yiso/Gameplay/Character/State Machine/Character State")]
    public class YisoCharacterStateSO: ScriptableObject, IYisoCharacterState {
        [SerializeField] private string stateName;

        public virtual void OnEnter(IYisoCharacterContext context) {
            
        }

        public virtual void OnExit(IYisoCharacterContext context) {
            
        }

        public virtual void OnUpdate(IYisoCharacterContext context) {
            
        }

        public virtual bool CheckTransitions(IYisoCharacterContext context, out YisoCharacterStateSO newState) {
            newState = this;
            return true;
        }
    }
}