using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Character.StateMachine {
    public class YisoCharacterStateMachine: RunIBehaviour {
        [Title("Settings")]
        [SerializeField] private string initialState;
        [SerializeField] private bool randomizeFrequencies = true;
        
        // Transition 체크 주기 설정
        [ShowIf("randomizeFrequencies")] 
        [SerializeField] private Vector2 actionFrequencyRange = new Vector2(0.1f, 0.2f); // 범위 내 랜덤 값 주기로 체크
        
        [HideIf("randomizeFrequencies")]
        [SerializeField] private float actionFrequency = 0.1f; // 정해진 주기로 체크
        
        [Title("Target System")]
        [SerializeField, Min(1)] private int maxTargetCount = 10; // 슬롯 개수 (기본 10개)

        [Title("States")]
        [SerializeField] private List<YisoCharacterState> states;

        public YisoCharacterState CurrentState { get; private set; }
        public IYisoCharacterContext Owner { get; private set; }
        public float TimeInCurrentState => Time.time - _lastStateEnterTime;
        public int MaxTargetCount => maxTargetCount;
        
        private float _lastStateEnterTime = 0f;
        private readonly Dictionary<string, YisoCharacterState> _stateMap =  new();

        private float _timer; // frequency 타이머
        private float _currentFrequency = 0f;
        
        private Transform[] _targetSlots;

        /// <summary>
        /// 0번 슬롯 (Main Target)
        /// </summary>
        public Transform MainTarget {
            get {
                if (_targetSlots == null || _targetSlots.Length == 0) return null;
                return _targetSlots[0]; 
            }
        }
        
        /// <summary>
        /// 전체 타겟 슬롯 배열 (읽기 전용으로 노출하거나 필요시 Get 메서드 사용)
        /// </summary>
        public Transform[] TargetSlots => _targetSlots;

        public void PreInitialize(IYisoCharacterContext owner) {
            // Context 찾기
            Owner = owner;
            
            // 타겟 슬롯 메모리 할당 (고정 크기)
            _targetSlots = new Transform[maxTargetCount];
            
            foreach (var state in states) {
                if (!_stateMap.TryAdd(state.StateName, state)) {
                    Debug.LogWarning($"[FSM] {name}: 중복된 상태 이름이 있습니다 -> {state.StateName}");
                }
            }
            
            ResetFrequency();
        }

        public void Initialize() {
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
                
            CurrentState?.PlayUpdateActions();

            _timer += Time.deltaTime;
            if (_timer >= _currentFrequency) {
                _timer = 0f;
                ResetFrequency();
                
                var nextStateKey = CurrentState.CheckTransitions();
                if (!string.IsNullOrEmpty(nextStateKey)) {
                    ChangeState(nextStateKey);
                }
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

        #region Target

        /// <summary>
        /// 특정 인덱스(슬롯)에 타겟을 설정합니다.
        /// index 0 = Main Target
        /// </summary>
        public void SetTarget(int index, Transform target) {
            if (index < 0 || index >= _targetSlots.Length) {
                Debug.LogWarning($"[FSM] 잘못된 타겟 인덱스 접근: {index}. Max: {maxTargetCount}");
                return;
            }
            _targetSlots[index] = target;
        }

        /// <summary>
        /// 특정 인덱스의 타겟을 가져옵니다.
        /// </summary>
        public Transform GetTarget(int index) {
            if (index < 0 || index >= _targetSlots.Length) return null;
            return _targetSlots[index];
        }

        /// <summary>
        /// 특정 인덱스의 타겟을 비웁니다.
        /// </summary>
        public void ClearTarget(int index) {
            if (index < 0 || index >= _targetSlots.Length) return;
            _targetSlots[index] = null;
        }

        /// <summary>
        /// 모든 타겟 슬롯을 초기화합니다.
        /// </summary>
        public void ClearAllTargets() {
            if (_targetSlots == null) return;
            for (int i = 0; i < _targetSlots.Length; i++) {
                _targetSlots[i] = null;
            }
        }
        
        /// <summary>
        /// 해당 슬롯에 유효한 타겟이 있는지 확인 (null 체크 + Destroy 체크)
        /// </summary>
        public bool HasValidTarget(int index) {
            if (index < 0 || index >= _targetSlots.Length) return false;
            return _targetSlots[index] != null;
        }

        #endregion
    }
}