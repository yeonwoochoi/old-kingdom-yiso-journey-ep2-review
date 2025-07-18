namespace Gameplay.Character.Core {
    public interface IYisoCharacterModule {
        void Initialize(YisoCharacter character);
        void OnUpdate();
        void OnDestroy();
    }
    
    public abstract class YisoCharacterModuleBase: IYisoCharacterModule {
        protected YisoCharacter Character { get; private set; }
        
        public virtual void Initialize(YisoCharacter character) {
            Character = character;
        }

        public virtual void OnUpdate() { }

        public virtual void OnDestroy() { }
    }
}