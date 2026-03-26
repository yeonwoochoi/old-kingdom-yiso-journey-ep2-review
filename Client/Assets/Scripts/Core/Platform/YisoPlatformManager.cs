using Core.Singleton;

namespace Core.Platform {
    public class YisoPlatformManager : YisoSingleton<YisoPlatformManager> {
#if UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        public bool IsMobile => true;
#else
        public bool IsMobile => false;
#endif
    }
}