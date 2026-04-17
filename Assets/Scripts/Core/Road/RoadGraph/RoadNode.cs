#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class RoadNode
    {
        public bool IsFinished { get; set; } = false;
        public Vector2 Position { get; }
        // Direction of (inbound) edge that led to creating this node
        internal int FailedExpansions { get; set; }

        // Stores road edges in counter clockwise order
        // With index 0 as the base/incoming edge
        private readonly RoadEdge?[] edges = new RoadEdge?[4];
        public IReadOnlyList<RoadEdge?> Edges => edges;
        public int Valence => System.Array.FindAll(edges, e => e != null).Length;
        public RoadEdge? BaseEdge => edges[0];

        public RoadEdge? GetEdge(EdgeSlot slot) => edges[(int)slot];
        public bool HasEdge(EdgeSlot slot) => edges[(int)slot] != null;

        public float? InitialAngle { get; set; }

        public float BaseAngle
        {
            get
            {
                if (BaseEdge == null) return InitialAngle ?? 0f;

                var parent = BaseEdge.Other(this);
                var dir = (Position - parent.Position).normalized;
                return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
        }

        public RoadNode(Vector2 position)
        {
            Position = position;
        }

        internal void AddEdge(RoadEdge edge, EdgeSlot slot)
        {
            Debug.Assert(edges[(int)slot] == null, $"Slot {slot} already occupied at {Position}");
            edges[(int)slot] = edge;
        }

        internal void RemoveEdge(RoadEdge edge)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                if (edges[i] == edge)
                {
                    edges[i] = null;
                    return;
                }
            }
        }

        public EdgeSlot SlotFor(RoadEdge edge)
        {
            for (int i = 0; i < edges.Length; i++)
                if (edges[i] == edge) return (EdgeSlot)i;
            throw new System.InvalidOperationException("Edge not found on node");
        }
    }
}