using UnityEngine;

namespace Gameplay.Tools.Procedural.GridGenerator {
    public class YisoGridGeneratorRandomWalk : YisoGridGenerator {
        public static int[,] Generate(int width, int height, int seed, int fillPercentage, Vector2Int startingPoint, int maxIterations) {
            var grid = YisoGridGeneratorFull.Generate(width, height, true);
            var random = new System.Random(seed);

            var requiredFillQuantity = (width * height) * fillPercentage / 100;
            var fillCounter = 0;

            var currentX = startingPoint.x;
            var currentY = startingPoint.y;
            grid[currentY, currentY] = 0;
            fillCounter++;
            var iterationsCounter = 0;

            while (fillCounter < requiredFillQuantity && iterationsCounter < maxIterations) {
                var direction = random.Next(4); // 0123 = 상하좌우

                switch (direction) {
                    case 0:
                        if ((currentY + 1) < height) {
                            currentY++;
                            grid = Carve(grid, currentX, currentY, ref fillCounter);
                        }
                        break;
                    case 1:
                        if ((currentY - 1) > 1) {
                            currentY--;
                            grid = Carve(grid, currentX, currentY, ref fillCounter);
                        }
                        break;
                    case 2:
                        if ((currentX - 1) > 1) {
                            currentX--;
                            grid = Carve(grid, currentX, currentY, ref fillCounter);
                        }
                        break;
                    case 3:
                        if ((currentX + 1) < width) {
                            currentX++;
                            grid = Carve(grid, currentX, currentY, ref fillCounter);
                        }
                        break;
                }
                iterationsCounter++;
            }
            return grid;
        }

        private static int[,] Carve(int[,] grid, int x, int y, ref int fillCounter) {
            if (grid[x, y] == 1) {
                grid[x, y] = 0;
                fillCounter++;
            }
            return grid;
        }
    }
}