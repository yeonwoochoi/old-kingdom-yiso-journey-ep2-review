using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Character.StateMachine.V2 {
    [Serializable]
    public class YisoCharacterTransition {
        [SerializeField] private bool random;
        [SerializeField, ShowIf("random")] private List<string> nextStates;
        [SerializeField, ShowIf("@!random")] private string nextState;
        [SerializeField] private List<YisoCharacterDecision> decisions;

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
            if (decisions == null || decisions.Count == 0) return true; // 조건이 없으면 즉시 이동
            foreach (var decision in decisions) {
                if (decision == null || !decision.Decide()) {
                    return false;
                }
            }
            return true;
        }
        
        public void OnEnterState() {
            if (decisions == null) return;
            foreach (var decision in decisions) {
                if (decision != null) decision.OnEnterState();
            }
        }

        public void OnExitState() {
            if (decisions == null) return;
            foreach (var decision in decisions) {
                if (decision != null) decision.OnExitState();
            }
        }
    }
}