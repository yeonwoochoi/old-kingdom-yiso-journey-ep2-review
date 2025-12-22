using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine.V2 {
    public class YisoCharacterStateMachine: RunIBehaviour {
        [Title("Settings")]
        [SerializeField] private string initialState;
        [SerializeField] private bool randomizeFrequencies = true;
        
        [ShowIf("randomizeFrequencies")] 
        [SerializeField] private Vector2 actionFrequencyRange = new Vector2(0.1f, 0.2f);
        
        [HideIf("randomizeFrequencies")]
        [SerializeField] private float actionFrequency = 0.1f;

        [Title("States")]
        [SerializeField] private List<YisoCharacterState> states;

        public YisoCharacterState CurrentState { get; private set; }
        public IYisoCharacterContext Owner { get; private set; }
        public float TimeInCurrentState => Time.time - _lastStateEnterTime;
        
        private float _lastStateEnterTime = 0f;
        private readonly Dictionary<string, YisoCharacterState> _stateMap =  new();

        private float _timer; // frequency 타이머
        private float _currentFrequency = 0f;

        protected override void Awake() {
            base.Awake();
            
            // Context 찾기
            Owner = GetComponentInParent<IYisoCharacterContext>();
            if (Owner == null) {
                Debug.LogError($"[FSM] {name}: IYisoCharacterContext를 찾을 수 없습니다!");
            }
            
            foreach (var state in states) {
                if (!_stateMap.TryAdd(state.StateName, state)) {
                    Debug.LogWarning($"[FSM] {name}: 중복된 상태 이름이 있습니다 -> {state.StateName}");
                }
            }
            
            ResetFrequency();
        }

        protected override void Start() {
            base.Start();
            if (!string.IsNullOrEmpty(initialState) && _stateMap.ContainsKey(initialState)) {
                ChangeState(initialState, true);
            }
            else {
                Debug.LogError($"[FSM] {name}: 초기 상태({initialState})가 유효하지 않습니다.");
            }
        }

        public override void OnUpdate() {
            base.OnUpdate();
            
            if (CurrentState == null) return;

            _timer += Time.deltaTime;

            if (_timer >= _currentFrequency) {
                _timer = 0f;
                ResetFrequency();
                
                // 1. Transition Check
                var nextStateKey = CurrentState.CheckTransitions();
                if (!string.IsNullOrEmpty(nextStateKey)) {
                    ChangeState(nextStateKey);
                    return; // 상태 바뀌었으면 다음 프레임부터 Update Action 호출됨 (선택사항)
                }
                
                // 2. Update Action
                CurrentState?.PlayUpdateActions();
            }
        }

        public void ChangeState(string newStateName, bool force = false) {
            if (!_stateMap.TryGetValue(newStateName, out var nextState)) {
                Debug.LogError($"[FSM] {name}: 존재하지 않는 상태로 전환 시도 -> {newStateName}");
                return;
            }

            // 같은 상태로의 전환 방지 (Force가 아닐 경우)
            if (!force && CurrentState == nextState) return;

            // 이전 상태 종료
            CurrentState?.PlayExitActions();

            // 상태 교체
            CurrentState = nextState;
            _lastStateEnterTime = Time.time;

            // 새 상태 진입
            CurrentState.PlayEnterActions();
        }

        private void ResetFrequency() {
            _currentFrequency = randomizeFrequencies ? Random.Range(actionFrequencyRange.x, actionFrequencyRange.y) : actionFrequency;
        }
    }
}