using System.Collections.Generic;
using Gameplay.Character.StateMachine;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.FSM.Views {
    /// <summary>
    /// FSM Transition을 시각화하는 GraphView Edge
    /// Animator 스타일: 노드 경계에서 시작/끝나는 화살표
    /// </summary>
    public class YisoTransitionEdgeView : Edge {
        public YisoStateNodeView SourceNode { get; private set; }
        public YisoStateNodeView TargetNode { get; private set; }
        public YisoCharacterTransition Transition { get; private set; }
        public int TransitionIndex { get; private set; }
        public bool IsRandom { get; private set; }

        // 캐시된 위치 (커스텀 렌더링용)
        private Vector2 _cachedStartPoint;
        private Vector2 _cachedEndPoint;
        private Vector2 _cachedDirection;

        // 커스텀 렌더링용 VisualElement
        private VisualElement _customEdgeRenderer;

        public YisoTransitionEdgeView(YisoStateNodeView sourceNode, YisoStateNodeView targetNode,
            YisoCharacterTransition transition, int transitionIndex, bool isRandom) {
            SourceNode = sourceNode;
            TargetNode = targetNode;
            Transition = transition;
            TransitionIndex = transitionIndex;
            IsRandom = isRandom;

            // 기본 스타일
            AddToClassList("yiso-transition-edge");

            if (isRandom) {
                AddToClassList("random-transition");
            }

            // 기본 EdgeControl 투명하게 (클릭 영역은 유지)
            schedule.Execute(() => {
                if (edgeControl != null) {
                    edgeControl.style.opacity = 0f;
                }
            }).ExecuteLater(1);

            // 커스텀 렌더러 생성
            CreateCustomRenderer();

            // 위치 업데이트 등록
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            // 선택 상태 변경 감지
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            // 스케줄러로 초기 업데이트
            schedule.Execute(UpdateEdgePositions).ExecuteLater(50);
        }

        private void OnMouseDown(MouseDownEvent evt) {
            // 클릭 시 다시 그리기 (선택 상태 반영)
            schedule.Execute(() => _customEdgeRenderer?.MarkDirtyRepaint()).ExecuteLater(10);
        }

        private void CreateCustomRenderer() {
            _customEdgeRenderer = new VisualElement();
            _customEdgeRenderer.name = "custom-edge-renderer";
            _customEdgeRenderer.pickingMode = PickingMode.Ignore;
            _customEdgeRenderer.style.position = Position.Absolute;
            _customEdgeRenderer.style.left = 0;
            _customEdgeRenderer.style.top = 0;
            _customEdgeRenderer.style.right = 0;
            _customEdgeRenderer.style.bottom = 0;
            _customEdgeRenderer.generateVisualContent += DrawEdgeAndArrow;
            Add(_customEdgeRenderer);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt) {
            // 소스/타겟 노드의 위치 변경 감지
            if (SourceNode != null) {
                SourceNode.RegisterCallback<GeometryChangedEvent>(OnNodeMoved);
            }
            if (TargetNode != null) {
                TargetNode.RegisterCallback<GeometryChangedEvent>(OnNodeMoved);
            }

            // 초기 업데이트
            schedule.Execute(UpdateEdgePositions).ExecuteLater(10);
        }

        private void OnNodeMoved(GeometryChangedEvent evt) {
            UpdateEdgePositions();
        }

        private void UpdateEdgePositions() {
            if (SourceNode == null || TargetNode == null) return;

            var sourceCenter = SourceNode.GetCenter();
            var targetCenter = TargetNode.GetCenter();

            // 두 노드 중심을 잇는 방향 계산
            var direction = (targetCenter - sourceCenter);
            if (direction.sqrMagnitude < 0.001f) {
                direction = Vector2.right;
            }
            direction = direction.normalized;

            // 각 노드 경계의 연결점 계산 (상대 위치에 따라 동적으로 변함)
            var startPoint = SourceNode.GetEdgeConnectionPoint(direction);
            var endPoint = TargetNode.GetEdgeConnectionPoint(-direction);

            // 캐시 업데이트
            _cachedStartPoint = startPoint;
            _cachedEndPoint = endPoint;
            _cachedDirection = direction;

            // 렌더러 다시 그리기
            _customEdgeRenderer?.MarkDirtyRepaint();
        }

        /// <summary>
        /// Bezier 곡선과 화살표를 직접 그립니다.
        /// </summary>
        private void DrawEdgeAndArrow(MeshGenerationContext ctx) {
            if (_cachedDirection.sqrMagnitude < 0.001f) return;

            var painter = ctx.painter2D;
            Color edgeColor = GetEdgeColor();

            Vector2 start = _cachedStartPoint;
            Vector2 end = _cachedEndPoint;
            Vector2 dir = _cachedDirection;

            // 화살표 크기
            float arrowLength = 14f;
            float arrowWidth = 10f;

            // 화살표 공간 확보를 위해 끝점 조정
            Vector2 lineEnd = end - dir * arrowLength;

            // === Bezier 곡선 그리기 ===
            float distance = Vector2.Distance(start, lineEnd);
            float tangentLength = Mathf.Clamp(distance * 0.3f, 30f, 100f);

            Vector2 ctrl1 = start + dir * tangentLength;
            Vector2 ctrl2 = lineEnd - dir * tangentLength;

            painter.strokeColor = edgeColor;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Round;

            painter.BeginPath();
            painter.MoveTo(start);
            painter.BezierCurveTo(ctrl1, ctrl2, lineEnd);
            painter.Stroke();

            // === 화살표 그리기 ===
            painter.fillColor = edgeColor;

            Vector2 perpendicular = new Vector2(-dir.y, dir.x);
            Vector2 arrowBack = end - dir * arrowLength;
            Vector2 arrowLeft = arrowBack - perpendicular * arrowWidth / 2;
            Vector2 arrowRight = arrowBack + perpendicular * arrowWidth / 2;

            painter.BeginPath();
            painter.MoveTo(end);           // 화살표 끝 (타겟 노드 경계)
            painter.LineTo(arrowLeft);     // 왼쪽 날개
            painter.LineTo(arrowRight);    // 오른쪽 날개
            painter.ClosePath();
            painter.Fill();
        }

        private Color GetEdgeColor() {
            if (selected) {
                return new Color(0.27f, 0.67f, 1f); // 선택됨: 파란색
            }
            if (IsRandom) {
                return new Color(0.67f, 0.53f, 1f); // 보라색
            }
            if (GetConditionCount() == 0) {
                return new Color(1f, 0.5f, 0.3f); // 주황색 경고
            }
            return new Color(0.7f, 0.7f, 0.7f); // 기본 회색
        }

        private int GetConditionCount() {
            if (Transition == null) return 0;

            var conditionsField = typeof(YisoCharacterTransition).GetField("conditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var conditions = conditionsField?.GetValue(Transition) as List<YisoCharacterTransition.TransitionCondition>;
            return conditions?.Count ?? 0;
        }

        public void RefreshDisplay() {
            UpdateEdgePositions();
        }

        /// <summary>
        /// 조건 정보를 문자열로 반환합니다.
        /// </summary>
        public string GetConditionsDescription() {
            if (Transition == null) return "No transition data";

            var conditionsField = typeof(YisoCharacterTransition).GetField("conditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var conditions = conditionsField?.GetValue(Transition) as List<YisoCharacterTransition.TransitionCondition>;
            if (conditions == null || conditions.Count == 0) {
                return "조건 없음 (항상 전환)";
            }

            var descriptions = new List<string>();
            foreach (var condition in conditions) {
                descriptions.Add($"  • {condition}");
            }

            return $"조건 (AND):\n{string.Join("\n", descriptions)}";
        }
    }
}
