namespace Gameplay.Character.Types {
    public static class YisoCharacterConstants {
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
    }
}