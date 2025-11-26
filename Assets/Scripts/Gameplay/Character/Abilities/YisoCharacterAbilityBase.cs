using System;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;

namespace Gameplay.Character.Abilities {
    /// <summary>
    /// 모든 캐릭터 어빌리티의 최상위 인터페이스.
    /// Pure C# 클래스이며, IDisposable을 구현하여 명시적인 리소스 정리를 지원.
    /// </summary>
    public interface IYisoCharacterAbility: IDisposable {
        bool IsAbilityEnabled { get; }
        
        // --- 생명 주기 ---
        void Initialize(IYisoCharacterContext context);
        void LateInitialize();
        void OnEnable();
        void OnDisable();
        
        // --- 매 프레임 실행 로직 ---
        void PreProcessAbility();
        void ProcessAbility();
        void PostProcessAbility();
        void UpdateAnimator();

        // --- 기타 제어 ---
        void ResetAbility();
    }
    
    /// <summary>
    /// 모든 Pure C# 어빌리티의 추상 기반 클래스.
    /// 공통 기능과 컨텍스트 접근, 생명주기 기본 구현 제공.
    /// </summary>
    public abstract class YisoCharacterAbilityBase : IYisoCharacterAbility {
        protected IYisoCharacterContext Context { get; private set; }
        protected YisoCharacterStateModule _stateModule;
        protected YisoCharacterAnimationModule _animationModule;

        public virtual bool IsAbilityEnabled => _stateModule?.CurrentState == null || _stateModule.CurrentState.CanCastAbility;

        /// <summary>
        /// 어빌리티 최초 생성 시 1회 호출. 컨텍스트 주입 등 기본 설정.
        /// </summary>
        public virtual void Initialize(IYisoCharacterContext context) {
            Context = context;
            _stateModule = Context.GetModule<YisoCharacterStateModule>();
            _animationModule = Context.GetModule<YisoCharacterAnimationModule>();
        }

        public virtual void LateInitialize() { }

        /// <summary>
        /// 어빌리티 활성화 시 호출 (풀링 지원).
        /// 이 곳에서 필요한 시스템 이벤트를 구독(Subscribe).
        /// </summary>
        public virtual void OnEnable() { }

        /// <summary>
        /// 어빌리티 비활성화 시 호출 (풀링 지원).
        /// OnEnable에서 구독했던 모든 이벤트를 해지(Unsubscribe).
        /// </summary>
        public virtual void OnDisable() { }

        /// <summary>
        /// 메인 로직 실행 전 사전 처리. (예: 입력 상태 읽기)
        /// </summary>
        public virtual void PreProcessAbility() { }

        /// <summary>
        /// 어빌리티의 핵심 로직.
        /// </summary>
        public virtual void ProcessAbility() { }

        /// <summary>
        /// 메인 로직 실행 후 후처리. (예: 쿨다운 계산)
        /// </summary>
        public virtual void PostProcessAbility() { }

        /// <summary>
        /// 애니메이터 파라미터 업데이트.
        /// </summary>
        public virtual void UpdateAnimator() {  }

        /// <summary>
        /// 어빌리티 상태를 초기화.
        /// </summary>
        public virtual void ResetAbility() {  }

        /// <summary>
        /// 객체 완전 소멸 시 호출. 모든 리소스 최종 정리.
        /// </summary>
        public virtual void Dispose() {  }
    }
}