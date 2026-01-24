using Core.Behaviour;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay.Tools.Tilemaps {
    public class YisoTilemap: RunIBehaviour {
        public static Vector2 GetRandomPosition(Tilemap targetTilemap, Grid grid, int width, int height, bool shouldBeFilled = true, int maxIterations = 1000) {
            var iterationsCount = 0;
            var randomCoordinate = Vector3Int.zero;

            while (iterationsCount <= maxIterations) {
                randomCoordinate.x = Random.Range(0, width);
                randomCoordinate.y = Random.Range(0, height);
                randomCoordinate += YisoTilemapGridRenderer.ComputeOffset(width-1, height-1);
                
                var hasTile = targetTilemap.HasTile(randomCoordinate);
                if (hasTile == shouldBeFilled) {
                    return targetTilemap.CellToWorld(randomCoordinate) + grid.cellSize / 2;
                }

                iterationsCount++;
            }
            
            return Vector2.zero;
        }
    }
}