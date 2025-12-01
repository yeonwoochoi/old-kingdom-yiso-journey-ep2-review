using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Character.Abilities;
using Gameplay.Character.Abilities.Definitions;

namespace Gameplay.Character.Core.Modules {
    /// <summary>
    /// 캐릭터의 모든 어빌리티(Pure C# 클래스)를 생성하고 생명주기를 관리하는 모듈.
    /// </summary>
    public sealed class YisoCharacterAbilityModule : YisoCharacterModuleBase {
        // 어빌리티 타입으로 빠른 조회를 위한 딕셔너리.
        private readonly Dictionary<Type, IYisoCharacterAbility> _abilities;
        // 순회(iteration) 성능을 위해 캐시된 어빌리티 리스트.
        // Priority 내림차순으로 정렬되어 있음 (높은 우선순위가 먼저 실행됨).
        private readonly List<IYisoCharacterAbility> _abilityList;

        private readonly Settings _settings;

        public YisoCharacterAbilityModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
            _abilities = new Dictionary<Type, IYisoCharacterAbility>();

            // Priority 기준으로 내림차순 정렬된 AbilitySO 리스트에서 어빌리티 인스턴스를 생성.
            // 이를 통해 동시 입력 시 우선순위 높은 어빌리티가 먼저 상태를 선점할 수 있음.
            var sortedAbilitySOs = settings.abilities
                .OrderByDescending(abilitySo => abilitySo.priority)
                .ToList();

            // ScriptableObject(데이터)로부터 실제 어빌리티(로직) 인스턴스를 생성.
            _abilityList = sortedAbilitySOs.Select(abilitySo => abilitySo.CreateAbility()).ToList();

            // 빠른 조회를 위해 딕셔너리에 등록.
            foreach (var ability in _abilityList) {
                _abilities[ability.GetType()] = ability;
            }
        }

        // --- 생명주기 전파 ---
        
        public override void Initialize() {
            base.Initialize();
            foreach (var ability in _abilityList) {
                ability.Initialize(Context);
            }
        }

        public override void LateInitialize() {
            base.LateInitialize();
            foreach (var ability in _abilityList) {
                ability.LateInitialize();
            }
        }

        public override void OnEnable() {
            base.OnEnable();
            foreach (var ability in _abilityList) {
                ability.OnEnable();
            }
        }

        public override void OnDisable() {
            base.OnDisable();
            foreach (var ability in _abilityList) {
                ability.OnDisable();
            }
        }

        public override void OnUpdate() {
            base.OnUpdate();

            // 활성화된 어빌리티에 대해서만 업데이트 로직 실행.
            // _abilityList는 Priority 내림차순으로 정렬되어 있음 (높은 우선순위가 먼저 실행).
            //
            // [Priority 시스템 동작 시나리오]
            // 상황: Idle 상태에서 Attack(Priority 50)과 Skill(Priority 100) 입력이 동시에 들어옴.
            //
            // 동작:
            // 1. AbilityModule이 리스트를 순회하며 SkillAbility(Priority 100)를 먼저 실행.
            // 2. SkillAbility가 PreProcessAbility()에서 RequestStateChange(Skill)을 수행.
            // 3. 상태가 Skill로 변경되고, CanCastAbility = false로 잠김.
            // 4. 그다음 AttackAbility(Priority 50)가 실행되려 하지만,
            //    이미 상태가 Skill로 바뀌었고 CanCastAbility가 false이므로
            //    IsAbilityEnabled 체크에서 차단되어 진입하지 못함.
            //
            // 결과: "따닥" 하는 Glitch 없이 우선순위 높은 스킬만 실행됨.
            foreach (var ability in _abilityList) {
                if (!ability.IsAbilityEnabled) continue;
                ability.PreProcessAbility();
                ability.ProcessAbility();
                ability.PostProcessAbility();
                ability.UpdateAnimator();
            }
        }

        public override void OnDestroy() {
            base.OnDestroy();
            // 모듈 파괴 시, 관리하던 모든 어빌리티의 리소스를 최종 정리.
            foreach (var ability in _abilityList) {
                ability.Dispose();
            }
        }
        
        // --- 공개 API ---

        /// <summary>
        /// 타입에 맞는 어빌리티 인스턴스를 반환.
        /// </summary>
        public T GetAbility<T>() where T : class, IYisoCharacterAbility {
            _abilities.TryGetValue(typeof(T), out var ability);
            return ability as T;
        }

        /// <summary>
        /// 모든 어빌리티의 상태를 리셋.
        /// </summary>
        public void ResetAbilities() {
            foreach (var ability in _abilityList) {
                ability.ResetAbility();
            }
        }

        /// <summary>
        /// 애니메이션 이벤트를 모든 어빌리티에게 전파합니다.
        /// Animator의 Animation Event에서 호출됩니다.
        ///
        /// 중요: IsAbilityEnabled 체크를 하지 않습니다.
        /// 이유: 공격 중인 어빌리티는 IsAbilityEnabled가 false일 수 있지만,
        /// 애니메이션 이벤트(EnableDamage, DisableDamage, AttackEnd)는 반드시 받아야 합니다.
        /// </summary>
        /// <param name="eventName">애니메이션 이벤트 이름</param>
        public void OnAnimationEvent(string eventName) {
            foreach (var ability in _abilityList) {
                ability.OnAnimationEvent(eventName);
            }
        }

        [Serializable]
        public class Settings {
            public List<YisoAbilitySO> abilities;
        }
    }
}