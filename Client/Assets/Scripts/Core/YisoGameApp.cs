using System.Collections.Generic;
using Core.Config;
using Core.Input;
using Core.Localization;
using Core.Pooling;
using Core.Scene;
using Core.Sound;
using Core.Time;
using Core.UI;
using UnityEngine;

namespace Core {
    /// <summary>
    /// Layer 1 — BootStrapper
    /// Awake: 모든 인스턴스 생성 (의존성 참조 없음)
    /// Start: 의존성 순서대로 Initialize() 호출
    /// </summary>
    public class YisoGameApp : YisoBehaviour {
        private readonly List<IYisoSystem> _systems = new();

        private void Awake() {
            DontDestroyOnLoad(gameObject);

            _systems.Add(CreateSystem<YisoConfigSystem>());    // 설정값 제공
            _systems.Add(YisoPoolingSystem.Instance);          // 오브젝트 풀
            _systems.Add(CreateSystem<YisoTimeSystem>());      // 시간 제어
            _systems.Add(CreateSystem<YisoInputSystem>());     // 입력
            _systems.Add(CreateSystem<YisoSoundSystem>());    // 사운드
            _systems.Add(CreateSystem<YisoUISystem>());        // UI 프레임워크
            _systems.Add(CreateSystem<YisoSceneSystem>());     // 씬 전환
            _systems.Add(YisoLocalizationSystem.Instance);     // 다국어
            _systems.Add(CreateSystem<YisoCameraSystem>());    // 카메라
        }

        private void Start() {
            foreach (var system in _systems)
                system.Initialize();

            YisoSceneSystem.Instance.LoadScene(YisoSceneType.Login);
        }

        private void Update() {
            // NetworkSystem (s) — Phase 3 Infra 초기화 이후 활성화
            // YisoNetworkSystem.Instance.Tick();
        }

        private T CreateSystem<T>() where T : Component, IYisoSystem {
            return gameObject.AddComponent<T>();
        }
    }
}
