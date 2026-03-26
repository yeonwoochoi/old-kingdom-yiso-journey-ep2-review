namespace Core.Scene.Controller {
    /// <summary>
    /// [역할] Login 씬 전용 컨트롤러
    /// [책임]
    ///   - Login 씬 진입/퇴장 시 패킷 핸들러 등록/해제
    ///   - 로그인 완료 → YisoSceneManager.LoadScene(GameMap) 요청
    /// [배치] LoginScene 루트 GameObject에 단독 배치
    /// </summary>
    public class SceneLogin : SceneBase {

    }
}
