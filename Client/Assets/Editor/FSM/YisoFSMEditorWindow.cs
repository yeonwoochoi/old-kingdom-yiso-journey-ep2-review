#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gameplay.Character.StateMachine;
using UnityEditor;
using UnityEngine;
using TransitionCondition = Gameplay.Character.StateMachine.YisoCharacterTransition.TransitionCondition;
using LogicMode = Gameplay.Character.StateMachine.YisoCharacterTransition.TransitionCondition.LogicMode;

namespace Editor.FSM {
    /// <summary>
    /// Unity Animator Controller와 유사한 시각적 FSM 에디터 윈도우.
    /// YisoCharacterStateMachine 컴포넌트를 가진 프리팹/게임오브젝트를 선택하면
    /// 해당 FSM의 상태(State)와 전이(Transition)를 그래프 형태로 시각화하고 편집할 수 있다.
    /// </summary>
    public class YisoFSMEditorWindow : EditorWindow {

        #region Constants

        private const float TOOLBAR_HEIGHT = 24f;
        private const float INSPECTOR_WIDTH = 330f;
        private const float NODE_WIDTH = 180f;
        private const float NODE_HEIGHT = 42f;
        private const float GRID_SMALL = 20f;
        private const float GRID_LARGE = 100f;
        private const float MIN_ZOOM = 0.2f;
        private const float MAX_ZOOM = 2.5f;
        private const float ZOOM_SPEED = 0.08f;
        private const float EDGE_CLICK_THRESHOLD = 10f;
        private const float ARROW_SIZE = 10f;
        private const float BIDIRECTIONAL_OFFSET = 14f;
        private const int BEZIER_SAMPLES = 25;

        // 색상
        private static readonly Color BG_COLOR = new(0.16f, 0.16f, 0.16f);
        private static readonly Color GRID_SMALL_COLOR = new(0.20f, 0.20f, 0.20f);
        private static readonly Color GRID_LARGE_COLOR = new(0.25f, 0.25f, 0.25f);
        private static readonly Color NODE_COLOR = new(0.24f, 0.24f, 0.24f);
        private static readonly Color NODE_INITIAL_COLOR = new(0.65f, 0.35f, 0.0f);
        private static readonly Color NODE_BORDER = new(0.38f, 0.38f, 0.38f);
        private static readonly Color NODE_SELECTED_BORDER = new(0.28f, 0.58f, 1.0f);
        private static readonly Color NODE_RUNTIME_BORDER = new(0.0f, 0.85f, 1.0f);
        private static readonly Color NODE_SEARCH_HL = new(1f, 1f, 0f, 0.25f);
        private static readonly Color EDGE_COLOR = new(0.75f, 0.75f, 0.75f);
        private static readonly Color EDGE_SELECTED_COLOR = new(0.3f, 0.65f, 1.0f);
        private static readonly Color EDGE_CREATING_COLOR = new(0.4f, 1f, 0.4f, 0.85f);
        private static readonly Color DIVIDER_COLOR = new(0.1f, 0.1f, 0.1f);

        #endregion

        #region State Fields

        // FSM 참조
        private YisoCharacterStateMachine _stateMachine;
        private GameObject _targetGo;

        // 캐시된 데이터
        private List<YisoCharacterState> _states;
        private string _initialStateName;

        // 선택 상태
        private int _selectedStateIdx = -1;
        private int _selTransStateIdx = -1; // 선택된 전이가 속한 상태 인덱스
        private int _selTransIdx = -1;      // 상태 내 전이 인덱스

        // 팬/줌
        private Vector2 _panOffset;
        private float _zoom = 1f;
        private bool _isPanning;

        // 노드 드래그
        private bool _isDragging;
        private int _dragIdx = -1;
        private Vector2 _dragOffset;

        // 전이 생성 모드
        private bool _isCreatingTransition;
        private int _transSourceIdx = -1;

        // 복사/붙여넣기
        private string _copiedStateName;
        private bool _isCut;
        private int _cutIdx = -1;

        // 검색
        private string _searchQuery = "";

        // 인스펙터 스크롤
        private Vector2 _inspScroll;

        // 에지 히트 테스트용 캐시
        private readonly List<EdgeData> _edgeCache = new();

        // 타입 캐시
        private Type[] _actionTypes;
        private string[] _actionNames;
        private Type[] _decisionTypes;
        private string[] _decisionNames;

        // 리플렉션 캐시
        private static FieldInfo _fStates, _fInitialState;
        private static FieldInfo _fStateName, _transitionsField;
        private static FieldInfo _fOnEnter, _fOnUpdate, _fOnExit;
        private static FieldInfo _fRandom, _fNextState, _fNextStates, _fConditions;
        private static bool _reflectionCached;

        private struct EdgeData {
            public int srcStateIdx, transIdx;
            public Vector2 p0, p1, t0, t1;
        }

        #endregion

        #region Menu & Lifecycle

        [MenuItem("Yiso/FSM Editor %#f")]
        public static void OpenWindow() {
            var w = GetWindow<YisoFSMEditorWindow>();
            w.titleContent = new GUIContent("FSM Editor");
            w.minSize = new Vector2(750, 350);
        }

        private void OnEnable() {
            CacheReflection();
            CacheTypes();
            wantsMouseMove = true;
            OnSelectionChange();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange change) => Repaint();

        private void OnSelectionChange() {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var fsm = go.GetComponent<YisoCharacterStateMachine>()
                      ?? go.GetComponentInChildren<YisoCharacterStateMachine>();
            if (fsm != null) LoadFSM(fsm, go);
            Repaint();
        }

        private void Update() {
            if (Application.isPlaying && _stateMachine != null) Repaint();
        }

        private void LoadFSM(YisoCharacterStateMachine fsm, GameObject go) {
            if (_stateMachine == fsm) return;
            _stateMachine = fsm;
            _targetGo = go;
            ClearSelection();
            RefreshCache();
            CenterView();
        }

        private void ClearSelection() {
            _selectedStateIdx = -1;
            _selTransStateIdx = -1;
            _selTransIdx = -1;
            _isCreatingTransition = false;
        }

        private void RefreshCache() {
            if (_stateMachine == null) return;
            _states = FGet<List<YisoCharacterState>>(_stateMachine, _fStates);
            _initialStateName = FGet<string>(_stateMachine, _fInitialState);
            if (_states == null) {
                _states = new List<YisoCharacterState>();
                FSet(_stateMachine, _fStates, _states);
            }
        }

