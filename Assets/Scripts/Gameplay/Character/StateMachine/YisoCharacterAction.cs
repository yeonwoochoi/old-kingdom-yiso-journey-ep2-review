using Core.Behaviour;
using Gameplay.Character.Abilities;
using Gameplay.Character.Core.Modules;
using UnityEngine;
using Utils;

namespace Gameplay.Character.StateMachine {
    public abstract class YisoCharacterAction: RunIBehaviour {
        protected YisoCharacterStateMachine StateMachine { get; private set; }
        private YisoMovementAbility _movementAbility;
        protected YisoMovementAbility MovementAbility
        {
            get
            {
                if (_movementAbility == null)
                {
                    var abilityModule = StateMachine.Owner.GetModule<YisoCharacterAbilityModule>();
                    _movementAbility = abilityModule?.GetAbility<YisoMovementAbility>();
                }
                return _movementAbility;
            }
        }

        protected override void Awake() {
            base.Awake();
            StateMachine = GetComponentInParent<YisoCharacterStateMachine>();
            if (StateMachine == null) {
                YisoLogger.LogError($"{name}: 부모에서 YisoCharacterStateMachine을 찾을 수 없습니다.");
            }
        }
        
        public abstract void PerformAction();
        
        public virtual void OnEnterState() { }
        public virtual void OnExitState() { }
    }
}