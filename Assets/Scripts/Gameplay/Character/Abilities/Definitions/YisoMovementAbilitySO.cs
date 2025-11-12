using UnityEngine;

namespace Gameplay.Character.Abilities.Definitions
{
    [CreateAssetMenu(fileName = "New MovementAbility", menuName = "Yiso/Abilities/Movement")]
    public class YisoMovementAbilitySO : YisoAbilitySO
    {
        // V1의 Settings 클래스 역할을 이 SO가 직접 합니다.
        [Header("Movement Settings")]
        [Tooltip("기본 이동 속도입니다. 실제 속도는 캐릭터의 스탯에 따라 달라질 수 있습니다.")]
        public float baseMovementSpeed = 5f;

        [Tooltip("이 값 이하의 입력은 'Idle'로 간주합니다.")]
        public float idleThreshold = 0.05f;

        [Tooltip("이동 시작 시의 가속도입니다. 높을수록 빨리 최고 속도에 도달합니다.")]
        public float acceleration = 10f;

        [Tooltip("이동 정지 시의 감속도입니다. 높을수록 빨리 멈춥니다.")]
        public float deceleration = 10f;

        [Tooltip("true: 조이스틱처럼 입력 강도에 따라 속도가 조절됩니다.\nfalse: WASD처럼 누르면 즉시 최대 속도로 움직입니다.")]
        public bool useAnalogInput = false;

        // "나는 YisoMovementAbility(로직)를 만드는 공장이야."
        public override IYisoCharacterAbility CreateAbility()
        {
            // 자신(SO)을 생성자의 인자로 넘겨, 로직 클래스가 데이터를 참조할 수 있게 함
            return new YisoMovementAbility(this);
        }
    }
}