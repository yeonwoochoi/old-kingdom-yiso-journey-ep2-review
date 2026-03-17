using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Core.Log;
using Core.Singleton;
using Mono.Cecil;
using UnityEngine;

namespace Core.Config {
    /// <summary>
    /// [역할] 게임 환경 설정 관리
    /// [책임]
    ///   - ConfigSO에서 서버 주소, 앱 기본값 로드
    ///   - PlayerPrefs 래퍼 (유저 설정 읽기/쓰기) — Key·Value 모두 암호화
    ///   - 볼륨, 언어 등 유저 설정 Get/Set API 제공
    ///   - 저장된 값이 없을 때 ConfigSO 기본값 반환 (SaveSystem 초기 로드 대비)
    /// [타입] MonoSingleton (SerializeField로 ConfigSO 수신)
    /// </summary>

    public enum PrefKeys {
        BgmVolume,
        SfxVolume,
        Language
    }
    
    public class YisoConfigSystem : YisoMonoSingleton<YisoConfigSystem>, IYisoSystem {
        private YisoConfigSO _config;
        private Dictionary<PrefKeys, object> _defaults;
        private byte[] _aesKey;
        
        // env var 이름
        private const string SECRET_ENV_KEY = "YISO_PREF_SECRET";
        // Live 빌드에서 env var 미설정 시 폴백 금지 - Dev/Stage 전용 임시값
        private const string DEV_FALLBACK_SECRET = "dev_fallback_do_not_use_in_live";

        public YisoConfigSO Config => _config;
        public AppEnvironment Environment => _config.environment;

        public void Initialize() {
            _config = Resources.Load<YisoConfigSO>(YisoResourcePath.Config);
            if (_config == null)
                throw new InvalidOperationException($"[ConfigSystem] ConfigSO를 찾을 수 없습니다.");
            
            _aesKey = DeriveKey(ResolveSecret());
            BuildDefaults();
            YisoLogSystem.Configure(_config.environment != AppEnvironment.Live);
        }

        private string ResolveSecret() {
            var secret = System.Environment.GetEnvironmentVariable(SECRET_ENV_KEY);
            if (!string.IsNullOrEmpty(secret)) return secret;

            if (_config.environment == AppEnvironment.Live)
                throw new InvalidOperationException($"[ConfigSystem] {SECRET_ENV_KEY} 환경 변수가 설정되지 않았습니다. Live 빌드에서는 필수입니다.");

            YisoLogSystem.Warn($"{SECRET_ENV_KEY} 미설정 - Dev 폴백 키 사용 (Live 빌드 절대 금지)");
            return DEV_FALLBACK_SECRET;
        }

        public float GetFloat(PrefKeys key, float fallback = 0f) {
            var encKey = EncryptKey(key);
            if (!PlayerPrefs.HasKey(encKey))
                return _defaults.TryGetValue(key, out var def) ? (float)def : fallback;

            return TryDecrypt(PlayerPrefs.GetString(encKey), out var plain)
                ? float.Parse(plain, CultureInfo.InvariantCulture)
                : fallback;
        }

        public int GetInt(PrefKeys key, int fallback = 0) {
            var encKey = EncryptKey(key);
            if (!PlayerPrefs.HasKey(encKey))
                return _defaults.TryGetValue(key, out var def) ? (int)def : fallback;

            return TryDecrypt(PlayerPrefs.GetString(encKey), out var plain)
                ? int.Parse(plain)
                : fallback;
        }

        public string GetString(PrefKeys key, string fallback = "") {
            var encKey = EncryptKey(key);
            if (!PlayerPrefs.HasKey(encKey))
                return _defaults.TryGetValue(key, out var def) ? (string)def : fallback;

            return TryDecrypt(PlayerPrefs.GetString(encKey), out var plain) ? plain : fallback;
        }

        public void SetFloat(PrefKeys key, float value) =>
            SavePref(key, value.ToString(CultureInfo.InvariantCulture));

        public void SetInt(PrefKeys key, int value) =>
            SavePref(key, value.ToString());

        public void SetString(PrefKeys key, string value) =>
            SavePref(key, value);

        public bool HasKey(PrefKeys key) =>
            PlayerPrefs.HasKey(EncryptKey(key));

        public void DeleteKey(PrefKeys key) {
            PlayerPrefs.DeleteKey(EncryptKey(key));
            PlayerPrefs.Save();
        }

        private void BuildDefaults() {
            _defaults = new Dictionary<PrefKeys, object> {
                { PrefKeys.BgmVolume, _config.defaultBgmVolume },
                { PrefKeys.SfxVolume, _config.defaultSfxVolume },
                { PrefKeys.Language,  (int)_config.defaultLanguage },
            };
        }

        private void SavePref(PrefKeys key, string plainValue) {
            PlayerPrefs.SetString(EncryptKey(key), Encrypt(plainValue));
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Key를 SHA256으로 해싱 - PlayerPrefs에 저장되는 실제 키 이름을 숨김
        /// enum.ToString()을 raw key로 사용하므로 enum 값 이름 변경 시 기존 저장값과 불일치 주의
        /// </summary>
        private static string EncryptKey(PrefKeys key) {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key.ToString() + "_yiso_salt"));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// AES-256 암호화. 매 호출마다 랜덤 IV를 생성하고 [IV(16B) | CipherText]를 Base64로 저장
        /// </summary>
        private string Encrypt(string plaintext) {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();

            using var enc = aes.CreateEncryptor();
            var plain = Encoding.UTF8.GetBytes(plaintext);
            var cipher = enc.TransformFinalBlock(plain, 0, plain.Length);

            var combined = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, combined, aes.IV.Length, cipher.Length);
            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// AES-256 복호화. 실패 시 false 반환 (데이터 손상 대비)
        /// </summary>
        private bool TryDecrypt(string ciphertext, out string plaintext) {
            plaintext = null;
            try {
                var combined = Convert.FromBase64String(ciphertext);
                using var aes = Aes.Create();
                aes.Key = _aesKey;

                var iv = new byte[aes.BlockSize / 8]; // 16 bytes
                var cipher = new byte[combined.Length - iv.Length];
                Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(combined, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;
                using var dec = aes.CreateDecryptor();
                var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                plaintext = Encoding.UTF8.GetString(plain);
                return true;
            }
            catch (Exception e) {
                YisoLogSystem.Warn($"PlayerPrefs 복호화 실패, 기본값 사용: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 비밀 문자열을 SHA256으로 변환해 AES-256 키(32바이트)로 사용
        /// </summary>
        private static byte[] DeriveKey(string secret) {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }
    }
}
