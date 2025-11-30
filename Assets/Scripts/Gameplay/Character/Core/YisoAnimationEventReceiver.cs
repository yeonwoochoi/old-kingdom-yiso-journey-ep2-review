using UnityEngine;

namespace Gameplay.Character.Core {
    /// <summary>
    /// Animator 컴포넌트와 함께 위치하여 애니메이션 이벤트를 수신하고 YisoCharacter로 전달하는 중계 컴포넌트.
    /// Unity Animation Event는 Animator가 붙어있는 GameObject에서만 호출되므로,
    /// 이 컴포넌트를 Animator 옆에 배치하여 이벤트를 캐치합니다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Yiso/Character/Animation Event Receiver")]
    public class YisoAnimationEventReceiver : MonoBehaviour {
        private YisoCharacter _character;

        private void Awake() {
            // 부모 계층에서 YisoCharacter 찾기
            _character = GetComponentInParent<YisoCharacter>();

            if (_character == null) {
                Debug.LogError($"[YisoAnimationEventReceiver] YisoCharacter를 찾을 수 없습니다. " +
                               $"이 컴포넌트는 YisoCharacter의 자식 오브젝트에 있어야 합니다. GameObject: {gameObject.name}");
            }
        }

        /// <summary>
        /// Unity Animation Event에서 호출되는 메서드.
        /// 이벤트를 YisoCharacter로 전달합니다.
        /// </summary>
        /// <param name="eventName">애니메이션 이벤트 이름</param>
        public void OnAnimationEvent(string eventName) {
            _character?.OnAnimationEvent(eventName);
        }
    }
}
