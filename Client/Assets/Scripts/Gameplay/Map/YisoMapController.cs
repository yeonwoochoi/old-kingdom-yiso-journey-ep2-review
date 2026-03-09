using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Map {
    public enum MapObjectType {
        None = 0,
        Player = 1,
        Enemy = 2,
        Npc = 3,
        Etc = 4
    }

    public enum MapType {
        BaseCamp, // 베이스 캠프
        MainTown, // 중심 마을
        Outfield, // 주변 마을
        Dungeon, // 던전
        BossRoom // 보스방
    }
    
    public interface IMapObject<T> where T: Enum {
        public T GetObjectType();
    }

    public abstract class MapObjectBase: IMapObject<MapObjectType> {
        public abstract MapObjectType GetObjectType();
    }

    public class NpcObject : MapObjectBase {
        public Vector3 position;
        
        public override MapObjectType GetObjectType() => MapObjectType.Npc;
    }

    public class EnemyObject : MapObjectBase {
        public Vector3 position;
        
        public override MapObjectType GetObjectType() => MapObjectType.Enemy;
    }

    public class ChapterData {
        public int chapterId;
        public List<int> maps;
        
        public List<MapData> mainFields; // 0번이 시작 field
        public List<MapData> outFields;
        public List<MapData> dungeons;
        public List<MapData> bossRooms;
        
        public List<MapConnection> mapConnections;
    }

    public class MapConnection {
        public int mapId1;
        public int mapId2;
    }
    
    public class MapData {
        public int mapId;
        public string addressableKey;
        public MapType mapType;

        public List<int> neighborMapIds;
        
        public List<NpcObject> npcObjects;
        public List<EnemyObject> enemyObjects;
    }

    public class AddressableLoader {
        private static Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize() {
            _handles.Clear();
        }

        public static async Task<GameObject> LoadAsync(string key) {
            if (_handles.TryGetValue(key, out var existing))
                return existing.Result;
            
            var handle = Addressables.LoadAssetAsync<GameObject>(key);
            _handles.Add(key, handle);
            await handle.Task;
            
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                _handles.Remove(key);
                return null;
            }
            return handle.Result;
        }

        public static async Task<GameObject> InstantiateAsync(string key) {
            var handle = Addressables.InstantiateAsync(key);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) {
                return null;
            }

            return handle.Result;
        }

        public static void Release(string key) {
            if (!_handles.TryGetValue(key, out var handle))
                return;

            Addressables.Release(handle);
            _handles.Remove(key);
        }

        public static void Release(GameObject obj) {
            Addressables.ReleaseInstance(obj);
        }
    }
    
    public class YisoMapController: MonoBehaviour {
        private Dictionary<int, MapData> _mapData = new(); // caching
        private HashSet<Tuple<int, int>> _mapConnections = new();

        public ChapterData CurrentChapterData { get; private set; }
        public MapData CurrentMap { get; private set; }
        
        public int CurrentChapterId => CurrentChapterData?.chapterId ?? -1;
        public int CurrentMapId => CurrentMap?.mapId ?? -1;

        private int _lastMapId;
        private Vector3 _lastPlayerPosition;

        public event Action<MapData> OnMapLoaded; // TODO: 각 load, unload 타이밍이 맞는 event 추가하기 
        public event Action<MapData> OnMapChanged;

        #region Load & Unload

        public void LoadAll() {
            // 초기화
            _mapData.Clear();
            _mapConnections.Clear();
            
            // Save Data 받아와 (Player)
            LoadPlayerData();
            
            // 1. Chapter Data 받아옴
            LoadChapterData(CurrentChapterId);
            
            // 2. Map Data 받아
            LoadMapData(CurrentChapterId, CurrentMapId);
            
            // 3. Load Map Object (Fire And Forget)
            _ = LoadMapObject(CurrentMapId, true);
        }

        public void UnloadAll() {
            UnloadAllMaps();
        }

        public void LoadPlayerData() {
            
        }
        
        public void LoadChapterData(int chapter) {
            
        }

        public void LoadMapData(int chapterId, int mapId) {
            if (_mapData.TryGetValue(mapId, out var mapData)) {
                CurrentMap = mapData;
                return;
            }
            
            // TODO 맵 받아오고
            // TODO 연결된 map 정보까지 받아와
        }

        public async Task LoadMapObject(int mapId, bool isInit = false) {
            if (!_mapData.TryGetValue(mapId, out var mapData)) {
                return;
            }

            try {
                if (!isInit) {
                    // todo: 기존맵 정리 로직   
                }
                
                var mapObj = await AddressableLoader.LoadAsync(mapData.addressableKey);
                if (mapObj == null) {
                    throw new Exception("Failed to load map object");
                }

                if (isInit) {
                    OnMapLoaded?.Invoke(mapData);   
                }
                else {
                    OnMapChanged?.Invoke(mapData);
                }
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public void UnloadMap(int mapId) {
            if (!_mapData.TryGetValue(mapId, out var mapData)) {
                return;
            }
            AddressableLoader.Release(mapData.addressableKey);
        }

        public void UnloadAllMaps() {
            // TODO
        }

        #endregion

        #region Map Connection

        public void MoveToNextMap(int nextMapId) {
            LoadMapData(CurrentChapterId, nextMapId);
            LoadMapObject(nextMapId);
        }

        #endregion
    }
}