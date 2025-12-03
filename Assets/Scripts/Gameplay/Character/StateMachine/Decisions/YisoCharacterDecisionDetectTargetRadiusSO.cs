using System.Collections.Generic;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// 일정 반경 내에서 타겟을 감지하는 Decision. (죽은 애들은 거름)
    /// 감지된 타겟은 Blackboard의 targetKey에 자동으로 저장됩니다.
    /// true: 타겟 감지됨 (Blackboard에 저장됨)
    /// false: 타겟 감지 안 됨
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_DetectTargetRadius", menuName = "Yiso/State Machine/Decision/DetectTargetRadius")]
    public class YisoCharacterDecisionDetectTargetRadiusSO: YisoCharacterDecisionSO {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRadius = 5f; // 감지 반경
        [SerializeField] private LayerMask targetLayerMask = ~0; // 감지 대상 레이어 (기본값: 모든 레이어)
        [SerializeField] private string targetTag = "Player"; // 감지 대상 태그 (빈 문자열이면 태그 체크 안 함)
        
        [Tooltip("최적화를 위한 최대 감지 개수 제한")]
        [SerializeField] private int maxDetectionCount = 10; 

        [Header("Blackboard Keys")]
        [SerializeField] private YisoBlackboardKeySO targetKey; // 감지된 타겟을 저장할 키
        
        // [최적화 핵심] 
        // 1. static으로 선언하여 모든 인스턴스가 하나의 버퍼를 공유 (메모리 절약)
        // 2. Unity 메인 스레드는 싱글 스레드이므로, 이 함수 내에서만 쓰고 빠지면 동시성 문제 없음
        private static readonly List<Collider2D> SharedResults = new(10);

        public override bool Decide(IYisoCharacterContext context) {
            var bb = context.GetModule<YisoCharacterBlackboardModule>();
            if (bb == null) return false; // Blackboard가 없으면 false

            var currentPosition = (Vector2) context.Transform.position;

            // ContactFilter2D 설정 (List 오버로드를 쓰려면 필터가 필요함)
            // ContactFilter2D는 struct임
            var filter = new ContactFilter2D {
                useTriggers = true,
                useLayerMask = true,
                layerMask = targetLayerMask
            };

            // Physics2D.OverlapCircle(point, radius, filter, list)
            var count = Physics2D.OverlapCircle(currentPosition, detectionRadius, filter, SharedResults);

            for (var i = 0; i < count; i++) {
                var col = SharedResults[i];
                
                // 자기 자신 제외
                if (col.gameObject == context.GameObject) continue;

                // 태그 체크 (targetTag가 빈 문자열이 아닐 때만)
                if (!string.IsNullOrEmpty(targetTag) && !col.CompareTag(targetTag)) continue;
                
                // 살아있는지 확인 (Dead Check)
                // YisoCharacterContext나 EntityHealth를 가져와서 확인
                var targetContext = col.GetComponent<IYisoCharacterContext>();
                
                // Context가 있고, 이미 죽은 상태라면 -> 감지 대상에서 제외 (Continue)
                if (targetContext != null && targetContext.IsDead()) {
                    continue; 
                }
                
                var health = col.GetComponent<YisoEntityHealth>();
                if (health != null && health.IsDead) continue;
                
                // 타겟 발견! Blackboard에 저장
                bb.SetObject(targetKey, col.transform);
                return true;
            }

            // 타겟을 찾지 못함
            return false;
        }
    }
}