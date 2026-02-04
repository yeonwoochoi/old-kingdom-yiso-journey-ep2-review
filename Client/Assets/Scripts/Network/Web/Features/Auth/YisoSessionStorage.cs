using UnityEngine;

namespace Network.Web.Features.Auth {
    /// <summary>
    /// PlayerPrefs 기반 세션 ID 저장소
    /// </summary>
    public class YisoSessionStorage {
        private const string SessionIdKey = "yiso_session_id";
        private const string UsernameKey = "yiso_auth_username";

        public void SaveSession(string sessionId, string username) {
            PlayerPrefs.SetString(SessionIdKey, sessionId);
            PlayerPrefs.SetString(UsernameKey, username);
            PlayerPrefs.Save();
        }

        public string LoadSessionId() {
            return PlayerPrefs.GetString(SessionIdKey, null);
        }

        public string LoadUsername() {
            return PlayerPrefs.GetString(UsernameKey, null);
        }

        public bool HasSession() {
            return !string.IsNullOrEmpty(LoadSessionId());
        }

        /// <summary>
        /// 저장된 세션 정보 삭제
        /// </summary>
        public void ClearSession() {
            PlayerPrefs.DeleteKey(SessionIdKey);
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.Save();
        }
    }
}
