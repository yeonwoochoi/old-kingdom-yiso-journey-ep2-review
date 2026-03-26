using System;
using System.Collections;
using Core.Log;
using Core.Singleton;
using Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;

namespace Core.Scene {
    /// <summary>
    /// [역할] 씬 전환 및 로딩 관리
    /// [책임]
    ///   - 씬 비동기 로딩 및 로딩 스크린 표시
    ///   - 이전 씬 메모리 해제
    ///   - 씬 전환 시 SceneTransitionEvent 발행 → CameraManager, SoundManager 등 구독
    /// [타입] MonoSingleton (LoadSceneAsync 코루틴 필요)
    /// </summary>
    public class YisoSceneManager : YisoMonoSingleton<YisoSceneManager> {
        public YisoSceneType PrevScene { get; private set; }
        public YisoSceneType CurrentScene { get; private set; }

        protected override void Awake() {
            base.Awake();
            if (Enum.TryParse<YisoSceneType>(SceneManager.GetActiveScene().name, out var type)) {
                PrevScene = CurrentScene = type;
                return;
            }
            PrevScene = CurrentScene = YisoSceneType.Login;
            YisoLogger.Warn($"{SceneManager.GetActiveScene().name}이 YisoSceneType으로 파싱되지 않습니다. (일단, Login으로 설정)");
        }

        public void LoadScene(YisoSceneType type) {
            StartCoroutine(LoadSceneCo(type));
        }

        private IEnumerator LoadSceneCo(YisoSceneType next) {
            if (next == YisoSceneType.Transition || next == CurrentScene) {
                yield break;
            }

            SceneTransitionStartEvent.TriggerEvent(PrevScene, CurrentScene);
            yield return YisoYieldCache.Seconds(YisoUIManager.FADE_DURATION);

            PrevScene = CurrentScene;
            yield return LoadSceneInternalCo(YisoSceneType.Transition); // 빈 Scene으로 이동했다가 target scene으로 이동해야 리소스가 100프로 정리됨.
            yield return LoadSceneInternalCo(next);
            CurrentScene = next;
            SceneTransitionCompleteEvent.TriggerEvent(PrevScene, CurrentScene);
        }

        private IEnumerator LoadSceneInternalCo(YisoSceneType type) {
            var handle = SceneManager.LoadSceneAsync(type.ToString());
            while (!handle.isDone) {
                yield return null;
            }
        }
    }
}
