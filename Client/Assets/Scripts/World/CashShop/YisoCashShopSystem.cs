using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 인앱 결제(IAP) 관리
    /// [책임]
    ///   - 영구 버프, 코스튬, 편의 아이템 결제 검증 및 지급
    /// [타입] MonoSingleton (IAP 코루틴 필요)
    /// </summary>
    public class YisoCashShopSystem : YisoMonoSingleton<YisoCashShopSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
