using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    /// <summary>
    /// AI가 스폰 위치로 돌아가는 액션입니다.
    /// StateMachine의 SpawnPosition을 목표로 설정하여 귀환합니다.
    /// </summary>
    public class YisoCharacterActionReturnToSpawn: YisoCharacterAction {
        [Tooltip("스폰 위치 도착 판정 거리")]
        [SerializeField] private float arrivalThreshold = 0.5f;

        public override void PerformAction() {
            if (StateMachine?.Owner == null) return;

            var distance = StateMachine.GetDistanceToSpawn();

            // 이미 스폰 위치에 있으면 이동하지 않음
            if (distance <= arrivalThreshold) {
                StateMachine.Owner.Move(Vector2.zero);
                return;
            }

            // 스폰 위치로 이동
            var direction = StateMachine.GetDirectionToSpawn();
            StateMachine.Owner.Move(direction);
        }
    }
}