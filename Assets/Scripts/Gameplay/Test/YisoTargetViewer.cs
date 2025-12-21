using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Test {
    public class YisoTargetViewer: MonoBehaviour {
        [ReadOnly] public GameObject target;
        [ReadOnly] public float distance;
        [ReadOnly] public float distanceFromSpawn;
        [SerializeField] private YisoBlackboardKeySO targetKey;
        [SerializeField] private YisoBlackboardKeySO spawnKey;

        private YisoCharacterBlackboardModule _bbModule;
        private Vector2 _spawnPosition;
        
        private void Start() {
            var context = GetComponent<IYisoCharacterContext>();
            if (context == null) {
                Debug.LogWarning("No YisoCharacterContext found!");
                return;
            }
            _bbModule = context.GetModule<YisoCharacterBlackboardModule>();
            _spawnPosition = _bbModule?.GetVector(spawnKey, Vector2.zero) ?? Vector2.zero;
        }

        private void Update() {
            if (_bbModule == null) {
                Debug.LogWarning("No YisoCharacterBlackboardModule found!");
                return;
            }
            
            distanceFromSpawn = Vector2.Distance(_spawnPosition, transform.position);

            target = _bbModule.GetObject<Transform>(targetKey)?.gameObject;
            if (target != null) {
                distance = Vector2.Distance(target.transform.position, transform.position);
                return;
            }
            var targetGameObject = _bbModule.GetObject<GameObject>(targetKey);
            if (targetGameObject != null) {
                target = targetGameObject;
            }

            distance = target == null ? 0f : Vector2.Distance(target.transform.position, transform.position);
        }
    }
}