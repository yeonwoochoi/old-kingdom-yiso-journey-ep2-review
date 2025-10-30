using UnityEngine;

namespace Gameplay.Tools.Procedural.GridGenerator {
    /// <summary>
    /// 2D 횡스크롤 (슈퍼 마리오)
    /// </summary>
    public class YisoGridGeneratorPerlinNoiseGround : YisoGridGenerator {
        public static int[,] Generate(int width, int height, float seed) {
            var grid = PrepareGrid(ref width, ref height);
            for (var i = 0; i < width; i++) {
                var groundHeight = Mathf.FloorToInt((Mathf.PerlinNoise(i, seed) - 0.5f) * height) + height / 2;
                for (var j = groundHeight; j >= 0; j--) {
                    SetGridCoordinate(grid, i, j, 1);
                }
            }
            return grid;
        }
    }
}