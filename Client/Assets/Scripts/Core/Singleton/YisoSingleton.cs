using UnityEngine;

namespace Core.Singleton {
    public abstract class YisoSingleton<T> where T : new() {
        private static T _instance;
        public static T Instance => _instance ??= new T();
    }
    
    public abstract class YisoMonoSingleton<T>: YisoBehaviour where T: Component {
        private static T _instance;

        public static T Instance {
            get {
                if (_instance != null) return _instance;
                _instance = FindFirstObjectByType<T>();
                return _instance;
            }
        }

        protected virtual void Awake() {
            if (_instance == null) {
                _instance = this as T;
                DontDestroyOnLoad(this);
            }
            else if (_instance != this) {
                Destroy(gameObject);
            }
        }
    }
}