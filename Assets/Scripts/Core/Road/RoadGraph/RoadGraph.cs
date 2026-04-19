#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class RoadGraph
    {
        private const float MIN_EDGE_LENGTH = 0.001f;
        private List<RoadNode> _nodes = new();
        private List<RoadEdge> _edges = new();

        public IReadOnlyList<RoadNode> Nodes => _nodes;
        public IReadOnlyList<RoadEdge> Edges => _edges;

        public RoadNode AddNode(Vector2 position, RoadType type)
        {
            var node = new RoadNode(position, type);
            _nodes.Add(node);
            return node;
        }

        public RoadEdge? AddEdge(RoadNode a, RoadNode b, RoadType type)
        {
            var edge = new RoadEdge(a, b, type);
            _edges.Add(edge);
            a.AddHalfEdge(edge.HalfA);
            b.AddHalfEdge(edge.HalfB);
            return edge;
        }

        public bool CouldFormCycle(RoadNode from, RoadNode to)
        {
            return _nodes.Contains(from) && _nodes.Contains(to);
        }

        public RoadNode SplitEdge(RoadEdge edge, Vector2 position)
        {
            RoadNode newNode = AddNode(position, edge.Type);
            RoadType type = edge.Type;

            RemoveEdge(edge);

            AddEdge(edge.A, newNode, type);
            AddEdge(newNode, edge.B, type);

            return newNode;
        }

        public void RemoveEdge(RoadEdge edge)
        {
            _edges.Remove(edge);
            edge.A.RemoveHalfEdge(edge.HalfA);
            edge.B.RemoveHalfEdge(edge.HalfB);
        }
    }
}