using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    public class YisoCharacterDecisionTimeInState: YisoCharacterDecision {
        [Title("Settings")]
        [MinMaxSlider(0f, 10f, true)]
        [SerializeField] private Vector2 timeRange = new Vector2(1f, 2f);

        private float _duration;

        public override bool Decide() {
            if (StateMachine == null) return false;
            return StateMachine.TimeInCurrentState >= _duration;
        }

        public override void OnEnterState() {
            var min = Mathf.Min(timeRange.x, timeRange.y);
            var max = Mathf.Max(timeRange.x, timeRange.y);
            
            _duration = Random.Range(min, max);
        }

        public override void OnExitState() {
            _duration = 0f;
        }
    }
}