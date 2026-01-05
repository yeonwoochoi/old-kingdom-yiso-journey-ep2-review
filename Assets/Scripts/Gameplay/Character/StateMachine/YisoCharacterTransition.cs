using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Character.StateMachine {
    [Serializable]
    public class YisoCharacterTransition {
        [Title("Destination")] [SerializeField]
        private bool random;

        [SerializeField, ShowIf("random")] private List<string> nextStates;
        [SerializeField, ShowIf("@!random")] private string nextState;

        [Title("Conditions (AND Logic)")] [Tooltip("리스트에 있는 모든 조건(Condition)이 True여야 전환됩니다.")] [SerializeField]
        private List<TransitionCondition> conditions;

        /// <summary>
        /// 다음 상태 이름 반환 (랜덤 처리 포함)
        /// </summary>
        public string NextState {
            get {
                if (!random) return nextState;
                if (nextStates == null || nextStates.Count == 0) return null;
                var nextRandomIndex = Random.Range(0, nextStates.Count);
                return nextStates[nextRandomIndex];
            }
        }

        public bool CanTransition() {
            if (conditions == null || conditions.Count == 0) return true; // 조건이 없으면 즉시 이동
            foreach (var decision in conditions) {
                if (decision == null || !decision.Decide()) {
                    return false;
                }
            }

            return true;
        }

        public void OnEnterState() {
            if (conditions == null) return;
            foreach (var decision in conditions) {
                if (decision != null) decision.OnEnterState();
            }
        }

        public void OnExitState() {
            if (conditions == null) return;
            foreach (var decision in conditions) {
                if (decision != null) decision.OnExitState();
            }
        }

        /// <summary>
        /// Decision을 감싸는 래퍼 클래스. 논리 연산(Not, And, Or)을 지원.
        /// </summary>
        [Serializable]
        public class TransitionCondition {
            public enum LogicMode {
                Single, // 단일 Decision (Leaf Node)
                And, // 모든 하위 조건이 True (Branch Node)
                Or // 하나라도 True (Branch Node)
            }

            // 인스펙터 가독성을 위한 헤더 그룹
            [HorizontalGroup("H", 0.2f), HideLabel]
            public bool invertResult = false;

            [HorizontalGroup("H", 0.8f), HideLabel] [OnValueChanged("OnModeChanged")] // 모드 변경 시 리스트 초기화 등 편의기능 (선택사항)
            public LogicMode mode = LogicMode.Single;

            // --- Single Mode (Leaf) ---
            [ShowIf("@mode == LogicMode.Single")] [LabelText("Decision")] [Indent] // 들여쓰기로 계층 구조 시각화
            public YisoCharacterDecision singleDecision;

            // --- And / Or Mode (Branch) ---
            // [핵심] 자기 자신(TransitionCondition)을 리스트로 가짐 -> 재귀 구조
            [ShowIf("@mode == LogicMode.And || mode == LogicMode.Or")]
            [LabelText("@this.mode + \" Group\"")]
            [ListDrawerSettings(ShowIndexLabels = false, CustomAddFunction = "CreateDefaultElement")]
            [Indent]
            [SerializeReference]        // 재귀 구조 직렬화 허용 (참조 방식으로 저장)
            [HideReferenceObjectPicker] // "Select Type" 드롭다운 숨기기 (타입이 하나뿐이므로)
            public List<TransitionCondition> subConditions;

            public bool Decide() {
                var result = false;

                switch (mode) {
                    case LogicMode.Single:
                        // [방어 코드] Decision이 비어있으면 true로 칠지 false로 칠지 정책 결정 (여기선 false)
                        result = singleDecision != null && singleDecision.Decide();
                        break;
                    case LogicMode.And:
                        result = CheckAnd();
                        break;
                    case LogicMode.Or:
                        result = CheckOr();
                        break;
                }

                return invertResult ? !result : result;
            }

            private bool CheckAnd() {
                if (subConditions == null || subConditions.Count == 0) return true;
                foreach (var condition in subConditions) {
                    // [재귀 호출] 하위 조건의 Decide를 호출
                    if (condition == null || !condition.Decide()) {
                        return false;
                    }
                }

                return true;
            }

            private bool CheckOr() {
                if (subConditions == null || subConditions.Count == 0) return true;
                foreach (var condition in subConditions) {
                    // [재귀 호출] 하위 조건의 Decide를 호출
                    if (condition != null && condition.Decide()) {
                        return true;
                    }
                }

                return false;
            }

            public void OnEnterState() {
                if (mode == LogicMode.Single) {
                    singleDecision?.OnEnterState();
                }
                else {
                    if (subConditions == null) return;
                    foreach (var condition in subConditions) {
                        condition?.OnEnterState(); // 재귀 전파
                    }
                }
            }

            public void OnExitState() {
                if (mode == LogicMode.Single) {
                    singleDecision?.OnExitState();
                }
                else {
                    if (subConditions == null) return;
                    foreach (var condition in subConditions) {
                        condition?.OnExitState(); // 재귀 전파
                    }
                }
            }

            // --- Odin Inspector 편의 기능 (선택) ---

            // 리스트에 새 요소 추가할 때 기본값 설정
            private TransitionCondition CreateDefaultElement() {
                return new TransitionCondition {
                    mode = LogicMode.Single,
                    invertResult = false
                };
            }

            // 인스펙터에서 이 클래스가 접혀있을 때 보여줄 요약 텍스트
            // 예: "!TargetIsAlive" 또는 "And Group (3)"
            public override string ToString() {
                string prefix = invertResult ? "NOT " : "";
                if (mode == LogicMode.Single) {
                    return prefix + (singleDecision != null
                        ? singleDecision.GetType().Name.Replace("YisoCharacterDecision", "")
                        : "None");
                }

                return $"{prefix}{mode} Group ({(subConditions != null ? subConditions.Count : 0)})";
            }
            
            private void OnModeChanged() {
                // Single -> Group으로 바꿀 때 리스트가 없으면 자동 생성
                if (mode == LogicMode.And || mode == LogicMode.Or) {
                    if (subConditions == null) {
                        subConditions = new List<TransitionCondition>();
                    }
                }
            }
        }
    }
}