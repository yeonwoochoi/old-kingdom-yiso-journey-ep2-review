using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers.Event {
    public interface IYisoEventListenerBase { }

    public interface IYisoEventListener<T>: IYisoEventListenerBase {
        public void OnEvent(T eventType);
    }

    public static class YisoEventManager {
        private static Dictionary<Type, List<IYisoEventListenerBase>> _subscribersList;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeStatics() {
            _subscribersList = new Dictionary<Type, List<IYisoEventListenerBase>>();
        }

        static YisoEventManager() {
            _subscribersList = new Dictionary<Type, List<IYisoEventListenerBase>>();
        }

        public static void AddListener<YisoEvent>(IYisoEventListener<YisoEvent> listener) where YisoEvent : struct {
            var eventType = typeof(YisoEvent);
            if (!_subscribersList.TryGetValue(eventType, out _)) {
                _subscribersList[eventType] = new List<IYisoEventListenerBase>();
            }

            if (!SubscriptionExists(eventType, listener)) {
                _subscribersList[eventType].Add(listener);
            }
        }

        public static void RemoveListener<YisoEvent>(IYisoEventListener<YisoEvent> listener) where YisoEvent : struct {
            var eventType = typeof(YisoEvent);
            if (!SubscriptionExists(eventType, listener)) return;
            _subscribersList[eventType].Remove(listener);
        }

        public static void TriggerEvent<YisoEvent>(YisoEvent newEvent) where YisoEvent : struct {
            if (!_subscribersList.TryGetValue(typeof(YisoEvent), out var listeners)) return;

            for (var i = listeners.Count - 1; i >= 0; i--) {
                (listeners[i] as IYisoEventListener<YisoEvent>)?.OnEvent(newEvent);
            }
        }

        private static bool SubscriptionExists(Type type, IYisoEventListenerBase receiver) {
            if (!_subscribersList.TryGetValue(type, out var receivers)) return false;

            for (var i = receivers.Count - 1; i >= 0; i--) {
                if (receivers[i] == receiver) return true;
            }

            return false;
        }
    }

    public static class YisoEventRegister {
        public static void YisoEventStartListening<YisoEvent>(this IYisoEventListener<YisoEvent> listener) where YisoEvent : struct {
            YisoEventManager.AddListener(listener);
        }
        public static void YisoEventStopListening<YisoEvent>(this IYisoEventListener<YisoEvent> listener) where YisoEvent : struct {
            YisoEventManager.RemoveListener(listener);
        }
    }
}