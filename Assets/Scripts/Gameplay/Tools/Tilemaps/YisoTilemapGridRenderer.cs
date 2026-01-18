using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using Gameplay.Tools.Procedural.GridGenerator;
using Gameplay.Tools.Procedural.TilemapGenerator;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils;

namespace Gameplay.Tools.Tilemaps {
    public class YisoTilemapGridRenderer {
        public static void RenderGrid(int[,] grid, YisoTilemapGeneratorLayer layer, bool slowRender = false,
            float slowRenderDuration = 1f, Ease slowRenderTweenType = Ease.InOutCubic,
            MonoBehaviour slowRenderSupport = null) {
            if (layer.fusionMode == YisoTilemapGeneratorLayer.FusionModes.Normal) {
                ClearTilemap(layer.targetTilemap);
            }

            var tile = layer.tile;
            if (layer.fusionMode == YisoTilemapGeneratorLayer.FusionModes.Combine) {
                grid = YisoGridGenerator.InvertGrid(grid);
                tile = null;
            }

            if (layer.fusionMode == YisoTilemapGeneratorLayer.FusionModes.Subtract) {
                grid = YisoGridGenerator.InvertGrid(grid);
            }

            if (!slowRender || !Application.isPlaying) {
                DrawGrid(grid, layer.targetTilemap, tile, 0, TotalFilledBlocks(grid));
            }
            else {
                slowRenderSupport?.StartCoroutine(SlowRenderGrid(grid, layer.targetTilemap, tile, slowRenderDuration,
                    slowRenderTweenType, 60));
            }

            if (!Application.isPlaying && slowRender) {
                YisoLogger.LogWarning("Rendering maps in SlowRender mode is only supported at runtime.");
            }
        }

        public static IEnumerator SlowRenderGrid(int[,] grid, Tilemap tilemap, TileBase tile, float slowRenderDuration,
            Ease slowRenderTweenType, int frameRate) {
            var totalBlocks = TotalFilledBlocks(grid);
            totalBlocks = totalBlocks == 0 ? 1 : totalBlocks;
            frameRate = frameRate == 0 ? 1 : frameRate;
            
            var refreshFrequency = 1f / frameRate;
            var startedAt = Time.unscaledTime;
            var lastWaitAt = startedAt;
            var drawnBlocks = 0;
            var lastIndex = 0;

            while (Time.unscaledTime - startedAt < slowRenderDuration) {
                while (Time.unscaledTime - lastWaitAt < refreshFrequency) {
                    yield return null;
                }
                
                var remainingBlocks = totalBlocks - drawnBlocks;
                var elapsedTime = Time.unscaledTime - startedAt;
                var remainingTime = slowRenderDuration - elapsedTime;
                var normalizedProgress = YisoMathUtils.Remap(elapsedTime, 0f, slowRenderDuration, 0f, 1f);
                var curveProgress = EaseManager.Evaluate(slowRenderTweenType, null, normalizedProgress, 0f, 1f, 0f);
                var ratio = 1 - (normalizedProgress - curveProgress);

                var blocksToDraw = Mathf.RoundToInt(remainingBlocks / remainingTime * refreshFrequency * ratio);

                lastIndex = DrawGrid(grid, tilemap, tile, lastIndex, blocksToDraw);
                drawnBlocks += blocksToDraw;
                lastWaitAt = Time.unscaledTime;
            }
            DrawGrid(grid, tilemap, tile, lastIndex, totalBlocks - lastIndex);
        }

        public static int TotalFilledBlocks(int[,] grid) {
            var width = grid.GetUpperBound(0);
            var height = grid.GetUpperBound(1);

            var totalBlocks = 0;
            for (var i = 0; i <= width; i++) {
                for (var j = 0; j <= height; j++) {
                    if (grid[i, j] == 1) {
                        totalBlocks++;
                    }
                }
            }

            return totalBlocks;
        }

        private static int DrawGrid(int[,] grid, Tilemap tilemap, TileBase tile, int startIndex,
            int numberOfTilesToDraw) {
            var width = grid.GetUpperBound(0);
            var height = grid.GetUpperBound(1);

            tilemap.RefreshAllTiles();

            var counter = 0;
            var drawCount = 0;

            for (var i = 0; i <= width; i++) {
                for (var j = 0; j <= height; j++) {
                    if (grid[i, j] == 1) {
                        if (counter >= startIndex) {
                            var tilePosition = new Vector3Int(i, j, 0);
                            tilePosition += ComputeOffset(width, height);
                            tilemap.SetTile(tilePosition, tile);
                            drawCount++;
                        }

                        if (drawCount > numberOfTilesToDraw) {
                            return counter;
                        }

                        counter++;
                    }
                }
            }

            return counter;
        }

        public static Vector3Int ComputeOffset(int width, int height) {
            var offsetX = Mathf.RoundToInt(-(width + 1) / 2.0f);
            var offsetY = Mathf.RoundToInt(-(height + 1) / 2.0f);

            return new Vector3Int(offsetX, offsetY, 0);
        }

        public static void ClearTilemap(Tilemap tilemap) {
            tilemap.ClearAllTiles();
            tilemap.RefreshAllTiles();
        }
    }
}