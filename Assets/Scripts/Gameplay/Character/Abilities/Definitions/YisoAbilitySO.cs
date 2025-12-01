using UnityEngine;

namespace Gameplay.Character.Abilities.Definitions {
    /// <summary>
    /// 캐릭터 어빌리티의 데이터와 설정을 정의하는 추상 스크립터블 오브젝트.
    /// 모든 구체적인 어빌리티 SO는 이 클래스를 상속하여 정의.
    /// </summary>
    public abstract class YisoAbilitySO : ScriptableObject {
        [Header("Priority")]
        [Tooltip("어빌리티 실행 우선순위 (높을수록 먼저 실행됨). " +
                 "동시 입력 시 높은 Priority의 어빌리티가 먼저 상태를 변경하여 " +
                 "낮은 Priority의 어빌리티가 실행되지 못하도록 차단함.")]
        public int priority = 50;

        /// <summary>
        /// 자신과 짝을 이루는 Pure C# 어빌리티(로직)의 인스턴스를 생성. (팩토리 메소드)
        /// </summary>
        /// <returns>생성된 IYisoCharacterAbility 인스턴스.</returns>
        public abstract IYisoCharacterAbility CreateAbility();
    }
}