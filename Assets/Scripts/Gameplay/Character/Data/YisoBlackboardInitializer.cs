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
            public YisoBlackboardKeySO key;
            public bool useCurrentPosition;
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
            if (context == null) {
                Debug.LogError($"[{gameObject.name}] YisoCharacter not found! BlackboardInitializer must be a child of YisoCharacter.", this);
                enabled = false;
                return;
            }

            var module = context.GetModule<YisoCharacterBlackboardModule>();
            if (module == null) {
                Debug.LogError($"[{gameObject.name}] YisoCharacterBlackboardModule not found!", this);
                enabled = false;
                return;
            }

            // Null key 체크와 함께 초기화
            foreach (var e in _floatEntries) {
                if (e.key == null) { Debug.LogWarning($"[{gameObject.name}] Float entry has null key. Skipping.", this); continue; }
                module.SetFloat(e.key, e.value);
            }
            foreach (var e in _intEntries) {
                if (e.key == null) { Debug.LogWarning($"[{gameObject.name}] Int entry has null key. Skipping.", this); continue; }
                module.SetInt(e.key, e.value);
            }
            foreach (var e in _stringEntries) {
                if (e.key == null) { Debug.LogWarning($"[{gameObject.name}] String entry has null key. Skipping.", this); continue; }
                module.SetString(e.key, e.value);
            }
            foreach (var e in _boolEntries) {
                if (e.key == null) { Debug.LogWarning($"[{gameObject.name}] Bool entry has null key. Skipping.", this); continue; }
                module.SetBool(e.key, e.value);
            }
            foreach (var e in _objectEntries) {
                if (e.key == null) { Debug.LogWarning($"[{gameObject.name}] Object entry has null key. Skipping.", this); continue; }
                module.SetObject(e.key, e.value);
            }
            foreach (var e in _vectorEntries) {
                if (e.key == null) { Debug.LogWarning($"[{gameObject.name}] Vector entry has null key. Skipping.", this); continue; }
                var finalValue = e.useCurrentPosition ? context.Transform.position : e.value;
                module.SetVector(e.key, finalValue);
            }

            Debug.Log($"Blackboard initialized on [{name}]");

            // 초기화 완료 후 컴포넌트 비활성화 (더 이상 필요 없음)
            enabled = false;
        }
    }
}