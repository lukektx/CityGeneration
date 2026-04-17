#nullable enable
using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Quarters
{
    public static class QuarterDetector
    {
        public static List<Quarter> FindQuartersForEdge(
            RoadEdge newEdge,
            RoadGraph graph,
            HashSet<Quarter> knownQuarters
        )
        {
            if (newEdge.Type != RoadType.Major) return new List<Quarter>();

            var newQuarters = new List<Quarter>();
            var visited = new HashSet<(RoadNode, RoadNode)>();

            foreach (var (start, next) in new[]
            {
                (newEdge.A, newEdge.B),
                (newEdge.B, newEdge.A)
            })
            {
                var edges = TraceCycle(start, next, graph, visited);
                if (edges != null && edges.Count >= 3)
                {
                    var quarter = new Quarter(edges);
                    if (knownQuarters.Add(quarter))
                    {
                        newQuarters.Add(quarter);
                    }
                }
            }

            return newQuarters;
        }

        private static List<(RoadEdge, RoadNode)>? TraceCycle
        (
            RoadNode start,
            RoadNode next,
            RoadGraph graph,
            HashSet<(RoadNode, RoadNode)> visited
        )
        {
            var edges = new List<(RoadEdge, RoadNode)>();
            var current = next;
            var previous = start;
            int maxSteps = graph.Nodes.Count + 1;

            while (current != start && edges.Count <= maxSteps)
            {
                RoadEdge? incomingEdge = null;
                foreach (var e in current.Edges)
                {
                    if (e != null && e.Other(current) == previous)
                    {
                        incomingEdge = e;
                        break;
                    }
                }
                if (incomingEdge == null) return null;

                edges.Add((incomingEdge, previous));

                var nextNode = MostClockwiseNeighbor(previous, current);
                if (nextNode == null) return null;

                visited.Add((previous, current));
                previous = current;
                current = nextNode;
            }

            if (current != start) return null;

            RoadEdge? closingEdge = null;
            foreach (var e in current.Edges)
            {
                if (e != null && e.Other(current) == previous)
                {
                    closingEdge = e;
                    break;
                }
            }
            if (closingEdge == null) return null;

            edges.Add((closingEdge, previous));
            visited.Add((previous, current));

            var nodes = edges.Select(e => e.Item2).ToList();
            if (IsClockwise(nodes)) return null;

            return edges;
        }

        private static RoadNode? MostClockwiseNeighbor(RoadNode from, RoadNode current)
        {
            RoadEdge? incomingEdge = null;
            foreach (var e in current.Edges)
            {
                if (e != null && e.Other(current) == from)
                {
                    incomingEdge = e;
                    break;
                }
            }

            if (incomingEdge == null) return null;

            EdgeSlot incomingSlot = current.SlotFor(incomingEdge);

            for (int i = 1; i <= 3; i++)
            {
                EdgeSlot slot = (EdgeSlot)(((int)incomingSlot + i) % 4);
                RoadNode? neighbor = current.GetEdge(slot)?.Other(current);
                if (neighbor != null && neighbor != from)
                    return neighbor;
            }

            return null;
        }

        private static bool IsClockwise(IReadOnlyList<RoadNode> cycle)
        {
            float area = 0f;
            for (int i = 0; i < cycle.Count; i++)
            {
                var a = cycle[i].Position;
                var b = cycle[(i + 1) % cycle.Count].Position;
                area += (b.x - a.x) * (b.y + a.y);
            }
            return area > 0f;
        }
    }
}