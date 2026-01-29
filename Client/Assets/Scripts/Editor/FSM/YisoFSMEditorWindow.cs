using System.Collections.Generic;
using Editor.FSM.Utils;
using Editor.FSM.Views;
using Gameplay.Character.StateMachine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.FSM {
    /// <summary>
    /// FSM 시각적 에디터 메인 윈도우
    /// Unity Animator Window와 유사한 UX 제공
    /// </summary>
    public class YisoFSMEditorWindow : EditorWindow {
        private YisoFSMGraphView _graphView;
        private YisoFSMInspectorPanel _inspectorPanel;
        private ObjectField _prefabField;
        private Label _statusLabel;

        private GameObject _currentPrefabAsset;
        private GameObject _prefabContents;
        private YisoCharacterStateMachine _currentFSM;
        private string _currentAssetPath;
        private string _originalGUID;
        private bool _isDirty;

        private const string WINDOW_TITLE = "FSM Editor";
        private const string USS_PATH = "Assets/Scripts/Editor/FSM/Styles/YisoFSMEditorStyles.uss";

        [MenuItem("Yiso/FSM Editor")]
        public static void OpenWindow() {
            var window = GetWindow<YisoFSMEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("d_AnimatorController Icon").image);
            window.minSize = new Vector2(800, 600);
        }

        /// <summary>
        /// 특정 Prefab으로 에디터를 엽니다.
        /// </summary>
        public static void OpenWithPrefab(GameObject prefab) {
            var window = GetWindow<YisoFSMEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(800, 600);
            window.LoadPrefab(prefab);
        }

        private void CreateGUI() {
            // 스타일시트 로드
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (styleSheet != null) {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // 메인 레이아웃 구성
            CreateToolbar();
            CreateMainContent();

            // 초기 상태 설정
            UpdateWindowTitle();
        }

        private void CreateToolbar() {
            var toolbar = new Toolbar();
            toolbar.AddToClassList("yiso-fsm-toolbar");

            // Prefab 선택 필드
            _prefabField = new ObjectField("FSM Prefab") {
                objectType = typeof(GameObject),
                allowSceneObjects = false
            };
            _prefabField.style.minWidth = 250;
            _prefabField.RegisterValueChangedCallback(evt => {
                if (evt.newValue != evt.previousValue) {
                    LoadPrefab(evt.newValue as GameObject);
                }
            });
            toolbar.Add(_prefabField);

            // 저장 버튼
            var saveButton = new Button(SavePrefab) { text = "Save" };
            saveButton.style.marginLeft = 10;
            toolbar.Add(saveButton);

            // Revert 버튼
            var revertButton = new Button(RevertChanges) { text = "Revert" };
            toolbar.Add(revertButton);

            // 구분선
            toolbar.Add(new ToolbarSpacer());

            // Auto Layout 버튼
            var autoLayoutButton = new Button(ApplyAutoLayout) { text = "Auto Layout" };
            toolbar.Add(autoLayoutButton);

            // Zoom to Fit 버튼
            var zoomToFitButton = new Button(ZoomToFit) { text = "Zoom to Fit" };
            toolbar.Add(zoomToFitButton);

            // 확장 가능한 공간
            toolbar.Add(new ToolbarSpacer { flex = true });

            // 상태 레이블
            _statusLabel = new Label("");
            _statusLabel.style.color = Color.gray;
            toolbar.Add(_statusLabel);

            rootVisualElement.Add(toolbar);
        }

        private void CreateMainContent() {
            var mainContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1
                }
            };

            // GraphView 영역
            var graphContainer = new VisualElement {
                style = { flexGrow = 1 }
            };

            _graphView = new YisoFSMGraphView(this);
            _graphView.StretchToParentSize();
            graphContainer.Add(_graphView);

            // GraphView 이벤트 등록
            _graphView.OnNodeSelected += OnNodeSelected;
            _graphView.OnEdgeSelected += OnEdgeSelected;
            _graphView.OnGraphChanged += OnGraphChanged;

            mainContainer.Add(graphContainer);

            // Inspector 패널
            _inspectorPanel = new YisoFSMInspectorPanel(this);
            mainContainer.Add(_inspectorPanel);

            rootVisualElement.Add(mainContainer);
        }

        #region Prefab 로드/저장

        public void LoadPrefab(GameObject prefabAsset) {
            // 기존 변경사항이 있으면 저장 확인
            if (_isDirty && _prefabContents != null) {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    "현재 Prefab에 저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?",
                    "저장", "저장 안 함")) {
                    SavePrefab();
                }
            }

            // 기존 Prefab 언로드
            UnloadCurrentPrefab();

            if (prefabAsset == null) {
                _prefabField.SetValueWithoutNotify(null);
                ClearGraph();
                return;
            }

            // FSM Prefab인지 확인
            var fsm = prefabAsset.GetComponent<YisoCharacterStateMachine>();
            if (fsm == null) {
                EditorUtility.DisplayDialog("Invalid Prefab",
                    "선택한 Prefab에 YisoCharacterStateMachine 컴포넌트가 없습니다.",
                    "확인");
                _prefabField.SetValueWithoutNotify(null);
                return;
            }

            // Prefab 로드
            var (contents, loadedFSM, assetPath) = YisoFSMPrefabHelper.LoadPrefab(prefabAsset);
            if (contents == null) {
                EditorUtility.DisplayDialog("Load Failed",
                    "Prefab을 로드하는 데 실패했습니다.",
                    "확인");
                _prefabField.SetValueWithoutNotify(null);
                return;
            }

            _currentPrefabAsset = prefabAsset;
            _prefabContents = contents;
            _currentFSM = loadedFSM;
            _currentAssetPath = assetPath;
            _originalGUID = YisoFSMPrefabHelper.GetPrefabGUID(prefabAsset);
            _isDirty = false;

            // UI 업데이트
            _prefabField.SetValueWithoutNotify(prefabAsset);
            PopulateGraph();
            UpdateWindowTitle();
            UpdateStatusLabel($"로드됨: {prefabAsset.name}");
        }

        private void UnloadCurrentPrefab() {
            if (_prefabContents != null) {
                YisoFSMPrefabHelper.UnloadPrefab(_prefabContents);
                _prefabContents = null;
            }
            _currentFSM = null;
            _currentAssetPath = null;
            _originalGUID = null;
            _currentPrefabAsset = null;
        }

        private void SavePrefab() {
            if (_prefabContents == null || string.IsNullOrEmpty(_currentAssetPath)) {
                EditorUtility.DisplayDialog("Save Failed",
                    "저장할 Prefab이 로드되어 있지 않습니다.",
                    "확인");
                return;
            }

            // 노드 위치 저장
            SaveNodePositions();

            // Prefab 저장
            if (YisoFSMPrefabHelper.SavePrefab(_prefabContents, _currentAssetPath)) {
                _isDirty = false;
                UpdateWindowTitle();
                UpdateStatusLabel("저장 완료");

                // GUID 무결성 확인
                if (!YisoFSMPrefabHelper.VerifyGUIDIntegrity(_originalGUID, _currentPrefabAsset)) {
                    Debug.LogWarning("[FSM Editor] GUID가 변경되었습니다. Scene 참조를 확인하세요.");
                }
            }
            else {
                UpdateStatusLabel("저장 실패!");
            }
        }

        private void RevertChanges() {
            if (_currentPrefabAsset == null) return;

            if (_isDirty) {
                if (!EditorUtility.DisplayDialog("Revert Changes",
                    "모든 변경사항을 취소하시겠습니까?",
                    "취소", "유지")) {
                    return;
                }
            }

            LoadPrefab(_currentPrefabAsset);
        }

        #endregion

        #region Graph 조작

        private void PopulateGraph() {
            if (_graphView == null || _currentFSM == null) return;

            _graphView.ClearGraph();
            _graphView.PopulateFromFSM(_currentFSM);
        }

        private void ClearGraph() {
            _graphView?.ClearGraph();
            _inspectorPanel?.ClearSelection();
        }

        private void SaveNodePositions() {
            if (_graphView == null || _currentFSM == null) return;

            var positions = _graphView.GetNodePositions();
            var statesField = typeof(YisoCharacterStateMachine).GetField("states",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var states = statesField?.GetValue(_currentFSM) as List<YisoCharacterState>;
            if (states == null) return;

            foreach (var state in states) {
                if (positions.TryGetValue(state.StateName, out var pos)) {
                    state.editorNodePosition = pos;
                }
            }
        }

        private void ApplyAutoLayout() {
            if (_graphView == null || _currentFSM == null) return;

            var statesField = typeof(YisoCharacterStateMachine).GetField("states",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var initialStateField = typeof(YisoCharacterStateMachine).GetField("initialState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var states = statesField?.GetValue(_currentFSM) as List<YisoCharacterState>;
            var initialState = initialStateField?.GetValue(_currentFSM) as string;

            if (states == null) return;

            var positions = YisoFSMLayoutHelper.CalculateLayout(states, initialState);
            _graphView.ApplyLayout(positions);
            MarkDirty();
        }

        private void ZoomToFit() {
            _graphView?.FrameAll();
        }

        #endregion

        #region 이벤트 핸들러

        private void OnNodeSelected(YisoStateNodeView nodeView) {
            _inspectorPanel?.ShowStateInspector(nodeView);
        }

        private void OnEdgeSelected(YisoTransitionEdgeView edgeView) {
            _inspectorPanel?.ShowTransitionInspector(edgeView);
        }

        private void OnGraphChanged() {
            MarkDirty();
        }

        public void MarkDirty() {
            _isDirty = true;
            UpdateWindowTitle();
        }

        #endregion

        #region UI 업데이트

        private void UpdateWindowTitle() {
            string title = WINDOW_TITLE;
            if (_currentPrefabAsset != null) {
                title += $" - {_currentPrefabAsset.name}";
            }
            if (_isDirty) {
                title += "*";
            }
            titleContent.text = title;
        }

        private void UpdateStatusLabel(string message) {
            if (_statusLabel != null) {
                _statusLabel.text = message;
            }
        }

        #endregion

        #region 외부 접근자

        public YisoCharacterStateMachine CurrentFSM => _currentFSM;
        public GameObject PrefabContents => _prefabContents;
        public bool IsDirty => _isDirty;

        /// <summary>
        /// 현재 로드된 FSM의 모든 상태를 반환합니다.
        /// </summary>
        public List<YisoCharacterState> GetStates() {
            if (_currentFSM == null) return new List<YisoCharacterState>();

            var statesField = typeof(YisoCharacterStateMachine).GetField("states",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return statesField?.GetValue(_currentFSM) as List<YisoCharacterState> ?? new List<YisoCharacterState>();
        }

        /// <summary>
        /// 초기 상태 이름을 반환합니다.
        /// </summary>
        public string GetInitialStateName() {
            if (_currentFSM == null) return null;

            var field = typeof(YisoCharacterStateMachine).GetField("initialState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return field?.GetValue(_currentFSM) as string;
        }

        /// <summary>
        /// 초기 상태를 설정합니다.
        /// </summary>
        public void SetInitialState(string stateName) {
            if (_currentFSM == null) return;

            var field = typeof(YisoCharacterStateMachine).GetField("initialState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null) {
                Undo.RecordObject(_currentFSM, "Set Initial State");
                field.SetValue(_currentFSM, stateName);
                _graphView?.RefreshInitialStateVisuals();
                MarkDirty();
            }
        }

        /// <summary>
        /// 새 상태를 추가합니다.
        /// </summary>
        public YisoCharacterState AddState(string stateName, Vector2 position) {
            if (_currentFSM == null) return null;

            var statesField = typeof(YisoCharacterStateMachine).GetField("states",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var states = statesField?.GetValue(_currentFSM) as List<YisoCharacterState>;
            if (states == null) return null;

            // 중복 이름 확인
            if (states.Exists(s => s.StateName == stateName)) {
                Debug.LogWarning($"[FSM Editor] 이미 존재하는 상태 이름: {stateName}");
                return null;
            }

            var newState = new YisoCharacterState();
            var stateNameField = typeof(YisoCharacterState).GetField("stateName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stateNameField?.SetValue(newState, stateName);
            newState.editorNodePosition = position;

            Undo.RecordObject(_currentFSM, "Add State");
            states.Add(newState);
            MarkDirty();

            return newState;
        }

        /// <summary>
        /// 상태를 삭제합니다.
        /// </summary>
        public void DeleteState(YisoCharacterState state) {
            if (_currentFSM == null || state == null) return;

            var statesField = typeof(YisoCharacterStateMachine).GetField("states",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var states = statesField?.GetValue(_currentFSM) as List<YisoCharacterState>;
            if (states == null) return;

            Undo.RecordObject(_currentFSM, "Delete State");
            states.Remove(state);
            MarkDirty();
        }

        /// <summary>
        /// 그래프를 새로고침합니다.
        /// </summary>
        public void RefreshGraph() {
            PopulateGraph();
        }

        #endregion

        private void OnDestroy() {
            // 저장하지 않은 변경사항 경고
            if (_isDirty && _prefabContents != null) {
                // OnDestroy에서는 Dialog를 띄울 수 없으므로 로그만 남김
                Debug.LogWarning("[FSM Editor] 저장하지 않은 변경사항이 있습니다.");
            }

            UnloadCurrentPrefab();
        }
    }
}
