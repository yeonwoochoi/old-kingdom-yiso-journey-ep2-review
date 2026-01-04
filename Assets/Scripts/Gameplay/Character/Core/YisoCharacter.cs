using System;
using System.Collections;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Abilities;
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
        [Header("Base Settings")] [SerializeField]
        private CharacterType characterType;

        [SerializeField] private string characterID = "";
        [SerializeField] private GameObject characterModel;
        [SerializeField] private Animator animator;

        // 각 모듈의 초기 설정을 담는 클래스. 인스펙터에서 값 조정 후 모듈 생성 시 주입.
        [Header("Module Settings")] [SerializeField]
        private YisoCharacterAnimationModule.Settings _animationSettings;

        [SerializeField] private YisoCharacterAbilityModule.Settings _abilitySettings;
        [SerializeField] private YisoCharacterCoreModule.Settings _coreSettings;
        [SerializeField, ShowIf("IsPlayer")] private YisoCharacterInputModule.Settings _inputSettings;
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

        public FacingDirections FacingDirection => GetModule<YisoCharacterAbilityModule>()?.GetAbility<YisoOrientationAbility>()?.CurrentFacingDirection ?? FacingDirections.Down;
        public Vector2 FacingDirectionVector => GetModule<YisoCharacterAbilityModule>()?.GetAbility<YisoOrientationAbility>()?.CurrentFacingDirectionVector ?? Vector2.zero;

        /// <summary>
        /// 캐릭터의 시각적 모델. 미할당 시 'Model' 이름의 자식 오브젝트 자동 탐색.
        /// </summary>
        public GameObject Model {
            get {
                if (!characterModel) {
                    Debug.LogWarning($"[YisoCharacter] '{gameObject.name}' 모델 미할당. 'Model' 자식 탐색.");
                    characterModel = transform.Find("Model")?.gameObject;
                    if (!characterModel)
                        Debug.LogError($"[YisoCharacter] '{gameObject.name}'에서 'Model' 탐색 실패. 수동 할당 필요.");
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

        public bool IsMovementAllowed {
            get {
                // 1. 죽었으면 못 움직임
                if (IsDead()) return false;

                // (TODO) 2. 상태이상(CC기) 걸렸으면 못 움직임 (나중에 추가)
                // if (StatusModule.IsStunned) return false;

                // 3. 다른 Ability가 이동을 막고 있으면 못 움직임
                var abilityModule = GetModule<YisoCharacterAbilityModule>();
                if (abilityModule != null && abilityModule.IsMovementBlocked) {
                    return false;
                }

                return true;
            }
        }

        public bool IsAttackAllowed {
            get {
                // 1. 죽었으면 못 움직임
                if (IsDead()) return false;

                // (TODO) 2. 상태이상(CC기) 걸렸으면 못 움직임 (나중에 추가)
                // if (StatusModule.IsStunned) return false;

                // 3. 다른 Ability가 이동을 막고 있으면 못 움직임
                var abilityModule = GetModule<YisoCharacterAbilityModule>();
                if (abilityModule != null && abilityModule.IsAttackBlocked) {
                    return false;
                }

                return true;
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
                Debug.LogError($"[{gameObject.name}]에 IPhysicsControllable을 구현한 컴포넌트(예: TopDownController)가 없습니다!",
                    this);
            }

            // [중요] InputModule은 AbilityModule보다 먼저 등록해야 함
            // 이유: AbilityModule의 MovementAbility가 Context.MovementVector(InputModule.MoveInput)를
            // 조회하므로, OnUpdate() 실행 순서상 Input 모듈이 먼저 업데이트되어야 최신 값을 사용할 수 있다.
            if (IsPlayer) {
                RegisterModule(new YisoCharacterInputModule(this, _inputSettings));
            }

            // 기능에 맞는 모듈 생성 및 등록.
            RegisterModule(new YisoCharacterAbilityModule(this, _abilitySettings));
            RegisterModule(new YisoCharacterAnimationModule(this, _animationSettings));
            RegisterModule(new YisoCharacterCoreModule(this, _coreSettings));
            RegisterModule(new YisoCharacterLifecycleModule(this, _lifecycleSettings));
            RegisterModule(new YisoCharacterSaveModule(this, _saveSettings));
            RegisterModule(new YisoCharacterStateModule(this, _stateSettings));
            RegisterModule(new YisoCharacterWeaponModule(this, _weaponSettings));

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

        public void Move(Vector2 finalMovementVector) {
            _physicsController.SetMovement(finalMovementVector);
        }

        public void Face(FacingDirections direction) {
            GetModule<YisoCharacterAbilityModule>()
                ?.GetAbility<YisoOrientationAbility>()
                ?.ForceFace(direction);
        }

        public void Face(Vector2 directionVector) {
            if (directionVector.sqrMagnitude < 0.01f) return;

            // Vector2를 FacingDirections로 변환
            var facingDirection = Mathf.Abs(directionVector.x) > Mathf.Abs(directionVector.y)
                ? (directionVector.x > 0 ? FacingDirections.Right : FacingDirections.Left)
                : (directionVector.y > 0 ? FacingDirections.Up : FacingDirections.Down);

            Face(facingDirection);
        }

        public void PlayAnimation(YisoCharacterAnimationState state, bool value) =>
            GetModule<YisoCharacterAnimationModule>().SetBool(state, value);

        public void PlayAnimation(YisoCharacterAnimationState state, float value) =>
            GetModule<YisoCharacterAnimationModule>().SetFloat(state, value);

        public void PlayAnimation(YisoCharacterAnimationState state, int value) =>
            GetModule<YisoCharacterAnimationModule>().SetInteger(state, value);

        public void PlayAnimation(YisoCharacterAnimationState state) =>
            GetModule<YisoCharacterAnimationModule>().SetTrigger(state);

        /// <summary>
        /// 애니메이션 이벤트를 AbilityModule로 라우팅합니다.
        /// Animator의 Animation Event에서 호출됩니다.
        /// </summary>
        /// <param name="eventName">애니메이션 이벤트 이름</param>
        public void OnAnimationEvent(string eventName) =>
            GetModule<YisoCharacterAbilityModule>()?.OnAnimationEvent(eventName);

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