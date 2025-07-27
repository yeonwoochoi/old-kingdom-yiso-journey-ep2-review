using System;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.StateMachine;
using UnityEngine;

namespace Gameplay.Character.Core {
    public interface IYisoCharacterContext {
        GameObject GameObject { get; }
        Transform Transform { get; }
        CharacterTypes Type { get; }
        string ID { get; }
        GameObject Model { get; }
        Animator Animator { get; }
        T GetModule<T>() where T : class, IYisoCharacterModule;
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

    [AddComponentMenu("Yiso/Gameplay/Character/Core/Character")]
    public class YisoCharacter : RunIBehaviour, IYisoCharacterContext {
        [Header("Base Settings")] [SerializeField]
        private CharacterTypes characterType = CharacterTypes.AI;

        [SerializeField] private string characterID = "";
        [SerializeField] private GameObject characterModel;
        [SerializeField] private Animator animator;

        [Header("Module Settings")]
        [SerializeField] private YisoCharacterAnimationModule.Settings _animationSettings;
        [SerializeField] private YisoCharacterAbilityModule.Settings _abilitySettings;
        [SerializeField] private YisoCharacterCoreModule.Settings _coreSettings;
        [SerializeField] private YisoCharacterInputModule.Settings _inputSettings;
        [SerializeField] private YisoCharacterLifecycleModule.Settings _lifecycleSettings;
        [SerializeField] private YisoCharacterStateModule.Settings _stateSettings;
        [SerializeField] private YisoCharacterSaveModule.Settings _saveSettings;

        public GameObject GameObject { get; }
        public Transform Transform { get; }
        public CharacterTypes Type => characterType;
        public string ID => characterID;

        public GameObject Model {
            get {
                if (!characterModel) {
                    Debug.LogWarning(
                        $"[YisoCharacter] Character Model field is not assigned for {gameObject.name}. Trying to find a child named 'Model'.");
                    characterModel = transform.Find("Model")?.gameObject;
                    if (!characterModel) {
                        Debug.LogError($"[YisoCharacter] No 'Model' child found for {gameObject.name}. Please assign characterModel manually or ensure a 'Model' child exists.");
                    }
                }

                return characterModel;
            }
        }

        public Animator Animator {
            get {
                if (!animator) {
                    Debug.LogWarning($"[YisoCharacter] Animator field is not assigned for {gameObject.name}. Trying to get it from the Character Model.");
                    if (Model != null) {
                        Model.TryGetComponent(out animator);
                    }

                    if (!animator) {
                        Debug.LogError($"[YisoCharacter] No Animator found on Character Model for {gameObject.name}. Please assign animator manually or ensure Animator component exists on the Model.");
                    }
                }

                return animator;
            }
        }


        private Dictionary<Type, IYisoCharacterModule> _modules;

        protected override void Awake() {
            base.Awake();
            Initialize();
        }

        private void Initialize() {
            _modules = new Dictionary<Type, IYisoCharacterModule>();

            RegisterModule(new YisoCharacterAbilityModule(this, _abilitySettings));
            RegisterModule(new YisoCharacterAnimationModule(this, _animationSettings));
            RegisterModule(new YisoCharacterCoreModule(this, _coreSettings));
            RegisterModule(new YisoCharacterLifecycleModule(this, _lifecycleSettings));
            RegisterModule(new YisoCharacterSaveModule(this, _saveSettings));
            RegisterModule(new YisoCharacterStateModule(this, _stateSettings));

            if (characterType == CharacterTypes.Player) {
                RegisterModule(new YisoCharacterInputModule(this, _inputSettings));
            }

            foreach (var module in _modules.Values) {
                module.Initialize();
            }
        }

        private void RegisterModule(IYisoCharacterModule module, bool forceSet = false) {
            var type = module.GetType();
            if (_modules.ContainsKey(type)) {
                if (forceSet) _modules[type] = module;
                return;
            }

            _modules.Add(module.GetType(), module);
        }

        public T GetModule<T>() where T : class, IYisoCharacterModule {
            _modules.TryGetValue(typeof(T), out var module);
            return module as T;
        }

        public void RequestStateChange(YisoCharacterStateSO newState) {
            // TODO
        }

        public void RequestStateChange(string newStateName) {
            // TODO
        }

        public void Move(Vector3 direction, float speedMultiplier = 1) {
           // TODO
        }

        public void PlayAnimation(YisoCharacterAnimationState state, bool value) {
            GetModule<YisoCharacterAnimationModule>().SetBool(state, value);
        }

        public void PlayAnimation(YisoCharacterAnimationState state, float value) {
            GetModule<YisoCharacterAnimationModule>().SetFloat(state, value);
        }

        public void PlayAnimation(YisoCharacterAnimationState state, int value) {
            GetModule<YisoCharacterAnimationModule>().SetInteger(state, value);
        }

        public void PlayAnimation(YisoCharacterAnimationState state) {
            GetModule<YisoCharacterAnimationModule>().SetTrigger(state);
        }

        public float GetCurrentHealth() {
            return 1f; // TODO
        }

        public bool IsDead() {
            return false; // TODO
        }

        public void TakeDamage(float damage) {
            // TODO
        }

        public override void OnUpdate() {
            foreach (var module in _modules.Values) {
                module.OnUpdate();
            }
        }

        public override void OnFixedUpdate() {
            foreach (var module in _modules.Values) {
                module.OnFixedUpdate();
            }
        }

        public override void OnLateUpdate() {
            foreach (var module in _modules.Values) {
                module.OnLateUpdate();
            }
        }

        protected override void OnDestroy() {
            foreach (var module in _modules.Values) {
                module.OnDestroy();
            }
        }
    }
}