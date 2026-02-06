using System.Collections.Generic;
using Gameplay.Character.Abilities.Definitions;
using Gameplay.Character.Core;
using Gameplay.Character.Core.Modules;
using Gameplay.Character.Types;
using Gameplay.Character.Weapon;
using Gameplay.Health;
using UnityEngine;
using Utils;

namespace Gameplay.Character.Abilities {
    /// <summary>
    /// 원거리 투사체 공격 Ability.
    /// 애니메이션 이벤트(FireProjectile) 기반으로 투사체를 발사합니다.
    /// 부채꼴 범위 내 타겟 감지 및 자동 조준 기능 포함.
    /// </summary>
    public class YisoProjectileAttackAbility : YisoAttackAbilityBase {
        private readonly YisoProjectileAttackAbilitySO _settings;

        private YisoCharacterWeaponModule _weaponModule;
        private YisoCharacterInputModule _inputModule; // Player 전용

        private float _lastAttackTime = -999f;

        // Input Edge Detection (단발 입력 감지용)
        private bool _wasAttackPressedLastFrame = false;

        // Safety Net: 애니메이션 이벤트가 씹혀도 강제 종료하는 안전장치
        private float _safetyTimer = 0f;

        // 다중 발사 관련
        private int _pendingProjectiles = 0; // 남은 발사 수
        private float _nextFireTime = 0f; // 다음 발사 시간
        private Vector2 _attackDirection; // 공격 방향 (발사 시작 시 고정)
        private Transform _currentTarget; // 현재 타겟 (있을 경우)

        // YisoAttackAbilityBase 추상 속성 구현
        protected override bool CanMoveWhileAttacking => _settings.canMoveWhileAttacking;

        public YisoProjectileAttackAbility(YisoProjectileAttackAbilitySO settings) {
            _settings = settings;
        }

        public override void Initialize(IYisoCharacterContext context) {
            base.Initialize(context);
            _weaponModule = Context.GetModule<YisoCharacterWeaponModule>();
            _inputModule = Context.GetModule<YisoCharacterInputModule>();

            if (_settings.projectilePrefab == null) {
                YisoLogger.LogWarning("[YisoProjectileAttackAbility] projectilePrefab이 설정되지 않았습니다.");
            }
        }

        public override void PreProcessAbility() {
            base.PreProcessAbility();

            // Player만 입력 처리
            if (_inputModule == null || Context.Type != CharacterType.Player) return;

            // Pull 방식: InputModule에서 공격 버튼 상태를 가져옴
            var isPressed = _inputModule.AttackInput;

            // 단발/연속 입력 모드 분기
            var shouldAttack = false;

            if (_settings.continuousPressAttack) {
                // 연속 입력 모드: 버튼을 누르고 있는 동안 계속 공격
                shouldAttack = isPressed;
            }
            else {
                // 단발 입력 모드: Edge Detection (버튼을 누르는 순간만 공격)
                shouldAttack = !_wasAttackPressedLastFrame && isPressed;
            }

            // Edge Detection 상태 업데이트
            _wasAttackPressedLastFrame = isPressed;

            // 공격 중이 아닐 때만 즉시 실행
            if (!_isAttacking && shouldAttack) {
                TryAttack();
            }

            // AI의 경우 외부에서 TriggerAttack() 호출
        }

        public override void ProcessAbility() {
            base.ProcessAbility();

            // "공격 중인데" 권한이 사라졌다면 강제 중단 (인터럽트)
            if (_isAttacking && !Context.IsAttackAllowed(this)) {
                HandleAttackEnd();
                return;
            }

            // 공격 중이 아니고 권한도 없다면 로직 패스
            if (!_isAttacking && !Context.IsAttackAllowed(this)) {
                return;
            }

            // Safety Net: 애니메이션 이벤트가 호출되지 않는 경우 강제 종료
            if (_isAttacking && _safetyTimer > 0f) {
                _safetyTimer -= Time.deltaTime;

                if (_safetyTimer <= 0f) {
                    YisoLogger.LogWarning($"[YisoProjectileAttackAbility] Safety Net 타이머 만료! 강제 종료합니다.");
                    HandleAttackEnd();
                }
            }

            // 다중 발사 처리 (fireInterval > 0 일 때)
            if (_isAttacking && _pendingProjectiles > 0 && Time.time >= _nextFireTime) {
                FireSingleProjectile();
            }
        }

