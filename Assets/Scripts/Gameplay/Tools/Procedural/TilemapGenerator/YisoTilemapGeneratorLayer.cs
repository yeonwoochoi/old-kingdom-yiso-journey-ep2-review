using System;
using System.Collections.Generic;
using Gameplay.Tools.Procedural.GridGenerator;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay.Tools.Procedural.TilemapGenerator {
    [Serializable]
    public class YisoTilemapGeneratorLayer {
        /// <summary>
        /// 이 레이어에서 생성된 그리드 데이터입니다. (1은 채움, 0은 비움)
        /// </summary>
        public virtual int[,] Grid { get; set; }
        
        /// <summary>
        /// 이 레이어의 결과를 이전 레이어와 어떻게 합칠지 결정하는 융합 모드입니다.
        /// Normal: 대상 타일맵을 지우고 새로 그립니다.
        /// NormalNoClear: 지우지 않고 덮어 그립니다.
        /// Intersect: 이전 레이어와 겹치는 부분만 남깁니다.
        /// Combine: 이전 레이어에 결과를 더합니다.
        /// Subtract: 이전 레이어에서 결과를 뺍니다.
        /// </summary>
        public enum FusionModes { Normal, NormalNoClear, Intersect, Combine, Subtract }

        [Tooltip("이 레이어의 이름입니다. 기능에 영향을 주지 않으며, 정리용으로 사용됩니다.")]
        public string name = "Layer";
        [Tooltip("활성화된 경우에만 맵 생성 시 이 레이어가 포함됩니다.")]
        public bool active = true;

        [Header("Tilemaps")]
        [Tooltip("타일을 그릴 대상 타일맵입니다.")]
        public Tilemap targetTilemap;
        [Tooltip("타일맵에 그릴 때 사용할 타일입니다.")]
        public TileBase tile;
        
        [Header("Grid")]
        [Tooltip("전역 그리드 크기 대신 이 레이어만의 독자적인 크기를 사용할지 여부를 결정합니다.")]
        public bool overrideGridSize = false;
        [ShowIf("overrideGridSize")] [Tooltip("이 레이어에서 사용할 그리드의 너비입니다.")] public int gridWidth = 50;
        [ShowIf("overrideGridSize")] [Tooltip("이 레이어에서 사용할 그리드의 높이입니다.")] public int gridHeight = 50;

        [Header("Method")]
        [Tooltip("이 레이어의 그리드를 생성할 때 사용할 알고리즘을 선택합니다.")]
        public YisoTilemapGenerator.GenerateMethods generateMethod = YisoTilemapGenerator.GenerateMethods.Perlin;
        [Tooltip("체크하면 전역 시드(Seed)를 무시하고 이 레이어의 고유 시드를 사용합니다.")]
        public bool doNotUseGlobalSeed = false;
        [ShowIf("doNotUseGlobalSeed")] [Tooltip("생성 버튼을 누를 때마다 이 레이어의 시드를 무작위로 변경합니다.")] public bool randomizeSeed = true;
        [ShowIf("doNotUseGlobalSeed")] [Tooltip("전역 시드를 사용하지 않을 경우, 이 레이어에서 사용할 고유 시드 값입니다.")] public int seed = 1;
        
        [Header("PostProcessing")]
        [Tooltip("생성된 그리드의 외곽을 부드럽게 처리하여 고립된 타일이나 뾰족한 부분을 제거합니다.")]
        public bool smooth = false;
        [Tooltip("그리드 결과를 반전시킵니다 (채워진 곳은 비우고, 비워진 곳은 채웁니다).")]
        public bool invertGrid = false;
        [Tooltip("이 레이어의 결과를 이전 레이어의 결과와 어떻게 합칠지 결정합니다.")]
        public FusionModes fusionMode = FusionModes.Normal;

        [Header("Settings")]
        [Tooltip("'Full' 생성 방식에서 그리드를 완전히 채울지(true), 비울지(false) 결정합니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Full)]
        public bool fullGenerationFilled = false;
        
        [Tooltip("'Random' 생성 방식에서 그리드를 채울 비율(%)입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Random)]
        public int randomFillPercentage = 50;
        
        [Tooltip("'Random Walk Ground' 방식에서, 지형의 높이가 변할 때의 최소 높이 차이입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkGround)]
        public int randomWalkGroundMinHeightDifference = 1;
        [Tooltip("'Random Walk Ground' 방식에서, 지형의 높이가 변할 때의 최대 높이 차이입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkGround)]
        public int randomWalkGroundMaxHeightDifference = 1;
        [Tooltip("'Random Walk Ground' 방식에서, 평평한 지형이 유지되는 최소 거리입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkGround)]
        public int randomWalkGroundMinFlatDistance = 1;
        [Tooltip("'Random Walk Ground' 방식에서, 평평한 지형이 유지되는 최대 거리입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkGround)]
        public int randomWalkGroundMaxFlatDistance = 1;
        [Tooltip("'Random Walk Ground' 방식에서, 생성될 수 있는 지형의 최대 높이입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkGround)]
        public int randomWalkGroundMaxHeight = 1;
        
        [Tooltip("'Random Walk' 방식에서, 전체 맵에서 채우려고 시도할 영역의 비율(%)입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalk)]
        public int randomWalkPercent = 1;
        [Tooltip("'Random Walk' 방식에서, 걷기를 시작할 좌표입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalk)]
        public Vector2Int randomWalkStartingPoint = Vector2Int.zero;
        [Tooltip("'Random Walk' 방식에서, 알고리즘이 중단되기 전까지의 최대 반복 횟수입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalk)]
        public int randomWalkMaxIterations = 1500;
        
        [Tooltip("'Random Walk Avoider' 방식에서, 채우려고 시도할 영역의 비율(%)입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkAvoider)]
        public int randomWalkAvoiderPercent = 50;
        [Tooltip("'Random Walk Avoider' 방식에서, 걷기를 시작할 좌표입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkAvoider)]
        public Vector2Int randomWalkAvoiderStartingPoint = Vector2Int.zero;
        [Tooltip("'Random Walk Avoider' 방식에서, 회피할 장애물이 있는 타일맵입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkAvoider)]
        public Tilemap randomWalkAvoiderObstaclesTilemap;
        [Tooltip("'Random Walk Avoider' 방식에서, 장애물로부터 유지하려는 최소 거리입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkAvoider)]
        public int randomWalkAvoiderObstaclesDistance = 1;
        [Tooltip("'Random Walk Avoider' 방식에서, 알고리즘의 최대 반복 횟수입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.RandomWalkAvoider)]
        public int randomWalkAvoiderMaxIterations = 100;
        
        [Tooltip("'Path' 방식에서, 경로가 시작될 좌표입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Path)]
        public Vector2Int pathStartPosition = Vector2Int.zero;
        [Tooltip("'Path' 방식에서, 경로가 진행될 주 방향입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Path)]
        public YisoGridGeneratorPath.Directions pathDirection = YisoGridGeneratorPath.Directions.BottomToTop;
        [Tooltip("'Path' 방식에서, 경로의 최소 너비입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Path)]
        public int pathMinWidth = 2;
        [Tooltip("'Path' 방식에서, 경로의 최대 너비입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Path)]
        public int pathMaxWidth = 4;
        [Tooltip("'Path' 방식에서, 경로가 방향을 바꿀 때 꺾이는 최대 거리입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Path)]
        public int pathDirectionChangeDistance = 2;
        [Tooltip("'Path' 방식에서, 매 단계마다 경로의 너비가 변경될 확률(%)입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Path)]
        public int pathWidthChangePercentage = 50;
        [Tooltip("'Path' 방식에서, 매 단계마다 경로의 방향이 변경될 확률(%)입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Path)]
        public int pathDirectionChangePercentage = 50;
        
        [Tooltip("'Copy' 방식에서, 복사할 원본 타일맵입니다.")]
        [ShowIf("generateMethod", YisoTilemapGenerator.GenerateMethods.Copy)]
        public Tilemap copyTilemap;

        [Header("Bounds")]
        [Tooltip("그리드의 위쪽 가장자리를 벽으로 막습니다.")]
        public bool boundsTop = false;
        [Tooltip("그리드의 아래쪽 가장자리를 벽으로 막습니다.")]
        public bool boundsBottom = false;
        [Tooltip("그리드의 왼쪽 가장자리를 벽으로 막습니다.")]
        public bool boundsLeft = false;
        [Tooltip("그리드의 오른쪽 가장자리를 벽으로 막습니다.")]
        public bool boundsRight = false;
        
        /// <summary>
        /// 타일 생성을 제외할 '안전 구역'의 좌표를 정의합니다.
        /// </summary>
        [Serializable]
        public struct YisoTilemapGeneratorLayerSafeSpot {
            public Vector2Int start;
            public Vector2Int end;
        }

        [Header("Safe Spot")]
        [Tooltip("비워진 상태로 유지할 '안전 구역' 목록입니다. 이 구역에는 타일이 생성되지 않습니다.")]
        public List<YisoTilemapGeneratorLayerSafeSpot> safeSpots;

        [HideInInspector] public bool initialized = false;

        public virtual void SetDefaults() {
            if (initialized) return;
            
            gridWidth = 50;
            gridHeight = 50;
            generateMethod = YisoTilemapGenerator.GenerateMethods.Perlin;
            randomizeSeed = true;
            doNotUseGlobalSeed = false;
            fusionMode = FusionModes.Normal;
            seed = 123456789;
            smooth = false;
            invertGrid = false;
            fullGenerationFilled = true;
            randomFillPercentage = 50;
            randomWalkGroundMinHeightDifference = 1;
            randomWalkGroundMaxHeightDifference = 3;
            randomWalkGroundMinFlatDistance = 1;
            randomWalkGroundMaxFlatDistance = 3;
            randomWalkGroundMaxHeight = 8;
            randomWalkPercent = 50;
            randomWalkStartingPoint = Vector2Int.zero;
            randomWalkMaxIterations = 1500;
            pathMinWidth = 2;
            pathMaxWidth = 4;
            pathDirectionChangeDistance = 2;
            pathWidthChangePercentage = 50;
            pathDirectionChangePercentage = 50;
            randomWalkAvoiderPercent = 50;
            randomWalkAvoiderStartingPoint = Vector2Int.zero;
            randomWalkAvoiderObstaclesTilemap = null;
            randomWalkAvoiderObstaclesDistance = 1;
            randomWalkAvoiderMaxIterations = 100;
            boundsTop = false; 
            boundsBottom = false; 
            boundsLeft = false; 
            boundsRight = false;
            pathStartPosition = Vector2Int.zero;
            pathDirection = YisoGridGeneratorPath.Directions.BottomToTop;
            initialized = true;
        }
    }
}