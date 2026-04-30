#nullable enable

using System.Collections.Generic;
using CityGenerator.Core.Road;

namespace CityGenerator.Core.Traffic
{
    public class NavigationGraph
    {
        private const float HighwaySpeed = 1f;
        private const float StreetSpeed = 2.5f;

        private readonly RoadGraph _graph;
        private readonly Dictionary<RoadNode, Dictionary<RoadNode, RoadNode?>> _prevCache = new();

        public NavigationGraph(RoadGraph graph) => _graph = graph;

        public List<RoadNode>? FindPath(RoadNode start, RoadNode end)
        {
            if (!_prevCache.TryGetValue(start, out var prev))
            {
                prev = RunDijkstra(start);
                _prevCache[start] = prev;
            }

            return prev.ContainsKey(end) ? Reconstruct(prev, end) : null;
        }

        private Dictionary<RoadNode, RoadNode?> RunDijkstra(RoadNode source)
        {
            var dist = new Dictionary<RoadNode, float>();
            var prev = new Dictionary<RoadNode, RoadNode?>();
            var open = new SortedSet<(float cost, int id, RoadNode node)>(
                Comparer<(float, int, RoadNode)>.Create((a, b) =>
                {
                    int c = a.Item1.CompareTo(b.Item1);
                    return c != 0 ? c : a.Item2.CompareTo(b.Item2);
                }));

            int counter = 0;
            foreach (var n in _graph.Nodes)
            {
                dist[n] = float.MaxValue;
                prev[n] = null;
            }

            dist[source] = 0f;
            open.Add((0f, counter++, source));

            while (open.Count > 0)
            {
                var (cost, _, current) = open.Min;
                open.Remove(open.Min);

                if (cost > dist[current]) continue;

                foreach (var he in current.OutgoingEdges)
                {
                    RoadNode neighbor = he.Destination;
                    float edgeCost = he.Edge.Length * (he.Edge.Type == RoadType.Major ? HighwaySpeed : StreetSpeed);
                    float newDist = dist[current] + edgeCost;

                    if (newDist < dist[neighbor])
                    {
                        dist[neighbor] = newDist;
                        prev[neighbor] = current;
                        open.Add((newDist, counter++, neighbor));
                    }
                }
            }

            return prev;
        }

        private static List<RoadNode> Reconstruct(Dictionary<RoadNode, RoadNode?> prev, RoadNode end)
        {
            var path = new List<RoadNode>();
            RoadNode? cur = end;
            while (cur != null) { path.Add(cur); prev.TryGetValue(cur, out cur); }
            path.Reverse();
            return path;
        }
    }
}