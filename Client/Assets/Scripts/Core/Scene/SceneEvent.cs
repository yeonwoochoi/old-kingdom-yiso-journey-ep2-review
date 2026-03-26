using Core.Event;

namespace Core.Scene {
    public enum YisoSceneType {
        Login,      // 로그인 / 슬롯 선택 / 캐릭터 생성 / 설정
        Field,      // 모든 인게임 필드 (맵 타입은 MapType으로 구분)
        Transition  // 씬 전환 중 빈 씬 (리소스 완전 해제용)
    }
    
    public struct SceneTransitionStartEvent {
        public YisoSceneType From;
        public YisoSceneType To;

        public static void TriggerEvent(YisoSceneType from, YisoSceneType to) {
            YisoEventManager.TriggerEvent(new SceneTransitionStartEvent {
                From = from,
                To = to
            });
        }
    }
    
    public struct SceneTransitionCompleteEvent {
        public YisoSceneType From;
        public YisoSceneType To;

        public static void TriggerEvent(YisoSceneType from, YisoSceneType to) {
            YisoEventManager.TriggerEvent(new SceneTransitionCompleteEvent {
                From = from,
                To = to
            });
        }
    }
}