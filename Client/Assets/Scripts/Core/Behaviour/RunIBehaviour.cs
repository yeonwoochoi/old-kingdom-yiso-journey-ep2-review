using UnityEngine;
using Core.Manager;

namespace Core.Behaviour {
    /// <summary>
    /// Abstract base class for components managed by the RunIUpdateManager. It handles
    /// registration with the update manager on enable and unregister on disable,
    /// and provides methods for component caching to improve performance.
    /// </summary>
    public abstract class RunIBehaviour : MonoBehaviour, RunIUpdateManager.IUpdatable {
        protected virtual void Awake() { }
        
        protected virtual void OnEnable() {
            RunIUpdateManager.Instance.Register(this);
        }
        
        protected virtual void Start() { }
        
        public virtual void OnUpdate() { }

        public virtual void OnFixedUpdate() { }

        public virtual void OnLateUpdate() { }
        
        protected virtual void OnDisable() {
            if (RunIUpdateManager.Instance != null)
                RunIUpdateManager.Instance.UnRegister(this);
        }
        
        protected virtual void OnDestroy() { }
    }
}