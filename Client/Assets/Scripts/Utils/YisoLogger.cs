using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices; // 이것이 필요합니다.
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Utils
{
    public static class YisoLogger
    {
        private const string DEFINE_SYMBOL = "ENABLE_LOG";
        private const string COLOR_INFO = "white"; // 기본 색상

        // 로그 포맷: [스크립트이름::메서드이름] 메시지
        // 예: [PlayerController::Move] 이동 시작

        [Conditional(DEFINE_SYMBOL)]
        public static void Log(
            object message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
            Debug.Log($"<color={COLOR_INFO}>[{scriptName}::{memberName}]</color> {message}");
        }

        [Conditional(DEFINE_SYMBOL)]
        public static void Log(
            object message,
            Object context,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
            // context가 있으면 유니티 콘솔에서 클릭 시 해당 오브젝트 하이라이팅됨
            Debug.Log($"<color={COLOR_INFO}>[{scriptName}::{memberName}]</color> {message}", context);
        }

        [Conditional(DEFINE_SYMBOL)]
        public static void LogWarning(
            object message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
            Debug.LogWarning($"[{scriptName}::{memberName}] {message}");
        }

        [Conditional(DEFINE_SYMBOL)]
        public static void LogWarning(
            object message,
            Object context, // context 추가
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
            // Debug.LogWarning에도 context 전달
            Debug.LogWarning($"[{scriptName}::{memberName}] {message}", context);
        }

        [Conditional(DEFINE_SYMBOL)]
        public static void LogError(
            object message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
            Debug.LogError($"[{scriptName}::{memberName}] {message}");
        }

        [Conditional(DEFINE_SYMBOL)]
        public static void LogError(
            object message,
            Object context, // context 추가
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
            // Debug.LogError에도 context 전달
            Debug.LogError($"[{scriptName}::{memberName}] {message}", context);
        }

        private static readonly Dictionary<string, string> _fileNameCache = new Dictionary<string, string>();

        // 파일 경로에서 스크립트 이름만 추출하는 헬퍼
        private static string GetScriptName(string filePath)
        {
            // 캐싱된거 있는지 확인
            if (_fileNameCache.TryGetValue(filePath, out string scriptName))
            {
                return scriptName;
            }

            // 없으면 계산 (문자열 연산 발생)
            scriptName = Path.GetFileNameWithoutExtension(filePath);

            // 계산 결과 캐싱
            _fileNameCache[filePath] = scriptName;

            return scriptName;
        }
    }
}