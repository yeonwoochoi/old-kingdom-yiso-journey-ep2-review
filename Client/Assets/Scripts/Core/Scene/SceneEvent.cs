using Core.Event;

namespace Core.Scene {
    public enum YisoSceneType {
        Login,
        BaseCamp,
        Chapter,
        InfiniteDojo,
        Transition
    }
    
    public struct SceneTransitionStartEvent {
        public YisoSceneType From;
        public YisoSceneType To;

        public static void TriggerEvent(YisoSceneType from, YisoSceneType to) {
            YisoEventSystem.TriggerEvent(new SceneTransitionStartEvent {
                From = from,
                To = to
            });
        }
    }
    
    public struct SceneTransitionCompleteEvent {
        public YisoSceneType From;
        public YisoSceneType To;

        public static void TriggerEvent(YisoSceneType from, YisoSceneType to) {
            YisoEventSystem.TriggerEvent(new SceneTransitionCompleteEvent {
                From = from,
                To = to
            });
        }
    }
}