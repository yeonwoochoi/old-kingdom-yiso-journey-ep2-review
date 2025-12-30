using System;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public class YisoCharacterAIModule: YisoCharacterModuleBase {
        private Settings _settings;

        /// <summary>
        /// AI가 스폰된 초기 위치 (FSM에서 복귀 지점 판단 등에 사용)
        /// </summary>
        public Vector3 SpawnPosition { get; private set; }

        /// <summary>
        /// AI가 이동할 방향 (단위 벡터).
        /// YisoMovementAbility가 Context.MovementVector를 통해 이 값을 조회함.
        /// </summary>
        public Vector2 PathDirection { get; private set; }

        /// <summary>
        /// AI가 이동할 목표 위치.
        /// </summary>
        public Vector2? Destination { get; private set; }

        /// <summary>
        /// 목표 지점 도착 판정 거리 (이 거리 이내면 도착으로 간주)
        /// </summary>
        public const float ArrivalThreshold = 0.3f;

        public YisoCharacterAIModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }

        public override void OnEnable() {
            base.OnEnable();
            // 모듈 활성화 시점의 위치를 스폰 위치로 저장
            SpawnPosition = Context.Transform.position;
        }

        public override void OnUpdate() {
            base.OnUpdate();

            // 목표 지점이 설정되어 있지 않으면 이동하지 않음
            if (!Destination.HasValue) {
                PathDirection = Vector2.zero;
                return;
            }

            var currentPosition = (Vector2)Context.Transform.position;
            var targetPosition = Destination.Value;

            // 현재 위치와 목표 위치 사이의 거리
            var distanceToTarget = Vector2.Distance(currentPosition, targetPosition);

            // 목표 지점 도착 판정
            if (distanceToTarget <= ArrivalThreshold) {
                // 도착했으면 이동 멈춤
                PathDirection = Vector2.zero;
                Destination = null; // 목표 지점 초기화
                return;
            }

            // 이동 방향 계산 (단순 직선 추적)
            // TODO: 향후 NavMesh, A* 등으로 교체 가능
            var direction = (targetPosition - currentPosition).normalized;
            PathDirection = direction;

            // 디버그: PathDirection 확인
            Debug.Log($"[AIModule] Current: {currentPosition}, Destination: {targetPosition}, PathDirection: {PathDirection}");
        }

        #region Public API

        /// <summary>
        /// AI의 목표 지점을 설정합니다.
        /// FSM Action에서 호출됩니다.
        /// </summary>
        /// <param name="targetPosition">목표 위치 (월드 좌표)</param>
        public void SetDestination(Vector2 targetPosition) {
            Destination = targetPosition;
        }

        /// <summary>
        /// 현재 목표 지점을 초기화하고 이동을 멈춥니다.
        /// </summary>
        public void StopMovement() {
            Destination = null;
            PathDirection = Vector2.zero;
        }
        
        #endregion

        [Serializable]
        public class Settings {
            // TODO: AI 관련 설정 (시야 범위 등)
            // 예: public float detectionRadius = 5f;
        }
    }
}