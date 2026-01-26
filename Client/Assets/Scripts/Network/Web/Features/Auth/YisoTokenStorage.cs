using System;
using UnityEngine;

namespace Network.Web.Features.Auth {
    /// <summary>
    /// PlayerPrefs 기반 JWT 토큰 저장소
    /// </summary>
    public class YisoTokenStorage {
        private const string TokenKey = "yiso_auth_token";
        private const string UsernameKey = "yiso_auth_username";
        private const string ExpiresAtKey = "yiso_auth_expires_at";
        
        public void SaveToken(string token, string username, DateTime expiresAt) {
            PlayerPrefs.SetString(TokenKey, token);
            PlayerPrefs.SetString(UsernameKey, username);
            PlayerPrefs.SetString(ExpiresAtKey, expiresAt.ToString("o")); // ISO 8601 형식
            PlayerPrefs.Save();
        }
        
        public string LoadToken() {
            return PlayerPrefs.GetString(TokenKey, null);
        }
        
        public string LoadUsername() {
            return PlayerPrefs.GetString(UsernameKey, null);
        }

        public DateTime? LoadExpiresAt() {
            var expiresAtStr = PlayerPrefs.GetString(ExpiresAtKey, null);
            if (string.IsNullOrEmpty(expiresAtStr)) return null;

            if (DateTime.TryParse(expiresAtStr, out var result)) {
                return result;
            }
            return null;
        }
        
        public bool HasToken() {
            return !string.IsNullOrEmpty(LoadToken());
        }
        
        public bool IsTokenExpired() {
            var expiresAt = LoadExpiresAt();
            if (!expiresAt.HasValue) return true;
            
            // 만료 5분 전부터 만료된 것으로 처리 (버퍼)
            return DateTime.UtcNow >= expiresAt.Value.AddMinutes(-5);
        }

        /// <summary>
        /// 저장된 토큰 정보 삭제
        /// </summary>
        public void ClearToken() {
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.DeleteKey(ExpiresAtKey);
            PlayerPrefs.Save();
        }
    }
}