        /// <summary>
        /// 애니메이션 이벤트를 처리합니다.
        /// </summary>
        public override void OnAnimationEvent(string eventName) {
            base.OnAnimationEvent(eventName);

            // 공격 중이 아니면 이벤트 무시
            if (!_isAttacking) return;

            switch (eventName) {
                case YisoAbilityAnimationEvents.ATTACK_FIRE_PROJECTILE:
                    // 애니메이션 이벤트로 발사 시작
                    StartFiringProjectiles();
                    break;

                case YisoAbilityAnimationEvents.ATTACK_END:
                    HandleAttackEnd();
                    break;

                default:
                    break;
            }
        }

        #region Public API

        /// <summary>
        /// 외부에서 공격을 트리거합니다. (AI 등에서 사용)
        /// </summary>
        public override void TriggerAttack() {
            base.TriggerAttack();
            TryAttack();
        }

        /// <summary>
        /// 외부에서 공격을 트리거합니다. 특정 타겟 방향으로 발사합니다.
        /// </summary>
        /// <param name="target">타겟 Transform</param>
        public void TriggerAttack(Transform target) {
            _currentTarget = target;
            TryAttack();
        }

        /// <summary>
        /// 부채꼴 범위 내에서 가장 가까운 타겟을 찾습니다.
        /// </summary>
        /// <returns>타겟이 있으면 Transform, 없으면 null</returns>
        public Transform FindTargetInCone() {
            return FindTargetInCone(_settings.detectionRange, _settings.detectionAngle);
        }

        /// <summary>
        /// 부채꼴 범위 내에서 가장 가까운 타겟을 찾습니다.
        /// </summary>
        /// <param name="range">감지 거리</param>
        /// <param name="angle">감지 각도 (도)</param>
        /// <returns>타겟이 있으면 Transform, 없으면 null</returns>
        public Transform FindTargetInCone(float range, float angle) {
            var position = Context.Transform.position;
            var facingDirection = _orientationAbility?.LastDirectionVector ?? Vector2.down;

            // 범위 내 모든 콜라이더 검색
            var colliders = Physics2D.OverlapCircleAll(position, range, _settings.targetLayerMask);
            if (colliders.Length == 0) return null;

            Transform closestTarget = null;
            float closestDistance = float.MaxValue;
            float halfAngle = angle / 2f;

            foreach (var col in colliders) {
                // 자기 자신 제외
                if (col.transform == Context.Transform) continue;
                if (IsChildOf(col.gameObject, Context.Transform.gameObject)) continue;

                // Health 확인 (Hurtbox 우선, 없으면 직접 찾기)
                var (health, targetTransform) = FindHealthAndTransform(col);
                if (health == null || health.IsDead) continue;

                // 방향 계산 (캐릭터 루트 Transform 기준)
                Vector2 toTarget = ((Vector2)targetTransform.position - (Vector2)position).normalized;
                float angleToTarget = Vector2.Angle(facingDirection, toTarget);

                // 부채꼴 범위 내인지 확인
                if (angleToTarget <= halfAngle) {
                    float distance = Vector2.Distance(position, targetTransform.position);
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestTarget = targetTransform;
                    }
                }
            }

            return closestTarget;
        }

        /// <summary>
        /// 부채꼴 범위 내의 모든 타겟을 찾습니다.
        /// </summary>
        /// <returns>타겟 리스트</returns>
        public List<Transform> FindAllTargetsInCone() {
            return FindAllTargetsInCone(_settings.detectionRange, _settings.detectionAngle);
        }

        /// <summary>
        /// 부채꼴 범위 내의 모든 타겟을 찾습니다.
        /// </summary>
        /// <param name="range">감지 거리</param>
        /// <param name="angle">감지 각도 (도)</param>
        /// <returns>타겟 리스트</returns>
        public List<Transform> FindAllTargetsInCone(float range, float angle) {
            var results = new List<Transform>();
            var position = Context.Transform.position;
            var facingDirection = _orientationAbility?.LastDirectionVector ?? Vector2.down;

            // 범위 내 모든 콜라이더 검색
            var colliders = Physics2D.OverlapCircleAll(position, range, _settings.targetLayerMask);
            if (colliders.Length == 0) return results;

            float halfAngle = angle / 2f;

            foreach (var col in colliders) {
                // 자기 자신 제외
                if (col.transform == Context.Transform) continue;
                if (IsChildOf(col.gameObject, Context.Transform.gameObject)) continue;

                // Health 확인 (Hurtbox 우선, 없으면 직접 찾기)
                var (health, targetTransform) = FindHealthAndTransform(col);
                if (health == null || health.IsDead) continue;

                // 중복 방지 (같은 캐릭터의 여러 Collider가 검색될 수 있음)
                if (results.Contains(targetTransform)) continue;

                // 방향 계산 (캐릭터 루트 Transform 기준)
                Vector2 toTarget = ((Vector2)targetTransform.position - (Vector2)position).normalized;
                float angleToTarget = Vector2.Angle(facingDirection, toTarget);

                // 부채꼴 범위 내인지 확인
                if (angleToTarget <= halfAngle) {
                    results.Add(targetTransform);
                }
            }

            // 거리순 정렬
            results.Sort((a, b) => {
                float distA = Vector2.Distance(position, a.position);
                float distB = Vector2.Distance(position, b.position);
                return distA.CompareTo(distB);
            });

            return results;
        }

