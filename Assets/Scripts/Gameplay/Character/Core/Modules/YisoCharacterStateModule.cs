using System;
using System.Collections.Generic;
using Gameplay.Character.StateMachine;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

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
            if (_settings.randomizeFrequencies) {
                // Settings에 저장된 값을 랜덤 범위 내의 새로운 값으로 덮어쓴다.
                // 이렇게 하면 이 모듈 인스턴스는 자신만의 랜덤한 주기를 갖게 됨.
                _settings.decisionCheckFrequency = Random.Range(_settings.randomDecisionFrequency.x,
                    _settings.randomDecisionFrequency.y);
                _settings.actionExecuteFrequency =
                    Random.Range(_settings.randomActionFrequency.x, _settings.randomActionFrequency.y);
            }

            if (_settings.stateMachine == null) {
                Debug.LogError($"[StateModule] '{Context.GameObject.name}' is not assigned!");
                return;
            }

            // 1. 상태 캐시 초기화 및 빌드
            _stateCache = new Dictionary<string, YisoCharacterStateSO>();
            if (_settings.stateMachine.allAvailableStates != null) {
                foreach (var state in _settings.stateMachine.allAvailableStates) {
                    if (state == null) continue;

                    var key = state.StateKey;

                    if (string.IsNullOrEmpty(key)) {
                        Debug.LogWarning($"상태 '{state.name}'의 키가 비어있어 캐시할 수 없습니다.", state);
                        continue;
                    }

                    if (!_stateCache.ContainsKey(key)) {
                        _stateCache.Add(key, state);
                    }
                    else {
                        Debug.LogWarning($"중복된 상태 키('{key}')가 감지되었습니다. 첫 번째 상태만 사용됩니다.");
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
            // Player는 Input 값에 맞게 직접 RequestStateChange 호출
            // AI만 체크해서 상태 전이
            if (Context.IsAIControlled) {
                if (Time.time - _lastDecisionCheckTime > _settings.decisionCheckFrequency) {
                    if (CurrentState.CheckTransitions(Context, out var nextState)) {
                        RequestStateChange(nextState);
                    }

                    _lastDecisionCheckTime = Time.time;
                }
            }

            // 2. 상태 업데이트 액션(Action) 실행 주기
            if (Time.time - _lastActionExecuteTime > _settings.actionExecuteFrequency) {
                CurrentState.OnUpdate(Context);
                _lastActionExecuteTime = Time.time;
            }
        }

        public void RequestStateChangeByRole(YisoStateRole role) {
            RequestStateChangeByKey(role.ToString());
        }

        public void RequestStateChangeByKey(string stateKey) {
            if (string.IsNullOrEmpty(stateKey)) return;
            var targetState = FindStateByName(stateKey);
            if (targetState != null) {
                RequestStateChange(targetState);
            }
            else {
                Debug.LogError($"'{stateKey}' 키를 가진 상태를 찾을 수 없습니다.");
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
            [Header("State Machine")] [Tooltip("이 캐릭터가 사용할 상태 머신 '설계도'.")]
            public YisoCharacterStateMachineSO stateMachine;

            [Tooltip("전환 조건을 얼마나 자주 체크할지 결정한다 (초 단위). 낮을수록 반응성이 높지만 연산량이 많아진다.")]
            public float decisionCheckFrequency = 0.1f;

            [Tooltip("현재 상태의 Update 액션을 얼마나 자주 실행할지 결정한다 (초 단위).")]
            public float actionExecuteFrequency = 0f;

            [Tooltip("활성화 시, 실행 주기를 지정된 범위 내에서 랜덤으로 설정한다.")]
            public bool randomizeFrequencies = false;

            [Tooltip("decisionCheckFrequency의 랜덤 범위.")] [ShowIf("randomizeFrequencies")]
            public Vector2 randomDecisionFrequency = new(0.1f, 0.3f);

            [Tooltip("ActionExecuteFrequency의 랜덤 범위.")] [ShowIf("randomizeFrequencies")]
            public Vector2 randomActionFrequency = new(0f, 0.1f);
        }
    }
}