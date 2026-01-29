using System;
using System.Collections.Generic;
using System.Linq;
using Editor.FSM.Utils;
using Gameplay.Character.StateMachine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.FSM.Views {
    /// <summary>
    /// FSM 에디터의 Inspector 패널
    /// 선택된 State 또는 Transition의 세부 정보를 표시하고 편집
    /// </summary>
    public class YisoFSMInspectorPanel : VisualElement {
        private readonly YisoFSMEditorWindow _window;

        private VisualElement _contentContainer;
        private YisoStateNodeView _selectedState;
        private YisoTransitionEdgeView _selectedEdge;

        public YisoFSMInspectorPanel(YisoFSMEditorWindow window) {
            _window = window;

            // 기본 스타일
            AddToClassList("yiso-fsm-inspector");
            style.width = 380;
            style.minWidth = 350;
            style.maxWidth = 500;
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);

            // 헤더
            var header = new VisualElement();
            header.AddToClassList("yiso-fsm-inspector-header");
            header.style.backgroundColor = new Color(0.23f, 0.23f, 0.23f);
            header.style.paddingLeft = 8;
            header.style.paddingTop = 8;
            header.style.paddingBottom = 8;

            var headerLabel = new Label("Inspector");
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.fontSize = 14;
            header.Add(headerLabel);
            Add(header);

            // 콘텐츠 컨테이너 (스크롤 가능)
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;

            _contentContainer = new VisualElement();
            _contentContainer.style.paddingLeft = 8;
            _contentContainer.style.paddingRight = 8;
            _contentContainer.style.paddingTop = 8;
            scrollView.Add(_contentContainer);

            Add(scrollView);

            // 초기 상태
            ShowEmptyMessage();
        }

        public void ClearSelection() {
            _selectedState = null;
            _selectedEdge = null;
            ShowEmptyMessage();
        }

        private void ShowEmptyMessage() {
            _contentContainer.Clear();
            var message = new Label("State 또는 Transition을 선택하세요");
            message.style.color = new Color(0.5f, 0.5f, 0.5f);
            message.style.unityTextAlign = TextAnchor.MiddleCenter;
            message.style.marginTop = 50;
            _contentContainer.Add(message);
        }

        #region State Inspector

        public void ShowStateInspector(YisoStateNodeView nodeView) {
            _selectedState = nodeView;
            _selectedEdge = null;

            _contentContainer.Clear();

            if (nodeView?.State == null) {
                ShowEmptyMessage();
                return;
            }

            var state = nodeView.State;

            // State 이름
            CreateStateNameSection(state);

            // 초기 상태 여부
            CreateInitialStateSection(state);

            // OnEnter Actions
            CreateActionSection("OnEnter Actions", state, "onEnterActions");

            // OnUpdate Actions
            CreateActionSection("OnUpdate Actions", state, "onUpdateActions");

            // OnExit Actions
            CreateActionSection("OnExit Actions", state, "onExitActions");

            // Transitions
            CreateTransitionsSection(state);
        }

        private void CreateStateNameSection(YisoCharacterState state) {
            var section = CreateSection("State 설정");

            var nameField = new TextField("이름");
            nameField.value = state.StateName;
            nameField.isReadOnly = true; // 현재는 읽기 전용 (이름 변경은 복잡한 참조 업데이트 필요)
            nameField.style.opacity = 0.7f;
            section.Add(nameField);

            _contentContainer.Add(section);
        }

        private void CreateInitialStateSection(YisoCharacterState state) {
            var initialState = _window.GetInitialStateName();
            bool isInitial = state.StateName == initialState;

            var section = CreateSection("상태");

            var initialToggle = new Toggle("초기 상태");
            initialToggle.value = isInitial;
            initialToggle.RegisterValueChangedCallback(evt => {
                if (evt.newValue) {
                    _window.SetInitialState(state.StateName);
                }
            });
            section.Add(initialToggle);

            _contentContainer.Add(section);
        }

        private void CreateActionSection(string title, YisoCharacterState state, string fieldName) {
            var section = CreateSection(title);

            var field = typeof(YisoCharacterState).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var actions = field?.GetValue(state) as List<YisoCharacterAction>;

            if (actions == null || actions.Count == 0) {
                var emptyLabel = new Label("(없음)");
                emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                section.Add(emptyLabel);
            }
            else {
                for (int i = 0; i < actions.Count; i++) {
                    var actionItem = CreateActionItem(actions[i], state, fieldName, i);
                    section.Add(actionItem);
                }
            }

            // 추가 버튼
            var addButton = new Button(() => ShowAddActionPopup(state, fieldName));
            addButton.text = "+ Action 추가";
            addButton.style.marginTop = 8;
            section.Add(addButton);

            _contentContainer.Add(section);
        }

        private VisualElement CreateActionItem(YisoCharacterAction action, YisoCharacterState state, string fieldName, int index) {
            var container = new VisualElement();
            container.AddToClassList("action-item");
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.marginTop = 2;
            container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;

            // 첫 번째 행: ObjectField + 버튼들
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            // Action ObjectField (기존 Action 선택 가능)
            var actionField = new ObjectField();
            actionField.objectType = typeof(YisoCharacterAction);
            actionField.value = action;
            actionField.style.flexGrow = 1;
            actionField.RegisterValueChangedCallback(evt => {
                ReplaceActionInList(state, fieldName, index, evt.newValue as YisoCharacterAction);
            });
            row.Add(actionField);

            // 새 Action 생성 버튼
            var addButton = new Button(() => ShowReplaceActionPopup(state, fieldName, index));
            addButton.text = "+";
            addButton.tooltip = "새 Action 생성";
            addButton.style.width = 24;
            addButton.style.height = 20;
            addButton.style.marginLeft = 4;
            row.Add(addButton);

            // 선택 버튼 (Inspector에서 해당 컴포넌트 선택)
            if (action != null) {
                var selectButton = new Button(() => {
                    Selection.activeObject = action;
                    EditorGUIUtility.PingObject(action);
                });
                selectButton.text = "▶";
                selectButton.tooltip = "Select in Inspector";
                selectButton.style.width = 24;
                selectButton.style.height = 20;
                selectButton.style.marginLeft = 2;
                row.Add(selectButton);
            }

            // 삭제 버튼
            var deleteButton = new Button(() => RemoveAction(action, state, fieldName));
            deleteButton.text = "×";
            deleteButton.tooltip = "Remove from list";
            deleteButton.style.width = 24;
            deleteButton.style.height = 20;
            deleteButton.style.marginLeft = 2;
            deleteButton.style.color = new Color(1f, 0.4f, 0.4f);
            row.Add(deleteButton);

            container.Add(row);

            // 두 번째 행: Action 정보 표시
            if (action != null) {
                var infoLabel = new Label($"  └ {action.gameObject.name}");
                infoLabel.style.fontSize = 10;
                infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                container.Add(infoLabel);
            }

            return container;
        }

        private void ReplaceActionInList(YisoCharacterState state, string fieldName, int index, YisoCharacterAction newAction) {
            var field = typeof(YisoCharacterState).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var actions = field?.GetValue(state) as List<YisoCharacterAction>;
            if (actions == null || index < 0 || index >= actions.Count) return;

            Undo.RecordObject(_window.CurrentFSM, "Replace Action");
            actions[index] = newAction;
            _window.MarkDirty();
        }

        private void ShowReplaceActionPopup(YisoCharacterState state, string fieldName, int index) {
            var actionTypes = YisoFSMPrefabHelper.FindAllActionTypes();

            var menu = new GenericMenu();
            foreach (var type in actionTypes) {
                string displayName = type.Name.Replace("YisoCharacterAction", "");
                menu.AddItem(new GUIContent(displayName), false, () => {
                    CreateNewActionAndReplace(state, fieldName, index, type);
                });
            }
            menu.ShowAsContext();
        }

        private void CreateNewActionAndReplace(YisoCharacterState state, string fieldName, int index, Type actionType) {
            if (_window.PrefabContents == null) {
                Debug.LogError("[FSM Editor] Prefab이 로드되지 않았습니다.");
                return;
            }

            // Prefab에 새 Action 컴포넌트 추가
            var newAction = YisoFSMPrefabHelper.AddAction(_window.PrefabContents, actionType);
            if (newAction == null) {
                Debug.LogError($"[FSM Editor] Action 생성 실패: {actionType.Name}");
                return;
            }

            // 리스트에서 교체
            ReplaceActionInList(state, fieldName, index, newAction);
            ShowStateInspector(_selectedState); // UI 새로고침
        }

        private void ShowAddActionPopup(YisoCharacterState state, string fieldName) {
            var actionTypes = YisoFSMPrefabHelper.FindAllActionTypes();

            var menu = new GenericMenu();
            foreach (var type in actionTypes) {
                string displayName = type.Name.Replace("YisoCharacterAction", "");
                menu.AddItem(new GUIContent(displayName), false, () => {
                    AddActionToState(state, fieldName, type);
                });
            }
            menu.ShowAsContext();
        }

        private void AddActionToState(YisoCharacterState state, string fieldName, Type actionType) {
            if (_window.PrefabContents == null) return;

            // Prefab에 Action 컴포넌트 추가
            var newAction = YisoFSMPrefabHelper.AddAction(_window.PrefabContents, actionType);
            if (newAction == null) return;

            // State에 참조 추가
            var field = typeof(YisoCharacterState).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var actions = field?.GetValue(state) as List<YisoCharacterAction>;
            if (actions == null) {
                actions = new List<YisoCharacterAction>();
                field?.SetValue(state, actions);
            }

            Undo.RecordObject(_window.CurrentFSM, "Add Action");
            actions.Add(newAction);

            _window.MarkDirty();
            ShowStateInspector(_selectedState); // 새로고침
        }

        private void RemoveAction(YisoCharacterAction action, YisoCharacterState state, string fieldName) {
            if (action == null) return;

            var field = typeof(YisoCharacterState).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var actions = field?.GetValue(state) as List<YisoCharacterAction>;
            if (actions == null) return;

            Undo.RecordObject(_window.CurrentFSM, "Remove Action");
            actions.Remove(action);

            _window.MarkDirty();
            ShowStateInspector(_selectedState); // 새로고침
        }

        private void CreateTransitionsSection(YisoCharacterState state) {
            var section = CreateSection("Transitions");

            var transitionsField = typeof(YisoCharacterState).GetField("transitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var transitions = transitionsField?.GetValue(state) as List<YisoCharacterTransition>;

            if (transitions == null || transitions.Count == 0) {
                var emptyLabel = new Label("(없음)");
                emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                section.Add(emptyLabel);
            }
            else {
                for (int i = 0; i < transitions.Count; i++) {
                    var transition = transitions[i];
                    var transitionItem = CreateTransitionSummaryItem(transition, state, i);
                    section.Add(transitionItem);
                }
            }

            _contentContainer.Add(section);
        }

        private VisualElement CreateTransitionSummaryItem(YisoCharacterTransition transition, YisoCharacterState state, int index) {
            var container = new VisualElement();
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 6;
            container.style.paddingBottom = 6;
            container.style.marginTop = 4;
            container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;

            // 목적지
            string destination = GetTransitionDestination(transition);
            var destLabel = new Label($"→ {destination}");
            destLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(destLabel);

            // 조건 요약
            var conditionsField = typeof(YisoCharacterTransition).GetField("conditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var conditions = conditionsField?.GetValue(transition) as List<YisoCharacterTransition.TransitionCondition>;

            string conditionText = conditions == null || conditions.Count == 0
                ? "조건 없음 (항상)"
                : $"조건 {conditions.Count}개";

            var condLabel = new Label(conditionText);
            condLabel.style.fontSize = 10;
            condLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            container.Add(condLabel);

            return container;
        }

        private string GetTransitionDestination(YisoCharacterTransition transition) {
            var randomField = typeof(YisoCharacterTransition).GetField("random",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isRandom = (bool)(randomField?.GetValue(transition) ?? false);

            if (isRandom) {
                var nextStatesField = typeof(YisoCharacterTransition).GetField("nextStates",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var nextStates = nextStatesField?.GetValue(transition) as List<string>;
                return nextStates != null && nextStates.Count > 0
                    ? $"Random [{string.Join(", ", nextStates)}]"
                    : "(없음)";
            }
            else {
                var nextStateField = typeof(YisoCharacterTransition).GetField("nextState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var nextState = nextStateField?.GetValue(transition) as string;
                return string.IsNullOrEmpty(nextState) ? "(없음)" : nextState;
            }
        }

        #endregion

        #region Transition Inspector

        public void ShowTransitionInspector(YisoTransitionEdgeView edgeView) {
            _selectedState = null;
            _selectedEdge = edgeView;

            _contentContainer.Clear();

            if (edgeView?.Transition == null) {
                ShowEmptyMessage();
                return;
            }

            var transition = edgeView.Transition;

            // Transition 기본 정보
            CreateTransitionInfoSection(edgeView);

            // Conditions
            CreateConditionsSection(transition);
        }

        private void CreateTransitionInfoSection(YisoTransitionEdgeView edgeView) {
            var section = CreateSection("Transition 정보");

            // 소스 → 타겟
            var routeLabel = new Label($"{edgeView.SourceNode.State.StateName} → {edgeView.TargetNode.State.StateName}");
            routeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(routeLabel);

            if (edgeView.IsRandom) {
                var randomLabel = new Label("(Random Transition)");
                randomLabel.style.color = new Color(0.67f, 0.53f, 1f);
                section.Add(randomLabel);
            }

            _contentContainer.Add(section);
        }

        private void CreateConditionsSection(YisoCharacterTransition transition) {
            var section = CreateSection("Conditions (AND Logic)");

            var conditionsField = typeof(YisoCharacterTransition).GetField("conditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var conditions = conditionsField?.GetValue(transition) as List<YisoCharacterTransition.TransitionCondition>;

            if (conditions == null || conditions.Count == 0) {
                var emptyLabel = new Label("조건 없음 - 항상 전환됩니다");
                emptyLabel.style.color = new Color(1f, 0.6f, 0.3f);
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                section.Add(emptyLabel);
            }
            else {
                foreach (var condition in conditions) {
                    var conditionUI = CreateConditionTreeUI(condition, 0);
                    section.Add(conditionUI);
                }
            }

            // 조건 추가 버튼
            var addButton = new Button(() => AddConditionToTransition(transition));
            addButton.text = "+ Condition 추가";
            addButton.style.marginTop = 8;
            section.Add(addButton);

            _contentContainer.Add(section);
        }

        private VisualElement CreateConditionTreeUI(YisoCharacterTransition.TransitionCondition condition, int depth) {
            var container = new VisualElement();
            container.AddToClassList("condition-item");
            container.style.marginLeft = depth * 16;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 6;
            container.style.paddingBottom = 6;
            container.style.marginTop = 4;
            container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            container.style.borderLeftWidth = 3;

            // 모드에 따른 색상
            switch (condition.mode) {
                case YisoCharacterTransition.TransitionCondition.LogicMode.Single:
                    container.style.borderLeftColor = new Color(0.53f, 0.67f, 0.53f);
                    break;
                case YisoCharacterTransition.TransitionCondition.LogicMode.And:
                    container.style.borderLeftColor = new Color(0.53f, 0.67f, 1f);
                    break;
                case YisoCharacterTransition.TransitionCondition.LogicMode.Or:
                    container.style.borderLeftColor = new Color(1f, 0.67f, 0.53f);
                    break;
            }

            if (condition.invertResult) {
                container.style.backgroundColor = new Color(0.31f, 0.19f, 0.19f);
            }

            // 헤더: NOT + Mode
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            var invertToggle = new Toggle("NOT");
            invertToggle.value = condition.invertResult;
            invertToggle.style.width = 50;
            invertToggle.RegisterValueChangedCallback(evt => {
                condition.invertResult = evt.newValue;
                _window.MarkDirty();
                ShowTransitionInspector(_selectedEdge);
            });
            header.Add(invertToggle);

            var modeField = new EnumField(condition.mode);
            modeField.style.flexGrow = 1;
            modeField.RegisterValueChangedCallback(evt => {
                condition.mode = (YisoCharacterTransition.TransitionCondition.LogicMode)evt.newValue;
                if (condition.mode != YisoCharacterTransition.TransitionCondition.LogicMode.Single) {
                    if (condition.subConditions == null) {
                        condition.subConditions = new List<YisoCharacterTransition.TransitionCondition>();
                    }
                }
                _window.MarkDirty();
                ShowTransitionInspector(_selectedEdge);
            });
            header.Add(modeField);

            container.Add(header);

            // 내용
            if (condition.mode == YisoCharacterTransition.TransitionCondition.LogicMode.Single) {
                // Decision 선택
                var decisionContainer = new VisualElement();
                decisionContainer.style.marginTop = 4;

                // Decision Field + 추가 버튼 가로 배치
                var decisionRow = new VisualElement();
                decisionRow.style.flexDirection = FlexDirection.Row;
                decisionRow.style.alignItems = Align.Center;

                var decisionField = new ObjectField("Decision");
                decisionField.objectType = typeof(YisoCharacterDecision);
                decisionField.value = condition.singleDecision;
                decisionField.style.flexGrow = 1;
                decisionField.RegisterValueChangedCallback(evt => {
                    condition.singleDecision = evt.newValue as YisoCharacterDecision;
                    _window.MarkDirty();
                });
                decisionRow.Add(decisionField);

                // 새 Decision 생성 버튼
                var addDecisionButton = new Button(() => ShowAddDecisionPopup(condition));
                addDecisionButton.text = "+";
                addDecisionButton.tooltip = "새 Decision 생성";
                addDecisionButton.style.width = 24;
                addDecisionButton.style.height = 20;
                addDecisionButton.style.marginLeft = 4;
                decisionRow.Add(addDecisionButton);

                decisionContainer.Add(decisionRow);

                // 현재 Decision 정보 표시
                if (condition.singleDecision != null) {
                    var infoLabel = new Label($"  └ {condition.singleDecision.gameObject.name}");
                    infoLabel.style.fontSize = 10;
                    infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    decisionContainer.Add(infoLabel);
                }

                container.Add(decisionContainer);
            }
            else {
                // Sub-conditions
                if (condition.subConditions != null) {
                    var subContainer = new VisualElement();
                    subContainer.AddToClassList("sub-conditions-container");
                    subContainer.style.marginTop = 4;
                    subContainer.style.marginLeft = 8;
                    subContainer.style.borderLeftWidth = 1;
                    subContainer.style.borderLeftColor = new Color(1f, 1f, 1f, 0.2f);
                    subContainer.style.paddingLeft = 8;

                    foreach (var subCondition in condition.subConditions) {
                        subContainer.Add(CreateConditionTreeUI(subCondition, depth + 1));
                    }

                    // 하위 조건 추가 버튼
                    var addSubButton = new Button(() => {
                        condition.subConditions.Add(new YisoCharacterTransition.TransitionCondition {
                            mode = YisoCharacterTransition.TransitionCondition.LogicMode.Single
                        });
                        _window.MarkDirty();
                        ShowTransitionInspector(_selectedEdge);
                    });
                    addSubButton.text = "+ Sub-condition";
                    addSubButton.style.marginTop = 4;
                    subContainer.Add(addSubButton);

                    container.Add(subContainer);
                }
            }

            return container;
        }

        private void AddConditionToTransition(YisoCharacterTransition transition) {
            var conditionsField = typeof(YisoCharacterTransition).GetField("conditions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var conditions = conditionsField?.GetValue(transition) as List<YisoCharacterTransition.TransitionCondition>;
            if (conditions == null) {
                conditions = new List<YisoCharacterTransition.TransitionCondition>();
                conditionsField?.SetValue(transition, conditions);
            }

            conditions.Add(new YisoCharacterTransition.TransitionCondition {
                mode = YisoCharacterTransition.TransitionCondition.LogicMode.Single
            });

            _window.MarkDirty();
            ShowTransitionInspector(_selectedEdge);
        }

        private void ShowAddDecisionPopup(YisoCharacterTransition.TransitionCondition condition) {
            var decisionTypes = YisoFSMPrefabHelper.FindAllDecisionTypes();

            var menu = new GenericMenu();
            foreach (var type in decisionTypes) {
                string displayName = type.Name.Replace("YisoCharacterDecision", "");
                menu.AddItem(new GUIContent(displayName), false, () => {
                    CreateNewDecisionForCondition(condition, type);
                });
            }
            menu.ShowAsContext();
        }

        private void CreateNewDecisionForCondition(YisoCharacterTransition.TransitionCondition condition, Type decisionType) {
            if (_window.PrefabContents == null) {
                Debug.LogError("[FSM Editor] Prefab이 로드되지 않았습니다.");
                return;
            }

            // Prefab에 새 Decision 컴포넌트 추가 (Decisions/ 하위에 새 GameObject 생성)
            var newDecision = YisoFSMPrefabHelper.AddDecision(_window.PrefabContents, decisionType);
            if (newDecision == null) {
                Debug.LogError($"[FSM Editor] Decision 생성 실패: {decisionType.Name}");
                return;
            }

            // Condition에 할당
            Undo.RecordObject(_window.CurrentFSM, "Add Decision");
            condition.singleDecision = newDecision;

            _window.MarkDirty();
            ShowTransitionInspector(_selectedEdge); // UI 새로고침
        }

        #endregion

        #region 유틸리티

        private VisualElement CreateSection(string title) {
            var section = new VisualElement();
            section.AddToClassList("yiso-fsm-inspector-section");
            section.style.marginBottom = 12;
            section.style.paddingLeft = 8;
            section.style.paddingRight = 8;
            section.style.paddingTop = 8;
            section.style.paddingBottom = 8;
            section.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            section.style.borderBottomLeftRadius = 4;
            section.style.borderBottomRightRadius = 4;
            section.style.borderTopLeftRadius = 4;
            section.style.borderTopRightRadius = 4;

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            titleLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            section.Add(titleLabel);

            return section;
        }

        #endregion
    }
}
