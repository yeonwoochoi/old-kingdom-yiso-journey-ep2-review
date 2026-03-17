using System;
using System.Collections;
using System.Collections.Generic;
using Core.Event;
using Core.Log;
using Core.Scene;
using Core.Singleton;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Sound {
    /// <summary>
    /// [역할] 오디오 재생 및 볼륨 관리
    /// [책임]
    ///   - BGM: DontDestroyOnLoad AudioSource 직접 소유, 씬 전환 무관하게 유지, 명시적 정지
    ///   - SFX: 자체 AudioSource 풀 관리, 씬 전환 시 재생 중인 SFX 중단
    ///   - Addressable AudioClip 로드 / 해제
    /// </summary>
    public class YisoSoundSystem : YisoMonoSingleton<YisoSoundSystem>, IYisoSystem,
        IYisoEventListener<SceneTransitionStartEvent> {

        private const int SfxPoolSize = 10;

        private YisoSoundDataSO _data;

        // BGM
        private AudioSource _bgmSource;
        private YisoSoundId? _currentBgmId;
        private AsyncOperationHandle<AudioClip> _bgmHandle;

        // SFX 자체 풀
        private class SfxEntry {
            public AudioSource Source;
            public AsyncOperationHandle<AudioClip> Handle;
            public Coroutine Coroutine;
        }
        private readonly Queue<AudioSource> _sfxPool = new();
        private readonly List<SfxEntry> _sfxActive = new();
        private Coroutine _crossFadeBgmCoroutine;
        private Coroutine _stopBgmCoroutine;

        public void Initialize() {
            _data = Resources.Load<YisoSoundDataSO>(YisoResourcePath.Sound);
            _data.Build();

            _bgmSource = CreateAudioSource("BGM");
            _bgmSource.loop = true;

            for (var i = 0; i < SfxPoolSize; i++)
                _sfxPool.Enqueue(CreateAudioSource($"SFX_{i}"));
        }

        private AudioSource CreateAudioSource(string objName) {
            var go = new GameObject(objName);
            go.transform.SetParent(transform);
            return go.AddComponent<AudioSource>();
        }

        public async void PlayBgm(YisoSoundId id) {
            if (_currentBgmId == id) return;

            try {
                var clipRef = _data.Get(id);
                var handle = clipRef.LoadAssetAsync();
                var clip = await handle.Task;

                if (_crossFadeBgmCoroutine == null) {
                    _crossFadeBgmCoroutine = StartCoroutine(PlayBgmCoroutine(clip, handle, id));
                }
                else {
                    Addressables.Release(handle);
                }
            }
            catch (Exception e) {
                YisoLogSystem.Error($"PlayBgm {id} failed: {e.Message}");
            }
        }

        public void StopBgm() {
            if (_stopBgmCoroutine != null)
                return;
            
            if (_crossFadeBgmCoroutine != null) {
                StopCoroutine(_crossFadeBgmCoroutine);
                _crossFadeBgmCoroutine = null;
            }
            _stopBgmCoroutine = StartCoroutine(StopBgmCoroutine());
        }

        private IEnumerator PlayBgmCoroutine(AudioClip clip, AsyncOperationHandle<AudioClip> handle, YisoSoundId id) {
            yield return StartCoroutine(StopBgmCoroutine());

            _bgmHandle = handle;
            _currentBgmId = id;

            _bgmSource.clip = clip;
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            while (_bgmSource.volume < 0.99f) {
                _bgmSource.volume = Mathf.Lerp(_bgmSource.volume, 1f, UnityEngine.Time.deltaTime);
                yield return null;
            }

            _bgmSource.volume = 1f;
            _crossFadeBgmCoroutine = null;
        }

        private IEnumerator StopBgmCoroutine() {
            if (_currentBgmId == null) yield break;
            _currentBgmId = null;

            while (_bgmSource.volume > 0.01f) {
                _bgmSource.volume = Mathf.Lerp(_bgmSource.volume, 0f, UnityEngine.Time.deltaTime);
                yield return null;
            }

            _bgmSource.volume = 0f;
            _bgmSource.Stop();
            _bgmSource.clip = null;

            if (_bgmHandle.IsValid())
                Addressables.Release(_bgmHandle);

            _stopBgmCoroutine = null;
        }

        public async void PlaySfx(YisoSoundId id) {
            try {
                var clipRef = _data.Get(id);
                var handle = clipRef.LoadAssetAsync();
                var clip = await handle.Task;

                if (_sfxPool.Count == 0) {
                    Addressables.Release(handle);
                    return;
                }

                var source = _sfxPool.Dequeue();
                var entry = new SfxEntry { Source = source, Handle = handle };
                _sfxActive.Add(entry);

                source.clip = clip;
                source.Play();

                entry.Coroutine = StartCoroutine(ReturnSfxWhenDone(entry));
            }
            catch (Exception e) {
                YisoLogSystem.Error($"PlaySfx {id} failed: {e.Message}");
            }
        }

        private IEnumerator ReturnSfxWhenDone(SfxEntry entry) {
            yield return new WaitUntil(() => !entry.Source.isPlaying);
            ReturnSfx(entry);
        }

        private void ReturnSfx(SfxEntry entry) {
            entry.Source.Stop();
            entry.Source.clip = null;
            _sfxActive.Remove(entry);
            _sfxPool.Enqueue(entry.Source);

            if (entry.Handle.IsValid())
                Addressables.Release(entry.Handle);
        }

        public void StopAllSfx() {
            for (var i = _sfxActive.Count - 1; i >= 0; i--) {
                var entry = _sfxActive[i];
                if (entry.Coroutine != null) StopCoroutine(entry.Coroutine);
                ReturnSfx(entry);
            }
        }

        public void OnEvent(SceneTransitionStartEvent e) {
            StopAllSfx();
        }

        private void OnEnable() => this.StartListening();

        private void OnDisable() => this.StopListening();
    }
}
