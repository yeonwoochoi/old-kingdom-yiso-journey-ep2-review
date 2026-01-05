using Gameplay.Character.StateMachine.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    /// <summary>
    /// 스폰 위치를 기준으로 설정된 오프셋 좌표들을 순찰하는 액션입니다.
    /// Prefab 상태에서 경로를 지정할 수 있어 재사용성이 높습니다.
    /// </summary>
    public class YisoCharacterActionPatrol : YisoCharacterAction {
        [Title("Settings")]
        [Tooltip("스폰 위치 기준 순찰 경로 오프셋 (Transform)")]
        [InfoBox("스폰 위치가 (0,0)입니다. 예: (0,0) -> (3,0) -> (3,3) -> (0,3)")]
        [SerializeField] private Transform[] patrolOffsets;

        [Tooltip("순찰 방식: True = 왕복(PingPong), False = 순환(Loop)")]
        [SerializeField] private bool pingPong = false;

        [Tooltip("각 포인트에서 대기할 시간")]
        [SerializeField] private float waitTimeAtPoint = 1f;

        [Tooltip("포인트 도착 판정 거리")]
        [SerializeField] private float arrivalThreshold = 0.1f;

        private int _currentPatrolIndex = 0;
        private bool _isReversing = false; // PingPong 모드용 역방향 플래그
        private float _waitTimer = 0f;
        private bool _isWaiting = false;

        public override void OnEnterState() {
            base.OnEnterState();
            _currentPatrolIndex = 0;
            _isReversing = false;
            _isWaiting = false;
            _waitTimer = 0f;
        }

        public override void PerformAction() {
            if (StateMachine?.Owner == null) return;

            // 오프셋이 설정되지 않았으면 실행 안함
            if (patrolOffsets == null || patrolOffsets.Length == 0) {
                return;
            }

            // 1. 대기 중 로직
            if (_isWaiting) {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= waitTimeAtPoint) {
                    _isWaiting = false;
                    _waitTimer = 0f;
                    SetNextPatrolIndex();
                }
                // 대기 중에는 정지
                StateMachine.Owner.Move(Vector2.zero);
                return;
            }

            // 2. 현재 목표 위치 계산 (스폰 위치 + 오프셋)
            // StateMachine.SpawnPosition은 Vector3이므로 Vector2로 캐스팅
            Vector2 spawnPos = StateMachine.SpawnPosition; 
            Vector2 targetOffset = patrolOffsets[_currentPatrolIndex].position;
            Vector2 targetPos = spawnPos + targetOffset;

            // 3. 거리 계산 및 이동
            Vector2 currentPos = StateMachine.Owner.Transform.position;
            
            // 거리 계산 최적화 (SqrMagnitude 사용)
            float distSqr = YisoStateMachineUtils.GetDistanceSqr(currentPos, targetPos);

            if (distSqr > arrivalThreshold * arrivalThreshold) {
                // 아직 도착 안 함 -> 이동
                Vector2 direction = (targetPos - currentPos).normalized;
                StateMachine.Owner.Move(direction);
            }
            else {
                // 도착함 -> 대기 시작
                StateMachine.Owner.Move(Vector2.zero);
                _isWaiting = true;
            }
        }

        private void SetNextPatrolIndex() {
            if (patrolOffsets.Length <= 1) return; // 포인트가 1개 이하면 인덱스 변경 불필요

            if (pingPong) {
                // 왕복 모드 (0 -> 1 -> 2 -> 1 -> 0)
                if (!_isReversing) {
                    _currentPatrolIndex++;
                    if (_currentPatrolIndex >= patrolOffsets.Length) {
                        _currentPatrolIndex = patrolOffsets.Length - 2;
                        _isReversing = true;
                    }
                }
                else {
                    _currentPatrolIndex--;
                    if (_currentPatrolIndex < 0) {
                        _currentPatrolIndex = 1;
                        _isReversing = false;
                    }
                }
            }
            else {
                // 순환 모드 (0 -> 1 -> 2 -> 0)
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolOffsets.Length;
            }
        }

        /// <summary>
        /// 에디터에서 순찰 경로를 미리보기 위한 기즈모
        /// </summary>
        private void OnDrawGizmosSelected() {
            if (patrolOffsets == null || patrolOffsets.Length == 0) return;

            // 기준점 설정
            // 런타임: 실제 스폰 위치 / 에디터: 현재 프리팹 위치
            Vector3 basePosition = (Application.isPlaying && StateMachine != null) 
                ? StateMachine.SpawnPosition 
                : transform.position;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < patrolOffsets.Length; i++) {
                Vector3 point = basePosition + (Vector3)patrolOffsets[i].position;
                
                // 포인트 표시
                Gizmos.DrawSphere(point, 0.2f);

                // 경로 선 연결
                if (i < patrolOffsets.Length - 1) {
                    Vector3 nextPoint = basePosition + (Vector3)patrolOffsets[i + 1].position;
                    Gizmos.DrawLine(point, nextPoint);
                }
                else if (!pingPong && patrolOffsets.Length > 1) {
                    // 순환 모드일 경우 마지막 -> 처음 연결
                    Vector3 firstPoint = basePosition + (Vector3)patrolOffsets[0].position;
                    Gizmos.DrawLine(point, firstPoint);
                }
            }

            // 현재 목표 지점 표시 (런타임 전용)
            if (Application.isPlaying && StateMachine != null) {
                Vector3 currentTarget = basePosition + (Vector3)patrolOffsets[_currentPatrolIndex].position;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(StateMachine.Owner.Transform.position, currentTarget);
            }
        }
    }
}