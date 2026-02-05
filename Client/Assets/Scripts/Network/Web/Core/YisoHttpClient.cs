using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine.Networking;
using Yiso.Shared.DTOs.Common;

namespace Network.Web.Core {
    /// <summary>
    /// UnityWebRequest 기반 HTTP 클라이언트
    /// </summary>
    public class YisoHttpClient {
        private readonly string baseUrl;
        private string sessionId;

        // 서버와 동일한 camelCase 직렬화 설정
        private static readonly JsonSerializerSettings JsonSettings = new() {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// 서버 에러 응답에서 메시지 추출
        /// </summary>
        private static string ParseErrorMessage(string responseText) {
            if (string.IsNullOrEmpty(responseText)) return null;

            try {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseText, JsonSettings);
                return errorResponse?.Message ?? responseText;
            }
            catch {
                // JSON 파싱 실패 시 원본 반환
                return responseText;
            }
        }

        public YisoHttpClient(string baseUrl) {
            this.baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>
        /// X-Session-Id 헤더에 사용할 세션 ID 설정
        /// </summary>
        public void SetSessionId(string sessionId) {
            this.sessionId = sessionId;
        }

        /// <summary>
        /// 세션 ID 제거
        /// </summary>
        public void ClearSessionId() {
            sessionId = null;
        }

        /// <summary>
        /// GET 요청
        /// </summary>
        public async Task<YisoHttpResponse<T>> GetAsync<T>(string endpoint) {
            var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

            using var request = UnityWebRequest.Get(url);
            ConfigureRequest(request);

            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// POST 요청 (JSON 바디)
        /// </summary>
        public async Task<YisoHttpResponse<T>> PostAsync<T>(string endpoint, object body) {
            var url = $"{baseUrl}/{endpoint.TrimStart('/')}";
            var json = JsonConvert.SerializeObject(body, JsonSettings);

            // UnityWebRequest.Post()은 Key-Value 형식의 Form 데이터 보낼때 씀
            // 우리는 JSON으로 보낼거니까 UnityWebRequest.Post() 안 쓰고 완전 쌩 객체 만들어서 일일히 설정해주는거
            using var request = new UnityWebRequest(url, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            ConfigureRequest(request);
            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequestAsync<T>(request);
        }

        /// <summary>
        /// POST 요청 (응답 데이터 없음)
        /// </summary>
        public async Task<YisoHttpResponse> PostAsync(string endpoint, object body) {
            var url = $"{baseUrl}/{endpoint.TrimStart('/')}";
            var json = JsonConvert.SerializeObject(body, JsonSettings);

            using var request = new UnityWebRequest(url, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            ConfigureRequest(request);
            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequestAsync(request);
        }

        /// <summary>
        /// POST 요청 (바디 없음, 응답 데이터 없음)
        /// </summary>
        public async Task<YisoHttpResponse> PostAsync(string endpoint) {
            var url = $"{baseUrl}/{endpoint.TrimStart('/')}";

            using var request = new UnityWebRequest(url, "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            ConfigureRequest(request);

            return await SendRequestAsync(request);
        }

        private void ConfigureRequest(UnityWebRequest request) {
            if (!string.IsNullOrEmpty(sessionId)) {
                request.SetRequestHeader("X-Session-Id", sessionId);
            }
        }

        private async Task<YisoHttpResponse<T>> SendRequestAsync<T>(UnityWebRequest request) {
            try {
                var operation = request.SendWebRequest();

                while (!operation.isDone) {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success) {
                    var rawError = request.downloadHandler?.text;
                    var errorMessage = !string.IsNullOrEmpty(rawError)
                        ? ParseErrorMessage(rawError)
                        : request.error;
                    return YisoHttpResponse<T>.Failure(errorMessage, request.responseCode);
                }

                var responseText = request.downloadHandler.text;
                var data = JsonConvert.DeserializeObject<T>(responseText, JsonSettings);
                return YisoHttpResponse<T>.Success(data, request.responseCode);
            }
            catch (Exception ex) {
                return YisoHttpResponse<T>.Failure(ex.Message);
            }
        }

        private async Task<YisoHttpResponse> SendRequestAsync(UnityWebRequest request) {
            try {
                var operation = request.SendWebRequest();

                while (!operation.isDone) {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success) {
                    var rawError = request.downloadHandler?.text;
                    var errorMessage = !string.IsNullOrEmpty(rawError)
                        ? ParseErrorMessage(rawError)
                        : request.error;
                    return YisoHttpResponse.Failure(errorMessage, request.responseCode);
                }

                return YisoHttpResponse.Success(request.responseCode);
            }
            catch (Exception ex) {
                return YisoHttpResponse.Failure(ex.Message);
            }
        }
    }
}
