using System.Collections.Generic;
using Core.Log;
using Core.Singleton;
using UnityEngine;

namespace Core.Localization {
    /// <summary>
    /// [역할] 다국어 텍스트 관리
    /// [책임]
    ///   - StreamingAssets/Localization/{locale}/StringTable.txt 로드
    ///   - 텍스트 키 → 현재 언어 문자열 변환 API 제공
    ///   - 런타임 언어 변경 지원
    /// [타입] Singleton (Unity lifecycle 불필요)
    /// </summary>
    public class YisoLocalizationSystem : YisoSingleton<YisoLocalizationSystem>, IYisoSystem {
        private readonly Dictionary<string, string> _data = new();

        public LocaleType CurrentLocale { get; private set; }

        public void Initialize() {
            LoadLocale();
            LoadStringTable(CurrentLocale);
        }

        public string Get(string key) {
            if (_data.TryGetValue(key, out var value)) return value;
            YisoLogSystem.Error($"[Localization] Key not found: {key}");
            return key;
        }

        // 런타임 언어 변경
        public void SetLocale(LocaleType locale) {
            if (CurrentLocale == locale) return;
            var prevLocale = CurrentLocale;
            CurrentLocale = locale;
            _data.Clear();
            LoadStringTable(locale);
            YisoLocaleChangeEvent.TriggerEvent(prevLocale, CurrentLocale);
        }

        private void LoadLocale() {
            var so = Resources.Load<YisoLocaleSO>(YisoResourcePath.Locale);
            if (so == null) {
                YisoLogSystem.Error("[Localization] LocaleSO 로드 실패. 기본값 KR 적용");
                CurrentLocale = LocaleType.KR;
                return;
            }
            CurrentLocale = so.localeType;
        }

        private void LoadStringTable(LocaleType locale) {
            var path = locale switch {
                LocaleType.KR => YisoResourcePath.StringTableKR,
                LocaleType.EN => YisoResourcePath.StringTableEN,
                _ => ""
            };
            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null) {
                YisoLogSystem.Error($"[Localization] StringTable 없음: {path}");
                return;
            }

            var lines = textAsset.text.Split('\n');
            
            foreach (var line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var idx = line.IndexOf('\t');
                if (idx == -1) {
                    YisoLogSystem.Error($"[Localization] 포맷 오류 (탭 없음): {line}");
                    continue;
                }

                var key   = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();

                if (!_data.TryAdd(key, value))
                    YisoLogSystem.Error($"[Localization] 중복 키: {key}");
            }
        }
    }
}
