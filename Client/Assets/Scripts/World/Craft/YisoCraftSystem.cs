using Core;
using Core.Singleton;

namespace World {
    /// <summary>
    /// [역할] 아이템 제작 (향후 추가 예정)
    /// [책임]
    ///   - 제작 레시피 관리
    ///   - 재료 소모 및 결과물 생성
    /// [타입] MonoSingleton (제작 흐름 코루틴 필요)
    /// [NOTE] 현재 기획서에 없음. 향후 추가될 수 있어 폴더·클래스만 선언.
    /// </summary>
    public class YisoCraftSystem : YisoMonoSingleton<YisoCraftSystem>, IYisoSystem {
        public void Initialize() { }
    }
}
