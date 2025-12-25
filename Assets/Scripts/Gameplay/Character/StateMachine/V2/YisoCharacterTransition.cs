using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Character.StateMachine.V2 {
    [Serializable]
    public class YisoCharacterTransition {
        [Title("Destination")]
        [SerializeField] private bool random;
        [SerializeField, ShowIf("random")] private List<string> nextStates;
        [SerializeField, ShowIf("@!random")] private string nextState;
        
        [Title("Conditions (AND Logic)")]
        [Tooltip("리스트에 있는 모든 조건(Condition)이 True여야 전환됩니다.")]
        [SerializeField] private List<TransitionCondition> conditions;

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
                Single, // 단일 Decision
                And,    // 모든 Decision이 True여야 함
                Or      // 하나라도 True면 됨
            }
            
            [HorizontalGroup("Header", 0.7f), HideLabel]
            public LogicMode mode = LogicMode.Single;

            [HorizontalGroup("Header", 0.3f), HideLabel]
            public bool invertResult = false;
            
            // --- Single Mode ---
            [ShowIf("@mode == LogicMode.Single")]
            [HideLabel]
            public YisoCharacterDecision singleDecision;
            
            // --- And / Or Mode ---
            [ShowIf("@mode == LogicMode.And || mode == LogicMode.Or")]
            [LabelText("$mode Decisions")] // "And Decisions" or "Or Decisions" 동적 라벨
            public List<YisoCharacterDecision> subDecisions;

            public bool Decide() {
                var result = false;

                switch (mode) {
                    case LogicMode.Single:
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
                if (subDecisions == null || subDecisions.Count == 0) return true;
                foreach (var subDecision in subDecisions) {
                    if (subDecision == null || !subDecision.Decide()) {
                        return false;
                    }
                }
                return true;
            }

            private bool CheckOr() {
                if (subDecisions == null || subDecisions.Count == 0) return true;
                foreach (var subDecision in subDecisions) {
                    if (subDecision != null && subDecision.Decide()) {
                        return true;
                    }
                }
                return false;
            }

            public void OnEnterState() {
                if (mode == LogicMode.Single) {
                    singleDecision?.OnEnterState();
                } else {
                    if (subDecisions == null) return;
                    foreach (var subDecision in subDecisions) {
                        subDecision?.OnEnterState();
                    }
                }
            }

            public void OnExitState() {
                if (mode == LogicMode.Single) {
                    singleDecision?.OnExitState();
                } else {
                    if (subDecisions == null) return;
                    foreach (var subDecision in subDecisions) {
                        subDecision?.OnExitState();
                    }
                }
            }
        }
    }
}