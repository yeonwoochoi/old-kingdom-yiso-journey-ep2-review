using System.Collections.Generic;
using System.Linq;
using Gameplay.Character.StateMachine;
using UnityEngine;

namespace Editor.FSM.Utils {
    /// <summary>
    /// FSM 노드 자동 배치 알고리즘 유틸리티
    /// Sugiyama 스타일 계층적 레이아웃 구현
    /// </summary>
    public static class YisoFSMLayoutHelper {
        // 레이아웃 설정
        private const float NODE_WIDTH = 180f;
        private const float NODE_HEIGHT = 60f;
        private const float HORIZONTAL_SPACING = 100f;
        private const float VERTICAL_SPACING = 80f;
        private const float INITIAL_X = 100f;
        private const float INITIAL_Y = 100f;

        /// <summary>
        /// 모든 상태 노드를 자동으로 배치합니다.
        /// 초기 상태를 루트로 하는 계층적 레이아웃을 적용합니다.
        /// </summary>
        public static Dictionary<string, Vector2> CalculateLayout(
            List<YisoCharacterState> states,
            string initialStateName) {

            if (states == null || states.Count == 0) {
                return new Dictionary<string, Vector2>();
            }

            // 상태 이름으로 인덱싱
            var stateDict = states.ToDictionary(s => s.StateName, s => s);

            // 그래프 연결 정보 구축
            var graph = BuildGraph(states);

            // BFS로 레이어 할당 (초기 상태부터 시작)
            var layers = AssignLayers(states, graph, initialStateName);

            // 레이어 내 순서 최적화 (교차 최소화)
            OptimizeLayerOrder(layers, graph);

            // 최종 위치 계산
            return CalculatePositions(layers);
        }

        /// <summary>
        /// 간단한 그리드 레이아웃을 적용합니다.
        /// </summary>
        public static Dictionary<string, Vector2> CalculateGridLayout(
            List<YisoCharacterState> states,
            int columnsCount = 3) {

            var positions = new Dictionary<string, Vector2>();
            if (states == null) return positions;

            for (int i = 0; i < states.Count; i++) {
                int col = i % columnsCount;
                int row = i / columnsCount;
                positions[states[i].StateName] = new Vector2(
                    INITIAL_X + col * (NODE_WIDTH + HORIZONTAL_SPACING),
                    INITIAL_Y + row * (NODE_HEIGHT + VERTICAL_SPACING)
                );
            }

            return positions;
        }

        /// <summary>
        /// 원형 레이아웃을 적용합니다.
        /// </summary>
        public static Dictionary<string, Vector2> CalculateCircularLayout(
            List<YisoCharacterState> states,
            Vector2 center,
            float radius = 300f) {

            var positions = new Dictionary<string, Vector2>();
            if (states == null || states.Count == 0) return positions;

            float angleStep = 360f / states.Count;
            for (int i = 0; i < states.Count; i++) {
                float angle = i * angleStep * Mathf.Deg2Rad;
                positions[states[i].StateName] = new Vector2(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius
                );
            }

            return positions;
        }

