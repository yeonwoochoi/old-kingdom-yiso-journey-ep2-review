using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Event {
    public interface IYisoEventListenerBase { }
    
    public interface IYisoEventListener<T>: IYisoEventListenerBase {
        public void OnEvent(T args);
    }
    
    public static class YisoEventManager {
        private static readonly Dictionary<Type, List<IYisoEventListenerBase>> _listeners = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void InitializeStatics() {
            _listeners.Clear();
        }

        public static void AddListener<T>(IYisoEventListener<T> listener) where T: struct {
            var eventType = typeof(T);
            if (!_listeners.ContainsKey(eventType)) {
                _listeners[eventType] = new List<IYisoEventListenerBase>();
            }

            if (!_listeners[eventType].Contains(listener)) {
                _listeners[eventType].Add(listener);                
            }
        }

        public static void RemoveListener<T>(IYisoEventListener<T> listener) where T: struct {
            var eventType = typeof(T);
            if (!_listeners.TryGetValue(eventType, out var listeners)) {
                throw new ArgumentException($"Removing listener \"{listener}\", but the event type \"{eventType.ToString()}\" isn't registered.");
            }

            var listenerFound = false;
            for (var i = listeners.Count - 1; i >= 0; i--) {
                if (listener == listeners[i]) {
                    listeners.RemoveAt(i);
                    listenerFound = true;
                }

                if (listeners.Count == 0) {
                    _listeners.Remove(eventType);
                }
            }

            if (!listenerFound) {
                throw new ArgumentException($"Removing listener, but the supplied receiver isn't subscribed to event type \"{eventType.ToString()}\".");
            }
        }

        public static void TriggerEvent<T>(T newEvent) where T: struct {
            if (!_listeners.TryGetValue(typeof(T), out var listeners)) {
                return;
            }

            for (var i = listeners.Count - 1; i >= 0; i--) {
                (listeners[i] as IYisoEventListener<T>)?.OnEvent(newEvent);
            }
        }

        private static bool ListenerExists(Type type, IYisoEventListenerBase receiver) {
            if (!_listeners.TryGetValue(type, out var receivers)) return false;

            var exists = false;

            for (var i = receivers.Count - 1; i >= 0; i--) {
                if (receivers[i] == receiver) {
                    exists = true;
                    break;
                }
            }

            return exists;
        }
    }

    public static class YisoEventRegisterer {
        public static void StartListening<T>(this IYisoEventListener<T> listener) where T: struct {
            YisoEventManager.AddListener<T>(listener);
        }
        public static void StopListening<T>(this IYisoEventListener<T> listener) where T: struct {
            YisoEventManager.RemoveListener<T>(listener);
        }
    }
}
