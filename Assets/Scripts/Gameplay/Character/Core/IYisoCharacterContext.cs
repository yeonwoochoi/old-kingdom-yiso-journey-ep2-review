using System.Collections;
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
        CharacterType Type { get; }
        bool IsPlayer { get; }
        bool IsAIControlled { get; }
        string ID { get; }
        GameObject Model { get; }
        Animator Animator { get; }
        Vector2 MovementVector { get; }
        T GetModule<T>() where T : class, IYisoCharacterModule;
        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine routine);
        void Move(Vector2 finalMovementVector);
        void PlayAnimation(YisoCharacterAnimationState state, bool value);
        void PlayAnimation(YisoCharacterAnimationState state, float value);
        void PlayAnimation(YisoCharacterAnimationState state, int value);
        void PlayAnimation(YisoCharacterAnimationState state);
        public void OnAnimationEvent(string eventName);
        float GetCurrentHealth();
        bool IsDead();
        void TakeDamage(DamageInfo damage);
        bool IsMovementAllowed { get; }
        bool IsAttackAllowed { get; }
    }
}