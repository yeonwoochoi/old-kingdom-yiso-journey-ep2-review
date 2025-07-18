using System.Collections.Generic;
using UnityEngine;


namespace Core.Manager {
    /// <summary>
    /// Manages the update lifecycle for objects implementing IUpdatable.
    /// Ensures that update methods such as OnUpdate, OnFixedUpdate, and OnLateUpdate
    /// are called according to the Unity Engine's lifecycle.
    /// </summary>
    public class RunIUpdateManager : MonoBehaviour {
        private static RunIUpdateManager instance = null;
        private readonly List<IUpdatable> updatables = new();

        private static bool isApplicationStop = false;

        /// <summary>
        /// Singleton instance of the update manager. It ensures only one instance
        /// of the manager exists within the game at any time.
        /// </summary>
        public static RunIUpdateManager Instance {
            get {
                if (isApplicationStop) return null;
                if (instance != null) return instance;
                instance = FindObjectOfType<RunIUpdateManager>();
                if (instance != null) return instance;

                GameObject newGameObject = new GameObject(typeof(RunIUpdateManager).ToString());
                newGameObject.AddComponent<RunIUpdateManager>();
                instance = newGameObject.GetComponent<RunIUpdateManager>();
                DontDestroyOnLoad(instance);
                return instance;
            }
        }

        /// <summary>
        /// Handles the cleanup of the singleton instance when the application stops.
        /// </summary>
        private void OnDestroy() {
            isApplicationStop = true;
        }
        
        /// <summary>
        /// Registers an object to be updated by this manager.
        /// </summary>
        /// <param name="updatable">The object to register.</param>
        public void Register(IUpdatable updatable) {
            if (updatables.Contains(updatable)) return;
            updatables.Add(updatable);
        }

        /// <summary>
        /// Unregisters an object from being updated by this manager.
        /// </summary>
        /// <param name="updatable">The object to unregister.</param>
        public void UnRegister(IUpdatable updatable) {
            if (!updatables.Contains(updatable)) return;
            updatables.Remove(updatable);
        }

        /// <summary>
        /// Called once per frame. Updates all registered IUpdatable objects.
        /// </summary>
        private void Update() {
            for (var i = updatables.Count - 1; i >= 0; i--) {
                updatables[i].OnUpdate();
            }
        }

        /// <summary>
        /// Called at a fixed interval. Updates all registered IUpdatable objects using FixedUpdate.
        /// </summary>
        private void FixedUpdate() {
            for (var i = updatables.Count - 1; i >= 0; i--) {
                updatables[i].OnFixedUpdate();
            }
        }

        /// <summary>
        /// Called after all Update functions. Updates all registered IUpdatable objects using LateUpdate.
        /// </summary>
        private void LateUpdate() {
            for (var i = updatables.Count - 1; i >= 0; i--) {
                updatables[i].OnLateUpdate();
            }
        }

        /// <summary>
        /// Interface for objects that need to be updated by RunIUpdateManager.
        /// </summary>
        public interface IUpdatable {
            void OnUpdate();
            void OnFixedUpdate();
            void OnLateUpdate();
        }
    }
}
