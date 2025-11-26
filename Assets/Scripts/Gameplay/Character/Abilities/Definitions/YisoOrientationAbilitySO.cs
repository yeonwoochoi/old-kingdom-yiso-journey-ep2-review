using UnityEngine;

namespace Gameplay.Character.Abilities.Definitions {
    [CreateAssetMenu(fileName = "SO_Ability_Orientation", menuName = "Yiso/Abilities/Orientation")]
    public class YisoOrientationAbilitySO: YisoAbilitySO {
        [Header("Orientation Settings")]
        [Tooltip("캐릭터가 처음 생성될 때 바라볼 기본 방향입니다.")]
        public FacingDirection initialFacingDirection = FacingDirection.East;
        
        [Tooltip("이 값 이하의 이동 입력은 방향 결정에 영향을 주지 않습니다.")]
        public float movementThreshold = 0.05f;

        [Tooltip("이 값 이하의 조준 입력은 방향 결정에 영향을 주지 않습니다.")]
        public float aimThreshold = 0.1f;
        
        public override IYisoCharacterAbility CreateAbility() {
            return new YisoOrientationAbility(this);
        }
    }
}