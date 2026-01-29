using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Character.StateMachine;
using UnityEditor;
using UnityEngine;

namespace Editor.FSM.Utils {
    /// <summary>
    /// FSM Prefab 로드/저장을 위한 유틸리티 클래스
    /// GUID 유지를 보장하며 Prefab 무결성을 관리
    /// </summary>
    public static class YisoFSMPrefabHelper {
        /// <summary>
        /// Prefab에서 FSM 컴포넌트를 로드합니다.
        /// 수정 가능한 Prefab 인스턴스를 반환합니다.
        /// </summary>
        public static (GameObject prefabContents, YisoCharacterStateMachine fsm, string assetPath) LoadPrefab(GameObject prefabAsset) {
            if (prefabAsset == null) {
                Debug.LogError("[FSM Editor] Prefab이 null입니다.");
                return (null, null, null);
            }

            var assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(assetPath)) {
                Debug.LogError("[FSM Editor] Prefab 경로를 찾을 수 없습니다.");
                return (null, null, null);
            }

            // Prefab 내용을 로드 (수정 가능한 임시 인스턴스)
            var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
            if (prefabContents == null) {
                Debug.LogError($"[FSM Editor] Prefab 로드 실패: {assetPath}");
                return (null, null, null);
            }

            // FSM 컴포넌트 찾기
            var fsm = prefabContents.GetComponent<YisoCharacterStateMachine>();
            if (fsm == null) {
                Debug.LogError($"[FSM Editor] Prefab에 YisoCharacterStateMachine이 없습니다: {assetPath}");
                PrefabUtility.UnloadPrefabContents(prefabContents);
                return (null, null, null);
            }

            return (prefabContents, fsm, assetPath);
        }