        private void CenterView() {
            float gw = position.width - INSPECTOR_WIDTH;
            float gh = position.height - TOOLBAR_HEIGHT;
            if (_states == null || _states.Count == 0) {
                _panOffset = new Vector2(gw / 2f, gh / 2f);
                return;
            }
            var c = Vector2.zero;
            foreach (var s in _states) c += s.editorNodePosition;
            c /= _states.Count;
            _panOffset = new Vector2(gw / 2f, gh / 2f) - c * _zoom;
        }

        #endregion

        #region OnGUI

        private void OnGUI() {
            if (_stateMachine == null) {
                DrawNoSelection();
                return;
            }
            RefreshCache();

            float inspW = Mathf.Min(INSPECTOR_WIDTH, position.width * 0.4f);
            Rect graphRect = new(0, TOOLBAR_HEIGHT, position.width - inspW, position.height - TOOLBAR_HEIGHT);
            Rect inspRect = new(position.width - inspW, TOOLBAR_HEIGHT, inspW, position.height - TOOLBAR_HEIGHT);

            DrawToolbar(new Rect(0, 0, position.width, TOOLBAR_HEIGHT), graphRect);
            DrawGraphView(graphRect);
            EditorGUI.DrawRect(new Rect(inspRect.x - 1, inspRect.y, 2, inspRect.height), DIVIDER_COLOR);
            DrawInspector(inspRect);
        }

        private void DrawNoSelection() {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("YisoCharacterStateMachine 컴포넌트가 있는\n게임오브젝트 또는 프리팹을 선택하세요.",
                EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        #endregion

        #region Toolbar

        private void DrawToolbar(Rect toolbarRect, Rect graphRect) {
            GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            // 검색
            GUILayout.Label("Search:", GUILayout.Width(48));
            _searchQuery = GUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(160));

            GUILayout.FlexibleSpace();

            // 자동 배치
            if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(80)))
                AutoLayout();

            // 뷰 맞춤
            if (GUILayout.Button("Fit View", EditorStyles.toolbarButton, GUILayout.Width(60)))
                FitToView(graphRect);

            // FSM 이름
            GUILayout.Label(_stateMachine != null ? _stateMachine.name : "", EditorStyles.toolbarButton);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        #endregion

        #region Graph View

        private void DrawGraphView(Rect graphRect) {
            // 배경
            EditorGUI.DrawRect(graphRect, BG_COLOR);

            // 클립 영역 시작
            GUI.BeginClip(graphRect);
            Rect local = new(0, 0, graphRect.width, graphRect.height);

            DrawGrid(local);

            // 에지 캐시 갱신 (Repaint 이벤트에서만)
            if (Event.current.type == EventType.Repaint)
                _edgeCache.Clear();

            DrawEdges();
            DrawNodes(local);

            if (_isCreatingTransition)
                DrawTransitionCreationLine();

            HandleGraphInput(local);

            GUI.EndClip();
        }

        #endregion

        #region Grid

        private void DrawGrid(Rect rect) {
            DrawGridLines(rect, GRID_SMALL * _zoom, GRID_SMALL_COLOR);
            DrawGridLines(rect, GRID_LARGE * _zoom, GRID_LARGE_COLOR);
        }

