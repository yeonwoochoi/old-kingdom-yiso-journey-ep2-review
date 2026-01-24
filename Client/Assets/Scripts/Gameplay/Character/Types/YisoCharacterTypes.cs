using UnityEngine;

namespace Gameplay.Character.Types {
    /// <summary>
    /// 캐릭터의 역할을 정의하는 유일한 타입
    /// 이 타입을 기준으로 행동 방식(Player/AI)과 피아식별 등이 결정됨
    /// </summary>
    public enum CharacterType {
        Player, // 플레이어의 직접 조작을 받음
        Enemy,
        Npc,
        Pet,
        Ally,
        Etc
    }
    
    /// <summary>
    /// 캐릭터와 무기가 바라보는 방향을 나타내는 Enum
    /// </summary>
    public enum FacingDirections {
        Up,
        Down,
        Left,
        Right
    }


    public static class YisoFacingDirectionExtensions {
        /// <summary>
        /// Enum -> Vector2 변환
        /// 사용법: var vec = myDirection.ToVector2();
        /// </summary>
        public static Vector2 ToVector2(this FacingDirections direction) {
            return direction switch {
                FacingDirections.Up => Vector2.up,
                FacingDirections.Down => Vector2.down,
                FacingDirections.Left => Vector2.left,
                FacingDirections.Right => Vector2.right,
                _ => Vector2.down
            };
        }

        /// <summary>
        /// Vector2 -> Enum 변환 (지배적인 축 기준)
        /// 사용법: var dir = myVector.ToFacingDirection();
        /// </summary>
        public static FacingDirections ToFacingDirection(this Vector2 vector) {
            // 벡터의 크기가 너무 작으면 기본값(예: Down) 반환 (필요 시 로직 추가)
            if (vector.sqrMagnitude < 0.001f) return FacingDirections.Down;

            // X축 성분이 Y축 성분보다 크면 좌우, 아니면 상하
            if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y)) {
                return vector.x > 0 ? FacingDirections.Right : FacingDirections.Left;
            }
            else {
                return vector.y > 0 ? FacingDirections.Up : FacingDirections.Down;
            }
        }
    }
}