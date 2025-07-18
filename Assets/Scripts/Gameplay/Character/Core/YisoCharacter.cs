using System;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.AI;
using Gameplay.Character.Core.Modules;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.Core {
    public enum CharacterTypes {
        Player,
        AI
    }
    
    [AddComponentMenu("Yiso/Character/Core/Character")]
    public class YisoCharacter : RunIBehaviour {
        [SerializeField] private CharacterTypes characterType = CharacterTypes.AI;
        [SerializeField] private string characterID = "";
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private GameObject characterModel;
        [SerializeField] private YisoHealth characterHealth;
        [SerializeField] private YisoAIBrain characterBrain;

        public Animator CharacterAnimator => characterAnimator;
        
        private Dictionary<Type, IYisoCharacterModule> _modules;

        protected override void Awake() {
            base.Awake();
            Initialize();
        }

        private void Initialize() {
            _modules = new Dictionary<Type, IYisoCharacterModule>();

            RegisterModule(new YisoCharacterAbilityModule());
            RegisterModule(new YisoCharacterAnimationModule());
            RegisterModule(new YisoCharacterCoreModule());
            RegisterModule(new YisoCharacterInputModule());
            RegisterModule(new YisoCharacterLifecycleModule());
            RegisterModule(new YisoCharacterSaveModule());
            RegisterModule(new YisoCharacterStateModule());

            foreach (var module in _modules.Values) {
                module.Initialize(this);
            }
        }

        private void RegisterModule(IYisoCharacterModule module, bool forceSet = false) {
            if (_modules.ContainsKey(module.GetType()) && !forceSet) return;
            _modules.Add(module.GetType(), module);
        }

        public T GetModule<T>() where T : class, IYisoCharacterModule {
            _modules.TryGetValue(typeof(T), out var module);
            return module as T;
        }

        public override void OnUpdate() {
            foreach (var module in _modules.Values) {
                module.OnUpdate();
            }
        }

        protected override void OnDestroy() {
            foreach (var module in _modules.Values) {
                module.OnDestroy();
            }
        }
    }
}