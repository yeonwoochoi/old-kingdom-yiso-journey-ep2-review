using Core.Event;

namespace Core.Localization {
    public struct YisoLocaleChangeEvent {
        public LocaleType prevType;
        public LocaleType newType;

        public static void TriggerEvent(LocaleType prevType, LocaleType newType) {
            var args = new YisoLocaleChangeEvent {
                prevType = prevType,
                newType = newType
            };
            YisoEventSystem.TriggerEvent(args);
        }
    }
}