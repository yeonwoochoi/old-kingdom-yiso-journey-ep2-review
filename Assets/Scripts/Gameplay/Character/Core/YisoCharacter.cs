using System;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.StateMachine;
using UnityEngine;

namespace Gameplay.Character.Core {
    /// <summary>
    /// 캐릭터의 핵심 데이터와 기능에 접근하기 위한 통로(인터페이스).
    /// 모듈은 이 컨텍스트를 통해 다른 모듈이나 캐릭터 정보와 상호작용.
    /// </summary>
    public interface IYisoCharacterContext {
        GameObject GameObject { get; }
        Transform Transform { get; }
        CharacterTypes Type { get; }
        string ID { get; }
        GameObject Model { get; }
        Animator Animator { get; }
        T GetModule<T>() where T : class, IYisoCharacterModule;
        YisoCharacterStateSO GetCurrentState();
        void RequestStateChange(YisoCharacterStateSO newStateSO);
        void RequestStateChange(string newStateName);
        void Move(Vector3 direction, float speedMultiplier = 1f);
        void PlayAnimation(YisoCharacterAnimationState state, bool value);
        void PlayAnimation(YisoCharacterAnimationState state, float value);
        void PlayAnimation(YisoCharacterAnimationState state, int value);
        void PlayAnimation(YisoCharacterAnimationState state);
        float GetCurrentHealth();
        bool IsDead();
        void TakeDamage(float damage);
    }
    
    public enum CharacterTypes {
        Player,
        AI
    }

    /// <summary>
    /// 캐릭터 모든 기능을 관리하는 중앙 허브 역할의 MonoBehaviour.
    /// 실제 로직은 각 IYisoCharacterModule에 위임, 이 클래스는 모듈 생명주기 관리 및 조율 담당.
    /// </summary>
    [AddComponentMenu("Yiso/Gameplay/Character/Core/Character")]
    public class YisoCharacter : RunIBehaviour, IYisoCharacterContext {
        [Header("Base Settings")]
        [SerializeField] private CharacterTypes characterType = CharacterTypes.AI;
        [SerializeField] private string characterID = "";
        [SerializeField] private GameObject characterModel;
        [SerializeField] private Animator animator;
        
        // 각 모듈의 초기 설정을 담는 클래스. 인스펙터에서 값 조정 후 모듈 생성 시 주입.
        [Header("Module Settings")]
        [SerializeField] private YisoCharacterAnimationModule.Settings _animationSettings;
        [SerializeField] private YisoCharacterAbilityModule.Settings _abilitySettings;
        [SerializeField] private YisoCharacterCoreModule.Settings _coreSettings;
        [SerializeField] private YisoCharacterInputModule.Settings _inputSettings;
        [SerializeField] private YisoCharacterLifecycleModule.Settings _lifecycleSettings;
        [SerializeField] private YisoCharacterStateModule.Settings _stateSettings;
        [SerializeField] private YisoCharacterSaveModule.Settings _saveSettings;

        public GameObject GameObject => gameObject;
        public Transform Transform => transform;
        public CharacterTypes Type => characterType;
        public string ID => characterID;

        /// <summary>
        /// 캐릭터의 시각적 모델. 미할당 시 'Model' 이름의 자식 오브젝트 자동 탐색.
        /// </summary>
        public GameObject Model {
            get {
                if (!characterModel) {
                    Debug.LogWarning($"[YisoCharacter] '{gameObject.name}' 모델 미할당. 'Model' 자식 탐색.");
                    characterModel = transform.Find("Model")?.gameObject;
                    if (!characterModel) Debug.LogError($"[YisoCharacter] '{gameObject.name}'에서 'Model' 탐색 실패. 수동 할당 필요.");
                }
                return characterModel;
            }
        }

        /// <summary>
        /// 캐릭터의 애니메이터. 미할당 시 모델에서 자동 탐색.
        /// </summary>
        public Animator Animator {
            get {
                if (!animator) {
                    Debug.LogWarning($"[YisoCharacter] '{gameObject.name}' 애니메이터 미할당. 모델에서 탐색.");
                    if (Model != null) Model.TryGetComponent(out animator);
                    if (!animator) Debug.LogError($"[YisoCharacter] '{gameObject.name}'의 모델에서 애니메이터 탐색 실패. 수동 할당 필요.");
                }
                return animator;
            }
        }
        
