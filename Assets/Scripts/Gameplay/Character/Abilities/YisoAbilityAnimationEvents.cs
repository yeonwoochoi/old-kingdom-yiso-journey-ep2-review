namespace Gameplay.Character.Abilities {
    /// <summary>
    /// Ability 애니메이션 이벤트 이름을 정의하는 상수 클래스.
    /// Magic String 방지 및 코드 간 일관성 유지를 위해 사용합니다.
    /// </summary>
    public static class YisoAbilityAnimationEvents {
        // ========== Attack Events ==========

        /// <summary>
        /// 공격 데미지 판정 활성화 이벤트.
        /// Animator의 Attack 애니메이션에서 호출됩니다.
        /// </summary>
        public const string ATTACK_ENABLE_DAMAGE = "EnableDamage";

        /// <summary>
        /// 공격 데미지 판정 비활성화 이벤트.
        /// Animator의 Attack 애니메이션에서 호출됩니다.
        /// </summary>
        public const string ATTACK_DISABLE_DAMAGE = "DisableDamage";

        /// <summary>
        /// 공격 종료 이벤트.
        /// Animator의 Attack 애니메이션 끝에서 호출됩니다.
        /// </summary>
        public const string ATTACK_END = "AttackEnd";

        // ========== Skill Events ==========

        /// <summary>
        /// 스킬 시작 이벤트.
        /// Animator의 Skill 애니메이션에서 호출됩니다.
        /// </summary>
        public const string SKILL_START = "SkillStart";

        /// <summary>
        /// 스킬 종료 이벤트.
        /// Animator의 Skill 애니메이션 끝에서 호출됩니다.
        /// </summary>
        public const string SKILL_END = "SkillEnd";

        // ========== Movement Events ==========

        /// <summary>
        /// 발소리/발자국 이벤트.
        /// Animator의 Walk/Run 애니메이션에서 호출됩니다.
        /// </summary>
        public const string MOVEMENT_FOOTSTEP = "Footstep";
    }
}