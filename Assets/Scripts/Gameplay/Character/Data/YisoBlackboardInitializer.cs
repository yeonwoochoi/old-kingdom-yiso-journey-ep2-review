using System;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using UnityEngine;

namespace Gameplay.Character.Data {
    public class YisoBlackboardInitializer : RunIBehaviour {
        [Serializable] public class FloatEntry { public YisoBlackboardKeySO key; public float value; }
        [Serializable] public class IntEntry { public YisoBlackboardKeySO key; public int value; }
        [Serializable] public class StringEntry { public YisoBlackboardKeySO key; public string value; }
        [Serializable] public class BoolEntry { public YisoBlackboardKeySO key; public bool value; }
        [Serializable] public class VectorEntry { public YisoBlackboardKeySO key; public Vector3 value; }
        [Serializable] public class UnityObjectEntry { public YisoBlackboardKeySO key; public UnityEngine.Object value; }

        [SerializeField] private List<FloatEntry> _floatEntries = new();
        [SerializeField] private List<IntEntry> _intEntries = new();
        [SerializeField] private List<StringEntry> _stringEntries = new();
        [SerializeField] private List<BoolEntry> _boolEntries = new();
        [SerializeField] private List<VectorEntry> _vectorEntries = new();
        [SerializeField] private List<UnityObjectEntry> _objectEntries = new();
        
        private bool _initialized = false;

        protected override void Awake() {
            base.Awake();
            InitializeBlackboard();
        }

        private void InitializeBlackboard() {
            if (_initialized) return;
            _initialized = true;
            
            var context = GetComponent<IYisoCharacterContext>();

            var module = context?.GetModule<YisoCharacterBlackboardModule>();
            if (module == null) return;

            foreach (var e in _floatEntries) module.SetFloat(e.key, e.value);
            foreach (var e in _intEntries) module.SetInt(e.key, e.value);
            foreach (var e in _stringEntries) module.SetString(e.key, e.value);
            foreach (var e in _boolEntries) module.SetBool(e.key, e.value);
            foreach (var e in _vectorEntries) module.SetVector(e.key, e.value);
            foreach (var e in _objectEntries) module.SetObject(e.key, e.value);
        }
    }
}