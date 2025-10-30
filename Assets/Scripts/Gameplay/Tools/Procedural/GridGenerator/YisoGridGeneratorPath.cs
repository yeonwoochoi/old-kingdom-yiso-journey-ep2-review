using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay.Tools.Procedural.GridGenerator {
    public class YisoGridGeneratorPath : YisoGridGenerator {
        public enum Directions {
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft
        }

        public static int[,] Generate(int width, int height, int seed, Directions direction, Vector2Int startPosition,
            int pathMinWidth, int pathMaxWidth, int directionChangeDistance, int widthChangePercentage,
            int directionChangePercentage) {
            var grid = YisoGridGeneratorFull.Generate(width, height, true);
            var random = new System.Random(seed);
            Random.InitState(seed);

            var pathWidth = 1;
            var initialX = startPosition.x;
            var initialY = startPosition.y;

            SetGridCoordinate(grid, initialX, initialY, 0);

            switch (direction) {
                case Directions.TopToBottom:
                    var x1 = initialX;
                    for (var i = -pathWidth; i <= pathWidth; i++) {
                        SetGridCoordinate(grid, x1 + i, initialY, 0);
                    }

                    for (var y = initialY; y > 0; y--) {
                        pathWidth = ComputeWidth(random, widthChangePercentage, pathMinWidth, pathMaxWidth, pathWidth);
                        x1 = DetermineNextStep(random, x1, directionChangeDistance, directionChangePercentage,
                            pathMaxWidth, width);
                        for (var i = -pathWidth; i <= pathWidth; i++) {
                            SetGridCoordinate(grid, x1 + i, y, 0);
                        }
                    }

                    break;
                case Directions.BottomToTop:
                    var x2 = initialX;
                    for (var i = -pathWidth; i <= pathWidth; i++) {
                        SetGridCoordinate(grid, x2 + i, initialY, 0);
                    }

                    for (var y = initialY; y < height; y++) {
                        pathWidth = ComputeWidth(random, widthChangePercentage, pathMinWidth, pathMaxWidth, pathWidth);
                        x2 = DetermineNextStep(random, x2, directionChangeDistance, directionChangePercentage,
                            pathMaxWidth, width);
                        for (var i = -pathWidth; i <= pathWidth; i++) {
                            SetGridCoordinate(grid, x2 + i, y, 0);
                        }
                    }

                    break;
                case Directions.LeftToRight:
                    var y1 = initialY;
                    for (var i = -pathWidth; i <= pathWidth; i++) {
                        SetGridCoordinate(grid, initialX, y1 + i, 0);
                    }

                    for (var x = initialX; x < width; x++) {
                        pathWidth = ComputeWidth(random, widthChangePercentage, pathMinWidth, pathMaxWidth, pathWidth);
                        y1 = DetermineNextStep(random, y1, directionChangeDistance, directionChangePercentage,
                            pathMaxWidth, height);
                        for (var i = -pathWidth; i <= pathWidth; i++) {
                            SetGridCoordinate(grid, x, y1 + i, 0);
                        }
                    }

                    break;
                case Directions.RightToLeft:
                    var y2 = initialY;
                    for (var i = -pathWidth; i <= pathWidth; i++) {
                        SetGridCoordinate(grid, initialX, y2 + i, 0);
                    }

                    for (var x = initialX; x > 0; x--) {
                        pathWidth = ComputeWidth(random, widthChangePercentage, pathMinWidth, pathMaxWidth, pathWidth);
                        y2 = DetermineNextStep(random, y2, directionChangeDistance, directionChangePercentage,
                            pathMaxWidth, height);
                        for (var i = -pathWidth; i <= pathWidth; i++) {
                            SetGridCoordinate(grid, x, y2 + i, 0);
                        }
                    }

                    break;
            }

            return grid;
        }

        /// <summary>
        /// 여기서 width는 진행 방향의 수직 너비를 의미하는거지 전체 맵의 width를 의미하는게 아님
        /// </summary>
        /// <param name="random"></param>
        /// <param name="changePercentage"></param>
        /// <param name="minWidth"></param>
        /// <param name="maxWidth"></param>
        /// <param name="currentWidth"></param>
        /// <returns></returns>
        private static int ComputeWidth(System.Random random, int changePercentage, int minWidth, int maxWidth,
            int currentWidth) {
            if (random.Next(0, 100) >= changePercentage) {
                var widthChange = Random.Range(-maxWidth, maxWidth);
                currentWidth += widthChange;
                currentWidth = Mathf.Clamp(currentWidth, minWidth, maxWidth);
            }

            return currentWidth;
        }

        /// <summary>
        /// 주 진행 방향에 수직인 경로의 위치를 무작위로 결정합니다.
        /// </summary>
        /// <param name="random">난수 생성기 인스턴스</param>
        /// <param name="currentCoordinate">현재 경로의 수직 좌표 (예: x 또는 y)</param>
        /// <param name="maxDisplacement">한 번에 꺾일 수 있는 최대 거리</param>
        /// <param name="changePercentage">위치가 변경되지 않을 확률(%)</param>
        /// <param name="pathWidth">현재 경로의 폭 (경계선 계산에 사용)</param>
        /// <param name="boundarySize">경로가 생성되는 공간의 최대 경계 (예: 맵의 너비 또는 높이)</param>
        /// <returns></returns>
        private static int DetermineNextStep(System.Random random, int currentCoordinate, int maxDisplacement,
            int changePercentage, int pathWidth, int boundarySize) {
            if (random.Next(0, 100) >= changePercentage) {
                var change = Random.Range(-maxDisplacement, maxDisplacement);
                currentCoordinate += change;

                var minBound = pathWidth;
                var maxBound = boundarySize - pathWidth;

                currentCoordinate = Mathf.Clamp(currentCoordinate, minBound, maxBound);
            }

            return currentCoordinate;
        }
    }
}