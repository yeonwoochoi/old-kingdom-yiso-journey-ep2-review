using System;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;

namespace Gameplay.Character.Abilities {
    [Serializable]
    public abstract class YisoAbilitySettingsBase { }
    
    /// <summary>
    /// Base interface for all character abilities.
    /// </summary>
    public interface IYisoCharacterAbility {
        void Initialize(IYisoCharacterContext context);
        void PreProcessAbility();
        void ProcessAbility();
        void PostProcessAbility();
        // Add other common ability methods here if needed (e.g., Activate, Deactivate, Reset)
    }
    
    /// <summary>
    /// Abstract base class for all pure C# character abilities.
    /// Provides common functionality and context access.
    /// </summary>
    public abstract class YisoCharacterAbilityBase : IYisoCharacterAbility {
        protected IYisoCharacterContext Context { get; private set; }

        /// <summary>
        /// Abstract property to expose the specific settings for the ability.
        /// </summary>
        protected abstract YisoAbilitySettingsBase Settings { get; }

        /// <summary>
        /// Initializes the ability with the character's context.
        /// </summary>
        /// <param name="context">The character's context.</param>
        public virtual void Initialize(IYisoCharacterContext context) {
            Context = context;
        }

        /// <summary>
        /// Called before processing the ability in the character's update loop.
        /// </summary>
        public virtual void PreProcessAbility() { }

        /// <summary>
        /// Called to process the ability's main logic in the character's update loop.
        /// </summary>
        public virtual void ProcessAbility() { }

        /// <summary>
        /// Called after processing the ability in the character's update loop.
        /// </summary>
        public virtual void PostProcessAbility() { }
    }
}