using System;
using System.Collections.Generic;
using System.Linq;
using Editor.FSM.Views;
using Gameplay.Character.StateMachine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.FSM {
    /// <summary>
    /// FSM 시각화를 위한 GraphView
    /// State를 노드로, Transition을 Edge로 표현
    /// </summary>
    public class YisoFSMGraphView : GraphView {
        private readonly YisoFSMEditorWindow _window;
        private readonly Dictionary<string, YisoStateNodeView> _nodeViews = new();
        private readonly List<YisoTransitionEdgeView> _edgeViews = new();

        // Transition 생성 모드
        private bool _isCreatingTransition;
        private YisoStateNodeView _transitionSourceNode;
        private VisualElement _transitionPreviewLine;

        // 이벤트
        public event Action<YisoStateNodeView> OnNodeSelected;
        public event Action<YisoTransitionEdgeView> OnEdgeSelected;
        public event Action OnGraphChanged;

        public YisoFSMGraphView(YisoFSMEditorWindow window) {
            _window = window;

            // 기본 스타일 클래스 추가
            AddToClassList("yiso-fsm-graph-view");

            // GraphView 기본 기능 설정
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 그리드 배경
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // 컨텍스트 메뉴 설정
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenuPopulate);

            // 선택 변경 이벤트
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            // Transition 생성 모드용 이벤트
            RegisterCallback<MouseMoveEvent>(OnMouseMoveForTransition);
            RegisterCallback<MouseUpEvent>(OnMouseUpForTransition);

            // Edge 연결 리스너
            graphViewChanged += OnGraphViewChanged;

            // Transition 미리보기 라인 생성
            CreateTransitionPreviewLine();
        }

        private void CreateTransitionPreviewLine() {
            _transitionPreviewLine = new VisualElement();
            _transitionPreviewLine.style.position = Position.Absolute;
            _transitionPreviewLine.style.backgroundColor = new Color(0.5f, 0.8f, 0.5f, 0.8f);
            _transitionPreviewLine.style.height = 2;
            _transitionPreviewLine.style.display = DisplayStyle.None;
            _transitionPreviewLine.pickingMode = PickingMode.Ignore;
            Add(_transitionPreviewLine);
        }

        #region 초기화 및 설정

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port => {
                // 자기 자신으로의 연결 방지
                if (port == startPort) return;
                // 같은 노드 내 연결 방지
                if (port.node == startPort.node) return;
                // 방향이 반대여야 함
                if (port.direction == startPort.direction) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        #endregion

        #region Graph 조작

        public void ClearGraph() {
            // 모든 노드와 엣지 제거
            foreach (var node in _nodeViews.Values.ToList()) {
                RemoveElement(node);
            }
            foreach (var edge in _edgeViews.ToList()) {
                RemoveElement(edge);
            }

            _nodeViews.Clear();
            _edgeViews.Clear();
        }

        public void PopulateFromFSM(YisoCharacterStateMachine fsm) {
            if (fsm == null) return;

            var states = _window.GetStates();
            var initialState = _window.GetInitialStateName();

            // 노드 생성
            foreach (var state in states) {
                CreateNodeView(state, state.StateName == initialState);
            }

            // Edge 생성
            foreach (var state in states) {
                CreateEdgesForState(state);
            }

            // 저장된 위치 적용 또는 자동 레이아웃
            ApplyStoredPositionsOrAutoLayout(states, initialState);
        }

        private void CreateNodeView(YisoCharacterState state, bool isInitial) {
            var nodeView = new YisoStateNodeView(state, isInitial, _window);
            nodeView.SetPosition(new Rect(state.editorNodePosition, Vector2.zero));

            // 선택 이벤트 등록
            nodeView.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button == 0) {
                    OnNodeSelected?.Invoke(nodeView);
                }
            });

            AddElement(nodeView);
            _nodeViews[state.StateName] = nodeView;
        }

        private void CreateEdgesForState(YisoCharacterState state) {
            if (!_nodeViews.TryGetValue(state.StateName, out var sourceNode)) return;

            var transitionsField = typeof(YisoCharacterState).GetField("transitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var transitions = transitionsField?.GetValue(state) as List<YisoCharacterTransition>;
            if (transitions == null) return;

            for (int i = 0; i < transitions.Count; i++) {
                var transition = transitions[i];
                CreateEdgesForTransition(sourceNode, transition, i);
            }
        }

        private void CreateEdgesForTransition(YisoStateNodeView sourceNode, YisoCharacterTransition transition, int transitionIndex) {
            var randomField = typeof(YisoCharacterTransition).GetField("random",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isRandom = (bool)(randomField?.GetValue(transition) ?? false);

            if (isRandom) {
                var nextStatesField = typeof(YisoCharacterTransition).GetField("nextStates",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var nextStates = nextStatesField?.GetValue(transition) as List<string>;

                if (nextStates != null) {
                    foreach (var nextStateName in nextStates) {
                        if (_nodeViews.TryGetValue(nextStateName, out var targetNode)) {
                            CreateEdge(sourceNode, targetNode, transition, transitionIndex, true);
                        }
                    }
                }
            }
            else {
                var nextStateField = typeof(YisoCharacterTransition).GetField("nextState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var nextState = nextStateField?.GetValue(transition) as string;

                if (!string.IsNullOrEmpty(nextState) && _nodeViews.TryGetValue(nextState, out var targetNode)) {
                    CreateEdge(sourceNode, targetNode, transition, transitionIndex, false);
                }
            }
        }

        private void CreateEdge(YisoStateNodeView sourceNode, YisoStateNodeView targetNode,
            YisoCharacterTransition transition, int transitionIndex, bool isRandom) {
            var edge = new YisoTransitionEdgeView(sourceNode, targetNode, transition, transitionIndex, isRandom);

            edge.output = sourceNode.OutputPort;
            edge.input = targetNode.InputPort;
            edge.output.Connect(edge);
            edge.input.Connect(edge);

            // 선택 이벤트 등록
            edge.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button == 0) {
                    OnEdgeSelected?.Invoke(edge);
                }
            });

            AddElement(edge);
            _edgeViews.Add(edge);
        }

        private void ApplyStoredPositionsOrAutoLayout(List<YisoCharacterState> states, string initialState) {
            bool hasStoredPositions = states.Any(s => s.editorNodePosition != Vector2.zero);

            if (hasStoredPositions) {
                // 저장된 위치 적용
                foreach (var state in states) {
                    if (_nodeViews.TryGetValue(state.StateName, out var nodeView)) {
                        nodeView.SetPosition(new Rect(state.editorNodePosition, Vector2.zero));
                    }
                }
            }
            else {
                // 자동 레이아웃 적용
                var positions = Utils.YisoFSMLayoutHelper.CalculateLayout(states, initialState);
                ApplyLayout(positions);
            }
        }

        public void ApplyLayout(Dictionary<string, Vector2> positions) {
            foreach (var kvp in positions) {
                if (_nodeViews.TryGetValue(kvp.Key, out var nodeView)) {
                    nodeView.SetPosition(new Rect(kvp.Value, Vector2.zero));
                }
            }
        }

        public Dictionary<string, Vector2> GetNodePositions() {
            var positions = new Dictionary<string, Vector2>();
            foreach (var kvp in _nodeViews) {
                var rect = kvp.Value.GetPosition();
                positions[kvp.Key] = new Vector2(rect.x, rect.y);
            }
            return positions;
        }

        public void RefreshInitialStateVisuals() {
            var initialState = _window.GetInitialStateName();
            foreach (var kvp in _nodeViews) {
                kvp.Value.SetInitialState(kvp.Key == initialState);
            }
        }

        /// <summary>
        /// 특정 노드와 연결된 모든 Edge를 새로고침합니다.
        /// </summary>
        private void RefreshEdgesForNode(YisoStateNodeView node) {
            foreach (var edge in _edgeViews) {
                if (edge.SourceNode == node || edge.TargetNode == node) {
                    edge.RefreshDisplay();
                }
            }
        }

        /// <summary>
        /// 모든 Edge를 새로고침합니다.
        /// </summary>
        public void RefreshAllEdges() {
            foreach (var edge in _edgeViews) {
                edge.RefreshDisplay();
            }
        }

        #endregion

        #region 이벤트 핸들러

        private GraphViewChange OnGraphViewChanged(GraphViewChange change) {
            // 노드 위치 변경 시 Edge 업데이트
            if (change.movedElements != null) {
                foreach (var element in change.movedElements) {
                    if (element is YisoStateNodeView movedNode) {
                        // 이 노드와 연결된 모든 Edge 업데이트
                        RefreshEdgesForNode(movedNode);
                    }
                }
                OnGraphChanged?.Invoke();
            }

            // Edge 생성
            if (change.edgesToCreate != null) {
                foreach (var edge in change.edgesToCreate) {
                    HandleNewEdge(edge);
                }
                OnGraphChanged?.Invoke();
            }

            // 요소 삭제
            if (change.elementsToRemove != null) {
                foreach (var element in change.elementsToRemove) {
                    if (element is YisoStateNodeView nodeView) {
                        HandleNodeRemoval(nodeView);
                    }
                    else if (element is YisoTransitionEdgeView edgeView) {
                        HandleEdgeRemoval(edgeView);
                    }
                }
                OnGraphChanged?.Invoke();
            }

            return change;
        }

        private void HandleNewEdge(Edge edge) {
            if (edge.output?.node is YisoStateNodeView sourceNode &&
                edge.input?.node is YisoStateNodeView targetNode) {
                // 새 Transition 생성
                var sourceState = sourceNode.State;
                AddTransitionToState(sourceState, targetNode.State.StateName);
            }
        }

        private void AddTransitionToState(YisoCharacterState sourceState, string targetStateName) {
            var transitionsField = typeof(YisoCharacterState).GetField("transitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var transitions = transitionsField?.GetValue(sourceState) as List<YisoCharacterTransition>;
            if (transitions == null) {
                transitions = new List<YisoCharacterTransition>();
                transitionsField?.SetValue(sourceState, transitions);
            }

            // 새 Transition 생성
            var newTransition = new YisoCharacterTransition();
            var nextStateField = typeof(YisoCharacterTransition).GetField("nextState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nextStateField?.SetValue(newTransition, targetStateName);

            transitions.Add(newTransition);
        }

        private void HandleNodeRemoval(YisoStateNodeView nodeView) {
            var state = nodeView.State;
            _nodeViews.Remove(state.StateName);

            // FSM에서 상태 제거
            _window.DeleteState(state);
        }

        private void HandleEdgeRemoval(YisoTransitionEdgeView edgeView) {
            _edgeViews.Remove(edgeView);

            // Transition 제거
            var sourceState = edgeView.SourceNode.State;
            RemoveTransitionFromState(sourceState, edgeView.TransitionIndex);
        }

        private void RemoveTransitionFromState(YisoCharacterState state, int transitionIndex) {
            var transitionsField = typeof(YisoCharacterState).GetField("transitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var transitions = transitionsField?.GetValue(state) as List<YisoCharacterTransition>;
            if (transitions != null && transitionIndex >= 0 && transitionIndex < transitions.Count) {
                transitions.RemoveAt(transitionIndex);
            }
        }

        private void OnContextMenuPopulate(ContextualMenuPopulateEvent evt) {
            var mousePos = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

            // Transition 생성 모드 취소
            if (_isCreatingTransition) {
                evt.menu.AppendAction("Cancel Transition", action => {
                    CancelTransitionCreation();
                });
                return;
            }

            evt.menu.AppendAction("Add State", action => {
                ShowAddStateDialog(mousePos);
            });

            evt.menu.AppendSeparator();

            // 선택된 노드가 있을 때만 표시
            var selectedNodes = selection.OfType<YisoStateNodeView>().ToList();
            if (selectedNodes.Count == 1) {
                var selectedNode = selectedNodes[0];
                var initialState = _window.GetInitialStateName();

                // Make Transition (Animator 스타일)
                evt.menu.AppendAction("Make Transition", action => {
                    StartTransitionCreation(selectedNode);
                });

                evt.menu.AppendSeparator();

                if (selectedNode.State.StateName != initialState) {
                    evt.menu.AppendAction("Set as Initial State", action => {
                        _window.SetInitialState(selectedNode.State.StateName);
                    });
                }

                evt.menu.AppendAction("Delete State", action => {
                    DeleteElements(new List<GraphElement> { selectedNode });
                });
            }
        }

        #region Transition 생성 모드 (Animator 스타일)

        private void StartTransitionCreation(YisoStateNodeView sourceNode) {
            _isCreatingTransition = true;
            _transitionSourceNode = sourceNode;
            _transitionPreviewLine.style.display = DisplayStyle.Flex;

            // 커서 변경을 위한 시각적 피드백
            AddToClassList("creating-transition");
        }

        private void CancelTransitionCreation() {
            _isCreatingTransition = false;
            _transitionSourceNode = null;
            _transitionPreviewLine.style.display = DisplayStyle.None;
            RemoveFromClassList("creating-transition");
        }

        private void OnMouseMoveForTransition(MouseMoveEvent evt) {
            if (!_isCreatingTransition || _transitionSourceNode == null) return;

            // 미리보기 라인 업데이트
            var sourceCenter = _transitionSourceNode.GetCenter();
            var mousePos = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

            UpdatePreviewLine(sourceCenter, mousePos);
        }

        private void OnMouseUpForTransition(MouseUpEvent evt) {
            if (!_isCreatingTransition || _transitionSourceNode == null) return;

            if (evt.button == 0) { // 좌클릭
                // 클릭한 위치의 노드 찾기
                var mousePos = evt.localMousePosition;
                var targetNode = GetNodeAtPosition(mousePos);

                if (targetNode != null && targetNode != _transitionSourceNode) {
                    // Transition 생성
                    CreateTransitionBetweenNodes(_transitionSourceNode, targetNode);
                }

                CancelTransitionCreation();
            }
            else if (evt.button == 1) { // 우클릭
                CancelTransitionCreation();
            }
        }

        private void UpdatePreviewLine(Vector2 start, Vector2 end) {
            var direction = end - start;
            var length = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            _transitionPreviewLine.style.left = start.x;
            _transitionPreviewLine.style.top = start.y;
            _transitionPreviewLine.style.width = length;
            _transitionPreviewLine.style.rotate = new Rotate(angle);
            _transitionPreviewLine.style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
        }

        private YisoStateNodeView GetNodeAtPosition(Vector2 localMousePos) {
            foreach (var kvp in _nodeViews) {
                var node = kvp.Value;
                var nodeRect = node.GetPosition();

                // 마우스 위치를 GraphView 좌표로 변환
                var graphPos = viewTransform.matrix.inverse.MultiplyPoint(localMousePos);

                if (nodeRect.Contains(graphPos)) {
                    return node;
                }
            }
            return null;
        }

        private void CreateTransitionBetweenNodes(YisoStateNodeView sourceNode, YisoStateNodeView targetNode) {
            // FSM에 Transition 추가
            AddTransitionToState(sourceNode.State, targetNode.State.StateName);

            // Edge 생성 (UI)
            var transitionsField = typeof(YisoCharacterState).GetField("transitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var transitions = transitionsField?.GetValue(sourceNode.State) as List<YisoCharacterTransition>;

            if (transitions != null && transitions.Count > 0) {
                var newTransition = transitions[transitions.Count - 1];
                CreateEdge(sourceNode, targetNode, newTransition, transitions.Count - 1, false);
            }

            OnGraphChanged?.Invoke();
        }

        #endregion

        private void OnKeyDown(KeyDownEvent evt) {
            // ESC로 Transition 생성 모드 취소
            if (evt.keyCode == KeyCode.Escape && _isCreatingTransition) {
                CancelTransitionCreation();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) {
                var selectedElements = selection.ToList();
                if (selectedElements.Count > 0) {
                    DeleteElements(selectedElements.OfType<GraphElement>());
                }
            }
        }

        private void ShowAddStateDialog(Vector2 position) {
            // 간단한 입력 다이얼로그 (EditorInputDialog 대체)
            var stateName = $"NewState_{_nodeViews.Count}";
            stateName = EditorInputDialog.Show("Add State", "상태 이름을 입력하세요:", stateName);

            if (!string.IsNullOrEmpty(stateName)) {
                var newState = _window.AddState(stateName, position);
                if (newState != null) {
                    CreateNodeView(newState, false);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 간단한 입력 다이얼로그
    /// </summary>
    public class EditorInputDialog : EditorWindow {
        private string _inputText;
        private string _result;
        private bool _confirmed;

        public static string Show(string title, string message, string defaultValue) {
            var window = CreateInstance<EditorInputDialog>();
            window._inputText = defaultValue;
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);
            window.ShowModalUtility();

            return window._confirmed ? window._result : null;
        }

        private void OnGUI() {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("상태 이름을 입력하세요:");
            _inputText = EditorGUILayout.TextField(_inputText);

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("확인", GUILayout.Width(80))) {
                _result = _inputText;
                _confirmed = true;
                Close();
            }

            if (GUILayout.Button("취소", GUILayout.Width(80))) {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
