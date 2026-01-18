using Gameplay.Character.Data;
using UnityEngine;
using Utils;

namespace Gameplay.Character.StateMachine.Actions.Attack {
    /// <summary>
    /// 캐릭터의 무기를 교체하는 액션입니다.
    /// 특정 상태 진입 시 무기를 변경하거나, 런타임에 무기를 교체할 때 사용합니다.
    /// </summary>
    public class YisoCharacterActionChangeWeapon: YisoCharacterAction {
        [Tooltip("교체할 무기 데이터")]
        [SerializeField] private YisoWeaponDataSO weaponData;

        [Tooltip("상태 진입 시 자동으로 무기 교체 (true) 또는 PerformAction 호출 시마다 교체 (false)")]
        [SerializeField] private bool changeOnEnter = true;

        public override void OnEnterState() {
            base.OnEnterState();

            if (changeOnEnter) {
                ChangeWeapon();
            }
        }

        public override void PerformAction() {
            if (!changeOnEnter) {
                ChangeWeapon();
            }
        }

        private void ChangeWeapon() {
            if (weaponData == null) {
                YisoLogger.LogWarning("weaponData가 설정되지 않았습니다.");
                return;
            }

            var weaponModule = StateMachine?.GetWeaponModule();
            if (weaponModule == null) {
                YisoLogger.LogWarning("WeaponModule을 찾을 수 없습니다.");
                return;
            }

            weaponModule.EquipWeapon(weaponData);
        }
    }
}