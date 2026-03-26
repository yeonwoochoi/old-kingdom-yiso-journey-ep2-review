#if UNITY_EDITOR
using UnityEditor;

namespace Editor.Map {
    /// <summary>
    /// [역할] 기획자용 맵 오브젝트 배치 도구
    /// [열기] Unity 메뉴 → Yiso → Map Editor
    /// [기능]
    ///   - MapDataSO 선택 → 맵 그리드 위에 오브젝트 시각적 배치
    ///   - NPC / 적 / 포탈 오브젝트 드래그&드롭 배치
    ///   - 배치 결과 MapDataSO에 자동 저장 (AssetDatabase.SaveAssets)
    ///   - "테스트 실행" 버튼 → 해당 맵 즉시 로드 (에디터 플레이 모드)
    /// [워크플로우]
    ///   1. MapData SO 생성 (우클릭 → Create → Yiso/Map/MapData)
    ///   2. MapEditorWindow 열기
    ///   3. SO 선택 후 오브젝트 배치
    ///   4. 저장 → 런타임에 MapDataBaker가 소비
    /// </summary>
    public class MapEditorWindow : EditorWindow {
    }
}
#endif