        #endregion

        #region Private Logic

        /// <summary>
        /// 공격을 시도합니다.
        /// </summary>
        private bool TryAttack() {
            if (!Context.IsAttackAllowed(this)) return false;

            // 쿨타임 체크
            if (Time.time - _lastAttackTime < _weaponModule.GetCurrentWeaponData().GetAttackCooldown()) {
                return false;
            }

            // 프리팹 체크
            if (_settings.projectilePrefab == null) {
                YisoLogger.LogWarning("[YisoProjectileAttackAbility] projectilePrefab이 null입니다.");
                return false;
            }

            // 공격 시작
            AttackStart();
            return true;
        }

        /// <summary>
        /// 공격을 시작합니다.
        /// </summary>
        private void AttackStart() {
            _isAttacking = true;
            _lastAttackTime = Time.time;
            _pendingProjectiles = 0; // 애니메이션 이벤트에서 설정됨

            // Safety Net 타이머 설정
            _safetyTimer = _weaponModule.GetCurrentWeaponData().GetAttackDuration(0) + 0.1f;

            // 공격 방향 결정 (타겟이 있으면 타겟 방향, 없으면 현재 바라보는 방향)
            if (_currentTarget != null) {
                _attackDirection = ((Vector2)_currentTarget.position - (Vector2)Context.Transform.position).normalized;
            }
            else {
                // 자동 타겟팅 시도
                var autoTarget = FindTargetInCone();
                if (autoTarget != null) {
                    _currentTarget = autoTarget;
                    _attackDirection = ((Vector2)autoTarget.position - (Vector2)Context.Transform.position).normalized;
                }
                else {
                    // 타겟 없음: 현재 바라보는 방향
                    _attackDirection = _orientationAbility?.LastDirectionVector ?? Vector2.down;
                }
            }

            // Orientation 잠금
            LockOrientation();

            // WeaponAim 방향 고정
            var currentWeapon = _weaponModule?.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.WeaponAim != null) {
                currentWeapon.WeaponAim.LockAimToDirection(_attackDirection);
            }

