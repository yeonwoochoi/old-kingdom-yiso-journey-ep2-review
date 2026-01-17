using System.Collections.Generic;
using Core.Behaviour;
using Gameplay.Character.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Gameplay.Character.StateMachine {
    public class YisoCharacterStateMachine : RunIBehaviour {
        [Title("Settings")] [SerializeField] private string initialState;
        [SerializeField] private bool randomizeFrequencies = true;

        // Transition 체크 주기 설정
        [ShowIf("randomizeFrequencies")] [SerializeField]
        private Vector2 actionFrequencyRange = new Vector2(0.1f, 0.2f); // 범위 내 랜덤 값 주기로 체크

        [HideIf("randomizeFrequencies")] [SerializeField]
        private float actionFrequency = 0.1f; // 정해진 주기로 체크

        [Title("Target System")] [SerializeField, Min(1)]
        private int maxTargetCount = 10; // 슬롯 개수 (기본 10개)

        [Title("States")] [SerializeField] private List<YisoCharacterState> states;

        public YisoCharacterState CurrentState { get; private set; }
        public IYisoCharacterContext Owner { get; private set; }
        public float TimeInCurrentState => Time.time - _lastStateEnterTime;
        public int MaxTargetCount => maxTargetCount;

        /// <summary>
        /// AI 캐릭터가 스폰된 초기 위치 (복귀 판단 등에 사용)
        /// </summary>
        public Vector3 SpawnPosition { get; private set; }

        private float _lastStateEnterTime = 0f;
        private readonly Dictionary<string, YisoCharacterState> _stateMap = new();

        private float _timer; // frequency 타이머
        private float _currentFrequency = 0f;

        private Transform[] _targetSlots;
        private IYisoCharacterContext[] _targetContexts;

        public void PreInitialize(IYisoCharacterContext owner) {
            // Context 찾기
            Owner = owner;

            // 스폰 위치 저장
            SpawnPosition = owner.Transform.position;

            // 타겟 슬롯 메모리 할당 (고정 크기)
            _targetSlots = new Transform[maxTargetCount];
            _targetContexts = new IYisoCharacterContext[maxTargetCount];

            CurrentState = null;
            _lastStateEnterTime = 0f;

            foreach (var state in states) {
                if (!_stateMap.TryAdd(state.StateName, state)) {
                    YisoLogger.LogWarning($"FSM {name}: 중복된 상태 이름 발견 -> {state.StateName}");
                }
            }

            YisoLogger.Log($"FSM PreInitialize 완료: Owner={owner.GameObject.name}, States={states.Count}");
            ResetFrequency();
        }

        public void Initialize() {
            if (!string.IsNullOrEmpty(initialState) && _stateMap.ContainsKey(initialState)) {
                ChangeState(initialState, true);
                YisoLogger.Log($"FSM Initialize 완료: 초기 상태={initialState}");
            }
            else {
                YisoLogger.LogError($"FSM {name}: 초기 상태({initialState})가 유효하지 않습니다.");
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
                YisoLogger.LogError($"FSM {name}: 존재하지 않는 상태로 전환 시도 -> {newStateName}");
                return;
            }

            // 같은 상태로의 전환 방지 (Force가 아닐 경우)
            if (!force && CurrentState == nextState) return;

            var oldStateName = CurrentState?.StateName ?? "None";

            // 이전 상태 종료
            CurrentState?.PlayExitActions();

            // 상태 교체
            CurrentState = nextState;
            _lastStateEnterTime = Time.time;

            // 새 상태 진입
            CurrentState.PlayEnterActions();

            YisoLogger.Log($"FSM 상태 전환: {oldStateName} -> {newStateName}");
        }

        private void ResetFrequency() {
            _currentFrequency = randomizeFrequencies
                ? Random.Range(actionFrequencyRange.x, actionFrequencyRange.y)
                : actionFrequency;
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
            
            // 캐싱
            if (target != null) {
                _targetContexts[index] = target.GetComponent<IYisoCharacterContext>();
            }
            else {
                _targetContexts[index] = null;
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
        
        public IYisoCharacterContext GetTargetContext(int index) {
            if (index < 0 || index >= _targetContexts.Length) return null;
            
            // 만약 타겟 오브젝트가 파괴되었다면 null 처리 (Safety Check)
            if (_targetSlots[index] == null) {
                _targetContexts[index] = null;
                return null;
            }
            
            return _targetContexts[index];
        }

        /// <summary>
        /// 특정 인덱스의 타겟을 비웁니다.
        /// </summary>
        public void ClearTarget(int index) {
            if (index < 0 || index >= _targetSlots.Length) return;
            _targetSlots[index] = null;
            _targetContexts[index] = null;
        }

        /// <summary>
        /// 모든 타겟 슬롯을 초기화합니다.
        /// </summary>
        public void ClearAllTargets() {
            if (_targetSlots == null) return;
            for (int i = 0; i < _targetSlots.Length; i++) {
                _targetSlots[i] = null;
                _targetContexts[i] = null;
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

        #region Utility Methods for Actions

        /// <summary>
        /// Weapon Module을 가져옵니다.
        /// </summary>
        public Core.Modules.YisoCharacterWeaponModule GetWeaponModule() {
            return Owner?.GetModule<Core.Modules.YisoCharacterWeaponModule>();
        }

        /// <summary>
        /// Ability Module을 가져옵니다.
        /// </summary>
        public Core.Modules.YisoCharacterAbilityModule GetAbilityModule() {
            return Owner?.GetModule<Core.Modules.YisoCharacterAbilityModule>();
        }

        /// <summary>
        /// 메인 타겟(슬롯 0)이 유효한지 확인합니다.
        /// </summary>
        public bool HasTarget(int index = 0) {
            return GetTarget(index) != null;
        }

        /// <summary>
        /// 캐릭터의 현재 위치를 반환합니다.
        /// </summary>
        public Vector2 GetCurrentPosition() {
            if (Owner?.Transform == null) return Vector2.zero;
            return Owner.Transform.position;
        }

        /// <summary>
        /// 메인 타겟을 향하는 방향 벡터를 반환합니다.
        /// </summary>
        public Vector2 GetDirectionToTarget(int index = 0) {
            if (!HasTarget(index)) return Vector2.zero;

            var currentPos = GetCurrentPosition();
            var targetPos = (Vector2) GetTarget(index).position;

            return (targetPos - currentPos).normalized;
        }

        /// <summary>
        /// 메인 타겟까지의 거리를 반환합니다.
        /// </summary>
        public float GetDistanceToTarget(int index = 0) {
            if (!HasTarget(index)) return float.MaxValue;

            var currentPos = GetCurrentPosition();
            var targetPos = (Vector2) GetTarget(index).position;

            return Vector2.Distance(currentPos, targetPos);
        }

        /// <summary>
        /// 스폰 위치로의 방향 벡터를 반환합니다.
        /// </summary>
        public Vector2 GetDirectionToSpawn() {
            var currentPos = GetCurrentPosition();
            var spawnPos = (Vector2) SpawnPosition;

            return (spawnPos - currentPos).normalized;
        }

        /// <summary>
        /// 스폰 위치까지의 거리를 반환합니다.
        /// </summary>
        public float GetDistanceToSpawn() {
            var currentPos = GetCurrentPosition();
            var spawnPos = (Vector2) SpawnPosition;

            return Vector2.Distance(currentPos, spawnPos);
        }

        #endregion
    }
}