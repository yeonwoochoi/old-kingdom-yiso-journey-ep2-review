using Gameplay.Character.Core.Modules;
using Utils;

namespace Gameplay.Character.Abilities {
    /// <summary>
    /// 모든 공격 Ability의 추상 기반 클래스.
    /// 공격 상태 관리, 방향 제어 등 공통 기능을 제공합니다.
    /// Melee, Ranged, Magic 등 다양한 공격 타입이 이 클래스를 상속합니다.
    /// </summary>
    public abstract class YisoAttackAbilityBase : YisoCharacterAbilityBase {
        protected YisoOrientationAbility _orientationAbility;
        protected bool _isAttacking = false;

        /// <summary>
        /// 공격 중에 이동 가능 여부를 반환합니다.
        /// 하위 클래스에서 구현하여 Settings에 따라 결정합니다.
        /// </summary>
        protected abstract bool CanMoveWhileAttacking { get; }

        /// <summary>
        /// 공격 중에는 이동을 막습니다 (CanMoveWhileAttacking이 false인 경우).
        /// </summary>
        public override bool PreventsMovement => _isAttacking && !CanMoveWhileAttacking;

        /// <summary>
        /// 공격 중에는 다른 공격을 막습니다.
        /// </summary>
        public override bool PreventsAttack => _isAttacking;

        public override void LateInitialize() {
            base.LateInitialize();

            // OrientationAbility 참조 가져오기
            var abilityModule = Context.GetModule<YisoCharacterAbilityModule>();
            if (abilityModule != null) {
                _orientationAbility = abilityModule.GetAbility<YisoOrientationAbility>();
            }

            if (_orientationAbility == null) {
                YisoLogger.LogWarning($"[{GetType().Name}] YisoOrientationAbility를 찾을 수 없습니다. 공격 중 방향 잠금이 작동하지 않습니다.");
            }
        }

        /// <summary>
        /// 현재 공격 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsAttacking() {
            return _isAttacking;
        }

        /// <summary>
        /// 캐릭터의 방향 전환을 잠급니다 (공격 중).
        /// </summary>
        protected void LockOrientation() {
            _orientationAbility?.LockOrientation();
        }

        /// <summary>
        /// 캐릭터의 방향 전환 잠금을 해제합니다 (공격 종료).
        /// </summary>
        protected void UnlockOrientation() {
            _orientationAbility?.UnlockOrientation();
        }

        public override void OnDeath() {
            base.OnDeath();

            // 공격 중이었다면 방향 잠금 해제
            if (_isAttacking) {
                UnlockOrientation();
            }
        }

        public override void OnRevive() {
            base.OnRevive();

            // 부활 시 방향 잠금 해제 (안전장치)
            UnlockOrientation();
        }
    }
}
