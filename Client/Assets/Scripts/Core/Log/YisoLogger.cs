using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Core.Log {
    /// <summary>
    /// [역할] 디버깅 및 유저 행동 데이터 로깅
    /// [책임]
    ///   - 콘솔 로그 출력 (Debug / Warning / Error 레벨 구분)
    ///   - 치명적 에러 트래킹
    ///   - 챕터 포기율 등 애널리틱스 이벤트 기록
    /// [타입] static
    /// </summary>
    public enum LogLevel {
        TRACE,
        DEBUG,
        INFO,
        WARN,
        ERROR,
        FATAL,
    }

    public class LogMessage {
        // {시간} [{레벨}] {호출자} : {메시지} ({메타데이터})
        private const string LogFormat = "{0:HH:mm:ss.fff} [{1}] {2} : {3}{4}";

        public LogLevel Level { get; set; }
        public string Caller { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string Metadata { get; set; }

        public override string ToString() {
            var meta = string.IsNullOrEmpty(Metadata) ? "" : $" ({Metadata})";
            return string.Format(LogFormat, Timestamp, Level, Caller, Message, meta);
        }
    }

    public static class YisoLogger {
        private static bool _enableDebugLog = true;

        private static readonly Dictionary<LogLevel, string> Colors = new() {
            { LogLevel.TRACE, "#888888" }, // 회색
            { LogLevel.DEBUG, "#AAAAAA" }, // 연회색
            { LogLevel.INFO,  "#55EEFF" }, // 하늘색
            { LogLevel.WARN,  "#FFD700" }, // 노랑
            { LogLevel.ERROR, "#FF4444" }, // 빨강
            { LogLevel.FATAL, "#FF00FF" }, // 마젠타
        };

        public static void Configure(bool enableDebugLog) {
            _enableDebugLog = enableDebugLog;
        }

        public static void Trace(string message, Dictionary<string, object> metaData = null) {
            if (!_enableDebugLog) return;
            UnityEngine.Debug.Log(Colorize(Build(LogLevel.TRACE, message, metaData), LogLevel.TRACE));
        }

        public static void Debug(string message, Dictionary<string, object> metaData = null) {
            if (!_enableDebugLog) return;
            UnityEngine.Debug.Log(Colorize(Build(LogLevel.DEBUG, message, metaData), LogLevel.DEBUG));
        }

        public static void Info(string message, Dictionary<string, object> metaData = null) =>
            UnityEngine.Debug.Log(Colorize(Build(LogLevel.INFO, message, metaData), LogLevel.INFO));

        public static void Warn(string message, Dictionary<string, object> metaData = null) =>
            UnityEngine.Debug.LogWarning(Colorize(Build(LogLevel.WARN, message, metaData), LogLevel.WARN));

        public static void Error(string message, Dictionary<string, object> metaData = null) =>
            UnityEngine.Debug.LogError(Colorize(Build(LogLevel.ERROR, message, metaData), LogLevel.ERROR));

        public static void Fatal(string message, Dictionary<string, object> metaData = null) =>
            UnityEngine.Debug.LogError(Colorize(Build(LogLevel.FATAL, message, metaData), LogLevel.FATAL));

        private static LogMessage Build(LogLevel level, string message, Dictionary<string, object> metadata) {
            var metaStr = metadata != null && metadata.Count > 0
                ? string.Join(", ", metadata.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "";

            return new LogMessage {
                Level = level,
                Caller = GetCaller(),
                Timestamp = DateTime.Now,
                Message = message,
                Metadata = metaStr,
            };
        }

        private static string Colorize(LogMessage msg, LogLevel level) =>
            $"<color={Colors[level]}>{msg}</color>";

        private static string GetCaller() {
            var stack = new StackTrace(skipFrames: 2, fNeedFileInfo: false);
            for (var i = 0; i < stack.FrameCount; i++) {
                var method = stack.GetFrame(i)?.GetMethod();
                if (method?.DeclaringType == typeof(YisoLogger)) continue;
                var typeName = method?.DeclaringType?.Name ?? "Unknown";
                return $"{typeName}.{method?.Name}";
            }
            return "Unknown";
        }
    }
}
