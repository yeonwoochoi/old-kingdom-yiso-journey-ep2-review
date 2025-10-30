using UnityEngine;

namespace Gameplay.Tools.Procedural.GridGenerator {
    /// <summary>
    /// 2D 탑다운 (바람의 나라)
    /// </summary>
    public class YisoGridGeneratorPerlinNoise: YisoGridGenerator {
        public static int[,] Generate(int width, int height, float seed) {
            var grid = PrepareGrid(ref width, ref height);
            for (var i = 0; i < width; i++) {
                for (var j = 0; j < height; j++) {
                    var value = Mathf.RoundToInt(Mathf.PerlinNoise(i * seed, j * seed));
                    SetGridCoordinate(grid, i, j, value);
                }
            }
            return grid;
        }
    }
}