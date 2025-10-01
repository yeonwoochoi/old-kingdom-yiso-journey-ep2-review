using Gameplay.Character.Core.Modules;
using Gameplay.Character.StateMachine;
using Gameplay.Character.Types;
using Gameplay.Health;
using UnityEngine;

namespace Gameplay.Character.Core {
    /// <summary>
    /// 캐릭터의 핵심 데이터와 기능에 접근하기 위한 통로(인터페이스).
    /// 모듈은 이 컨텍스트를 통해 다른 모듈이나 캐릭터 정보와 상호작용.
    /// </summary>
    public interface IYisoCharacterContext {
        GameObject GameObject { get; }
        Transform Transform { get; }
        YisoCharacterConstants.CharacterType Type { get; }
        bool IsPlayer { get; }
        bool IsAIControlled { get; }
        string ID { get; }
        GameObject Model { get; }
        Animator Animator { get; }
        T GetModule<T>() where T : class, IYisoCharacterModule;
        YisoCharacterStateSO GetCurrentState();
        void RequestStateChange(YisoCharacterStateSO newStateSO);
        void RequestStateChangeByKey(string newStateName);
        void RequestStateChangeByRole(YisoStateRole newStateRole);
        void Move(Vector3 direction, float speedMultiplier = 1f);
        void PlayAnimation(YisoCharacterAnimationState state, bool value);
        void PlayAnimation(YisoCharacterAnimationState state, float value);
        void PlayAnimation(YisoCharacterAnimationState state, int value);
        void PlayAnimation(YisoCharacterAnimationState state);
        float GetCurrentHealth();
        bool IsDead();
        void TakeDamage(DamageInfo damage);
    }
}