        // 모든 기능 모듈을 타입별로 저장하는 딕셔너리.
        private Dictionary<Type, IYisoCharacterModule> _modules;

        protected override void Awake() {
            base.Awake();
            Initialize();
        }

        /// <summary>
        /// 캐릭터와 모든 모듈 초기화.
        /// </summary>
        private void Initialize() {
            _modules = new Dictionary<Type, IYisoCharacterModule>();

            // 기능에 맞는 모듈 생성 및 등록.
            RegisterModule(new YisoCharacterAbilityModule(this, _abilitySettings));
            RegisterModule(new YisoCharacterAnimationModule(this, _animationSettings));
            RegisterModule(new YisoCharacterCoreModule(this, _coreSettings));
            RegisterModule(new YisoCharacterLifecycleModule(this, _lifecycleSettings));
            RegisterModule(new YisoCharacterSaveModule(this, _saveSettings));
            RegisterModule(new YisoCharacterStateModule(this, _stateSettings));

            // 플레이어 타입일 경우, 입력 모듈 추가.
            if (characterType == CharacterTypes.Player) {
                RegisterModule(new YisoCharacterInputModule(this, _inputSettings));
            }

            // 1단계: 모듈 독립 초기화. (다른 모듈 참조 금지)
            foreach (var module in _modules.Values) {
                module.Initialize();
            }

            // 2단계: 모듈 연결 초기화. (다른 모듈 참조 가능)
            foreach (var module in _modules.Values) {
                module.LateInitialize();
            }
        }
        
        /// <summary>
        /// 모듈을 딕셔너리에 등록. 중복 등록 방지.
        /// </summary>
        private void RegisterModule(IYisoCharacterModule module, bool forceSet = false) {
            var type = module.GetType();
            if (_modules.ContainsKey(type)) {
                if (forceSet) _modules[type] = module;
                return;
            }
            _modules.Add(module.GetType(), module);
        }

        /// <summary>
        /// 타입에 맞는 모듈 인스턴스 반환.
        /// </summary>
        public T GetModule<T>() where T : class, IYisoCharacterModule {
            _modules.TryGetValue(typeof(T), out var module);
            return module as T;
        }

        public YisoCharacterStateSO GetCurrentState() {
            return GetModule<YisoCharacterStateModule>().CurrentState;
        }

        // --- 기능 위임 메소드 (Facade Pattern): 복잡한 내부 구조를 숨기고 간단한 사용법 제공. ---
        
        public void RequestStateChange(YisoCharacterStateSO newState) => GetModule<YisoCharacterStateModule>().RequestStateChange(newState);
        public void RequestStateChange(string newStateName) => GetModule<YisoCharacterStateModule>().RequestStateChange(newStateName);
        public void Move(Vector3 direction, float speedMultiplier = 1) { /* TODO: CoreModule 또는 MovementModule에 위임 */ }
        public void PlayAnimation(YisoCharacterAnimationState state, bool value) => GetModule<YisoCharacterAnimationModule>().SetBool(state, value);
        public void PlayAnimation(YisoCharacterAnimationState state, float value) => GetModule<YisoCharacterAnimationModule>().SetFloat(state, value);
        public void PlayAnimation(YisoCharacterAnimationState state, int value) => GetModule<YisoCharacterAnimationModule>().SetInteger(state, value);
        public void PlayAnimation(YisoCharacterAnimationState state) => GetModule<YisoCharacterAnimationModule>().SetTrigger(state);
        public float GetCurrentHealth() => GetModule<YisoCharacterLifecycleModule>().CurrentHealth;
        public bool IsDead() => GetModule<YisoCharacterLifecycleModule>().IsDead;
        public void TakeDamage(float damage) => GetModule<YisoCharacterLifecycleModule>().TakeDamage(damage);
        
        /// <summary>
        /// 모든 모듈에 프레임 업데이트 신호 전파.
        /// </summary>
        public override void OnUpdate() {
            foreach (var module in _modules.Values) {
                module.OnUpdate();
            }
        }

        /// <summary>
        /// 모든 모듈에 파괴 신호 전파. 리소스 정리 유도.
        /// </summary>
        protected override void OnDestroy() {
            foreach (var module in _modules.Values) {
                module.OnDestroy();
            }
        }
        
        protected override void OnEnable() {
            base.OnEnable();
            foreach (var module in _modules.Values) {
                module.OnEnable();
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            foreach (var module in _modules.Values) {
                module.OnDisable();
            }
        }
    }
}