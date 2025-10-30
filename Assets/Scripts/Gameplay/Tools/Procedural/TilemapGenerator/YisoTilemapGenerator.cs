using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Behaviour;
using DG.Tweening;
using Gameplay.Tools.Procedural.GridGenerator;
using Gameplay.Tools.Tilemaps;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace Gameplay.Tools.Procedural.TilemapGenerator {
    public class YisoTilemapGenerator: RunIBehaviour {
        [Header("Grid")]
        public Vector2Int gridWidth = new Vector2Int(50, 50); // min max
        public Vector2Int gridHeight = new Vector2Int(50, 50); // min max

        [Header("Data")]
        public List<YisoTilemapGeneratorLayer> layers;
        public int globalSeed = 0;
        public bool randomizeGlobalSeed = true;
        
        [Header("Slow Render")]
        public bool slowRender = false;
        public float slowRenderDuration = 1f;
        public Ease slowRenderTweenType = Ease.InOutCubic;

        protected int[,] _grid;
        
        public enum GenerateMethods {
            Full,
            Perlin,
            PerlinGround,
            Random,
            RandomWalk,
            RandomWalkAvoider,
            RandomWalkGround,
            Path,
            Copy
        }

        [Button]
        public virtual void Generate() {
            Random.InitState((int) DateTime.Now.Ticks);
            if (randomizeGlobalSeed) {
                globalSeed = Mathf.Abs(Random.Range(int.MinValue, int.MaxValue));
            }

            foreach (var layer in layers) {
                GenerateLayer(layer);
            }
        }

        protected virtual void GenerateLayer(YisoTilemapGeneratorLayer layer) {
            if (!layer.active) return;
            
            if (layer.targetTilemap == null) { Debug.LogError("Tilemap Generator : you need to specify a Target Tilemap to paint on."); }
            if (layer.tile == null) { Debug.LogError("Tilemap Generator : you need to specify a Tile to paint with."); }
            if (layer.gridWidth == 0) { Debug.LogError("Tilemap Generator : grid width can't be 0."); }
            if (layer.gridHeight == 0) { Debug.LogError("Tilemap Generator : grid height can't be 0."); }

            var globalSeedFloat = 0f;
            var layerSeedFloat = 0f;

            Random.InitState(globalSeed);
            var width = layer.overrideGridSize ? layer.gridWidth : Random.Range(gridWidth.x, gridWidth.y);
            var height = layer.overrideGridSize ? layer.gridHeight : Random.Range(gridHeight.x, gridHeight.y);

            globalSeedFloat = Random.value;

            if (layer.doNotUseGlobalSeed) {
                Random.InitState((int) DateTime.Now.Ticks);
                if (layer.randomizeSeed) {
                    layer.seed = Mathf.Abs(Random.Range(int.MinValue, int.MaxValue));
                }
                Random.InitState(layer.seed);
                layerSeedFloat = Random.value;
            }
            
            var seed = layer.doNotUseGlobalSeed ? layer.seed : globalSeed;
            var seedFloat = layer.doNotUseGlobalSeed ? layerSeedFloat : globalSeedFloat;

            switch (layer.generateMethod) {
                case GenerateMethods.Full:
                    _grid = YisoGridGeneratorFull.Generate(width, height, layer.fullGenerationFilled);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.Perlin:
                    _grid = YisoGridGeneratorPerlinNoise.Generate(width, height, seedFloat);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.PerlinGround:
                    _grid = YisoGridGeneratorPerlinNoiseGround.Generate(width, height, seedFloat);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.Random:
                    _grid = YisoGridGeneratorRandom.Generate(width, height, seed, layer.randomFillPercentage);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.RandomWalk:
                    _grid = YisoGridGeneratorRandomWalk.Generate(width, height, seed, layer.randomWalkPercent, layer.randomWalkStartingPoint, layer.randomWalkMaxIterations);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.RandomWalkAvoider:
                    var obstacleGrid = YisoGridGenerator.TilemapToGrid(layer.randomWalkAvoiderObstaclesTilemap, width, height);
                    _grid = YisoGridGeneratorRandomWalkAvoider.Generate(width, height, seed,
                        layer.randomWalkAvoiderPercent, layer.randomWalkAvoiderStartingPoint, obstacleGrid,
                        layer.randomWalkAvoiderObstaclesDistance, layer.randomWalkAvoiderMaxIterations);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.RandomWalkGround:
                    _grid = YisoGridGeneratorRandomWalkGround.Generate(width, height, seed, layer.randomWalkGroundMinHeightDifference, layer.randomWalkGroundMaxHeightDifference, layer.randomWalkGroundMinFlatDistance, layer.randomWalkGroundMaxFlatDistance, layer.randomWalkGroundMaxHeight);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.Path:
                    _grid = YisoGridGeneratorPath.Generate(width, height, seed, layer.pathDirection, layer.pathStartPosition, layer.pathMinWidth, layer.pathMaxWidth, layer.pathDirectionChangeDistance, layer.pathWidthChangePercentage, layer.pathDirectionChangePercentage);
                    layer.Grid = _grid;
                    break;
                case GenerateMethods.Copy:
                    layer.targetTilemap.ClearAllTiles();
                    DelayedCopy(layer);
                    break;
            }

            if (layer.smooth) _grid = YisoGridGenerator.SmoothenGrid(_grid);
            if (layer.invertGrid) _grid = YisoGridGenerator.InvertGrid(_grid);
            
            _grid = YisoGridGenerator.BindGrid(_grid, layer.boundsTop, layer.boundsBottom, layer.boundsLeft, layer.boundsRight);
            _grid = YisoGridGenerator.ApplySafeSpots(_grid, layer.safeSpots);
            
            RenderGrid(layer);
        }

        protected virtual void RenderGrid(YisoTilemapGeneratorLayer layer) {
            YisoTilemapGridRenderer.RenderGrid(_grid, layer, slowRender, slowRenderDuration, slowRenderTweenType, this);
        }

        private static async void DelayedCopy(YisoTilemapGeneratorLayer layer) {
            await Task.Delay(500);
            Copy(layer.copyTilemap, layer.targetTilemap);
        }

        private static void Copy(Tilemap source, Tilemap destination) {
            source.RefreshAllTiles();
            destination.RefreshAllTiles();

            var referenceTilemapPositions = new List<Vector3Int>();

            foreach (var pos in source.cellBounds.allPositionsWithin) {
                var localPlace = new Vector3Int(pos.x, pos.y, pos.z);
                if (source.HasTile(localPlace)) {
                    referenceTilemapPositions.Add(localPlace);
                }
            }

            var positions = new Vector3Int[referenceTilemapPositions.Count];
            var allTiles = new TileBase[referenceTilemapPositions.Count];
            var i = 0;
            foreach (var tilePosition in referenceTilemapPositions) {
                positions[i] = tilePosition;
                allTiles[i] = source.GetTile(tilePosition);
                i++;
            }
            
            destination.ClearAllTiles();
            destination.RefreshAllTiles();
            destination.size = source.size;
            destination.origin = source.origin;
            destination.ResizeBounds();
            destination.SetTiles(positions, allTiles);
        }

        private void Reset() {
            layers = new List<YisoTilemapGeneratorLayer>();
        }

        protected virtual void OnValidate() {
            if (layers == null || layers.Count <= 0) {
                return;
            }

            foreach (var layer in layers) {
                layer.SetDefaults();
            }
        }
    }
}