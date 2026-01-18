using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils {
    /// <summary>
    /// Unity 기본 타입들에 대한 Extension 메서드 모음.
    /// GetOrAddComponent, Instantiate 래핑 등 자주 사용하는 유틸리티 제공.
    ///
    /// [주의] Static 캐싱은 메모리 누수 위험이 있어 제공하지 않습니다.
    /// 자주 접근하는 컴포넌트 캐싱이 필요하면 ComponentCacheLocal을 사용하세요.
    /// </summary>
    public static class YisoUnityExtensions {

        #region Component Extensions

        /// <summary>
        /// 컴포넌트가 있으면 가져오고, 없으면 추가하여 반환합니다.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        /// <summary>
        /// 컴포넌트가 있으면 가져오고, 없으면 추가하여 반환합니다. (Component 버전)
        /// </summary>
        public static T GetOrAddComponent<T>(this Component comp) where T : Component {
            return comp.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// 부모 또는 자식에서 컴포넌트를 찾습니다.
        /// </summary>
        /// <param name="includeInactive">비활성화된 오브젝트도 검색할지 여부</param>
        public static T GetComponentInParentOrChildren<T>(this GameObject go, bool includeInactive = false) where T : Component {
            // 1. 자기 자신
            var component = go.GetComponent<T>();
            if (component != null) return component;

            // 2. 부모에서 검색
            component = go.GetComponentInParent<T>(includeInactive);
            if (component != null) return component;

            // 3. 자식에서 검색
            return go.GetComponentInChildren<T>(includeInactive);
        }

        /// <summary>
        /// 부모 또는 자식에서 컴포넌트를 찾습니다. (Component 버전)
        /// </summary>
        public static T GetComponentInParentOrChildren<T>(this Component comp, bool includeInactive = false) where T : Component {
            return comp.gameObject.GetComponentInParentOrChildren<T>(includeInactive);
        }

        #endregion

        #region Destroy Extensions

        /// <summary>
        /// Editor와 Runtime 모두에서 안전하게 오브젝트를 파괴합니다.
        /// </summary>
        public static void SafeDestroy(this UnityEngine.Object obj) {
            if (obj == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }

        /// <summary>
        /// GameObject를 안전하게 파괴합니다.
        /// </summary>
        public static void SafeDestroy(this GameObject go) {
            if (go == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEngine.Object.DestroyImmediate(go);
                return;
            }
#endif
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>
        /// 지연 후 안전하게 오브젝트를 파괴합니다. (Runtime 전용)
        /// </summary>
        public static void SafeDestroy(this UnityEngine.Object obj, float delay) {
            if (obj == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj, delay);
        }

        #endregion

        #region Instantiate Extensions

        /// <summary>
        /// Prefab을 인스턴스화합니다. (추후 오브젝트 풀링 확장 가능)
        /// </summary>
        public static T Instantiate<T>(this Component owner, T prefab) where T : UnityEngine.Object {
            return UnityEngine.Object.Instantiate(prefab, owner.transform);
        }

        /// <summary>
        /// Prefab을 지정된 위치와 회전으로 인스턴스화합니다.
        /// </summary>
        public static T Instantiate<T>(this Component owner, T prefab, Vector3 position, Quaternion rotation) where T : UnityEngine.Object {
            return UnityEngine.Object.Instantiate(prefab, position, rotation);
        }

        /// <summary>
        /// Prefab을 지정된 부모 아래에 인스턴스화합니다.
        /// </summary>
        public static T Instantiate<T>(this Component owner, T prefab, Transform parent, bool worldPositionStays = false) where T : UnityEngine.Object {
            return UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);
        }

        /// <summary>
        /// Prefab을 지정된 위치, 회전, 부모로 인스턴스화합니다.
        /// </summary>
        public static T Instantiate<T>(this Component owner, T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : UnityEngine.Object {
            return UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
        }

        #endregion

        #region Transform Extensions

        /// <summary>
        /// 모든 자식 오브젝트를 파괴합니다.
        /// </summary>
        public static void DestroyAllChildren(this Transform transform) {
            for (var i = transform.childCount - 1; i >= 0; i--) {
                transform.GetChild(i).gameObject.SafeDestroy();
            }
        }

        /// <summary>
        /// Transform의 위치, 회전, 스케일을 리셋합니다.
        /// </summary>
        public static void ResetLocal(this Transform transform) {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 2D 방향 벡터로 Transform을 회전시킵니다. (Top-Down 2D용)
        /// </summary>
        public static void LookAt2D(this Transform transform, Vector2 direction) {
            if (direction == Vector2.zero) return;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// 2D 타겟 위치를 바라보도록 Transform을 회전시킵니다.
        /// </summary>
        public static void LookAt2D(this Transform transform, Vector3 targetPosition) {
            var direction = (targetPosition - transform.position).normalized;
            transform.LookAt2D(direction);
        }

        #endregion

        #region GameObject Extensions

        /// <summary>
        /// GameObject와 모든 자식의 레이어를 설정합니다.
        /// </summary>
        public static void SetLayerRecursively(this GameObject go, int layer) {
            go.layer = layer;
            foreach (Transform child in go.transform) {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        /// <summary>
        /// GameObject와 모든 자식의 레이어를 설정합니다. (레이어 이름으로)
        /// </summary>
        public static void SetLayerRecursively(this GameObject go, string layerName) {
            go.SetLayerRecursively(LayerMask.NameToLayer(layerName));
        }

        /// <summary>
        /// GameObject가 특정 레이어에 속하는지 확인합니다.
        /// </summary>
        public static bool IsInLayerMask(this GameObject go, LayerMask layerMask) {
            return ((1 << go.layer) & layerMask) != 0;
        }

        #endregion

        #region LayerMask Extensions

        /// <summary>
        /// LayerMask에 특정 레이어가 포함되어 있는지 확인합니다.
        /// </summary>
        public static bool Contains(this LayerMask layerMask, int layer) {
            return ((1 << layer) & layerMask) != 0;
        }

        #endregion

        #region Vector Extensions

        /// <summary>
        /// Vector3를 Vector2로 변환합니다 (XY 평면).
        /// </summary>
        public static Vector2 ToVector2XY(this Vector3 v) {
            return new Vector2(v.x, v.y);
        }

        /// <summary>
        /// Vector3를 Vector2로 변환합니다 (XZ 평면).
        /// </summary>
        public static Vector2 ToVector2XZ(this Vector3 v) {
            return new Vector2(v.x, v.z);
        }

        /// <summary>
        /// Vector2를 Vector3로 변환합니다 (z = 0).
        /// </summary>
        public static Vector3 ToVector3XY(this Vector2 v, float z = 0f) {
            return new Vector3(v.x, v.y, z);
        }

        /// <summary>
        /// Vector2를 Vector3로 변환합니다 (XZ 평면, y = 0).
        /// </summary>
        public static Vector3 ToVector3XZ(this Vector2 v, float y = 0f) {
            return new Vector3(v.x, y, v.y);
        }

        /// <summary>
        /// Vector2의 특정 성분만 변경한 새 벡터를 반환합니다.
        /// </summary>
        public static Vector2 With(this Vector2 v, float? x = null, float? y = null) {
            return new Vector2(x ?? v.x, y ?? v.y);
        }

        /// <summary>
        /// Vector3의 특정 성분만 변경한 새 벡터를 반환합니다.
        /// </summary>
        public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null) {
            return new Vector3(x ?? v.x, y ?? v.y, z ?? v.z);
        }

        /// <summary>
        /// 두 Vector2 사이의 각도를 반환합니다 (도 단위).
        /// </summary>
        public static float AngleTo(this Vector2 from, Vector2 to) {
            return Vector2.SignedAngle(from, to);
        }

        /// <summary>
        /// Vector2를 지정된 각도만큼 회전시킵니다.
        /// </summary>
        public static Vector2 Rotate(this Vector2 v, float degrees) {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }

        #endregion

        #region Collection Extensions

        /// <summary>
        /// 리스트에서 랜덤한 요소를 반환합니다.
        /// </summary>
        public static T GetRandom<T>(this IList<T> list) {
            if (list == null || list.Count == 0) return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 배열에서 랜덤한 요소를 반환합니다.
        /// </summary>
        public static T GetRandom<T>(this T[] array) {
            if (array == null || array.Length == 0) return default;
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// 리스트를 Fisher-Yates 알고리즘으로 섞습니다.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list) {
            for (var i = list.Count - 1; i > 0; i--) {
                var randomIndex = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        #endregion
    }

    /// <summary>
    /// 인스턴스 단위 컴포넌트 캐싱을 위한 헬퍼 클래스.
    /// MonoBehaviour에서 필드로 선언하여 사용합니다.
    /// 오브젝트와 함께 생명주기가 관리되므로 메모리 누수가 없습니다.
    /// </summary>
    /// <example>
    /// private readonly ComponentCacheLocal _cache = new();
    ///
    /// private void Update() {
    ///     var rb = _cache.Get&lt;Rigidbody2D&gt;(gameObject);
    /// }
    /// </example>
    public class ComponentCacheLocal {
        private readonly Dictionary<Type, Component> _localCache = new();
        private GameObject _cachedGameObject;

        /// <summary>
        /// 로컬 캐시에서 컴포넌트를 가져옵니다.
        /// GameObject가 변경되면 캐시가 자동으로 비워집니다.
        /// </summary>
        public T Get<T>(GameObject go) where T : Component {
            if (go == null) return null;

            // GameObject가 변경되었으면 캐시 초기화
            if (_cachedGameObject != go) {
                _localCache.Clear();
                _cachedGameObject = go;
            }

            var type = typeof(T);

            if (_localCache.TryGetValue(type, out var cached)) {
                if (cached != null) {
                    return cached as T;
                }
                _localCache.Remove(type);
            }

            var component = go.GetComponent<T>();
            if (component != null) {
                _localCache[type] = component;
            }

            return component;
        }

        /// <summary>
        /// 로컬 캐시를 비웁니다.
        /// </summary>
        public void Clear() {
            _localCache.Clear();
            _cachedGameObject = null;
        }
    }
}
