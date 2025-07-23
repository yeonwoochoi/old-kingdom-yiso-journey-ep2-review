using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Tools.Event {
    [ExecuteAlways]
    public static class YisoEventManager {
        private static Dictionary<Type, List<IYisoEventListenerBase>> subscribersList;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeStatics() {
            subscribersList = new Dictionary<Type, List<IYisoEventListenerBase>>();
        }

        static YisoEventManager() {
            subscribersList = new Dictionary<Type, List<IYisoEventListenerBase>>();
        }

        public static void AddListener<YisoEvent>(IYisoEventListener<YisoEvent> listener) where YisoEvent : struct {
            Type eventType = typeof(YisoEvent);

            if (!subscribersList.ContainsKey(eventType)) {
                subscribersList[eventType] = new List<IYisoEventListenerBase>();
            }

            if (!SubscriptionExists(eventType, listener)) {
                subscribersList[eventType].Add(listener);
            }
        }

        public static void RemoveListener<YisoEvent>(IYisoEventListener<YisoEvent> listener) where YisoEvent : struct {
            Type eventType = typeof(YisoEvent);
            var listenerFound = false;

            if (!subscribersList.ContainsKey(eventType)) {
                throw new ArgumentException(
                    $"Removing listener \"{listener}\", but the event type \"{eventType.ToString()}\" isn't registered.");
            }

            var subscriberList = subscribersList[eventType];

            for (var i = subscriberList.Count - 1; i >= 0; i--) {
                if (subscriberList[i] == listener) {
                    subscriberList.Remove(subscriberList[i]);
                    listenerFound = true;

                    if (subscriberList.Count == 0) {
                        subscribersList.Remove(eventType);
                    }

                    return;
                }
            }

            if (!listenerFound) {
                throw new ArgumentException(
                    $"Removing listener, but the supplied receiver isn't subscribed to event type \"{eventType.ToString()}\".");
            }
        }

        public static void TriggerEvent<YisoEvent>(YisoEvent newEvent) where YisoEvent : struct {
            if (!subscribersList.TryGetValue(typeof(YisoEvent), out var listeners)) {
                // throw new ArgumentException($"Attempting to send event of type \"{typeof(YisoEvent).ToString()}\", but no listener for this type has been found. Make sure this.Subscribe<{typeof(YisoEvent).ToString()}>(EventRouter) has been called, or that all listeners to this event haven't been unsubscribed.");
                return;
            }

            for (var i = listeners.Count - 1; i >= 0; i--) {
                (listeners[i] as IYisoEventListener<YisoEvent>)?.OnEvent(newEvent);
            }
        }

        private static bool SubscriptionExists(Type type, IYisoEventListenerBase receiver) {
            if (!subscribersList.TryGetValue(type, out var receivers)) return false;

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

    public static class EventRegister {
        public static void YisoEventStartListening<YisoEvent>(this IYisoEventListener<YisoEvent> caller)
            where YisoEvent : struct {
            YisoEventManager.AddListener(caller);
        }

        public static void YisoEventStopListening<YisoEvent>(this IYisoEventListener<YisoEvent> caller)
            where YisoEvent : struct {
            YisoEventManager.RemoveListener(caller);
        }
    }

    public interface IYisoEventListenerBase {
    }

    public interface IYisoEventListener<T> : IYisoEventListenerBase {
        void OnEvent(T eventType);
    }
}