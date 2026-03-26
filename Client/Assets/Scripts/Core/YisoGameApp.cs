using Core.Config;
using Core.Input;
using Core.Scene;
using Core.Sound;
using Core.Time;
using Core.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core {
    /// <summary>
    /// Layer 1 — BootStrapper
    /// [구조]
    ///   - YisoResourceManager: 직속 자식 프리팹 (씬에 미리 배치)
    ///   - 나머지 매니저: Addressable 키로 동적 로드 → 자식으로 등록
    /// [키 규칙] nameof(YisoXxxManager) — Addressable 그룹에서 동일한 이름으로 등록
    /// </summary>
    public class YisoGameApp : YisoBehaviour {
        // Addressable로 로드할 매니저 키 목록 (의존성 순서 유지)
        private static readonly string[] ManagerKeys = {
            nameof(YisoConfigManager),
            nameof(YisoSceneManager),
            nameof(YisoCameraManager),
            nameof(YisoSoundManager),
            nameof(YisoInputManager),
            nameof(YisoUIManager),
            nameof(YisoTimeManager),
        };

        private void Awake() {
            DontDestroyOnLoad(gameObject);
        }

        private async void Start() {
            // ResourceManager는 직속 자식으로 이미 존재 — Awake 완료 상태
            foreach (var key in ManagerKeys) {
                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                var prefab = await handle.Task;
                Instantiate(prefab, transform);
            }

            YisoSceneManager.Instance.LoadScene(YisoSceneType.Login);
        }
    }
}