            // 애니메이션 이벤트 없이 즉시 발사하려면 아래 주석 해제
            // StartFiringProjectiles();
        }

        /// <summary>
        /// 투사체 발사를 시작합니다. (애니메이션 이벤트에서 호출)
        /// </summary>
        private void StartFiringProjectiles() {
            _pendingProjectiles = _settings.projectileCount;
            _nextFireTime = Time.time; // 즉시 첫 발 발사

            // 첫 발 즉시 발사
            FireSingleProjectile();
        }

        /// <summary>
        /// 단일 투사체를 발사합니다.
        /// </summary>
        private void FireSingleProjectile() {
            if (_pendingProjectiles <= 0) return;

            // 발사 방향 계산
            Vector2 fireDirection = CalculateFireDirection(_settings.projectileCount - _pendingProjectiles);

            // 투사체 생성
            SpawnProjectile(fireDirection);

            // 남은 발사 수 감소
            _pendingProjectiles--;

            // 다음 발사 시간 설정
            if (_pendingProjectiles > 0) {
                _nextFireTime = Time.time + _settings.fireInterval;
            }
        }

        /// <summary>
        /// 발사 방향을 계산합니다.
        /// </summary>
        /// <param name="projectileIndex">현재 투사체 인덱스 (0부터 시작)</param>
        /// <returns>발사 방향 벡터</returns>
        private Vector2 CalculateFireDirection(int projectileIndex) {
            // 타겟이 있으면 타겟 방향으로
            if (_currentTarget != null) {
                return ((Vector2)_currentTarget.position - (Vector2)Context.Transform.position).normalized;
            }

            // 타겟이 없으면 산발 패턴 적용
            int totalCount = _settings.projectileCount;

            if (totalCount == 1 || _settings.spreadAngle <= 0f) {
                // 단일 발사 또는 산발 없음: 정면으로
                return _attackDirection;
            }

            if (_settings.evenSpread) {
                // 균등 분배 (부채꼴)
                float halfSpread = _settings.spreadAngle / 2f;
                float angleStep = totalCount > 1 ? _settings.spreadAngle / (totalCount - 1) : 0f;
                float angle = -halfSpread + (angleStep * projectileIndex);
                return RotateVector(_attackDirection, angle);
            }
            else {
                // 랜덤 산발
                float randomAngle = Random.Range(-_settings.spreadAngle / 2f, _settings.spreadAngle / 2f);
                return RotateVector(_attackDirection, randomAngle);
            }
        }

        /// <summary>
        /// 투사체를 스폰합니다.
        /// </summary>
        private void SpawnProjectile(Vector2 direction) {
            // 스폰 위치 (캐릭터 위치 + 약간의 오프셋)
            Vector2 spawnPosition = (Vector2) Context.Transform.position + _settings.GetSpawnOffset(_orientationAbility.CurrentFacingDirection);

            // 투사체 생성
            // TODO: Object Pooler 연동
            var projectileGO = Object.Instantiate(_settings.projectilePrefab, spawnPosition, Quaternion.identity);
            var projectile = projectileGO.GetComponent<YisoProjectile>();

            if (projectile != null) {
                // 데미지 계산 (WeaponModule이 있으면 무기 데미지, 없으면 설정 데미지)
                float damage = _settings.GetRandomDamage();
                if (_weaponModule != null && _weaponModule.HasWeapon()) {
                    var weaponData = _weaponModule.GetCurrentWeaponData();
                    if (weaponData != null) {
                        damage = weaponData.GetRandomDamage();
                    }
                }

                // 투사체 초기화
                projectile.Initialize(
                    Context.Transform.gameObject,
                    direction,
                    _settings.projectileSpeed,
                    _settings.maxRange,
                    damage,
                    _settings.targetLayerMask
                );
            }
            else {
                YisoLogger.LogWarning("[YisoProjectileAttackAbility] 프리팹에 YisoProjectile 컴포넌트가 없습니다.");
            }
        }

        /// <summary>
        /// 벡터를 주어진 각도만큼 회전시킵니다.
        /// </summary>
        private Vector2 RotateVector(Vector2 v, float angleDegrees) {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }

        /// <summary>
        /// Collider에서 YisoEntityHealth와 타겟 Transform을 찾습니다.
        /// Hurtbox가 있으면 Health의 GameObject Transform(캐릭터 루트)을 반환합니다.
        /// </summary>
        private (YisoEntityHealth health, Transform targetTransform) FindHealthAndTransform(Collider2D collider) {
            // 1. Hurtbox 컴포넌트 확인 (피격 판정 영역 분리된 경우)
            var hurtbox = collider.GetComponent<YisoHurtbox>();
            if (hurtbox != null && hurtbox.Health != null) {
                // Hurtbox가 있으면 Health의 GameObject Transform 반환 (캐릭터 루트)
                return (hurtbox.Health, hurtbox.Health.transform);
            }

            // 2. 직접 Health 컴포넌트 확인 (기존 방식, 하위 호환)
            var health = collider.GetComponent<YisoEntityHealth>();
            return (health, collider.transform);
        }

        /// <summary>
        /// target이 owner의 자식인지 확인.
        /// </summary>
        private bool IsChildOf(GameObject target, GameObject owner) {
            if (target == owner) return true;

            var current = target.transform;
            while (current != null) {
                if (current.gameObject == owner) {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        #endregion

        #region Animation Event Handlers

        /// <summary>
        /// 공격 종료 처리.
        /// </summary>
        private void HandleAttackEnd() {
            _safetyTimer = 0f;
            _isAttacking = false;
            StopAttackAnimation();
            _pendingProjectiles = 0;
            _currentTarget = null;

            // Orientation 잠금 해제
            UnlockOrientation();

            // WeaponAim 잠금 해제
            var currentWeapon = _weaponModule?.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.WeaponAim != null) {
                currentWeapon.WeaponAim.UnlockAim();
            }
        }

        #endregion

        public override void ResetAbility() {
            base.ResetAbility();

            _isAttacking = false;
            StopAttackAnimation();
            _safetyTimer = 0f;
            _wasAttackPressedLastFrame = false;
            _pendingProjectiles = 0;
            _currentTarget = null;

            UnlockOrientation();

            var currentWeapon = _weaponModule?.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.WeaponAim != null) {
                currentWeapon.WeaponAim.UnlockAim();
            }
        }

        public override void OnDeath() {
            base.OnDeath();
            ResetAbility();
        }

        public override void OnRevive() {
            base.OnRevive();
            ResetAbility();
        }
    }
}
