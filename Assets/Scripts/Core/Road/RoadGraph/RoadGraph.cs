#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class RoadGraph
    {
        private List<RoadNode> _nodes = new();
        private List<RoadEdge> _edges = new();

        public IReadOnlyList<RoadNode> Nodes => _nodes;
        public IReadOnlyList<RoadEdge> Edges => _edges;

        private readonly Dictionary<int, HashSet<RoadNode>> _unfinishedMajorNodes = new();
        public HashSet<RoadNode> UnfinishedMajorNodes(int valence) => _unfinishedMajorNodes.GetValueOrDefault(valence, new());
        public bool HasUnfinishedMajorNodes(int valence) => _unfinishedMajorNodes.TryGetValue(valence, out var nodes) && nodes.Count > 0;
        public HashSet<RoadNode> UnfinishedMinorNodes { get; } = new();
        public bool HasUnfinishedMinorNodes => UnfinishedMinorNodes.Count > 0;

        public int UnfinishedMinorCount => UnfinishedMinorNodes.Count;
        public int UnfinishedMajorCount => _unfinishedMajorNodes.Values.Sum(nodes => nodes.Count);
        public int UnfinishedCount => UnfinishedMinorNodes.Count + UnfinishedMajorCount;

        private int _majorValenceTwoCount;
        private int _majorValenceFourCount;

        public float MajorValenceRatio => _majorValenceTwoCount > 0
            ? _majorValenceFourCount / (float)_majorValenceTwoCount
            : 0f;

        public RoadNode AddNode(Vector2 position, RoadType type)
        {
            var node = new RoadNode(position, type);
            _nodes.Add(node);

            UpdateUnfinished(node);
            return node;
        }

        public RoadEdge AddEdge(RoadNode a, RoadNode b, RoadType type)
        {
            UpdateMajorValenceCounts(a, -1);
            UpdateMajorValenceCounts(b, -1);

            var edge = new RoadEdge(a, b, type);
            _edges.Add(edge);
            a.AddHalfEdge(edge.HalfA);
            b.AddHalfEdge(edge.HalfB);

            UpdateMajorValenceCounts(a, 1);
            UpdateMajorValenceCounts(b, 1);

            UpdateUnfinished(a);
            UpdateUnfinished(b);

            return edge;
        }

        public RoadNode SplitEdge(RoadEdge edge, Vector2 position)
        {
            RoadNode newNode = AddNode(position, edge.Type);
            RoadType type = edge.Type;

            RemoveEdge(edge);

            AddEdge(edge.A, newNode, type);
            AddEdge(newNode, edge.B, type);

            FinishNode(newNode);

            return newNode;
        }

        public void RemoveEdge(RoadEdge edge)
        {
            _edges.Remove(edge);
            edge.A.RemoveHalfEdge(edge.HalfA);
            edge.B.RemoveHalfEdge(edge.HalfB);
        }

        public void FinishNode(RoadNode node)
        {
            node.IsFinished = true;
            RemoveFromUnfinished(node);
        }

        private void UpdateUnfinished(RoadNode node)
        {
            RemoveFromUnfinished(node);
            if (!node.IsFinished)
            {
                AddToUnfinished(node);
            }
        }

        private void AddToUnfinished(RoadNode node)
        {
            if (node.IsFinished || node.Valence >= 4) return;

            if (node.Type == RoadType.Major)
            {
                var nodes = _unfinishedMajorNodes.GetValueOrDefault(node.Valence, new());
                nodes.Add(node);
                _unfinishedMajorNodes[node.Valence] = nodes;
            }
            else if (node.Type == RoadType.Minor)
            {
                UnfinishedMinorNodes.Add(node);
            }
        }

        private void RemoveFromUnfinished(RoadNode node)
        {
            foreach (HashSet<RoadNode> nodes in _unfinishedMajorNodes.Values)
            {
                nodes.Remove(node);
            }
            UnfinishedMinorNodes.Remove(node);
        }

        private void UpdateMajorValenceCounts(RoadNode node, int delta)
        {
            if (node.Type != RoadType.Major) return;
            if (node.Valence == 2) _majorValenceTwoCount += delta;
            else if (node.Valence >= 4) _majorValenceFourCount += delta;
        }
    }
}