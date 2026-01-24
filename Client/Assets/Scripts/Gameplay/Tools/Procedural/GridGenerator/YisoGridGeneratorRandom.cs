using System;

namespace Gameplay.Tools.Procedural.GridGenerator {
    public class YisoGridGeneratorRandom: YisoGridGenerator {
        public static int[,] Generate(int width, int height, int seed, int fillPercentage) {
            var grid = YisoGridGeneratorFull.Generate(width, height, true);
            var random = new Random(seed);
            
            for (var i = 0; i < width; i++) {
                for (var j = 0; j < height; j++) {
                    var value = random.Next(0, 100) < fillPercentage ? 1 : 0;
                    SetGridCoordinate(grid, i, j, value);
                }
            }
            return grid;
        }
    }
}