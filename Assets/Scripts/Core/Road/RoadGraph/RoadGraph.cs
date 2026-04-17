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

        public RoadEdge? AddEdge(RoadNode a, RoadNode b, EdgeSlot slotOnA, EdgeSlot slotOnB, RoadType type)
        {
            var edge = new RoadEdge(a, b, type);
            _edges.Add(edge);
            a.AddEdge(edge, slotOnA);
            b.AddEdge(edge, slotOnB);
            return edge;
        }

        public bool CouldFormCycle(RoadNode from, RoadNode to)
        {
            return _nodes.Contains(from) && _nodes.Contains(to);
        }

        public RoadNode SplitEdge(RoadEdge edge, Vector2 position)
        {
            RoadNode newNode = AddNode(position);
            RoadType type = edge.Type;

            // Find what slots this edge occupied on each endpoint
            EdgeSlot slotOnA = edge.A.SlotFor(edge);
            EdgeSlot slotOnB = edge.B.SlotFor(edge);

            // Remove original edge
            RemoveEdge(edge);

            // A to newNode reuses A's slot, newNode receives as Base
            AddEdge(edge.A, newNode, slotOnA, EdgeSlot.Base, type);
            // newNode to B continues straight (Opposite), B reuses its slot
            AddEdge(newNode, edge.B, EdgeSlot.Opposite, slotOnB, type);

            return newNode;
        }

        public void RemoveEdge(RoadEdge edge)
        {
            _edges.Remove(edge);
            edge.A.RemoveEdge(edge);
            edge.B.RemoveEdge(edge);
        }
    }
}