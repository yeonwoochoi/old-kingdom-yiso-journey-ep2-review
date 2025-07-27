namespace Gameplay.Character.Core {
    public interface IYisoCharacterModule {
        void Initialize();
        void OnUpdate();
        void OnFixedUpdate();
        void OnLateUpdate();
        void OnDestroy();
    }
    
    public abstract class YisoCharacterModuleBase: IYisoCharacterModule {
        protected IYisoCharacterContext Context { get; }

        protected YisoCharacterModuleBase(IYisoCharacterContext context) {
            Context = context;
        }
        
        public virtual void Initialize() { }

        public virtual void OnFixedUpdate() { }
        public virtual void OnUpdate() { }
        public virtual void OnLateUpdate() { }

        public virtual void OnDestroy() { }
    }
}