using System.IO;
using System.Text;
using Core.Log;
using UnityEditor;
using UnityEngine;

namespace Editor {
    public class ConvertCsvToStringTable : EditorWindow {
        private const string PrefKeyKrCsv = "CsvToStringTable_KR_CsvPath";
        private const string PrefKeyKrOut = "CsvToStringTable_KR_OutputPath";
        private const string PrefKeyEnCsv = "CsvToStringTable_EN_CsvPath";
        private const string PrefKeyEnOut = "CsvToStringTable_EN_OutputPath";

        private string _krCsvPath;
        private string _krOutputPath;
        private string _enCsvPath;
        private string _enOutputPath;

        [MenuItem("Tools/Convert Csv To String Table")]
        public static void ShowWindow() {
            GetWindow<ConvertCsvToStringTable>("Convert Csv To String Table");
        }

        private void OnEnable() {
            _krCsvPath = EditorPrefs.GetString(PrefKeyKrCsv, "");
            _krOutputPath = EditorPrefs.GetString(PrefKeyKrOut, "");
            _enCsvPath = EditorPrefs.GetString(PrefKeyEnCsv, "");
            _enOutputPath = EditorPrefs.GetString(PrefKeyEnOut, "");
        }

        private void OnGUI() {
            DrawLocaleSection("KR", ref _krCsvPath, ref _krOutputPath, PrefKeyKrCsv, PrefKeyKrOut);
            EditorGUILayout.Space(8);
            DrawLocaleSection("EN", ref _enCsvPath, ref _enOutputPath, PrefKeyEnCsv, PrefKeyEnOut);
            EditorGUILayout.Space(12);

            if (GUILayout.Button("Convert All")) {
                Convert(_krCsvPath, _krOutputPath);
                Convert(_enCsvPath, _enOutputPath);
            }
        }

        private void DrawLocaleSection(string locale, ref string csvPath, ref string outputPath, string csvPrefKey, string outPrefKey) {
            EditorGUILayout.LabelField(locale, EditorStyles.boldLabel);

            EditorGUI.indentLevel++; // 한칸 오른쪽으로 들여쓰기 (앞으로)
            
            EditorGUI.BeginChangeCheck();
            csvPath = EditorGUILayout.TextField("CSV Path", csvPath);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetString(csvPrefKey, csvPath);
                EditorPrefs.SetString(outPrefKey, outputPath);
            }

            EditorGUI.indentLevel--; // 들여쓰기 복구

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Convert {locale}", GUILayout.Width(120)))
                Convert(csvPath, outputPath);
            EditorGUILayout.EndHorizontal();
        }

        private static void Convert(string csvPath, string outputPath) {
            if (string.IsNullOrWhiteSpace(csvPath) || string.IsNullOrWhiteSpace(outputPath)) {
                Debug.LogError("CSV Path 또는 Output Path가 비어있습니다.");
                return;
            }

            if (!File.Exists(csvPath)) {
                Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
                return;
            }

            var lines = File.ReadAllLines(csvPath);
            var sb = new StringBuilder();

            foreach (var line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var idx = line.IndexOf(',');
                if (idx == -1) {
                    YisoLogSystem.Error($"Invalid CSV format (expected 2 cols): {line}");
                    continue;
                }

                sb.Append(line[..idx]);
                sb.Append('\t');
                sb.AppendLine(line[(idx + 1)..]);
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[StringTable] {Path.GetFileName(csvPath)} → {outputPath}");
        }
    }
}