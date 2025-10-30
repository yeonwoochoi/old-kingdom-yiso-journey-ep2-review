using System;
using System.Collections.Generic;
using Gameplay.Tools.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace Gameplay.Tools.Procedural.TilemapGenerator {
    public class YisoTilemapStageGenerator: YisoTilemapGenerator {
        [Serializable]
        public class SpawnData {
            public GameObject prefab;
            public int quantity;
        }

        [Header("Settings")]
        public bool generateOnAwake = true;
        public Grid targetGrid;
        public Tilemap obstacleTilemap;
        public BoxCollider2D mapBoundary;

        [Header("Spawn")]
        public Transform initialSpawn;
        public Transform exit;
        public float minDistanceFromSpawnToExit = 2f;
        
        public List<SpawnData> prefabsToSpawn = new List<SpawnData>();
        public float prefabsSpawnMinDistance = 2f;
        
        protected const int _maxIterationsCount = 100;
        protected List<Vector3> _filledPositions;

        protected override void Awake() {
            base.Awake();
            if (generateOnAwake) {
                Generate();
            }
        }

        public override void Generate() {
            base.Generate();
            _filledPositions = new List<Vector3>();
            PlaceEntryAndExit();
            SpawnPrefabs();
            ResizeCameraBoundary();
        }

        protected virtual void ResizeCameraBoundary() {
            if (mapBoundary == null) {
                if (!TryGetComponent(out mapBoundary)) {
                    mapBoundary = gameObject.AddComponent<BoxCollider2D>();
                    mapBoundary.isTrigger = true;
                }
            }

            var bounds = obstacleTilemap.localBounds;
            mapBoundary.offset = bounds.center;
            mapBoundary.size = new Vector2(bounds.size.x, bounds.size.y);
        }

        protected virtual void PlaceEntryAndExit() {
            Random.InitState(globalSeed);
            var width = Random.Range(gridWidth.x, gridWidth.y);
            var height = Random.Range(gridHeight.x, gridHeight.y);

            var spawnPosition = YisoTilemap.GetRandomPosition(obstacleTilemap, targetGrid, width, height, false, width * height * 2);
            initialSpawn.transform.position = spawnPosition;
            _filledPositions.Add(spawnPosition);

            var exitPosition = spawnPosition;
            var iterationsCount = 0;

            while (iterationsCount < _maxIterationsCount && Vector3.Distance(exitPosition, spawnPosition) < minDistanceFromSpawnToExit) {
                exitPosition = YisoTilemap.GetRandomPosition(obstacleTilemap, targetGrid, width, height, false, width * height * 2);
                exit.transform.position = exitPosition;
                iterationsCount++;
            }
            _filledPositions.Add(exit.transform.position);
        }

        protected virtual void SpawnPrefabs() {
            if (!Application.isPlaying) return;
            
            Random.InitState(globalSeed);
            var width = Random.Range(gridWidth.x, gridWidth.y);
            var height = Random.Range(gridHeight.x, gridHeight.y);

            foreach (var data in prefabsToSpawn) {
                for (var i = 0; i < data.quantity; i++) {
                    var spawnPosition = Vector3.zero;
                    var tooClose = true;
                    var iterationsCount = 0;

                    while (tooClose && (iterationsCount < _maxIterationsCount)) {
                        spawnPosition = YisoTilemap.GetRandomPosition(obstacleTilemap, targetGrid, width, height, false, width * height * 2);
                        tooClose = false;
                        foreach (var filledPosition in _filledPositions) {
                            if (Vector3.Distance(spawnPosition, filledPosition) < prefabsSpawnMinDistance) {
                                tooClose = true;
                                break;
                            }
                        }
                        iterationsCount++;
                    }

                    Instantiate(data.prefab, spawnPosition, Quaternion.identity);
                    _filledPositions.Add(spawnPosition);
                }
            }
        }
    }
}