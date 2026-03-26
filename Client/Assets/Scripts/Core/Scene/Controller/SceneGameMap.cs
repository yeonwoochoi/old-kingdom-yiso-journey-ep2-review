using UnityEngine;
using UnityEngine.Tilemaps;

namespace Core.Scene.Controller {
    /// <summary>
    /// [역할] GameMap 씬 전용 컨트롤러
    /// [책임]
    ///   - GameMap 씬 진입/퇴장 시 패킷 핸들러 등록/해제
    ///   - 맵 데이터 수신 후 씬 초기화 요청
    /// [배치] GameMap 씬 루트 GameObject에 단독 배치
    /// </summary>

    public enum MapObjectType {
        Static,
        UserLocal,
        UserRemote,
        Mob,
        Npc,
        Reactor,
        Effect,
        Etc
    }
    
    public class TilemapInfo {
        
    }

    public abstract class ObjectInfo {
        public int Id { set; get; }
        public string Name { get; set; }
        public abstract MapObjectType Type { get; }
        public int Layer { get; set; }
        public int OrderInLayer { get; set; } = 0;
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        
        public int[] SpriteId { get; set; } // 0번이 default
        public int[] Frame { get; set; } // animation 있는 경우
    }

    public class MobInfo : ObjectInfo {
        public override MapObjectType Type => MapObjectType.Mob;
        public int FsmId { get; set; }
        // todo
    }
    
    
    public class SceneGameMap : SceneBase {
        private void CreateMapObj(GameObject prefab, int layer, Transform parent) {
            
        }

        private void CreateTilemap(Tile tile, int layer, Transform parent) {
            
        }
    }
}