        /// <summary>
        /// 수정된 Prefab 내용을 저장합니다.
        /// GUID를 유지하면서 저장합니다.
        /// </summary>
        public static bool SavePrefab(GameObject prefabContents, string assetPath) {
            if (prefabContents == null || string.IsNullOrEmpty(assetPath)) {
                Debug.LogError("[FSM Editor] 저장할 Prefab이 유효하지 않습니다.");
                return false;
            }

            try {
                // Prefab 저장 (기존 GUID 유지)
                PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
                Debug.Log($"[FSM Editor] Prefab 저장 완료: {assetPath}");
                return true;
            }
            catch (Exception e) {
                Debug.LogError($"[FSM Editor] Prefab 저장 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Prefab 편집을 종료하고 리소스를 해제합니다.
        /// 저장하지 않은 변경사항은 버려집니다.
        /// </summary>
        public static void UnloadPrefab(GameObject prefabContents) {
            if (prefabContents != null) {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        /// <summary>
        /// Prefab 내에서 Actions 컨테이너 GameObject를 찾거나 생성합니다.
        /// </summary>
        public static Transform GetOrCreateActionsContainer(GameObject prefabRoot) {
            var existing = prefabRoot.transform.Find("Actions");
            if (existing != null) return existing;

            var container = new GameObject("Actions");
            container.transform.SetParent(prefabRoot.transform);
            container.transform.localPosition = Vector3.zero;
            return container.transform;
        }

        /// <summary>
        /// Prefab 내에서 Decisions 컨테이너 GameObject를 찾거나 생성합니다.
        /// </summary>
        public static Transform GetOrCreateDecisionsContainer(GameObject prefabRoot) {
            var existing = prefabRoot.transform.Find("Decisions");
            if (existing != null) return existing;

            var container = new GameObject("Decisions");
            container.transform.SetParent(prefabRoot.transform);
            container.transform.localPosition = Vector3.zero;
            return container.transform;
        }

        /// <summary>
        /// Prefab 내의 모든 Action 컴포넌트를 수집합니다.
        /// </summary>
        public static List<YisoCharacterAction> GetAllActions(GameObject prefabRoot) {
            return prefabRoot.GetComponentsInChildren<YisoCharacterAction>(true).ToList();
        }

        /// <summary>
        /// Prefab 내의 모든 Decision 컴포넌트를 수집합니다.
        /// </summary>
        public static List<YisoCharacterDecision> GetAllDecisions(GameObject prefabRoot) {
            return prefabRoot.GetComponentsInChildren<YisoCharacterDecision>(true).ToList();
        }

        /// <summary>
        /// 새 Action 컴포넌트를 Prefab에 추가합니다.
        /// </summary>
        public static T AddAction<T>(GameObject prefabRoot) where T : YisoCharacterAction {
            var container = GetOrCreateActionsContainer(prefabRoot);
            var actionObj = new GameObject(typeof(T).Name);
            actionObj.transform.SetParent(container);
            return actionObj.AddComponent<T>();
        }

        /// <summary>
        /// 새 Action 컴포넌트를 Prefab에 추가합니다. (타입 지정)
        /// </summary>
        public static YisoCharacterAction AddAction(GameObject prefabRoot, Type actionType) {
            if (!typeof(YisoCharacterAction).IsAssignableFrom(actionType)) {
                Debug.LogError($"[FSM Editor] {actionType.Name}은 YisoCharacterAction을 상속하지 않습니다.");
                return null;
            }

            var container = GetOrCreateActionsContainer(prefabRoot);
            var actionObj = new GameObject(actionType.Name);
            actionObj.transform.SetParent(container);
            return (YisoCharacterAction)actionObj.AddComponent(actionType);
        }

        /// <summary>
        /// 새 Decision 컴포넌트를 Prefab에 추가합니다.
        /// </summary>
        public static T AddDecision<T>(GameObject prefabRoot) where T : YisoCharacterDecision {
            var container = GetOrCreateDecisionsContainer(prefabRoot);
            var decisionObj = new GameObject(typeof(T).Name);
            decisionObj.transform.SetParent(container);
            return decisionObj.AddComponent<T>();
        }

        /// <summary>
        /// 새 Decision 컴포넌트를 Prefab에 추가합니다. (타입 지정)
        /// </summary>
        public static YisoCharacterDecision AddDecision(GameObject prefabRoot, Type decisionType) {
            if (!typeof(YisoCharacterDecision).IsAssignableFrom(decisionType)) {
                Debug.LogError($"[FSM Editor] {decisionType.Name}은 YisoCharacterDecision을 상속하지 않습니다.");
                return null;
            }

            var container = GetOrCreateDecisionsContainer(prefabRoot);
            var decisionObj = new GameObject(decisionType.Name);
            decisionObj.transform.SetParent(container);
            return (YisoCharacterDecision)decisionObj.AddComponent(decisionType);
        }

        /// <summary>
        /// Action 또는 Decision을 안전하게 삭제합니다.
        /// 모든 참조를 먼저 제거합니다.
        /// </summary>
        public static void SafeDeleteComponent(Component component, YisoCharacterStateMachine fsm) {
            if (component == null) return;

            // FSM에서 참조 제거
            RemoveComponentReferences(component, fsm);

            // GameObject가 다른 컴포넌트를 가지고 있는지 확인
            var otherComponents = component.gameObject.GetComponents<Component>();
            var hasOtherComponents = otherComponents.Count(c =>
                c != component &&
                c.GetType() != typeof(Transform)) > 0;

            if (hasOtherComponents) {
                // 다른 컴포넌트가 있으면 이 컴포넌트만 제거
                UnityEngine.Object.DestroyImmediate(component);
            }
            else {
                // 이 컴포넌트만 있으면 GameObject 전체 삭제
                UnityEngine.Object.DestroyImmediate(component.gameObject);
            }
        }

        /// <summary>
        /// FSM의 모든 상태에서 해당 컴포넌트에 대한 참조를 제거합니다.
        /// </summary>
        private static void RemoveComponentReferences(Component component, YisoCharacterStateMachine fsm) {
            var statesField = typeof(YisoCharacterStateMachine).GetField("states",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (statesField == null) return;

            var states = statesField.GetValue(fsm) as List<YisoCharacterState>;
            if (states == null) return;

            foreach (var state in states) {
                if (component is YisoCharacterAction action) {
                    RemoveActionFromState(state, action);
                }
                else if (component is YisoCharacterDecision decision) {
                    RemoveDecisionFromState(state, decision);
                }
            }
        }

        private static void RemoveActionFromState(YisoCharacterState state, YisoCharacterAction action) {
            var onEnterField = typeof(YisoCharacterState).GetField("onEnterActions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var onUpdateField = typeof(YisoCharacterState).GetField("onUpdateActions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var onExitField = typeof(YisoCharacterState).GetField("onExitActions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            RemoveFromList(onEnterField?.GetValue(state) as List<YisoCharacterAction>, action);
            RemoveFromList(onUpdateField?.GetValue(state) as List<YisoCharacterAction>, action);
            RemoveFromList(onExitField?.GetValue(state) as List<YisoCharacterAction>, action);
        }

        private static void RemoveDecisionFromState(YisoCharacterState state, YisoCharacterDecision decision) {
            var transitionsField = typeof(YisoCharacterState).GetField("transitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var transitions = transitionsField?.GetValue(state) as List<YisoCharacterTransition>;
            if (transitions == null) return;

            foreach (var transition in transitions) {
                var conditionsField = typeof(YisoCharacterTransition).GetField("conditions",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var conditions = conditionsField?.GetValue(transition) as List<YisoCharacterTransition.TransitionCondition>;
                if (conditions == null) continue;

                RemoveDecisionFromConditions(conditions, decision);
            }
        }

        private static void RemoveDecisionFromConditions(List<YisoCharacterTransition.TransitionCondition> conditions, YisoCharacterDecision decision) {
            foreach (var condition in conditions) {
                if (condition.singleDecision == decision) {
                    condition.singleDecision = null;
                }

                if (condition.subConditions != null) {
                    RemoveDecisionFromConditions(condition.subConditions, decision);
                }
            }
        }

        private static void RemoveFromList<T>(List<T> list, T item) where T : class {
            if (list == null) return;
            while (list.Remove(item)) { }
        }

        /// <summary>
        /// 모든 Action 타입을 찾아 반환합니다.
        /// </summary>
        public static List<Type> FindAllActionTypes() {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(YisoCharacterAction).IsAssignableFrom(t) &&
                           !t.IsAbstract &&
                           t != typeof(YisoCharacterAction))
                .OrderBy(t => t.Name)
                .ToList();
        }

        /// <summary>
        /// 모든 Decision 타입을 찾아 반환합니다.
        /// </summary>
        public static List<Type> FindAllDecisionTypes() {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(YisoCharacterDecision).IsAssignableFrom(t) &&
                           !t.IsAbstract &&
                           t != typeof(YisoCharacterDecision))
                .OrderBy(t => t.Name)
                .ToList();
        }

        /// <summary>
        /// FSM Prefab의 GUID를 반환합니다.
        /// </summary>
        public static string GetPrefabGUID(GameObject prefabAsset) {
            var assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        /// <summary>
        /// GUID가 변경되지 않았는지 확인합니다.
        /// </summary>
        public static bool VerifyGUIDIntegrity(string originalGUID, GameObject prefabAsset) {
            var currentGUID = GetPrefabGUID(prefabAsset);
            if (originalGUID != currentGUID) {
                Debug.LogError($"[FSM Editor] GUID 무결성 위반! 원본: {originalGUID}, 현재: {currentGUID}");
                return false;
            }
            return true;
        }
    }
}
