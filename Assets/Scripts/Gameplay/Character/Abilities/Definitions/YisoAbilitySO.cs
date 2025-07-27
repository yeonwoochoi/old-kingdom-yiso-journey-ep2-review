using UnityEngine;

namespace Gameplay.Character.Abilities.Definitions {
    /// <summary>
    /// Abstract ScriptableObject base class for defining character abilities.
    /// Each concrete ability will have its own SO inheriting from this.
    /// </summary>
    public abstract class YisoAbilitySO : ScriptableObject {
        /// <summary>
        /// Creates and returns an instance of the corresponding pure C# character ability.
        /// </summary>
        /// <returns>An instance of IYisoCharacterAbility.</returns>
        public abstract IYisoCharacterAbility CreateAbility();
    }
}