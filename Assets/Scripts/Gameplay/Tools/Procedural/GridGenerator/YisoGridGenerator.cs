using System.Collections.Generic;
using Gameplay.Tools.Procedural.TilemapGenerator;
using Gameplay.Tools.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay.Tools.Procedural.GridGenerator {
    public class YisoGridGenerator {
        public static int[,] PrepareGrid(ref int width, ref int height) {
            var grid = new int[width, height];
            return grid;
        }

        /// <summary>
        /// 그냥 grid에 값 설정.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <returns>설정 제대로 됐냐 안됐냐 여부</returns>
        public static bool SetGridCoordinate(int[,] grid, int x, int y, int value) {
            if (
                (x >= 0)
                && (x <= grid.GetUpperBound(0))
                && (y >= 0)
                && (y <= grid.GetUpperBound(1))
            ) {
                grid[x, y] = value;
                return true;
            }

            return false;
        }

        public static int[,] TilemapToGrid(Tilemap tilemap, int width, int height) {
            if (tilemap == null) {
                Debug.LogError(
                    "[YisoGridGenerator] You're trying to convert a tilemap into a grid but didn't specify what tilemap to convert.");
                return null;
            }

            var grid = new int[width, height];
            var currentPosition = Vector3Int.zero;

            for (var i = 0; i < width; i++) {
                for (var j = 0; j < height; j++) {
                    currentPosition.x = i;
                    currentPosition.y = j;
                    currentPosition += YisoTilemapGridRenderer.ComputeOffset(width - 1, height - 1);
                    grid[i, j] = tilemap.GetTile(currentPosition) == null ? 0 : 1;
                }
            }

            return grid;
        }

        public static int GetValueAtGridCoordinate(int[,] grid, int x, int y, int errorValue) {
            if (
                (x >= 0)
                && (x <= grid.GetUpperBound(0))
                && (y >= 0)
                && (y <= grid.GetUpperBound(1))
            ) {
                return grid[x, y];
            }

            return errorValue;
        }

        public static int[,] InvertGrid(int[,] grid) {
            for (var i = 0; i <= grid.GetUpperBound(0); i++) {
                for (var j = 0; j <= grid.GetUpperBound(1); j++) {
                    grid[i, j] = grid[i, j] == 0 ? 1 : 0;
                }
            }

            return grid;
        }

        public static int[,] SmoothenGrid(int[,] grid) {
            var width = grid.GetUpperBound(0);
            var height = grid.GetUpperBound(1);

            for (var i = 0; i <= width; i++) {
                for (var j = 0; j <= height; j++) {
                    var adjacentWallsCount = GetAdjacentWallsCount(grid, i, j);
                    if (adjacentWallsCount > 4) {
                        grid[i, j] = 1;
                    }
                    else if (adjacentWallsCount < 4) {
                        grid[i, j] = 0;
                    }
                }
            }
            return grid;
        }

        /// <summary>
        /// 캐릭터 스폰위치에 벽(1)이 있으면 안되니까 그 부분 비위주는 함수.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="safeSpots"></param>
        /// <returns></returns>
        public static int[,] ApplySafeSpots(int[,] grid, List<YisoTilemapGeneratorLayer.YisoTilemapGeneratorLayerSafeSpot> safeSpots) {
            foreach (var safeSpot in safeSpots) {
                var minX = Mathf.Min(safeSpot.start.x, safeSpot.end.x);
                var maxX = Mathf.Max(safeSpot.start.x, safeSpot.end.x);
                var minY = Mathf.Min(safeSpot.start.y, safeSpot.end.y);
                var maxY = Mathf.Max(safeSpot.start.y, safeSpot.end.y);
                
                for (var i = minX; i < maxX; i++) {
                    for (var j = minY; j < maxY; j++) {
                        SetGridCoordinate(grid, i, j, 0);
                    }
                }
            }

            return grid;
        }

        /// <summary>
        /// 맵 외벽 세우는거임.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static int[,] BindGrid(int[,] grid, bool top, bool bottom, bool left, bool right) {
            var width = grid.GetUpperBound(0);
            var height = grid.GetUpperBound(1);

            if (top) {
                for (var i = 0; i <= width; i++) {
                    grid[i, height] = 1;
                }
            }

            if (bottom) {
                for (var i = 0; i <= width; i++) {
                    grid[i, 0] = 1;
                }
            }

            if (left) {
                for (var j = 0; j <= height; j++) {
                    grid[0, j] = 1;
                }
            }

            if (right) {
                for (var j = 0; j <= height; j++) {
                    grid[width, j] = 1;
                }
            }

            return grid;
        }

        /// <summary>
        /// 인접 벽 개수
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int GetAdjacentWallsCount(int[,] grid, int x, int y) {
            var width = grid.GetUpperBound(0);
            var height = grid.GetUpperBound(1);
            var wallCount = 0;
            for (var i = x - 1; i <= x + 1; i++) {
                for (var j = y - 1; j <= y + 1; j++) {
                    if ((i >= 0) && (i <= width) && (j >= 0) && (j <= height)) {
                        if ((i != x) || (j != y)) {
                            wallCount += grid[i, j];
                        }
                    }
                    else {
                        wallCount++;
                    }
                }
            }

            return wallCount;
        }
    }
}