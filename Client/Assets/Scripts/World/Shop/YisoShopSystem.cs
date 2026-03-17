using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] NPC 상점 거래
    /// [책임]
    ///   - 상점 목록 데이터 관리
    ///   - 구매 / 판매 시 골드 처리 → InventorySystem 연동
    ///   - UIManager에 ShopWindow 열기 요청
    /// [타입] Singleton (데이터 관리, Unity lifecycle 불필요)
    /// </summary>
    public class YisoShopSystem : YisoSingleton<YisoShopSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
