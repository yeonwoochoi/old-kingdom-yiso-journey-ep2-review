using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices; // �̰��� �ʿ��մϴ�.
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Utils
{
    public static class YisoLogger
    {
        private const string DEFINE_SYMBOL = "ENABLE_LOG";
        private const string COLOR_INFO = "white";
        
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
            Object context,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
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
            Object context,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "")
        {
            string scriptName = GetScriptName(filePath);
            Debug.LogError($"[{scriptName}::{memberName}] {message}", context);
        }

        private static readonly Dictionary<string, string> _fileNameCache = new Dictionary<string, string>();

        private static string GetScriptName(string filePath)
        {
            if (_fileNameCache.TryGetValue(filePath, out string scriptName))
            {
                return scriptName;
            }
            scriptName = Path.GetFileNameWithoutExtension(filePath);
            _fileNameCache[filePath] = scriptName;
            return scriptName;
        }
    }
}