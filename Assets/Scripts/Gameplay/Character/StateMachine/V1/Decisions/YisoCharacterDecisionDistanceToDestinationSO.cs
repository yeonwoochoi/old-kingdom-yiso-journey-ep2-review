using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.Decisions {
    /// <summary>
    /// AI의 현재 위치와 목표 지점(Destination) 간 거리를 체크하는 Decision.
    /// 1. 도착 판정 (AI 모듈의 기본 정지 거리 사용)
    /// 2. 거리 판정 (직접 입력한 거리 사용)
    /// 두 가지 모드를 모두 지원.
    /// true: 거리가 distanceThreshold 이하 (목표 지점에 충분히 가까움)
    /// false: 거리가 distanceThreshold 초과 (아직 멀리 있음)
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FSM_Decision_Common_DistanceToDestination", menuName = "Yiso/State Machine/Decision/DistanceToDestination")]
    public class YisoCharacterDecisionDistanceToDestinationSO : YisoCharacterDecisionSO {
        [Tooltip("체크하면 AI 모듈에 설정된 기본 정지 거리(StoppingDistance)를 사용합니다.\n(주로 이동 완료 판정에 사용)")]
        [SerializeField] private bool useDefaultStoppingDistance = true;

        [Tooltip("직접 설정할 거리 값")]
        [SerializeField, HideIf(nameof(useDefaultStoppingDistance))] 
        private float distanceThreshold = 0.1f;

        public override bool Decide(IYisoCharacterContext context) {
            // AI 모듈 가져오기
            var aiModule = context.GetModule<YisoCharacterAIModule>();

            // 목표 지점이 설정되어 있지 않으면 false (거리 계산 불가)
            if (aiModule?.Destination == null) {
                return false;
            }

            // 현재 위치와 목표 지점 간 거리 계산
            var currentPosition = (Vector2)context.Transform.position;
            var targetPosition = aiModule.Destination.Value;
            var distance = Vector2.Distance(currentPosition, targetPosition);

            // 비교할 기준 거리 결정
            var threshold = useDefaultStoppingDistance ? YisoCharacterAIModule.ArrivalThreshold : distanceThreshold;
            
            // 거리가 threshold 이하면 true
            return distance <= threshold;
        }
    }
}