using Core.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Health {
    [AddComponentMenu("Yiso/Health/Health Animator")]
    public class YisoHealthAnimator: RunIBehaviour {
        [SerializeField] private bool updateAnimatorParameters = false;
        [ShowIf("updateAnimatorParameters")] public string damagedAnimationParameterName;
        [ShowIf("updateAnimatorParameters")] public string deathAnimationParameterName;
        
        private Animator _animator;
        
        // TODO: 리스너 (Listener) 이벤트를 구독하여 애니메이션 재생
        // Bool, Trigger, Int, Float 모두 받기 -> struct 각각 만들면 되겠네.
    }
}