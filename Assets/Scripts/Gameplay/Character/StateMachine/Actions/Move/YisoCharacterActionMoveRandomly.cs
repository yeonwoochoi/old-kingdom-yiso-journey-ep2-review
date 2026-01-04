using Gameplay.Character.StateMachine.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    /// <summary>
    /// 랜덤한 방향으로 이동하는 액션입니다.
    /// 일정 시간마다 새로운 랜덤 방향을 선택합니다.
    /// </summary>
    public class YisoCharacterActionMoveRandomly: YisoCharacterAction {
        [Title("Settings")]
        [Tooltip("다음 랜덤 위치를 탐색하기 전까지의 간격 (초)")]
        [SerializeField] private float directionChangeInterval = 2f;
        
        [Tooltip("스폰 위치 기준 배회 반경")]
        [SerializeField] private float radius = 4f;
        
        [Tooltip("목표 지점 도착으로 간주할 거리")]
        [SerializeField] private float stopDistance = 0.1f;
        
        [Tooltip("이동 불가능한 구역(벽 등) 레이어")]
        [SerializeField] private LayerMask obstacleMask;

        private float _nextDirectionChangeTime;
        private Vector2 _targetPosition;
        private bool _isWaiting = false;

        public override void OnEnterState() {
            base.OnEnterState();
            // 상태 진입 시 즉시 새로운 목표 설정
            SetNewDestination();
        }

        public override void PerformAction() {
            if (StateMachine?.Owner == null) return;

            // 1. 시간 체크: 일정 시간이 지나면 새로운 목표 설정
            if (Time.time >= _nextDirectionChangeTime) {
                SetNewDestination();
            }

            // 2. 이동 로직 수행
            MoveToTarget();
        }

        private void SetNewDestination() {
            // 현재 위치가 아닌 '스폰 위치(SpawnPosition)'를 기준으로 랜덤 점을 찾음.
            // 이렇게 해야 몬스터가 초기 위치에서 멀리 벗어나지 않음.
            Vector2 origin = StateMachine.SpawnPosition;
            
            // 유틸리티를 사용하여 장애물이 없는 랜덤 위치 획득
            _targetPosition = YisoStateMachineUtils.GetRandomPointInCircle(origin, radius, obstacleMask);
            
            // 다음 변경 시간 설정
            _nextDirectionChangeTime = Time.time + directionChangeInterval;
            _isWaiting = false;
        }

        private void MoveToTarget() {
            // 현재 위치
            Vector2 currentPos = StateMachine.GetCurrentPosition();
            
            // 목표까지의 거리 제곱 계산 (최적화)
            var distSqr = YisoStateMachineUtils.GetDistanceSqr(currentPos, _targetPosition);

            // 목표 지점에 거의 도달했는지 확인
            if (distSqr <= stopDistance * stopDistance) {
                // 도착했으면 멈춤 (불필요한 물리 연산 및 떨림 방지)
                if (!_isWaiting) {
                    StateMachine.Owner.Move(Vector2.zero);
                    _isWaiting = true;
                }
            }
            else {
                // 도착하지 않았으면 목표 방향으로 이동
                var direction = (_targetPosition - currentPos).normalized;
                StateMachine.Owner.Move(direction);
                _isWaiting = false;
            }
        }
    }
}