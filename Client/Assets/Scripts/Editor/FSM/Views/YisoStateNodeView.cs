using System.Collections.Generic;
using Gameplay.Character.StateMachine;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.FSM.Views {
    /// <summary>
    /// FSM 상태를 시각화하는 GraphView 노드
    /// Animator 스타일: Port 숨김, 노드 경계에서 Edge 연결
    /// </summary>
    public class YisoStateNodeView : Node {
        public YisoCharacterState State { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private readonly YisoFSMEditorWindow _window;
        private VisualElement _initialStateMarker;
        private Label _actionCountLabel;
        private Label _transitionCountLabel;

        public YisoStateNodeView(YisoCharacterState state, bool isInitial, YisoFSMEditorWindow window) {
            State = state;
            _window = window;

            // 기본 스타일
            AddToClassList("yiso-state-node");

            // 제목 설정
            title = state.StateName;

            // 초기 상태 표시
            if (isInitial) {
                SetInitialState(true);
            }

            // Port 생성 (숨김 처리)
            CreatePorts();

            // 노드 내용 생성
            CreateContents();

            // 크기 새로고침
            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreatePorts() {
            // 입력 Port - 숨김 처리 (내부 연결용으로만 사용)
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "";
            InputPort.style.display = DisplayStyle.None;
            inputContainer.Add(InputPort);

            // 출력 Port - 숨김 처리
            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output,
                Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "";
            OutputPort.style.display = DisplayStyle.None;
            outputContainer.Add(OutputPort);

            // Port 컨테이너 숨기기
            inputContainer.style.display = DisplayStyle.None;
            outputContainer.style.display = DisplayStyle.None;
        }

        // 기본 노드 크기 (레이아웃 전 사용)
        private const float DEFAULT_WIDTH = 150f;
        private const float DEFAULT_HEIGHT = 60f;

        /// <summary>
        /// 노드의 실제 크기를 반환합니다. 레이아웃 전이면 기본값 사용.
        /// </summary>
        private Vector2 GetActualSize() {
            var rect = GetPosition();
            float width = rect.width > 10 ? rect.width : DEFAULT_WIDTH;
            float height = rect.height > 10 ? rect.height : DEFAULT_HEIGHT;
            return new Vector2(width, height);
        }

        /// <summary>
        /// 노드의 중심 좌표를 반환합니다.
        /// </summary>
        public Vector2 GetCenter() {
            var rect = GetPosition();
            var size = GetActualSize();
            return new Vector2(rect.x + size.x / 2, rect.y + size.y / 2);
        }

        /// <summary>
        /// 노드 경계와 주어진 방향 벡터가 만나는 점을 계산합니다.
        /// 상대 위치에 따라 동적으로 연결점이 변경됩니다.
        /// </summary>
        public Vector2 GetEdgeConnectionPoint(Vector2 direction) {
            var rect = GetPosition();
            var size = GetActualSize();
            var center = new Vector2(rect.x + size.x / 2, rect.y + size.y / 2);

            // 노드 크기의 절반 + 약간의 여백
            float halfWidth = size.x / 2 + 2f;
            float halfHeight = size.y / 2 + 2f;

            // 방향이 0이면 중심 반환
            if (direction.sqrMagnitude < 0.0001f) {
                return center;
            }

            direction = direction.normalized;

            // 수평/수직 경계와의 교차점 계산 (레이캐스트 방식)
            float tX = direction.x != 0 ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
            float tY = direction.y != 0 ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;

            // 더 가까운 경계 선택
            float t = Mathf.Min(tX, tY);

            return center + direction * t;
        }

        private void CreateContents() {
            var container = new VisualElement();
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;

            // Action 카운트 정보
            var actionInfo = CreateActionInfo();
            if (actionInfo != null) {
                container.Add(actionInfo);
            }

            // Transition 카운트 정보
            var transitionInfo = CreateTransitionInfo();
            if (transitionInfo != null) {
                container.Add(transitionInfo);
            }

            extensionContainer.Add(container);
        }

        private VisualElement CreateActionInfo() {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginTop = 4;

            // Action 개수 계산
            int enterCount = GetActionCount("onEnterActions");
            int updateCount = GetActionCount("onUpdateActions");
            int exitCount = GetActionCount("onExitActions");

            if (enterCount + updateCount + exitCount == 0) return null;

            // 아이콘과 레이블
            _actionCountLabel = new Label($"Actions: E:{enterCount} U:{updateCount} X:{exitCount}");
            _actionCountLabel.style.fontSize = 10;
            _actionCountLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(_actionCountLabel);

            return container;
        }

        private VisualElement CreateTransitionInfo() {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginTop = 2;

            int transitionCount = GetTransitionCount();
            if (transitionCount == 0) return null;

            _transitionCountLabel = new Label($"Transitions: {transitionCount}");
            _transitionCountLabel.style.fontSize = 10;
            _transitionCountLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            container.Add(_transitionCountLabel);

            return container;
        }

        private int GetActionCount(string fieldName) {
            var field = typeof(YisoCharacterState).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var actions = field?.GetValue(State) as List<YisoCharacterAction>;
            return actions?.Count ?? 0;
        }

        private int GetTransitionCount() {
            var field = typeof(YisoCharacterState).GetField("transitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var transitions = field?.GetValue(State) as List<YisoCharacterTransition>;
            return transitions?.Count ?? 0;
        }

        public void SetInitialState(bool isInitial) {
            if (isInitial) {
                AddToClassList("initial-state");

                // 초기 상태 마커 추가
                if (_initialStateMarker == null) {
                    _initialStateMarker = new VisualElement();
                    _initialStateMarker.AddToClassList("initial-state-marker");
                    _initialStateMarker.tooltip = "Initial State";
                    titleContainer.Insert(0, _initialStateMarker);
                }
            }
            else {
                RemoveFromClassList("initial-state");

                if (_initialStateMarker != null) {
                    _initialStateMarker.RemoveFromHierarchy();
                    _initialStateMarker = null;
                }
            }
        }

        public void RefreshDisplay() {
            // 제목 업데이트
            title = State.StateName;

            // Action 카운트 업데이트
            if (_actionCountLabel != null) {
                int enterCount = GetActionCount("onEnterActions");
                int updateCount = GetActionCount("onUpdateActions");
                int exitCount = GetActionCount("onExitActions");
                _actionCountLabel.text = $"Actions: E:{enterCount} U:{updateCount} X:{exitCount}";
            }

            // Transition 카운트 업데이트
            if (_transitionCountLabel != null) {
                _transitionCountLabel.text = $"Transitions: {GetTransitionCount()}";
            }
        }

        /// <summary>
        /// 노드가 삭제 가능한지 확인 (초기 상태는 경고)
        /// </summary>
        public bool CanBeDeleted() {
            var initialState = _window.GetInitialStateName();
            if (State.StateName == initialState) {
                return UnityEditor.EditorUtility.DisplayDialog(
                    "Delete Initial State",
                    "이 상태는 초기 상태입니다. 삭제하면 FSM이 동작하지 않을 수 있습니다.\n정말 삭제하시겠습니까?",
                    "삭제", "취소");
            }
            return true;
        }
    }
}
