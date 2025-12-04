using System.Collections;
using Core.Behaviour;
using Gameplay.Health.GUI;
using Gameplay.Tools.Movement;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Health {
    [AddComponentMenu("Yiso/Health/Health UI Controller")]
    public class YisoHealthUIController: RunIBehaviour {
        [Title("Health Bar")]
        [Tooltip("인스턴스화할 YisoProgressBar가 포함된 Prefab")]
        [SerializeField] private YisoProgressBar healthBarPrefab;

        [Tooltip("이 개체의 Transform을 기준으로 체력 바가 표시될 위치 오프셋")]
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);
        
        [Title("Health Bar Visibility")]
        [Tooltip("체크 시 항상 체력 바를 표시")]
        [SerializeField] private bool alwaysVisible = false;

        [Tooltip("피격 시 체력 바를 표시할 시간 (Always Visible가 false일 때)")]
        [SerializeField, HideIf("alwaysVisible")] private float displayDurationOnHit = 2f;

        [Tooltip("체력이 0이 되었을 때 체력 바를 숨길지 여부")]
        [SerializeField] private bool hideBarAtZero = true;
        
        [Tooltip("체력이 0이 된 후, 몇 초 뒤에 체력 바를 숨길지 결정")]
        [SerializeField, ShowIf("hideBarAtZero")] private float hideBarAtZeroDelay = 1f;


        [Title("Floating Text")]
        [Tooltip("인스턴스화할 데미지 텍스트 Prefab입니다.")]
        [SerializeField] private YisoFloatingText floatingTextPrefab;

        [Tooltip("데미지 텍스트가 생성될 위치 오프셋입니다.")]
        [SerializeField] private Vector3 floatingTextOffset = new Vector3(0, 1f, 0);

        [Title("Floating Text Colors")]
        [Tooltip("일반 데미지일 때의 텍스트 색상")]
        [SerializeField] private Color normalDamageColor = Color.white;

        [Tooltip("크리티컬 데미지일 때의 텍스트 색상")]
        [SerializeField] private Color criticalDamageColor = Color.yellow;

        [Tooltip("회복할 때의 텍스트 색상")]
        [SerializeField] private Color healColor = Color.green;
        
        private YisoEntityHealth _entityHealth;
        private YisoProgressBar _healthBarInstance;
        private Coroutine _hideBarCoroutine;

        protected override void Awake() {
            base.Awake();
            _entityHealth = GetComponentInParent<YisoEntityHealth>();
        }

        protected override void Start() {
            base.Start();
            InstantiateHealthBar();
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (_entityHealth != null) {
                _entityHealth.OnHealthChanged += HandleHealthChanged;
                _entityHealth.OnDamaged += HandleDamage;
                _entityHealth.OnDied += HandleDeath;
                _entityHealth.OnRevived += HandleRevive;
                _entityHealth.OnHealed += HandleHeal;
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (_entityHealth != null) {
                _entityHealth.OnHealthChanged -= HandleHealthChanged;
                _entityHealth.OnDamaged -= HandleDamage;
                _entityHealth.OnDied -= HandleDeath;
                _entityHealth.OnRevived -= HandleRevive;
                _entityHealth.OnHealed -= HandleHeal;
            }
        }

        private void InstantiateHealthBar() {
            if (healthBarPrefab == null) {
                Debug.LogWarning($"[{gameObject.name}] HealthBar Prefab is not registered. So, Health UI Controller is not worked", this);
                return;
            }

            _healthBarInstance = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            _healthBarInstance.name = $"HealthBar | {gameObject.name}";

            var followTarget = _healthBarInstance.GetComponent<YisoFollowTarget>();
            if (followTarget != null) {
                followTarget.target = transform;
                followTarget.offset = healthBarOffset;
            }

            _healthBarInstance.SetBar(_entityHealth.CurrentHealth, 0, _entityHealth.MaxHealth);

            if (alwaysVisible) _healthBarInstance.ShowBar();
            else _healthBarInstance.HideBar(0);
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth) {
            if (_healthBarInstance != null) {
                _healthBarInstance.UpdateBar(currentHealth, 0, maxHealth);
            }
        }
        
        /// <summary>
        /// 피격 시 호출되어 체력 바를 표시하고 데미지 텍스트를 생성한다.
        /// 이 메서드에서 게임 로직(데미지 크기, 크리티컬 여부)에 따라 텍스트 내용과 색상을 결정한다.
        /// </summary>
        private void HandleDamage(DamageInfo damageInfo) {
            ShowBarOnAction(displayDurationOnHit);

            if (floatingTextPrefab != null && damageInfo.FinalDamage > 0) {
                // 데미지 값을 텍스트로 변환 (소수점 버림)
                var damageText = Mathf.FloorToInt(damageInfo.FinalDamage).ToString();

                // 크리티컬 여부에 따라 색상 결정
                var textColor = damageInfo.IsCritical ? criticalDamageColor : normalDamageColor;

                // TODO: 추후 Pooling Service 이용하기
                var textInstance = Instantiate(floatingTextPrefab, transform.position + floatingTextOffset, Quaternion.identity);
                textInstance.Initialize(damageText, textColor);
            }
        }

        /// <summary>
        /// 회복 시 호출되어 체력 바를 표시하고 회복 텍스트를 생성한다.
        /// 이 메서드에서 회복량에 따라 텍스트 내용과 색상을 결정한다.
        /// </summary>
        private void HandleHeal(float healAmount) {
            ShowBarOnAction(displayDurationOnHit);

            if (floatingTextPrefab != null && healAmount > 0) {
                // 회복 값을 텍스트로 변환 (소수점 버림, + 기호 추가)
                var healText = $"+{Mathf.FloorToInt(healAmount)}";

                // TODO: 추후 Pooling Service 이용하기
                var textInstance = Instantiate(floatingTextPrefab, transform.position + floatingTextOffset, Quaternion.identity);
                textInstance.Initialize(healText, healColor);
            }
        }

        private void HandleDeath() {
            if (hideBarAtZero && _healthBarInstance != null) {
                _healthBarInstance.HideBar(hideBarAtZeroDelay);
            }
        }

        private void HandleRevive() {
            if (_healthBarInstance != null) {
                _healthBarInstance.SetBar(_entityHealth.CurrentHealth, 0, _entityHealth.MaxHealth);
                
                if (alwaysVisible) _healthBarInstance.ShowBar();
                else _healthBarInstance.HideBar(0);
            }
        }

        private void ShowBarOnAction(float duration) {
            if (alwaysVisible || _healthBarInstance == null) return;

            if (_hideBarCoroutine != null) {
                StopCoroutine(_hideBarCoroutine);
            }
            
            _healthBarInstance.ShowBar();
            _hideBarCoroutine = StartCoroutine(HideBarAfterDelay(duration));
        }

        private IEnumerator HideBarAfterDelay(float delay) {
            yield return new WaitForSeconds(delay);
            if (!alwaysVisible && !_entityHealth.IsDead) {
                _healthBarInstance.HideBar(0);
            }
        }
    }
}