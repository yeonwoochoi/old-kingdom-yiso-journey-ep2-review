using System;
using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.Data {
    public class YisoBlackboardInitializer : RunIBehaviour {
        [Serializable] public class FloatEntry { public YisoBlackboardKeySO key; public float value; }
        [Serializable] public class IntEntry { public YisoBlackboardKeySO key; public int value; }
        [Serializable] public class StringEntry { public YisoBlackboardKeySO key; public string value; }
        [Serializable] public class BoolEntry { public YisoBlackboardKeySO key; public bool value; }
        [Serializable] public class UnityObjectEntry { public YisoBlackboardKeySO key; public UnityEngine.Object value; }

        [Serializable]
        public class VectorEntry {
            [HorizontalGroup("Line", 0.4f), HideLabel]
            public YisoBlackboardKeySO key;
            
            [HorizontalGroup("Line", 0.2f), LabelText("Current Pos")]
            public bool useCurrentPosition;
            
            [HorizontalGroup("Line"), HideLabel]
            [HideIf(nameof(useCurrentPosition))] // 체크되면 수동 입력 필드 숨김
            public Vector3 value;
        }

        [SerializeField] private List<FloatEntry> _floatEntries = new();
        [SerializeField] private List<IntEntry> _intEntries = new();
        [SerializeField] private List<StringEntry> _stringEntries = new();
        [SerializeField] private List<BoolEntry> _boolEntries = new();
        [SerializeField] private List<VectorEntry> _vectorEntries = new();
        [SerializeField] private List<UnityObjectEntry> _objectEntries = new();
        
        private bool _initialized = false;

        protected override void Start() {
            base.Start();
            InitializeBlackboard();
        }

        private void InitializeBlackboard() {
            if (_initialized) return;
            _initialized = true;
            
            var context = GetComponentInParent<IYisoCharacterContext>();

            var module = context?.GetModule<YisoCharacterBlackboardModule>();
            if (module == null) {
                Debug.LogError($"YisoCharacterBlackboardModule not found");
                return;
            }

            foreach (var e in _floatEntries) module.SetFloat(e.key, e.value);
            foreach (var e in _intEntries) module.SetInt(e.key, e.value);
            foreach (var e in _stringEntries) module.SetString(e.key, e.value);
            foreach (var e in _boolEntries) module.SetBool(e.key, e.value);
            foreach (var e in _objectEntries) module.SetObject(e.key, e.value);
            foreach (var e in _vectorEntries) {
                var finalValue = e.useCurrentPosition ? context.Transform.position : e.value;
                module.SetVector(e.key, finalValue);
            }
        }
    }
}