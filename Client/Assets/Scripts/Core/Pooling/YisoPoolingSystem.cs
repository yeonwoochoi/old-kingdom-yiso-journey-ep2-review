using System;
using System.Collections.Generic;
using Core.Event;
using Core.Scene;
using Core.Singleton;
using UnityEngine;

namespace Core.Pooling {
    public interface IPoolable {
        void OnRent();
        
        void OnReturn();
    }
    
    public abstract class PoolableInstance : IPoolable {
        public virtual void OnRent() { }
        public virtual void OnReturn() { }
    }
    
    public abstract class PoolableObject : YisoBehaviour, IPoolable {
        public virtual void OnRent() { }
        public virtual void OnReturn() { }
    }

    /// <summary>
    /// [역할] 오브젝트 풀 관리 (GC 병목 방지)
    /// [책임]
    ///   - 몬스터, 투사체, 데미지 폰트, 드랍 아이템, 이펙트, AudioSource 풀 관리
    ///   - 풀에서 오브젝트 대여(Rent) / 반납(Return) API 제공
    /// [타입] Singleton (Unity API 불필요, GameObject 생성은 new GameObject()로 처리)
    /// </summary>
    public class YisoPoolingSystem : YisoSingleton<YisoPoolingSystem>, IYisoSystem,
        IYisoEventListener<SceneTransitionStartEvent> {
        // 순수 인스턴스 풀: Type -> 대기 큐
        private readonly Dictionary<Type, Queue<PoolableInstance>> _instancePools = new();

        // GameObject 컴포넌트 풀: Type -> 풀 정보
        private readonly Dictionary<Type, ObjectPoolEntry> _objectPools = new();

        private readonly Dictionary<Type, Transform> _poolParents = new();
        private Transform _rootParent;

        private class ObjectPoolEntry {
            public PoolableObject Prefab;
            public Transform Parent;
            public readonly Queue<PoolableObject> Queue = new();
        }

        public void Initialize() {
            CreateRootObject();
            this.StartListening();
        }

        private void CreateRootObject() {
            _rootParent = new GameObject("ObjectPool").transform;
            _rootParent.transform.position = Vector3.zero;
            UnityEngine.Object.DontDestroyOnLoad(_rootParent.gameObject);
        }

        private Transform CreateParent(Type type) {
            if (!_poolParents.TryGetValue(type, out var parent)) {
                parent = new GameObject(type.ToString()).transform;
                parent.SetParent(_rootParent);
                parent.position = Vector3.zero;
            }
            _poolParents[type] = parent;
            return parent;
        }

        public void OnEvent(SceneTransitionStartEvent e) {
            ClearAll();
        }

        private void ClearAll() {
            foreach (var entry in _objectPools.Values) {
                while (entry.Queue.Count > 0) {
                    var obj = entry.Queue.Dequeue();
                    if (obj != null) UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            _objectPools.Clear();
            _instancePools.Clear();
            _poolParents.Clear();
        }
        
        public void CreatePool<T>(int size) where T : PoolableInstance, new() {
            var type = typeof(T);
            if (_instancePools.ContainsKey(type)) return;

            var queue = new Queue<PoolableInstance>(size);
            for (var i = 0; i < size; i++)
                queue.Enqueue(new T());
            _instancePools[type] = queue;
        }

        public T Rent<T>(int extendPoolSize = 10) where T : PoolableInstance, new() {
            var type = typeof(T);
            if (!_instancePools.ContainsKey(type))
                CreatePool<T>(extendPoolSize);

            var queue = _instancePools[type];
            if (queue.Count == 0)
                ExtendInstancePool<T>(queue, extendPoolSize);

            var obj = (T)queue.Dequeue();
            obj.OnRent();
            return obj;
        }
        
        public void Return<T>(T obj) where T : PoolableInstance {
            if (obj == null) return;
            var type = typeof(T);
            if (!_instancePools.ContainsKey(type)) return;

            obj.OnReturn();
            _instancePools[type].Enqueue(obj);
        }
        
        public void CreateObjectPool<T>(T prefab, int size) where T : PoolableObject {
            var type = typeof(T);
            if (_objectPools.ContainsKey(type)) return;
            
            var parent = CreateParent(type);
            var entry = new ObjectPoolEntry { Prefab = prefab, Parent = parent };
            _objectPools[type] = entry;

            for (var i = 0; i < size; i++)
                entry.Queue.Enqueue(CreateInstance(prefab, parent));
        }

        public bool TryRentObject<T>(out T obj, int extendPoolSize = 10) where T : PoolableObject {
            obj = null;
            var type = typeof(T);
            if (!_objectPools.TryGetValue(type, out var entry)) return false;

            if (entry.Queue.Count == 0)
                ExtendObjectPool(entry, extendPoolSize);

            obj = (T)entry.Queue.Dequeue();
            obj.gameObject.SetActive(true);
            obj.OnRent();
            return true;
        }

        public void ReturnObject<T>(T obj) where T : PoolableObject {
            if (obj == null) return;
            var type = typeof(T);
            if (!_objectPools.TryGetValue(type, out var entry)) return;

            obj.OnReturn();
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(entry.Parent);
            entry.Queue.Enqueue(obj);
        }
        
        private static void ExtendInstancePool<T>(Queue<PoolableInstance> queue, int count)
            where T : PoolableInstance, new() {
            for (var i = 0; i < count; i++)
                queue.Enqueue(new T());
        }

        private static void ExtendObjectPool(ObjectPoolEntry entry, int count) {
            for (var i = 0; i < count; i++)
                entry.Queue.Enqueue(CreateInstance(entry.Prefab, entry.Parent));
        }

        private static T CreateInstance<T>(T prefab, Transform parent) where T : PoolableObject {
            var obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}
