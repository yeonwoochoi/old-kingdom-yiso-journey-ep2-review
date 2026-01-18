using Core.Behaviour;
using UnityEngine;
using Utils;

namespace Gameplay.Character.StateMachine {
    public abstract class YisoCharacterDecision: RunIBehaviour {
        protected enum ComparisonMode {
            LessThan,
            LessOrEqual,
            Equal,
            GreaterOrEqual,
            GreaterThan,
            NotEqual
        }
        
        protected YisoCharacterStateMachine StateMachine { get; private set; }

        /// <summary>
        /// Odin Inspector의 MaxValue 속성에서 사용할 수 있는 최대 슬롯 인덱스
        /// </summary>
        protected int MaxSlotIndex {
            get {
                if (StateMachine != null) {
                    return Mathf.Max(1, StateMachine.MaxTargetCount - 1);
                }

                var fsm = GetComponentInParent<YisoCharacterStateMachine>();
                return fsm != null ? Mathf.Max(1, fsm.MaxTargetCount - 1) : 9;
            }
        }

        protected override void Awake() {
            base.Awake();
            StateMachine = GetComponentInParent<YisoCharacterStateMachine>();
            if (StateMachine == null) {
                YisoLogger.LogError($"{name}: 부모에서 YisoCharacterStateMachine을 찾을 수 없습니다.");
            }
        }

        public abstract bool Decide();

        public virtual void OnEnterState() { }
        public virtual void OnExitState() { }
    }
}