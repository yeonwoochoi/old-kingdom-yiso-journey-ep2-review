using System;
using System.Collections;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.StateMachine;
using Gameplay.Character.Types;
using Gameplay.Core;
using Gameplay.Health;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.Core {
    /// <summary>
    /// 캐릭터 모든 기능을 관리하는 중앙 허브 역할의 MonoBehaviour.
    /// 실제 로직은 각 IYisoCharacterModule에 위임, 이 클래스는 모듈 생명주기 관리 및 조율 담당.
    /// </summary>
    [AddComponentMenu("Yiso/Gameplay/Character/Core/Character")]
    public class YisoCharacter : RunIBehaviour, IYisoCharacterContext {
        [Header("Base Settings")]
        [SerializeField] private CharacterType characterType;
        [SerializeField] private string characterID = "";
        [SerializeField] private GameObject characterModel;
        [SerializeField] private Animator animator;
        
        // 각 모듈의 초기 설정을 담는 클래스. 인스펙터에서 값 조정 후 모듈 생성 시 주입.
        [Header("Module Settings")]
        [SerializeField] private YisoCharacterAnimationModule.Settings _animationSettings;
        [SerializeField] private YisoCharacterAbilityModule.Settings _abilitySettings;
        [SerializeField] private YisoCharacterCoreModule.Settings _coreSettings;
        [SerializeField, ShowIf("IsPlayer")] private YisoCharacterInputModule.Settings _inputSettings;
        [SerializeField, ShowIf("@!IsPlayer")] private YisoCharacterAIModule.Settings _aiSettings;
        [SerializeField] private YisoCharacterLifecycleModule.Settings _lifecycleSettings;
        [SerializeField] private YisoCharacterStateModule.Settings _stateSettings;
        [SerializeField] private YisoCharacterSaveModule.Settings _saveSettings;
        [SerializeField] private YisoCharacterWeaponModule.Settings _weaponSettings;
        
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;
        public CharacterType Type => characterType;
        public bool IsPlayer => Type == CharacterType.Player; // 추가 조건이 있으면 여기다.
        public bool IsAIControlled => characterType != CharacterType.Player;
        public string ID => characterID;

        public Vector2 MovementVector {
            get {
                if (IsPlayer) {
                    return GetModule<YisoCharacterInputModule>()?.MoveInput ?? Vector2.zero;
                }
                else {
                    return GetModule<YisoCharacterAIModule>()?.PathDirection ?? Vector2.zero;
                }
            }
        }

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
        private IPhysicsControllable _physicsController;

        protected override void Awake() {
            base.Awake();
            Initialize();
        }

        protected override void Start() {
            base.Start();
            LateInitialize();
        }

        /// <summary>
        /// 캐릭터와 모든 모듈 초기화.
        /// </summary>
        private void Initialize() {
            _modules = new Dictionary<Type, IYisoCharacterModule>();
            _physicsController = GetComponent<IPhysicsControllable>();

            if (_physicsController == null) {
                Debug.LogError($"[{gameObject.name}]에 IPhysicsControllable을 구현한 컴포넌트(예: TopDownController)가 없습니다!", this);
            }

            // 기능에 맞는 모듈 생성 및 등록.
            RegisterModule(new YisoCharacterAbilityModule(this, _abilitySettings));
            RegisterModule(new YisoCharacterAnimationModule(this, _animationSettings));
            RegisterModule(new YisoCharacterBlackboardModule(this));
            RegisterModule(new YisoCharacterCoreModule(this, _coreSettings));
            RegisterModule(new YisoCharacterLifecycleModule(this, _lifecycleSettings));
            RegisterModule(new YisoCharacterSaveModule(this, _saveSettings));
            RegisterModule(new YisoCharacterStateModule(this, _stateSettings));
            RegisterModule(new YisoCharacterWeaponModule(this, _weaponSettings));

            // 플레이어 타입일 경우, 입력 모듈 추가.
            if (IsPlayer) {
                RegisterModule(new YisoCharacterInputModule(this, _inputSettings));
            }
            else {
                RegisterModule(new YisoCharacterAIModule(this, _aiSettings));
            }

            // 1단계: 모듈 독립 초기화. (다른 모듈 참조 금지)
            foreach (var module in _modules.Values) {
                module.Initialize();
            }
        }

        private void LateInitialize() {
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

        public new Coroutine StartCoroutine(IEnumerator routine) {
            return base.StartCoroutine(routine);
        }

        public new void StopCoroutine(Coroutine routine) {
            base.StopCoroutine(routine);
        }

        public YisoCharacterStateSO GetCurrentState() {
            return GetModule<YisoCharacterStateModule>().CurrentState;
        }

        // --- 기능 위임 메소드 (Facade Pattern): 복잡한 내부 구조를 숨기고 간단한 사용법 제공. ---
        
        public void RequestStateChange(YisoCharacterStateSO newState) => GetModule<YisoCharacterStateModule>().RequestStateChange(newState);
        public void RequestStateChangeByKey(string newStateName) => GetModule<YisoCharacterStateModule>().RequestStateChangeByKey(newStateName);
        public void RequestStateChangeByRole(YisoStateRole newStateRole) => GetModule<YisoCharacterStateModule>().RequestStateChangeByRole(newStateRole);

        public void Move(Vector2 finalMovementVector) {
            _physicsController.SetMovement(finalMovementVector);
        }
        public void PlayAnimation(YisoCharacterAnimationState state, bool value) => GetModule<YisoCharacterAnimationModule>().SetBool(state, value);
        public void PlayAnimation(YisoCharacterAnimationState state, float value) => GetModule<YisoCharacterAnimationModule>().SetFloat(state, value);
        public void PlayAnimation(YisoCharacterAnimationState state, int value) => GetModule<YisoCharacterAnimationModule>().SetInteger(state, value);
        public void PlayAnimation(YisoCharacterAnimationState state) => GetModule<YisoCharacterAnimationModule>().SetTrigger(state);

        /// <summary>
        /// 애니메이션 이벤트를 AbilityModule로 라우팅합니다.
        /// Animator의 Animation Event에서 호출됩니다.
        /// </summary>
        /// <param name="eventName">애니메이션 이벤트 이름</param>
        public void OnAnimationEvent(string eventName) => GetModule<YisoCharacterAbilityModule>()?.OnAnimationEvent(eventName);
        public float GetCurrentHealth() => GetModule<YisoCharacterLifecycleModule>().CurrentHealth;
        public bool IsDead() => GetModule<YisoCharacterLifecycleModule>().IsDead;
        public void TakeDamage(DamageInfo damage) => GetModule<YisoCharacterLifecycleModule>().TakeDamage(damage);
        
        /// <summary>
        /// 모든 모듈에 프레임 업데이트 신호 전파.
        /// </summary>
        public override void OnUpdate() {
            foreach (var module in _modules.Values) {
                module.OnUpdate();
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

        /// <summary>
        /// 모든 모듈에 파괴 신호 전파. 리소스 정리 유도.
        /// </summary>
        protected override void OnDestroy() {
            foreach (var module in _modules.Values) {
                module.OnDestroy();
            }
        }
    }
}