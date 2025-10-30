using UnityEngine;

namespace Gameplay.Tools.Procedural.GridGenerator {
    public class YisoGridGeneratorRandomWalkGround : YisoGridGenerator {
        public static int[,] Generate(int width, int height, int seed, int minHeightDifference, int maxHeightDifference,
            int minFlatDistance, int maxFlatDistance, int maxHeight) {
            var random = new System.Random(seed.GetHashCode());
            Random.InitState(seed);

            var grid = PrepareGrid(ref width, ref height);

            var groundHeight = Random.Range(0, maxHeight);
            var previousGroundHeight = groundHeight;
            var currentFlatDistance = -1;

            for (var i = 0; i < width; i++) {
                groundHeight = previousGroundHeight;
                var newElevation = Random.Range(minHeightDifference, maxHeightDifference);
                var flatDistance = Random.Range(minFlatDistance, maxFlatDistance);

                if (currentFlatDistance >= flatDistance - 1) {
                    if (random.Next(2) > 0) {
                        groundHeight -= newElevation;
                    }
                    else if (previousGroundHeight + newElevation < height) {
                        groundHeight += newElevation;
                    }

                    groundHeight = Mathf.Clamp(groundHeight, 1, maxHeight);
                    currentFlatDistance = 0;
                }
                else {
                    currentFlatDistance++;
                }

                for (var j = groundHeight; j >= 0; j--) {
                    grid[i, j] = 1;
                }

                previousGroundHeight = groundHeight;
            }
            return grid;
        }
    }
}