using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Gameplay.Character.StateMachine.Utils {
    public static class YisoStateMachineUtils {
        // ========================================================================
        // 1. 거리 및 방향 계산 (Math)
        // ========================================================================

        /// <summary>
        /// 두 위치 사이의 거리 제곱(SqrMagnitude)을 반환. (성능 최적화용)
        /// 루트 연산(Mathf.Sqrt)을 하지 않아 가벼움. (거리 비교시 이거 사용하셈)
        /// </summary>
        public static float GetDistanceSqr(Vector2 a, Vector2 b) {
            return (a - b).sqrMagnitude;
        }

        /// <summary>
        /// 두 위치 사이의 실제 거리를 반환.
        /// 거리 비교는 GetDistanceSqr, 실제 거리 필요하면 GetDistance
        /// </summary>
        public static float GetDistance(Vector2 a, Vector2 b) {
            return Vector2.Distance(a, b);
        }

        /// <summary>
        /// 'From'에서 'To'로 향하는 정규화된(Normalized) 방향 벡터를 반환.
        /// </summary>
        public static Vector2 GetDirectionNormalized(Vector2 from, Vector2 to) {
            return (to - from).normalized;
        }

        // ========================================================================
        // 2. 타겟 감지 (Detection - Radius)
        // ========================================================================

        // 메모리 할당 방지를 위한 재사용 버퍼 (필요 시 크기 조절)
        private static Collider2D[] _targetBuffer = new Collider2D[50];

        /// <summary>
        /// 반경 내에서 '가장 가까운' 타겟 하나를 반환.
        /// </summary>
        /// <returns>찾은 타겟의 Transform (없으면 null)</returns>
        public static Transform FindClosestTarget(Vector2 origin, float radius, LayerMask targetLayer) {
            var filter = new ContactFilter2D {useTriggers = true, useLayerMask = true, layerMask = targetLayer};
            
            while (true) {
                var count = Physics2D.OverlapCircle(origin, radius, filter, _targetBuffer);

                // [방어 코드] 버퍼가 꽉 찼는지 확인 (더 있을 가능성 있음) -> 리사이징
                if (count == _targetBuffer.Length) {
                    // [안전 장치] 너무 무식하게 커지면(예: 1000개 이상) 1000개로 제한 (그 이상은 버려)
                    if (_targetBuffer.Length >= 1000) {
                        YisoLogger.LogWarning($"FindClosestTarget: 타겟이 너무 많습니다({count}). 검색 범위를 제한합니다.");
                    }
                    else {
                        // 버퍼 2배 확장 후 재검색 (continue)
                        _targetBuffer = new Collider2D[_targetBuffer.Length * 2];
                        continue;
                    }
                }
                
                Transform closestTarget = null;
                var closestDistSqr = float.MaxValue;

                for (var i = 0; i < count; i++) {
                    var targetCol = _targetBuffer[i];
                    if (targetCol == null) continue; // 방어 코드

                    var distSqr = GetDistanceSqr(origin, targetCol.transform.position);

                    if (distSqr < closestDistSqr) {
                        closestDistSqr = distSqr;
                        closestTarget = targetCol.transform;
                    }
                }

                Array.Clear(_targetBuffer, 0, count);

                return closestTarget;
            }
            
            return null; // Safety Cap에 걸렸을 때의 처리
        }

        /// <summary>
        /// 반경 내의 '모든' 타겟을 리스트에 담아 반환합니다.
        /// </summary>
        public static int FindAllTargets(Vector2 origin, float radius, LayerMask targetLayer, List<Transform> results) {
            results.Clear();
            var filter = new ContactFilter2D {useTriggers = true, useLayerMask = true, layerMask = targetLayer};
            
            while (true) {
                var count = Physics2D.OverlapCircle(origin, radius, filter, _targetBuffer);
        
                // 1. 버퍼가 꽉 찼는지 확인 (더 있을 가능성 있음)
                if (count == _targetBuffer.Length) {

                    // 2. 안전장치: 너무 크면(1000개 이상) 더 늘리지 않고 현재 찾은 것만 처리
                    if (_targetBuffer.Length >= 1000) {
                        YisoLogger.LogWarning($"타겟이 너무 많습니다({count}+). 버퍼 한계({_targetBuffer.Length})로 검색을 중단하고 현재 분량만 반환합니다.");
                        // continue 하지 않고 아래로 내려가서 처리함
                    }
                    else {
                        // 3. 버퍼 2배 확장 후 재검색
                        _targetBuffer = new Collider2D[_targetBuffer.Length * 2];
                        continue;
                    }
                }

                // 4. 결과 담기
                for (var i = 0; i < count; i++) {
                    // 혹시 모를 null 방어
                    if (_targetBuffer[i] != null) {
                        results.Add(_targetBuffer[i].transform);
                    }
                }

                // 5. 메모리 정리
                Array.Clear(_targetBuffer, 0, count);

                return count; 
            }
        }

        // ========================================================================
        // 3. 시야각 감지 (Detection - Cone of Vision)
        // ========================================================================

        /// <summary>
        /// 타겟이 부채꼴 시야각(Cone) 안에 있고, 장애물에 가려지지 않았는지 확인합니다.
        /// </summary>
        /// <param name="observer">감시자 (Transform)</param>
        /// <param name="target">대상 (Transform)</param>
        /// <param name="viewAngle">시야각 (예: 90도)</param>
        /// <param name="viewDistance">시야 거리</param>
        /// <param name="obstacleMask">장애물(벽) 레이어</param>
        public static bool IsTargetInSight(Transform observer, Transform target, float viewAngle, float viewDistance, LayerMask obstacleMask) {
            if (target == null) return false;

            Vector2 observerPos = observer.position;
            Vector2 targetPos = target.position;
            var dirToTarget = (targetPos - observerPos);

            // 1. 거리 체크 (SqrMagnitude 활용)
            if (dirToTarget.sqrMagnitude > viewDistance * viewDistance) return false;

            // 2. 각도 체크
            // 2D Top-Down에서 캐릭터 정면은 보통 transform.up 또는 transform.right 임을 유의
            // 여기서는 transform.right(X축 위)을 정면으로 가정. (스프라이트 회전에 따라 수정 필요)
            Vector2 forward = observer.right;

            // 정규화된 방향 벡터 필요
            var dirToTargetNormalized = dirToTarget.normalized;
            var angle = Vector2.Angle(forward, dirToTargetNormalized);
            
            if (angle > viewAngle * 0.5f) return false; // 시야각의 절반보다 크면 시야 밖

            // 3. 장애물 체크 (Raycast - Line of Sight)
            if (!HasLineOfSight(observerPos, targetPos, viewDistance, obstacleMask)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 두 지점 사이에 장애물이 없는지 확인합니다. (Raycast)
        /// </summary>
        public static bool HasLineOfSight(Vector2 start, Vector2 end, float distance, LayerMask obstacleMask) {
            var direction = (end - start).normalized;
            var distToTarget = Vector2.Distance(start, end);

            // 타겟까지의 거리만큼만 Ray를 쏘아 장애물이 걸리는지 확인
            // RaycastDistance를 min(시야거리, 실제 타겟거리)로 설정하여 타겟 뒤의 벽은 무시
            var checkDistance = Mathf.Min(distance, distToTarget);

            // 자기 자신 Collider 충돌 방지를 위해 start에서 약간 띄우거나, LayerCollisionMatrix 설정 권장
            // 여기서는 2D Raycast 사용
            var hit = Physics2D.Raycast(start, direction, checkDistance, obstacleMask);

            // 충돌한 것이 있다면(장애물) 시야가 막힌 것
            return hit.collider == null;
        }

        // ========================================================================
        // 4. 랜덤 위치 계산 (Wander)
        // ========================================================================

        /// <summary>
        /// 원점(origin) 기준 반경(radius) 내에서, 장애물이 없는 랜덤한 위치를 찾습니다.
        /// </summary>
        /// <param name="origin">중심점 (보통 SpawnPosition)</param>
        /// <param name="radius">반경</param>
        /// <param name="obstacleMask">피해야 할 장애물 레이어</param>
        /// <param name="tryCount">실패 시 재시도 횟수</param>
        /// <returns>유효한 위치 (실패 시 origin 반환)</returns>
        public static Vector2 GetRandomPointInCircle(Vector2 origin, float radius, LayerMask obstacleMask, int tryCount = 20) {
            for (var i = 0; i < tryCount; i++) {
                // 랜덤 벡터 생성
                var randomOffset = Random.insideUnitCircle * radius;
                var candidatePos = origin + randomOffset;

                // 해당 위치에 장애물이 있는지 체크 (OverlapCircle or OverlapPoint)
                // 캐릭터 크기만큼의 반경(예: 0.5f)을 체크하는 것이 안전함
                if (!Physics2D.OverlapCircle(candidatePos, 0.5f, obstacleMask)) {
                    return candidatePos;
                }
            }

            // 적절한 위치를 못 찾으면 원래 위치 반환
            return origin;
        }
    }
}