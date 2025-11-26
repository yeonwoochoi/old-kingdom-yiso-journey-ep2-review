using System.Collections.Generic;
using Gameplay.Character.Data;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public class YisoCharacterBlackboardModule : YisoCharacterModuleBase {
        // 타입별 저장소
        private readonly Dictionary<int, float> _floatData = new();
        private readonly Dictionary<int, int> _intData = new();
        private readonly Dictionary<int, string> _stringData = new();
        private readonly Dictionary<int, bool> _boolData = new();
        private readonly Dictionary<int, Vector3> _vectorData = new();
        private readonly Dictionary<int, Object> _objectData = new();

        public YisoCharacterBlackboardModule(IYisoCharacterContext context) : base(context) { }

        // --- Setters ---
        public void SetFloat(YisoBlackboardKeySO key, float value) => _floatData[GetKeyID(key)] = value;
        public void SetInt(YisoBlackboardKeySO key, int value) => _intData[GetKeyID(key)] = value;
        public void SetString(YisoBlackboardKeySO key, string value) => _stringData[GetKeyID(key)] = value;
        public void SetBool(YisoBlackboardKeySO key, bool value) => _boolData[GetKeyID(key)] = value;
        public void SetVector(YisoBlackboardKeySO key, Vector3 value) => _vectorData[GetKeyID(key)] = value;
        public void SetObject(YisoBlackboardKeySO key, Object value) => _objectData[GetKeyID(key)] = value;

        // --- Getters ---
        public float GetFloat(YisoBlackboardKeySO key, float defaultValue = 0f) => 
            _floatData.GetValueOrDefault(GetKeyID(key), defaultValue);

        public int GetInt(YisoBlackboardKeySO key, int defaultValue = 0) => 
            _intData.GetValueOrDefault(GetKeyID(key), defaultValue);

        public string GetString(YisoBlackboardKeySO key, string defaultValue = "") => 
            _stringData.GetValueOrDefault(GetKeyID(key), defaultValue);

        public bool GetBool(YisoBlackboardKeySO key, bool defaultValue = false) => 
            _boolData.GetValueOrDefault(GetKeyID(key), defaultValue);

        public Vector3 GetVector(YisoBlackboardKeySO key, Vector3 defaultValue = default) => 
            _vectorData.GetValueOrDefault(GetKeyID(key), defaultValue);

        public T GetObject<T>(YisoBlackboardKeySO key) where T : Object {
            var id = GetKeyID(key);
            if (_objectData.TryGetValue(id, out var obj)) {
                return obj as T;
            }
            return null;
        }

        // --- Helper ---
        private int GetKeyID(YisoBlackboardKeySO key) {
            return key != null ? key.GetInstanceID() : 0;
        }

        // --- Debugging (Odin Inspector 사용 시 유용) ---
        public void DebugLogAllData() {
            Debug.Log($"[Blackboard] Float: {_floatData.Count}, Int: {_intData.Count}, Bool: {_boolData.Count}, Vector: {_vectorData.Count}");
        }
    }
}