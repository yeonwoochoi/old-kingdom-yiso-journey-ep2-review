using Core.Behaviour;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Gameplay.Health.GUI {
    /// <summary>
    /// 개별 데미지 텍스트의 애니메이션과 소멸을 책임지는 컴포넌트
    /// 이 스크립트는 Floating Text Prefab에 직접 붙여서 사용한다.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class YisoFloatingText : RunIBehaviour {
        [Header("References")] [Tooltip("텍스트를 표시할 TextMeshProUGUI 컴포넌트")] [SerializeField]
        private TextMeshProUGUI damageText;

        [Header("Animation Settings")] [Tooltip("텍스트가 위로 올라갈 거리")] [SerializeField]
        private float moveAmount = 1.5f;

        [Tooltip("애니메이션이 지속될 시간")] [SerializeField]
        private float duration = 1.5f;

        [Tooltip("애니메이션이 시작될 때의 크기")] [SerializeField]
        private float startScale = 1.2f;

        [Header("Color Settings")] [Tooltip("일반 데미지일 때의 텍스트 색상")] [SerializeField]
        private Color normalDamageColor = Color.white;

        [Tooltip("크리티컬 데미지일 때의 텍스트 색상")] [SerializeField]
        private Color criticalDamageColor = Color.yellow;

        protected override void Awake() {
            base.Awake();
            if (damageText == null) {
                damageText = GetComponent<TextMeshProUGUI>();
            }
        }

        public void Initialize(float damage, bool isCritical) {
            damageText.text = Mathf.FloorToInt(damage).ToString();
            damageText.color = isCritical ? criticalDamageColor : normalDamageColor;

            PlayAnimation();
        }

        private void PlayAnimation() {
            var sequence = DOTween.Sequence();

            transform.localScale = Vector3.one * startScale;
            damageText.alpha = 1f;

            // 애니메이션 정의
            // 1. 지정된 시간 동안 Y축으로 이동
            sequence.Join(transform.DOMoveY(transform.position.y + moveAmount, duration)).SetEase(Ease.OutQuad);
            
            // 2. 애니메이션의 마지막 30% 구간에서 서서히 투명해짐
            sequence.Insert(duration * 0.7f, damageText.DOFade(0f, duration * 0.3f));

            sequence.OnComplete(() => {
                Destroy(gameObject);
            });

            sequence.Play();
        }
    }
}