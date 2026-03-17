using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 유저 소지품 관리
    /// [책임]
    ///   - 장비 장착 / 해제
    ///   - 귀환 주문서 등 아이템 사용
    ///   - 골드 관리
    /// [타입] Singleton (데이터 관리, Unity lifecycle 불필요)
    /// </summary>
    public class YisoInventorySystem : YisoSingleton<YisoInventorySystem>, IYisoSystem {
        public void Initialize() { }
    }
}
