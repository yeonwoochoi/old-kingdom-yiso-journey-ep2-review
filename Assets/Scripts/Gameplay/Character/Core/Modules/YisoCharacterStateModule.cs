using System;
using System.Collections.Generic;
using Gameplay.Character.StateMachine;
using UnityEngine;

namespace Gameplay.Character.Core.Modules {
    public sealed class YisoCharacterStateModule : YisoCharacterModuleBase {
        private readonly Settings _settings;
        
        public YisoCharacterStateModule(IYisoCharacterContext context, Settings settings) : base(context) {
            _settings = settings;
        }
        
        public YisoCharacterStateSO CurrentState { get; private set; }
        public float TimeInCurrentState { get; private set; }
        private float _lastDecisionCheckTime;
        private float _lastActionExecuteTime;
        
        private Dictionary<string, YisoCharacterStateSO> _stateCache;

        public override void Initialize() {
            if (_settings.stateMachine == null) {
                Debug.LogError($"[StateModule] '{Context.GameObject.name}' is not assigned!");
                return;
            }
            
            // 1. 상태 캐시 초기화 및 빌드
            _stateCache = new Dictionary<string, YisoCharacterStateSO>();
            if (_settings.stateMachine.allAvailableStates != null) {
                foreach (var state in _settings.stateMachine.allAvailableStates) {
                    if (state != null && !_stateCache.ContainsKey(state.name)) {
                        _stateCache.Add(state.name, state);
                    }
                }
            }
            
            // 2. 초기 상태 설정
            if (_settings.stateMachine.initialState == null) {
                Debug.LogError($"[StateModule] Initial State is not set in '{Context.GameObject.name}'!");
                return;
            }
            RequestStateChange(_settings.stateMachine.initialState, true);
        }

        public override void OnUpdate() {
            if (CurrentState == null) return;

            TimeInCurrentState += Time.deltaTime;

            // 1. 전이(Transition) 확인 주기
            if (Time.time - _lastDecisionCheckTime > _settings.decisionCheckFrequency) {
                if (CurrentState.CheckTransitions(Context, out var nextState)) {
                    RequestStateChange(nextState);
                }
                _lastDecisionCheckTime = Time.time;
            }

            // 2. 상태 업데이트 액션(Action) 실행 주기
            if (Time.time - _lastActionExecuteTime > _settings.actionExecuteFrequency) {
                CurrentState.OnUpdate(Context);
                _lastActionExecuteTime = Time.time;
            }
        }

        public void RequestStateChange(string newStateName) {
            var targetState = FindStateByName(newStateName);
            if (targetState != null) {
                RequestStateChange(targetState);
            }
        }

        public void RequestStateChange(YisoCharacterStateSO newState, bool force = false) {
            if (!force && (newState == null || newState == CurrentState)) {
                return;
            }

            CurrentState?.OnExit(Context);
            
            CurrentState = newState;
            TimeInCurrentState = 0f;
            
            CurrentState?.OnEnter(Context);
            
            Debug.Log($"[StateModule] State Change to {(CurrentState != null ? CurrentState.name : "null")}");
        }

        private YisoCharacterStateSO FindStateByName(string newStateName) {
            if (_stateCache.TryGetValue(newStateName, out var state)) {
                return state;
            }
            
            Debug.LogError($"[StateModule] Cannot find {newStateName}");
            return null;
        }

        [Serializable]
        public class Settings {
            [Header("State Machine")]
            [Tooltip("이 캐릭터가 사용할 상태 머신 '설계도'.")]
            public YisoCharacterStateMachineSO stateMachine;
            
            [Tooltip("전환 조건을 얼마나 자주 체크할지 결정한다 (초 단위). 낮을수록 반응성이 높지만 연산량이 많아진다.")]
            public float decisionCheckFrequency = 0.1f;
            
            [Tooltip("현재 상태의 Update 액션을 얼마나 자주 실행할지 결정한다 (초 단위).")]
            public float actionExecuteFrequency = 0f;
        }
    }
}