        /// <summary>
        /// 그래프 연결 정보를 구축합니다.
        /// </summary>
        private static Dictionary<string, List<string>> BuildGraph(List<YisoCharacterState> states) {
            var graph = new Dictionary<string, List<string>>();

            foreach (var state in states) {
                var connections = new List<string>();

                // Transition 필드에 접근
                var transitionsField = typeof(YisoCharacterState).GetField("transitions",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var transitions = transitionsField?.GetValue(state) as List<YisoCharacterTransition>;
                if (transitions != null) {
                    foreach (var transition in transitions) {
                        // random 필드 확인
                        var randomField = typeof(YisoCharacterTransition).GetField("random",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var isRandom = (bool)(randomField?.GetValue(transition) ?? false);

                        if (isRandom) {
                            // nextStates 리스트
                            var nextStatesField = typeof(YisoCharacterTransition).GetField("nextStates",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var nextStates = nextStatesField?.GetValue(transition) as List<string>;
                            if (nextStates != null) {
                                connections.AddRange(nextStates.Where(s => !string.IsNullOrEmpty(s)));
                            }
                        }
                        else {
                            // 단일 nextState
                            var nextStateField = typeof(YisoCharacterTransition).GetField("nextState",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var nextState = nextStateField?.GetValue(transition) as string;
                            if (!string.IsNullOrEmpty(nextState)) {
                                connections.Add(nextState);
                            }
                        }
                    }
                }

                graph[state.StateName] = connections.Distinct().ToList();
            }

            return graph;
        }

        /// <summary>
        /// BFS를 사용하여 각 노드에 레이어를 할당합니다.
        /// </summary>
        private static List<List<string>> AssignLayers(
            List<YisoCharacterState> states,
            Dictionary<string, List<string>> graph,
            string initialStateName) {

            var layers = new List<List<string>>();
            var visited = new HashSet<string>();
            var nodeLayer = new Dictionary<string, int>();

            // 초기 상태 먼저 찾기
            string startState = states.FirstOrDefault(s => s.StateName == initialStateName)?.StateName
                               ?? states.FirstOrDefault()?.StateName;

            if (string.IsNullOrEmpty(startState)) {
                return layers;
            }

            // BFS로 레이어 할당
            var queue = new Queue<(string node, int layer)>();
            queue.Enqueue((startState, 0));
            visited.Add(startState);
            nodeLayer[startState] = 0;

            while (queue.Count > 0) {
                var (node, layer) = queue.Dequeue();

                // 레이어 확장
                while (layers.Count <= layer) {
                    layers.Add(new List<string>());
                }
                layers[layer].Add(node);

                // 연결된 노드 탐색
                if (graph.TryGetValue(node, out var connections)) {
                    foreach (var next in connections) {
                        if (!visited.Contains(next)) {
                            visited.Add(next);
                            nodeLayer[next] = layer + 1;
                            queue.Enqueue((next, layer + 1));
                        }
                    }
                }
            }

            // 방문하지 않은 노드 처리 (분리된 노드)
            foreach (var state in states) {
                if (!visited.Contains(state.StateName)) {
                    // 마지막 레이어에 추가
                    if (layers.Count == 0) {
                        layers.Add(new List<string>());
                    }
                    layers[layers.Count - 1].Add(state.StateName);
                }
            }

            return layers;
        }

        /// <summary>
        /// 레이어 내 순서를 최적화하여 Edge 교차를 최소화합니다.
        /// 단순화된 Barycenter 방법 사용
        /// </summary>
        private static void OptimizeLayerOrder(
            List<List<string>> layers,
            Dictionary<string, List<string>> graph) {

            // 역방향 그래프 구축 (들어오는 Edge 추적)
            var reverseGraph = new Dictionary<string, List<string>>();
            foreach (var kvp in graph) {
                foreach (var target in kvp.Value) {
                    if (!reverseGraph.ContainsKey(target)) {
                        reverseGraph[target] = new List<string>();
                    }
                    reverseGraph[target].Add(kvp.Key);
                }
            }

            // 여러 번 반복하여 최적화
            for (int iteration = 0; iteration < 4; iteration++) {
                // 아래에서 위로
                for (int i = 1; i < layers.Count; i++) {
                    SortLayerByBarycenter(layers[i], layers[i - 1], reverseGraph);
                }
                // 위에서 아래로
                for (int i = layers.Count - 2; i >= 0; i--) {
                    SortLayerByBarycenter(layers[i], layers[i + 1], graph);
                }
            }
        }

        /// <summary>
        /// Barycenter 방법으로 레이어 내 순서를 정렬합니다.
        /// </summary>
        private static void SortLayerByBarycenter(
            List<string> currentLayer,
            List<string> adjacentLayer,
            Dictionary<string, List<string>> connections) {

            var barycenters = new Dictionary<string, float>();
            var adjacentPositions = new Dictionary<string, int>();

            // 인접 레이어의 위치 인덱스
            for (int i = 0; i < adjacentLayer.Count; i++) {
                adjacentPositions[adjacentLayer[i]] = i;
            }

            // 각 노드의 Barycenter 계산
            foreach (var node in currentLayer) {
                if (connections.TryGetValue(node, out var connectedNodes)) {
                    var positions = connectedNodes
                        .Where(n => adjacentPositions.ContainsKey(n))
                        .Select(n => adjacentPositions[n])
                        .ToList();

                    barycenters[node] = positions.Count > 0 ? (float) positions.Average() : float.MaxValue;
                }
                else {
                    barycenters[node] = float.MaxValue;
                }
            }

            // Barycenter 순으로 정렬
            currentLayer.Sort((a, b) => barycenters[a].CompareTo(barycenters[b]));
        }

        /// <summary>
        /// 레이어 정보를 기반으로 최종 위치를 계산합니다.
        /// </summary>
        private static Dictionary<string, Vector2> CalculatePositions(List<List<string>> layers) {
            var positions = new Dictionary<string, Vector2>();

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++) {
                var layer = layers[layerIndex];
                float y = INITIAL_Y + layerIndex * (NODE_HEIGHT + VERTICAL_SPACING);

                // 레이어 내 노드들을 중앙 정렬
                float totalWidth = layer.Count * NODE_WIDTH + (layer.Count - 1) * HORIZONTAL_SPACING;
                float startX = INITIAL_X;

                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++) {
                    float x = startX + nodeIndex * (NODE_WIDTH + HORIZONTAL_SPACING);
                    positions[layer[nodeIndex]] = new Vector2(x, y);
                }
            }

            return positions;
        }

        /// <summary>
        /// 뷰포트에 맞게 모든 노드를 스케일링합니다.
        /// </summary>
        public static Dictionary<string, Vector2> FitToViewport(
            Dictionary<string, Vector2> positions,
            Rect viewport,
            float padding = 50f) {

            if (positions.Count == 0) return positions;

            // 현재 경계 계산
            float minX = positions.Values.Min(p => p.x);
            float maxX = positions.Values.Max(p => p.x) + NODE_WIDTH;
            float minY = positions.Values.Min(p => p.y);
            float maxY = positions.Values.Max(p => p.y) + NODE_HEIGHT;

            float currentWidth = maxX - minX;
            float currentHeight = maxY - minY;

            // 스케일 계산 (비율 유지)
            float availableWidth = viewport.width - 2 * padding;
            float availableHeight = viewport.height - 2 * padding;
            float scale = Mathf.Min(
                availableWidth / currentWidth,
                availableHeight / currentHeight,
                1f // 확대는 하지 않음
            );

            // 변환 적용
            var result = new Dictionary<string, Vector2>();
            foreach (var kvp in positions) {
                result[kvp.Key] = new Vector2(
                    padding + (kvp.Value.x - minX) * scale,
                    padding + (kvp.Value.y - minY) * scale
                );
            }

            return result;
        }

        /// <summary>
        /// 모든 노드의 중심점을 계산합니다.
        /// </summary>
        public static Vector2 GetCenter(Dictionary<string, Vector2> positions) {
            if (positions.Count == 0) return Vector2.zero;

            float sumX = positions.Values.Sum(p => p.x + NODE_WIDTH / 2);
            float sumY = positions.Values.Sum(p => p.y + NODE_HEIGHT / 2);

            return new Vector2(sumX / positions.Count, sumY / positions.Count);
        }

        /// <summary>
        /// 모든 노드를 포함하는 경계 사각형을 계산합니다.
        /// </summary>
        public static Rect GetBounds(Dictionary<string, Vector2> positions) {
            if (positions.Count == 0) return Rect.zero;

            float minX = positions.Values.Min(p => p.x);
            float maxX = positions.Values.Max(p => p.x) + NODE_WIDTH;
            float minY = positions.Values.Min(p => p.y);
            float maxY = positions.Values.Max(p => p.y) + NODE_HEIGHT;

            return new Rect(minX, maxX, maxX - minX, maxY - minY);
        }
    }
}
