using UnityEngine;

namespace Gameplay.Character.Abilities.Definitions {
    [CreateAssetMenu(fileName = "NewMovementAbilitySO", menuName = "Yiso/Character/Ability/Definitions/Movement Ability Definition")]
    public class YisoMovementAbilitySO: YisoAbilitySO {
        /// <summary>
        /// Creates a new MovementAbility instance using the settings defined in this SO.
        /// </summary>
        /// <returns>A new MovementAbility instance.</returns>
        public override IYisoCharacterAbility CreateAbility() {
            return new YisoCharacterMovementAbility();
        }
    }
}