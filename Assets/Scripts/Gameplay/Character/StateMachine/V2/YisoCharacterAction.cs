using Core.Behaviour;
using UnityEngine;

namespace Gameplay.Character.StateMachine.V2 {
    public abstract class YisoCharacterAction: RunIBehaviour {
        protected YisoCharacterStateMachine StateMachine { get; private set; }

        protected override void Awake() {
            base.Awake();
            StateMachine = GetComponentInParent<YisoCharacterStateMachine>();
            if (StateMachine == null) {
                Debug.LogError($"[Action] {name}: 부모에서 YisoCharacterStateMachine을 찾을 수 없습니다.");
            }
        }
        
        public abstract void PerformAction();
        
        public virtual void OnEnterState() { }
        public virtual void OnExitState() { }
    }
}