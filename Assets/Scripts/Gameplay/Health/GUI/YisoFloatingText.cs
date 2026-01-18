using Core.Behaviour;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Utils;

namespace Gameplay.Health.GUI {
    /// <summary>
    /// Floating 텍스트의 애니메이션과 소멸을 책임지는 컴포넌트.
    /// 게임 로직(데미지 계산, 크리티컬 판단 등)은 알지 못하며, 오직 전달받은 텍스트와 색상을 애니메이션과 함께 표시하는 역할만 수행한다.
    /// 이 스크립트는 Floating Text Prefab에 직접 붙여서 사용한다.
    /// </summary>
    public class YisoFloatingText : RunIBehaviour {
        [Header("References")] [Tooltip("텍스트를 표시할 TextMeshProUGUI 컴포넌트")] [SerializeField]
        private TextMeshProUGUI textComponent;

        [Header("Animation Settings")] [Tooltip("텍스트가 위로 올라갈 거리")] [SerializeField]
        private float moveAmount = 1.5f;

        [Tooltip("애니메이션이 지속될 시간")] [SerializeField]
        private float duration = 1.5f;

        [Tooltip("애니메이션이 시작될 때의 크기")] [SerializeField]
        private float startScale = 1.2f;

        protected override void Awake() {
            base.Awake();
            if (textComponent == null) {
                textComponent = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        /// <summary>
        /// 표시할 텍스트와 색상을 설정하고 애니메이션을 시작한다.
        /// 게임 로직(데미지 계산, 크리티컬 판단 등)은 호출자가 처리하며, 이 메서드는 단순히 전달받은 값을 표시한다.
        /// </summary>
        /// <param name="text">표시할 텍스트 내용</param>
        /// <param name="color">텍스트 색상</param>
        public void Initialize(string text, Color color) {
            textComponent.text = text;
            textComponent.color = color;

            PlayAnimation();
        }

        /// <summary>
        /// 텍스트의 이동 및 페이드 아웃 애니메이션을 재생한다.
        /// 애니메이션이 끝나면 자동으로 GameObject를 파괴한다.
        /// </summary>
        private void PlayAnimation() {
            var sequence = DOTween.Sequence();

            transform.localScale = Vector3.one * startScale;
            textComponent.alpha = 1f;

            // 애니메이션 정의
            // 1. 지정된 시간 동안 Y축으로 이동
            sequence.Join(transform.DOMoveY(transform.position.y + moveAmount, duration)).SetEase(Ease.OutQuad);

            // 2. 애니메이션의 마지막 30% 구간에서 서서히 투명해짐
            sequence.Insert(duration * 0.7f, textComponent.DOFade(0f, duration * 0.3f));

            sequence.OnComplete(() => {
                gameObject.SafeDestroy();
            });

            sequence.Play();
        }
    }
}