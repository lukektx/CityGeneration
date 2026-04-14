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

        public RoadNode AddNode(Vector2 position)
        {
            var node = new RoadNode(position);
            _nodes.Add(node);
            return node;
        }

        public RoadEdge? AddEdge(RoadNode from, RoadNode to, RoadType type)
        {
            if (Vector2.Distance(from.Position, to.Position) < MIN_EDGE_LENGTH)
                return null;

            var edge = new RoadEdge(from, to, type);
            _edges.Add(edge);
            from.AddEdge(edge);
            to.AddEdge(edge);
            return edge;
        }

        public RoadNode SplitEdge(RoadEdge edge, Vector2 position)
        {
            RoadNode newNode = AddNode(position);
            RoadType type = edge.Type;
            RoadNode originalTo = edge.To;

            // Remove original edge
            RemoveEdge(edge);

            // Add two new edges
            AddEdge(edge.From, newNode, type);
            AddEdge(newNode, originalTo, type);

            return newNode;
        }

        public void RemoveEdge(RoadEdge edge)
        {
            _edges.Remove(edge);
            edge.From.RemoveEdge(edge);
            edge.To.RemoveEdge(edge);
        }
    }
}