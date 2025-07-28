namespace Gameplay.Character.Core {
    /// <summary>
    /// 모든 캐릭터 모듈이 구현해야 하는 생명주기 인터페이스.
    /// </summary>
    public interface IYisoCharacterModule {
        void Initialize();
        void LateInitialize();
        void OnEnable();
        void OnDisable();
        void OnUpdate();
        void OnDestroy();
    }
    
    /// <summary>
    /// 모든 캐릭터 모듈의 추상 기반 클래스.
    /// 공통 컨텍스트와 생명주기 메소드의 기본 구현 제공.
    /// </summary>
    public abstract class YisoCharacterModuleBase: IYisoCharacterModule {
        /// <summary>
        /// 캐릭터의 핵심 기능 및 다른 모듈에 접근하기 위한 컨텍스트.
        /// </summary>
        protected IYisoCharacterContext Context { get; }

        protected YisoCharacterModuleBase(IYisoCharacterContext context) {
            Context = context;
        }
        
        /// <summary>
        /// 1단계 초기화. 다른 모듈 참조 없이, 자신의 Settings만으로 내부 상태 준비.
        /// </summary>
        public virtual void Initialize() { }
        
        /// <summary>
        /// 2단계 초기화. 다른 모듈의 초기화 완료 후, 모듈 간 상호 참조 및 연결 설정.
        /// </summary>
        public virtual void LateInitialize() { }

        /// <summary>
        /// 캐릭터 활성화(OnEnable) 시 호출. 오브젝트 풀링 지원.
        /// </summary>
        public virtual void OnEnable() { }

        /// <summary>
        /// 캐릭터 비활성화(OnDisable) 시 호출. 오브젝트 풀링 지원.
        /// 이벤트 구독 해지 등 임시 정리 수행.
        /// </summary>
        public virtual void OnDisable() { }

        /// <summary>
        /// 매 프레임 업데이트 로직.
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// 캐릭터 완전 파괴(OnDestroy) 시 호출.
        /// 모든 리소스의 영구 정리 수행.
        /// </summary>
        public virtual void OnDestroy() { }
    }
}