        private void DrawGridLines(Rect rect, float spacing, Color color) {
            if (spacing < 4f) return;
            Handles.color = color;

            float ox = _panOffset.x % spacing;
            float oy = _panOffset.y % spacing;
            if (ox < 0) ox += spacing;
            if (oy < 0) oy += spacing;

            for (float x = ox; x < rect.width; x += spacing)
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, rect.height));
            for (float y = oy; y < rect.height; y += spacing)
                Handles.DrawLine(new Vector3(0, y), new Vector3(rect.width, y));
        }

        #endregion

        #region Node Drawing

        private Rect GetNodeRect(int idx) {
            var pos = G2S(_states[idx].editorNodePosition);
            float w = NODE_WIDTH * _zoom, h = NODE_HEIGHT * _zoom;
            return new Rect(pos.x - w / 2f, pos.y - h / 2f, w, h);
        }

        private void DrawNodes(Rect clipRect) {
            if (_states == null) return;

            string runtimeStateName = null;
            if (Application.isPlaying && _stateMachine.CurrentState != null)
                runtimeStateName = _stateMachine.CurrentState.StateName;

            for (int i = 0; i < _states.Count; i++) {
                var r = GetNodeRect(i);
                // 시야 밖이면 스킵
                if (r.xMax < 0 || r.x > clipRect.width || r.yMax < 0 || r.y > clipRect.height) continue;

                string name = _states[i].StateName ?? "(unnamed)";
                bool isInitial = name == _initialStateName;
                bool isSelected = _selectedStateIdx == i;
                bool isRuntime = runtimeStateName != null && name == runtimeStateName;
                bool isSearch = !string.IsNullOrEmpty(_searchQuery) &&
                                name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0;

                // 그림자
                EditorGUI.DrawRect(new Rect(r.x + 3, r.y + 3, r.width, r.height), new Color(0, 0, 0, 0.35f));

                // 테두리
                Color borderCol = isSelected ? NODE_SELECTED_BORDER :
                    isRuntime ? NODE_RUNTIME_BORDER : NODE_BORDER;
                float bw = isSelected || isRuntime ? 3f : 1.5f;
                EditorGUI.DrawRect(new Rect(r.x - bw, r.y - bw, r.width + bw * 2, r.height + bw * 2), borderCol);

                // 배경
                EditorGUI.DrawRect(r, isInitial ? NODE_INITIAL_COLOR : NODE_COLOR);

                // 런타임 오버레이
                if (isRuntime)
                    EditorGUI.DrawRect(r, new Color(0, 0.8f, 1f, 0.12f));

                // 검색 하이라이트
                if (isSearch)
                    EditorGUI.DrawRect(r, NODE_SEARCH_HL);

                // 라벨
                var style = new GUIStyle(EditorStyles.label) {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.RoundToInt(11 * Mathf.Clamp(_zoom, 0.6f, 1.5f))
                };
                GUI.Label(r, name, style);
            }
        }

        #endregion

        #region Edge Drawing

        private void DrawEdges() {
            if (_states == null) return;

            // 양방향 엣지 탐지 (쌍 미리 계산)
            var biSet = new HashSet<(int, int)>();
            for (int si = 0; si < _states.Count; si++) {
                var trans = GetTransitions(si);
                if (trans == null) continue;
                foreach (var t in trans) {
                    foreach (int ti in GetTargetIndices(t)) {
                        if (ti < 0 || ti == si) continue;
                        // 역방향 존재 확인
                        var revTrans = GetTransitions(ti);
                        if (revTrans != null && revTrans.Any(rt => GetTargetIndices(rt).Contains(si))) {
                            biSet.Add((Mathf.Min(si, ti), Mathf.Max(si, ti)));
                        }
                    }
                }
            }

            for (int si = 0; si < _states.Count; si++) {
                var trans = GetTransitions(si);
                if (trans == null) continue;
                for (int ti = 0; ti < trans.Count; ti++) {
                    var targets = GetTargetIndices(trans[ti]);
                    foreach (int targetIdx in targets) {
                        if (targetIdx < 0) continue;
                        bool isSel = _selTransStateIdx == si && _selTransIdx == ti;
                        Color col = isSel ? EDGE_SELECTED_COLOR : EDGE_COLOR;
                        float width = isSel ? 3.5f : 2f;

                        bool isBi = targetIdx != si &&
                                    biSet.Contains((Mathf.Min(si, targetIdx), Mathf.Max(si, targetIdx)));

                        DrawSingleEdge(si, targetIdx, ti, col, width, isBi);
                    }
                }
            }
        }

        private void DrawSingleEdge(int srcIdx, int dstIdx, int transIdx, Color col, float width, bool isBi) {
            Rect srcRect = GetNodeRect(srcIdx);
            Rect dstRect = GetNodeRect(dstIdx);

            Vector2 srcC = srcRect.center;
            Vector2 dstC = dstRect.center;

            // 자기 자신으로의 전이 (루프)
            if (srcIdx == dstIdx) {
                DrawSelfLoop(srcRect, transIdx, col, width);
                return;
            }

            Vector2 dir = (dstC - srcC).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            // 양방향 오프셋
            Vector2 offset = isBi ? perp * BIDIRECTIONAL_OFFSET * _zoom : Vector2.zero;

            Vector2 p0 = GetBorderPoint(srcRect, dir) + offset;
            Vector2 p3 = GetBorderPoint(dstRect, -dir) + offset;

            float dist = Vector2.Distance(p0, p3);
            float tStr = Mathf.Clamp(dist * 0.35f, 15f, 130f);

            Vector2 t0 = p0 + dir * tStr;
            Vector2 t1 = p3 - dir * tStr;

            Handles.DrawBezier(p0, p3, t0, t1, col, null, width);
            DrawArrowhead(p3, (p3 - t1).normalized, col);

            // 에지 캐시 (히트 테스트용)
            if (Event.current.type == EventType.Repaint) {
                _edgeCache.Add(new EdgeData {
                    srcStateIdx = srcIdx, transIdx = transIdx,
                    p0 = p0, p1 = p3, t0 = t0, t1 = t1
                });
            }
        }

        private void DrawSelfLoop(Rect nodeRect, int transIdx, Color col, float width) {
            float loopH = 45f * _zoom;
            float loopW = 25f * _zoom;
            Vector2 top = new(nodeRect.center.x, nodeRect.yMin);
            Vector2 p0 = new(top.x - loopW, top.y);
            Vector2 p3 = new(top.x + loopW, top.y);
            Vector2 t0 = new(p0.x - loopW, p0.y - loopH);
            Vector2 t1 = new(p3.x + loopW, p3.y - loopH);

            Handles.DrawBezier(p0, p3, t0, t1, col, null, width);
            DrawArrowhead(p3, new Vector2(0.5f, 1f).normalized, col);
        }

        /// <summary>
        /// 노드 경계선의 가장 자연스러운 연결점 계산
        /// </summary>
        private static Vector2 GetBorderPoint(Rect rect, Vector2 dir) {
            if (dir.sqrMagnitude < 0.0001f) return rect.center;
            dir.Normalize();
            float hw = rect.width / 2f, hh = rect.height / 2f;
            float tx = dir.x != 0 ? hw / Mathf.Abs(dir.x) : float.MaxValue;
            float ty = dir.y != 0 ? hh / Mathf.Abs(dir.y) : float.MaxValue;
            return rect.center + dir * Mathf.Min(tx, ty);
        }

        private void DrawArrowhead(Vector2 tip, Vector2 dir, Color col) {
            if (dir.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(dir.y, dir.x);
            float halfAngle = 28f * Mathf.Deg2Rad;
            float size = ARROW_SIZE * _zoom;

            Vector2 left = tip - new Vector2(Mathf.Cos(angle - halfAngle), Mathf.Sin(angle - halfAngle)) * size;
            Vector2 right = tip - new Vector2(Mathf.Cos(angle + halfAngle), Mathf.Sin(angle + halfAngle)) * size;

            Handles.color = col;
            Handles.DrawAAConvexPolygon(
                new Vector3(tip.x, tip.y, 0),
                new Vector3(left.x, left.y, 0),
                new Vector3(right.x, right.y, 0));
        }

        private void DrawTransitionCreationLine() {
            if (_transSourceIdx < 0 || _transSourceIdx >= _states.Count) return;
            Rect srcRect = GetNodeRect(_transSourceIdx);
            Vector2 mouse = Event.current.mousePosition;
            Vector2 dir = (mouse - srcRect.center).normalized;
            Vector2 start = GetBorderPoint(srcRect, dir);

            Handles.DrawBezier(start, mouse,
                start + dir * 40f * _zoom, mouse - dir * 40f * _zoom,
                EDGE_CREATING_COLOR, null, 2.5f);
            Repaint();
        }

        /// <summary>
        /// 마우스 위치가 베지에 커브 근처인지 판정
        /// </summary>
        private static bool IsNearBezier(Vector2 pt, Vector2 p0, Vector2 p3, Vector2 t0, Vector2 t1, float threshold) {
            for (int i = 0; i <= BEZIER_SAMPLES; i++) {
                float t = i / (float)BEZIER_SAMPLES;
                Vector2 b = CubicBezier(t, p0, t0, t1, p3);
                if (Vector2.Distance(pt, b) < threshold) return true;
            }
            return false;
        }

        private static Vector2 CubicBezier(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        #endregion

        #region Graph Input

        private void HandleGraphInput(Rect local) {
            Event e = Event.current;
            Vector2 mp = e.mousePosition;
            bool inRect = local.Contains(mp);
            if (!inRect && e.type != EventType.MouseUp && e.type != EventType.MouseDrag) return;

            switch (e.type) {
                case EventType.ScrollWheel:
                    HandleZoom(e, mp);
                    break;
                case EventType.MouseDown:
                    HandleMouseDown(e, mp);
                    break;
                case EventType.MouseDrag:
                    HandleMouseDrag(e, mp);
                    break;
                case EventType.MouseUp:
                    HandleMouseUp(e);
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape && _isCreatingTransition) {
                        _isCreatingTransition = false;
                        e.Use();
                    }
                    if (e.keyCode == KeyCode.Delete && _selectedStateIdx >= 0) {
                        DeleteState(_selectedStateIdx);
                        e.Use();
                    }
                    break;
            }
        }

        private void HandleZoom(Event e, Vector2 mp) {
            float oldZoom = _zoom;
            float delta = -e.delta.y * ZOOM_SPEED;
            _zoom = Mathf.Clamp(_zoom + delta, MIN_ZOOM, MAX_ZOOM);

            // 마우스 위치 중심으로 줌
            _panOffset = mp - (_zoom / oldZoom) * (mp - _panOffset);
            e.Use();
        }

        private void HandleMouseDown(Event e, Vector2 mp) {
            // 미들 클릭 -> 팬
            if (e.button == 2 || (e.button == 0 && e.alt)) {
                _isPanning = true;
                e.Use();
                return;
            }

            // 전이 생성 모드에서 좌클릭
            if (e.button == 0 && _isCreatingTransition) {
                int hitNode = HitTestNode(mp);
                if (hitNode >= 0 && hitNode != _transSourceIdx) {
                    CreateTransition(_transSourceIdx, hitNode);
                }
                _isCreatingTransition = false;
                e.Use();
                return;
            }

            // 좌클릭
            if (e.button == 0) {
                int hitNode = HitTestNode(mp);
                if (hitNode >= 0) {
                    _selectedStateIdx = hitNode;
                    _selTransStateIdx = -1;
                    _selTransIdx = -1;
                    _isDragging = true;
                    _dragIdx = hitNode;
                    _dragOffset = _states[hitNode].editorNodePosition - S2G(mp);
                    e.Use();
                    return;
                }

                // 에지 히트 테스트
                var hitEdge = HitTestEdge(mp);
                if (hitEdge.srcStateIdx >= 0) {
                    _selTransStateIdx = hitEdge.srcStateIdx;
                    _selTransIdx = hitEdge.transIdx;
                    _selectedStateIdx = -1;
                    e.Use();
                    return;
                }

                // 빈 공간 클릭 -> 선택 해제
                ClearSelection();
                e.Use();
            }

            // 우클릭
            if (e.button == 1) {
                int hitNode = HitTestNode(mp);
                if (hitNode >= 0) {
                    _selectedStateIdx = hitNode;
                    ShowNodeMenu(hitNode, mp);
                } else {
                    ShowEmptyMenu(S2G(mp));
                }
                e.Use();
            }
        }

        private void HandleMouseDrag(Event e, Vector2 mp) {
            if (_isPanning) {
                _panOffset += e.delta;
                e.Use();
                Repaint();
                return;
            }
            if (_isDragging && _dragIdx >= 0 && _dragIdx < _states.Count) {
                RecordUndo("Move State Node");
                _states[_dragIdx].editorNodePosition = S2G(mp) + _dragOffset;
                MarkDirty();
                e.Use();
                Repaint();
            }
        }

        private void HandleMouseUp(Event e) {
            if (_isPanning) { _isPanning = false; e.Use(); }
            if (_isDragging) { _isDragging = false; _dragIdx = -1; e.Use(); }
        }

        private int HitTestNode(Vector2 mp) {
            // 역순으로 검사 (위에 그려진 노드가 우선)
            for (int i = _states.Count - 1; i >= 0; i--) {
                if (GetNodeRect(i).Contains(mp)) return i;
            }
            return -1;
        }

        private EdgeData HitTestEdge(Vector2 mp) {
            foreach (var ed in _edgeCache) {
                if (IsNearBezier(mp, ed.p0, ed.p1, ed.t0, ed.t1, EDGE_CLICK_THRESHOLD))
                    return ed;
            }
            return new EdgeData { srcStateIdx = -1, transIdx = -1 };
        }

        #endregion

        #region Context Menus

        private void ShowEmptyMenu(Vector2 graphPos) {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create State"), false, () => CreateState(graphPos));
            if (_copiedStateName != null)
                menu.AddItem(new GUIContent("Paste"), false, () => PasteState(graphPos));
            else
                menu.AddDisabledItem(new GUIContent("Paste"));
            menu.ShowAsContext();
        }

        private void ShowNodeMenu(int idx, Vector2 mousePos) {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Make Transition"), false, () => {
                _isCreatingTransition = true;
                _transSourceIdx = idx;
            });
            menu.AddItem(new GUIContent("Set as Initial State"), false, () => SetInitialState(idx));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Copy"), false, () => CopyState(idx));
            menu.AddItem(new GUIContent("Cut"), false, () => CutState(idx));
            menu.AddItem(new GUIContent("Delete"), false, () => DeleteState(idx));
            menu.ShowAsContext();
        }

        #endregion

        #region Inspector Panel

        private void DrawInspector(Rect rect) {
            GUILayout.BeginArea(rect);
            EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.19f, 0.19f, 0.19f));
            _inspScroll = EditorGUILayout.BeginScrollView(_inspScroll);
            EditorGUILayout.Space(4);

            if (_selectedStateIdx >= 0 && _selectedStateIdx < _states.Count)
                DrawStateInspector(_selectedStateIdx);
            else if (_selTransStateIdx >= 0)
                DrawTransitionInspector();
            else
                DrawMachineInfo();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMachineInfo() {
            EditorGUILayout.LabelField("FSM Info", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Name", _stateMachine.name);
            EditorGUILayout.LabelField("States", _states != null ? _states.Count.ToString() : "0");
            EditorGUILayout.LabelField("Initial State", _initialStateName ?? "(none)");

            if (Application.isPlaying && _stateMachine.CurrentState != null) {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Current State", _stateMachine.CurrentState.StateName);
                EditorGUILayout.LabelField("Time in State", $"{_stateMachine.TimeInCurrentState:F2}s");
            }
        }

        #endregion

        #region State Inspector

        private void DrawStateInspector(int idx) {
            var state = _states[idx];
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 상태 이름
            string oldName = state.StateName ?? "";
            string newName = EditorGUILayout.TextField("State Name", oldName);
            if (newName != oldName) RenameState(idx, newName);

            // 초기 상태 토글
            bool isInitial = state.StateName == _initialStateName;
            bool newInitial = EditorGUILayout.Toggle("Initial State", isInitial);
            if (newInitial && !isInitial) SetInitialState(idx);
            else if (!newInitial && isInitial) {
                RecordUndo("Clear Initial State");
                FSet(_stateMachine, _fInitialState, "");
                _initialStateName = "";
                MarkDirty();
            }

            // Actions
            EditorGUILayout.Space(8);
            DrawActionList("On Enter Actions", state, _fOnEnter, idx);
            EditorGUILayout.Space(4);
            DrawActionList("On Update Actions", state, _fOnUpdate, idx);
            EditorGUILayout.Space(4);
            DrawActionList("On Exit Actions", state, _fOnExit, idx);

            // Transitions
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
            var trans = GetTransitions(idx);
            if (trans != null) {
                for (int i = 0; i < trans.Count; i++) {
                    EditorGUILayout.BeginHorizontal();
                    string dest = GetTransitionLabel(trans[i]);
                    bool sel = _selTransStateIdx == idx && _selTransIdx == i;
                    if (GUILayout.Toggle(sel, $"  -> {dest}", EditorStyles.toolbarButton)) {
                        _selTransStateIdx = idx;
                        _selTransIdx = i;
                        _selectedStateIdx = -1;
                    }
                    if (GUILayout.Button("\u00d7", GUILayout.Width(22))) {
                        DeleteTransition(idx, i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DrawActionList(string label, YisoCharacterState state, FieldInfo field, int stateIdx) {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            var actions = FGet<List<YisoCharacterAction>>(state, field);

            if (actions != null) {
                for (int i = 0; i < actions.Count; i++) {
                    EditorGUILayout.BeginHorizontal();
                    var newAction = (YisoCharacterAction)EditorGUILayout.ObjectField(
                        actions[i], typeof(YisoCharacterAction), true);
                    if (newAction != actions[i]) {
                        RecordUndo("Change Action Reference");
                        actions[i] = newAction;
                        MarkDirty();
                    }
                    if (GUILayout.Button("-", GUILayout.Width(22))) {
                        RemoveAction(state, field, i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("+ Add " + label.Replace("Actions", "Action").Replace("On ", ""))) {
                ShowAddComponentMenu<YisoCharacterAction>(_actionTypes, _actionNames,
                    comp => {
                        RecordUndo("Add Action");
                        var list = FGet<List<YisoCharacterAction>>(state, field);
                        if (list == null) { list = new List<YisoCharacterAction>(); FSet(state, field, list); }
                        list.Add((YisoCharacterAction)comp);
                        MarkDirty();
                    });
            }
        }

        private void RemoveAction(YisoCharacterState state, FieldInfo field, int index) {
            RecordUndo("Remove Action");
            var list = FGet<List<YisoCharacterAction>>(state, field);
            if (list == null || index >= list.Count) return;
            var removed = list[index];
            list.RemoveAt(index);
            if (removed != null && removed.gameObject != _stateMachine.gameObject)
                Undo.DestroyObjectImmediate(removed.gameObject);
            MarkDirty();
        }

        #endregion

        #region Transition Inspector

        private void DrawTransitionInspector() {
            if (_selTransStateIdx < 0 || _selTransStateIdx >= _states.Count) return;
            var trans = GetTransitions(_selTransStateIdx);
            if (trans == null || _selTransIdx < 0 || _selTransIdx >= trans.Count) return;
            var t = trans[_selTransIdx];

            EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Source
            EditorGUILayout.LabelField("From", _states[_selTransStateIdx].StateName);

            // Destination
            bool isRandom = FGet<bool>(t, _fRandom);
            bool newRandom = EditorGUILayout.Toggle("Random Destination", isRandom);
            if (newRandom != isRandom) {
                RecordUndo("Toggle Random Transition");
                FSet(t, _fRandom, newRandom);
                MarkDirty();
            }

            if (!newRandom) {
                string next = FGet<string>(t, _fNextState) ?? "";
                int selectedIdx = _states.FindIndex(s => s.StateName == next);
                string[] names = _states.Select(s => s.StateName ?? "").ToArray();
                int newSelected = EditorGUILayout.Popup("Next State", selectedIdx, names);
                if (newSelected != selectedIdx && newSelected >= 0) {
                    RecordUndo("Change Transition Destination");
                    FSet(t, _fNextState, _states[newSelected].StateName);
                    MarkDirty();
                }
            } else {
                var nextStates = FGet<List<string>>(t, _fNextStates);
                if (nextStates == null) { nextStates = new List<string>(); FSet(t, _fNextStates, nextStates); }
                string[] names = _states.Select(s => s.StateName ?? "").ToArray();

                EditorGUILayout.LabelField("Next States (Random)", EditorStyles.miniBoldLabel);
                for (int i = 0; i < nextStates.Count; i++) {
                    EditorGUILayout.BeginHorizontal();
                    int selIdx = Array.IndexOf(names, nextStates[i]);
                    int newSel = EditorGUILayout.Popup(selIdx, names);
                    if (newSel != selIdx && newSel >= 0) {
                        RecordUndo("Change Random Destination");
                        nextStates[i] = names[newSel];
                        MarkDirty();
                    }
                    if (GUILayout.Button("-", GUILayout.Width(22))) {
                        RecordUndo("Remove Random Destination");
                        nextStates.RemoveAt(i);
                        MarkDirty();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("+ Add Random Destination")) {
                    RecordUndo("Add Random Destination");
                    nextStates.Add(_states.Count > 0 ? _states[0].StateName : "");
                    MarkDirty();
                }
            }

            // Conditions
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Conditions (All must be true)", EditorStyles.boldLabel);
            var conditions = FGet<List<TransitionCondition>>(t, _fConditions);
            if (conditions == null) {
                conditions = new List<TransitionCondition>();
                FSet(t, _fConditions, conditions);
            }

            for (int i = 0; i < conditions.Count; i++) {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                bool removed = DrawConditionNode(conditions[i], 0, conditions, i);
                EditorGUILayout.EndVertical();
                if (removed) break;
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Condition")) {
                RecordUndo("Add Condition");
                conditions.Add(new TransitionCondition { mode = LogicMode.Single });
                MarkDirty();
            }
        }

        /// <summary>
        /// TransitionCondition 재귀 트리 드로잉. true 반환 시 상위에서 break 필요 (삭제됨).
        /// </summary>
        private bool DrawConditionNode(TransitionCondition cond, int depth,
            List<TransitionCondition> parentList, int indexInParent) {

            EditorGUILayout.BeginHorizontal();

            // 들여쓰기
            if (depth > 0) GUILayout.Space(depth * 18);

            // NOT 토글
            bool newInvert = EditorGUILayout.Toggle(cond.invertResult, GUILayout.Width(16));
            if (newInvert != cond.invertResult) {
                RecordUndo("Toggle NOT");
                cond.invertResult = newInvert;
                MarkDirty();
            }
            GUILayout.Label(cond.invertResult ? "NOT" : "   ", GUILayout.Width(28));

            // 모드
            var newMode = (LogicMode)EditorGUILayout.EnumPopup(cond.mode, GUILayout.Width(65));
            if (newMode != cond.mode) {
                RecordUndo("Change Condition Mode");
                cond.mode = newMode;
                if (newMode != LogicMode.Single && cond.subConditions == null)
                    cond.subConditions = new List<TransitionCondition>();
                MarkDirty();
            }

            // Single 모드: Decision 필드
            if (cond.mode == LogicMode.Single) {
                var newDec = (YisoCharacterDecision)EditorGUILayout.ObjectField(
                    cond.singleDecision, typeof(YisoCharacterDecision), true);
                if (newDec != cond.singleDecision) {
                    RecordUndo("Change Decision");
                    cond.singleDecision = newDec;
                    MarkDirty();
                }
            } else {
                int subCount = cond.subConditions?.Count ?? 0;
                GUILayout.Label($"({subCount} conditions)", EditorStyles.miniLabel);
            }

            // 삭제 버튼
            if (GUILayout.Button("\u00d7", GUILayout.Width(22))) {
                RecordUndo("Remove Condition");
                parentList.RemoveAt(indexInParent);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                return true;
            }

            EditorGUILayout.EndHorizontal();

            // And/Or 그룹: 하위 조건 재귀
            if (cond.mode != LogicMode.Single && cond.subConditions != null) {
                EditorGUI.indentLevel++;
                for (int i = 0; i < cond.subConditions.Count; i++) {
                    bool removed = DrawConditionNode(cond.subConditions[i], depth + 1, cond.subConditions, i);
                    if (removed) {
                        EditorGUI.indentLevel--;
                        return false; // 리스트 수정됨, 부모에서 다시 그려짐
                    }
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * 18);
                if (GUILayout.Button("+ Add Sub-Condition", EditorStyles.miniButton)) {
                    RecordUndo("Add Sub-Condition");
                    cond.subConditions.Add(new TransitionCondition { mode = LogicMode.Single });
                    MarkDirty();
                }
                EditorGUILayout.EndHorizontal();

                // Decision 추가 (Single 모드에서 새 Decision 컴포넌트 생성)
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * 18);
                if (GUILayout.Button("+ Create Decision Component", EditorStyles.miniButton)) {
                    ShowAddComponentMenu<YisoCharacterDecision>(_decisionTypes, _decisionNames,
                        comp => {
                            RecordUndo("Create Decision");
                            var newCond = new TransitionCondition {
                                mode = LogicMode.Single,
                                singleDecision = (YisoCharacterDecision)comp
                            };
                            cond.subConditions.Add(newCond);
                            MarkDirty();
                        });
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            // Single 모드에서 Decision이 없을 때 생성 버튼
            if (cond.mode == LogicMode.Single && cond.singleDecision == null) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(depth * 18 + 110);
                if (GUILayout.Button("Create New Decision", EditorStyles.miniButton)) {
                    ShowAddComponentMenu<YisoCharacterDecision>(_decisionTypes, _decisionNames,
                        comp => {
                            RecordUndo("Assign Decision");
                            cond.singleDecision = (YisoCharacterDecision)comp;
                            MarkDirty();
                        });
                }
                EditorGUILayout.EndHorizontal();
            }

            return false;
        }

        #endregion

        #region Data Operations

        private void RecordUndo(string desc) => Undo.RecordObject(_stateMachine, desc);

        private void MarkDirty() {
            EditorUtility.SetDirty(_stateMachine);
            if (PrefabUtility.IsPartOfPrefabInstance(_stateMachine))
                PrefabUtility.RecordPrefabInstancePropertyModifications(_stateMachine);
        }

        private void CreateState(Vector2 graphPos) {
            RecordUndo("Create State");
            var state = new YisoCharacterState();
            string baseName = "New State";
            string name = baseName;
            int counter = 1;
            while (_states.Any(s => s.StateName == name))
                name = baseName + " " + counter++;

            FSet(state, _stateNameField, name);
            FSet(state, _transitionsField, new List<YisoCharacterTransition>());
            FSet(state, _fOnEnter, new List<YisoCharacterAction>());
            FSet(state, _fOnUpdate, new List<YisoCharacterAction>());
            FSet(state, _fOnExit, new List<YisoCharacterAction>());
            state.editorNodePosition = graphPos;

            _states.Add(state);
            _selectedStateIdx = _states.Count - 1;
            MarkDirty();
        }

        private void DeleteState(int idx) {
            if (idx < 0 || idx >= _states.Count) return;
            RecordUndo("Delete State");

            string deletedName = _states[idx].StateName;

            // 이 상태의 Action 게임오브젝트 정리
            CleanupActionsForState(_states[idx]);

            // 다른 상태에서 이 상태를 참조하는 전이 삭제
            for (int i = 0; i < _states.Count; i++) {
                if (i == idx) continue;
                var trans = GetTransitions(i);
                if (trans == null) continue;
                trans.RemoveAll(t => {
                    var targets = GetTargetIndices(t);
                    return targets.Any(ti => ti >= 0 && _states[ti].StateName == deletedName);
                });
            }

            _states.RemoveAt(idx);

            // 초기 상태가 삭제된 경우
            if (_initialStateName == deletedName) {
                FSet(_stateMachine, _fInitialState, _states.Count > 0 ? _states[0].StateName : "");
                _initialStateName = FGet<string>(_stateMachine, _fInitialState);
            }

            ClearSelection();
            MarkDirty();
        }

        private void CleanupActionsForState(YisoCharacterState state) {
            void CleanList(List<YisoCharacterAction> list) {
                if (list == null) return;
                foreach (var a in list) {
                    if (a != null && a.gameObject != _stateMachine.gameObject)
                        Undo.DestroyObjectImmediate(a.gameObject);
                }
            }
            CleanList(FGet<List<YisoCharacterAction>>(state, _fOnEnter));
            CleanList(FGet<List<YisoCharacterAction>>(state, _fOnUpdate));
            CleanList(FGet<List<YisoCharacterAction>>(state, _fOnExit));
        }

        private void SetInitialState(int idx) {
            RecordUndo("Set Initial State");
            FSet(_stateMachine, _fInitialState, _states[idx].StateName);
            _initialStateName = _states[idx].StateName;
            MarkDirty();
        }

        private void RenameState(int idx, string newName) {
            string oldName = _states[idx].StateName;
            if (oldName == newName) return;
            RecordUndo("Rename State");

            FSet(_states[idx], _stateNameField, newName);

            // 모든 전이 참조 업데이트
            foreach (var state in _states) {
                var trans = FGet<List<YisoCharacterTransition>>(state, _transitionsField);
                if (trans == null) continue;
                foreach (var t in trans) {
                    if (FGet<string>(t, _fNextState) == oldName)
                        FSet(t, _fNextState, newName);
                    var nextStates = FGet<List<string>>(t, _fNextStates);
                    if (nextStates != null) {
                        for (int j = 0; j < nextStates.Count; j++)
                            if (nextStates[j] == oldName) nextStates[j] = newName;
                    }
                }
            }

            // 초기 상태 업데이트
            if (_initialStateName == oldName) {
                FSet(_stateMachine, _fInitialState, newName);
                _initialStateName = newName;
            }
            MarkDirty();
        }

        private void CreateTransition(int fromIdx, int toIdx) {
            RecordUndo("Create Transition");
            var trans = GetTransitions(fromIdx);
            if (trans == null) {
                trans = new List<YisoCharacterTransition>();
                FSet(_states[fromIdx], _transitionsField, trans);
            }

            var newTrans = new YisoCharacterTransition();
            FSet(newTrans, _fRandom, false);
            FSet(newTrans, _fNextState, _states[toIdx].StateName);
            FSet(newTrans, _fNextStates, new List<string>());
            FSet(newTrans, _fConditions, new List<TransitionCondition>());
            trans.Add(newTrans);

            _selTransStateIdx = fromIdx;
            _selTransIdx = trans.Count - 1;
            _selectedStateIdx = -1;
            MarkDirty();
        }

        private void DeleteTransition(int stateIdx, int transIdx) {
            RecordUndo("Delete Transition");
            var trans = GetTransitions(stateIdx);
            if (trans == null || transIdx >= trans.Count) return;
            trans.RemoveAt(transIdx);
            if (_selTransStateIdx == stateIdx && _selTransIdx == transIdx)
                ClearSelection();
            MarkDirty();
        }

        #endregion

        #region Copy / Cut / Paste

        private void CopyState(int idx) {
            _copiedStateName = _states[idx].StateName;
            _isCut = false;
            _cutIdx = -1;
        }

        private void CutState(int idx) {
            _copiedStateName = _states[idx].StateName;
            _isCut = true;
            _cutIdx = idx;
        }

        private void PasteState(Vector2 graphPos) {
            if (_copiedStateName == null) return;

            if (_isCut && _cutIdx >= 0 && _cutIdx < _states.Count) {
                // 잘라내기: 기존 노드 이동
                RecordUndo("Cut & Paste State");
                _states[_cutIdx].editorNodePosition = graphPos;
                _isCut = false;
                _cutIdx = -1;
                MarkDirty();
            } else {
                // 복사: 새 상태 생성
                RecordUndo("Paste State");
                var newState = new YisoCharacterState();
                string baseName = _copiedStateName + " (Copy)";
                string name = baseName;
                int c = 1;
                while (_states.Any(s => s.StateName == name))
                    name = baseName + " " + c++;

                FSet(newState, _stateNameField, name);
                FSet(newState, _transitionsField, new List<YisoCharacterTransition>());
                FSet(newState, _fOnEnter, new List<YisoCharacterAction>());
                FSet(newState, _fOnUpdate, new List<YisoCharacterAction>());
                FSet(newState, _fOnExit, new List<YisoCharacterAction>());
                newState.editorNodePosition = graphPos;
                _states.Add(newState);
                _selectedStateIdx = _states.Count - 1;
                MarkDirty();
            }
        }

        #endregion

        #region Auto Layout

        private void AutoLayout() {
            if (_states == null || _states.Count == 0) return;
            RecordUndo("Auto Layout");

            // 초기 상태 찾기
            int initialIdx = _states.FindIndex(s => s.StateName == _initialStateName);

            // BFS 기반 레이어 할당
            var layer = new Dictionary<int, int>();
            var queue = new Queue<int>();

            if (initialIdx >= 0) {
                queue.Enqueue(initialIdx);
                layer[initialIdx] = 0;
            }

            while (queue.Count > 0) {
                int cur = queue.Dequeue();
                int curLayer = layer[cur];
                var trans = GetTransitions(cur);
                if (trans == null) continue;

                foreach (var t in trans) {
                    foreach (int ti in GetTargetIndices(t)) {
                        if (ti >= 0 && !layer.ContainsKey(ti)) {
                            layer[ti] = curLayer + 1;
                            queue.Enqueue(ti);
                        }
                    }
                }
            }

            // 미연결 상태 처리
            int maxLayer = layer.Count > 0 ? layer.Values.Max() + 1 : 0;
            for (int i = 0; i < _states.Count; i++)
                if (!layer.ContainsKey(i)) layer[i] = maxLayer;

            // 레이어별 배치
            float hSpacing = 230f;
            float vSpacing = 130f;

            var groups = layer.GroupBy(kv => kv.Value).OrderBy(g => g.Key);
            foreach (var g in groups) {
                var indices = g.Select(kv => kv.Key).ToList();
                float totalW = (indices.Count - 1) * hSpacing;
                float startX = -totalW / 2f;
                for (int i = 0; i < indices.Count; i++) {
                    _states[indices[i]].editorNodePosition = new Vector2(
                        startX + i * hSpacing,
                        g.Key * vSpacing);
                }
            }

            MarkDirty();
            CenterView();
        }

        private void FitToView(Rect graphRect) {
            if (_states == null || _states.Count == 0) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var s in _states) {
                var p = s.editorNodePosition;
                minX = Mathf.Min(minX, p.x - NODE_WIDTH / 2f);
                maxX = Mathf.Max(maxX, p.x + NODE_WIDTH / 2f);
                minY = Mathf.Min(minY, p.y - NODE_HEIGHT / 2f);
                maxY = Mathf.Max(maxY, p.y + NODE_HEIGHT / 2f);
            }

            float gw = maxX - minX + 80f;
            float gh = maxY - minY + 80f;
            _zoom = Mathf.Clamp(Mathf.Min(graphRect.width / gw, graphRect.height / gh), MIN_ZOOM, MAX_ZOOM);

            Vector2 center = new((minX + maxX) / 2f, (minY + maxY) / 2f);
            _panOffset = new Vector2(graphRect.width / 2f, graphRect.height / 2f) - center * _zoom;
        }

        #endregion

        #region Component Creation Helper

        private void ShowAddComponentMenu<T>(Type[] types, string[] names, Action<Component> onCreated)
            where T : Component {
            var menu = new GenericMenu();
            if (types == null || types.Length == 0) {
                menu.AddDisabledItem(new GUIContent($"No {typeof(T).Name} types found"));
            } else {
                for (int i = 0; i < types.Length; i++) {
                    int idx = i;
                    menu.AddItem(new GUIContent(names[i]), false, () => {
                        var childGo = new GameObject(names[idx]);
                        childGo.transform.SetParent(_stateMachine.transform);
                        Undo.RegisterCreatedObjectUndo(childGo, "Create " + names[idx]);
                        var comp = childGo.AddComponent(types[idx]);
                        onCreated?.Invoke(comp);
                    });
                }
            }
            menu.ShowAsContext();
        }

        #endregion

        #region Coordinate Helpers

        // Graph -> Screen (클립 로컬 좌표)
        private Vector2 G2S(Vector2 graphPos) => graphPos * _zoom + _panOffset;

        // Screen (클립 로컬 좌표) -> Graph
        private Vector2 S2G(Vector2 screenPos) => (screenPos - _panOffset) / _zoom;

        #endregion

        #region Transition Helpers

        private List<YisoCharacterTransition> GetTransitions(int stateIdx) {
            if (stateIdx < 0 || stateIdx >= _states.Count) return null;
            return FGet<List<YisoCharacterTransition>>(_states[stateIdx], _transitionsField);
        }

        private int FindStateIndex(string name) {
            if (string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < _states.Count; i++)
                if (_states[i].StateName == name) return i;
            return -1;
        }

        /// <summary>
        /// 전이의 목적지 상태 인덱스 목록 반환 (랜덤 전이 대응)
        /// </summary>
        private List<int> GetTargetIndices(YisoCharacterTransition t) {
            var result = new List<int>();
            bool isRandom = FGet<bool>(t, _fRandom);
            if (isRandom) {
                var names = FGet<List<string>>(t, _fNextStates);
                if (names != null) result.AddRange(names.Select(FindStateIndex));
            } else {
                result.Add(FindStateIndex(FGet<string>(t, _fNextState)));
            }
            return result;
        }

        private string GetTransitionLabel(YisoCharacterTransition t) {
            bool isRandom = FGet<bool>(t, _fRandom);
            if (!isRandom) return FGet<string>(t, _fNextState) ?? "(none)";
            var names = FGet<List<string>>(t, _fNextStates);
            if (names == null || names.Count == 0) return "(random: empty)";
            return $"(random: {string.Join(", ", names)})";
        }

        #endregion

        #region Reflection

        private static void CacheReflection() {
            if (_reflectionCached) return;
            _reflectionCached = true;

            const BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var smT = typeof(YisoCharacterStateMachine);
            var stT = typeof(YisoCharacterState);
            var trT = typeof(YisoCharacterTransition);

            _fStates = smT.GetField("states", bf);
            _fInitialState = smT.GetField("initialState", bf);

            _fStateName = stT.GetField("stateName", bf);
            _transitionsField = stT.GetField("transitions", bf);
            _fOnEnter = stT.GetField("onEnterActions", bf);
            _fOnUpdate = stT.GetField("onUpdateActions", bf);
            _fOnExit = stT.GetField("onExitActions", bf);

            _fRandom = trT.GetField("random", bf);
            _fNextState = trT.GetField("nextState", bf);
            _fNextStates = trT.GetField("nextStates", bf);
            _fConditions = trT.GetField("conditions", bf);
        }

        private static T FGet<T>(object obj, FieldInfo fi) {
            if (fi == null || obj == null) return default;
            return (T)fi.GetValue(obj);
        }

        private static void FSet(object obj, FieldInfo fi, object val) {
            if (fi == null || obj == null) return;
            fi.SetValue(obj, val);
        }

        // CreateState, RenameState에서 사용하는 별칭
        private static FieldInfo _stateNameField => _fStateName;

        #endregion

        #region Type Cache

        private void CacheTypes() {
            var aList = new List<Type>();
            var dList = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    foreach (var t in asm.GetTypes()) {
                        if (!t.IsAbstract && !t.IsGenericType) {
                            if (typeof(YisoCharacterAction).IsAssignableFrom(t)) aList.Add(t);
                            if (typeof(YisoCharacterDecision).IsAssignableFrom(t)) dList.Add(t);
                        }
                    }
                } catch {
                    // ReflectionTypeLoadException 등 무시
                }
            }

            aList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            dList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            _actionTypes = aList.ToArray();
            _actionNames = aList.Select(t => t.Name).ToArray();
            _decisionTypes = dList.ToArray();
            _decisionNames = dList.Select(t => t.Name).ToArray();
        }

        #endregion
    }
}
#endif
