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
        private readonly List<IYisoCharacterAbility> _abilityList;
        
        private readonly Settings _settings;

        public YisoCharacterAbilityModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
            _abilities = new Dictionary<Type, IYisoCharacterAbility>();

            // ScriptableObject(데이터)로부터 실제 어빌리티(로직) 인스턴스를 생성.
            _abilityList = settings.abilities.Select(abilitySo => abilitySo.CreateAbility()).ToList();
            
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
        
        [Serializable]
        public class Settings {
            public List<YisoAbilitySO> abilities;
        }
    }
}