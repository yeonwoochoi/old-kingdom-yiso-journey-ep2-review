using UnityEngine;

namespace Gameplay.Character.StateMachine.Actions.Move {
    /// <summary>
    /// 설정된 순찰 포인트들을 순차적으로 이동하는 액션입니다.
    /// 각 포인트에 도착하면 다음 포인트로 자동으로 전환됩니다.
    /// </summary>
    public class YisoCharacterActionPatrol: YisoCharacterAction {
        [Tooltip("순찰할 위치들 (Transform 배열)")]
        [SerializeField] private Transform[] patrolPoints;

        [Tooltip("순찰 경로를 역순으로도 이동 (왕복)")]
        [SerializeField] private bool loop = false;

        [Tooltip("각 포인트에서 대기할 시간")]
        [SerializeField] private float waitTimeAtPoint = 1f;

        [Tooltip("포인트 도착 판정 거리")]
        [SerializeField] private float arrivalThreshold = 0.5f;

        private int _currentPatrolIndex = 0;
        private bool _isReversing = false; // ping-pong 모드에서 역방향 여부
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

            // 순찰 포인트가 없으면 실행 안함
            if (patrolPoints == null || patrolPoints.Length == 0) {
                Debug.LogWarning($"[Patrol] {name}: 순찰 포인트가 설정되지 않았습니다.");
                return;
            }

            // 대기 중이면 타이머 체크
            if (_isWaiting) {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= waitTimeAtPoint) {
                    _isWaiting = false;
                    _waitTimer = 0f;
                    MoveToNextPatrolPoint();
                }
                StateMachine.Owner.Move(Vector2.zero);
                return;
            }

            // 현재 목표 포인트
            var targetPoint = patrolPoints[_currentPatrolIndex];
            if (targetPoint == null) {
                Debug.LogWarning($"[Patrol] {name}: 순찰 포인트 {_currentPatrolIndex}가 null입니다.");
                MoveToNextPatrolPoint();
                return;
            }

            var currentPos = (Vector2)StateMachine.Owner.Transform.position;
            var targetPos = (Vector2)targetPoint.position;
            var distance = Vector2.Distance(currentPos, targetPos);

            // 아직 도착하지 않았으면 계속 이동
            if (distance > arrivalThreshold) {
                var direction = (targetPos - currentPos).normalized;
                StateMachine.Owner.Move(direction);
            }
            else {
                // 도착했으면 대기 시작
                StateMachine.Owner.Move(Vector2.zero);
                _isWaiting = true;
            }
        }

        private void MoveToNextPatrolPoint() {
            if (loop) {
                // 왕복 모드
                if (!_isReversing) {
                    _currentPatrolIndex++;
                    if (_currentPatrolIndex >= patrolPoints.Length) {
                        _currentPatrolIndex = patrolPoints.Length - 2;
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
                // 순환 모드
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
    }
}