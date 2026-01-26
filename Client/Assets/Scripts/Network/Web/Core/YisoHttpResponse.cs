namespace Network.Web.Core {
    /// <summary>
    /// HTTP 응답을 래핑하는 클래스
    /// </summary>
    /// <typeparam name="T">응답 데이터 타입</typeparam>
    public class YisoHttpResponse<T> {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string Error { get; }
        public long StatusCode { get; }

        private YisoHttpResponse(bool isSuccess, T data, string error, long statusCode) {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
            StatusCode = statusCode;
        }

        public static YisoHttpResponse<T> Success(T data, long statusCode = 200) {
            return new YisoHttpResponse<T>(true, data, null, statusCode);
        }

        public static YisoHttpResponse<T> Failure(string error, long statusCode = 0) {
            return new YisoHttpResponse<T>(false, default, error, statusCode);
        }
    }

    /// <summary>
    /// 데이터가 없는 HTTP 응답용
    /// </summary>
    public class YisoHttpResponse {
        public bool IsSuccess { get; }
        public string Error { get; }
        public long StatusCode { get; }

        private YisoHttpResponse(bool isSuccess, string error, long statusCode) {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }

        public static YisoHttpResponse Success(long statusCode = 200) {
            return new YisoHttpResponse(true, null, statusCode);
        }

        public static YisoHttpResponse Failure(string error, long statusCode = 0) {
            return new YisoHttpResponse(false, error, statusCode);
        }
    }
}
