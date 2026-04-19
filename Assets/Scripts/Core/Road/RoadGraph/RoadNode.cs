#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class RoadNode
    {
        public Vector2 Position { get; }
        public RoadType Type { get; }
        public bool IsFinished { get; set; } = false;
        public float? InitialAngle { get; set; }
        internal int FailedExpansions { get; set; }

        // Stores road edges in counter clockwise order
        // With index 0 as the base/incoming edge
        private readonly List<HalfEdge> _outgoing = new();
        public IReadOnlyList<HalfEdge> OutgoingEdges => _outgoing;
        public int Valence => _outgoing.Count;
        public HalfEdge? BaseEdge { get; private set; }

        public float BaseAngle
        {
            get
            {
                if (BaseEdge == null) return InitialAngle ?? 0f;
                return GetAngle(BaseEdge);
            }
        }

        public RoadNode(Vector2 position, RoadType type)
        {
            Position = position;
            Type = type;
        }

        public float GetAngle(HalfEdge he)
        {
            var dir = (he.Destination.Position - Position).normalized;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        internal void AddHalfEdge(HalfEdge he)
        {
            if (BaseEdge == null) BaseEdge = he;
            float angle = GetAngle(he);

            int insertIndex = _outgoing.Count;
            for (int i = 0; i < _outgoing.Count; i++)
            {
                if (GetAngle(_outgoing[i]) > angle)
                {
                    insertIndex = i;
                    break;
                }
            }
            _outgoing.Insert(insertIndex, he);
            UpdateNextPointers();
        }


        internal void RemoveHalfEdge(HalfEdge he)
        {
            _outgoing.Remove(he);
            UpdateNextPointers();
        }

        // Each incoming half-edge's next is the next outgoing half-edge CCW
        private void UpdateNextPointers()
        {
            for (int i = 0; i < _outgoing.Count; i++)
            {
                // The incoming twin of the next CCW outgoing edge
                // points to the current outgoing edge as its next
                int prevIndex = (i - 1 + _outgoing.Count) % _outgoing.Count;
                _outgoing[prevIndex].Twin.Next = _outgoing[i];
            }
        }
    }
}