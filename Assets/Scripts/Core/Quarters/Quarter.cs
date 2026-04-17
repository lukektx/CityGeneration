#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Road;
using Codice.Client.Common.TreeGrouper;
using UnityEngine;

namespace CityGenerator.Core.Quarters
{
    public class Quarter : IEquatable<Quarter>
    {
        public IReadOnlyList<RoadEdge> BoundaryEdges { get; }

        private readonly List<RoadNode> _boundaryNodes;
        private readonly List<EdgeSlot> _boundaryNodeSlots;

        private readonly HashSet<RoadEdge> _boundaryEdgeSet;
        public IReadOnlyList<RoadNode> Nodes => _boundaryNodes;

        public Vector2 Centroid { get; }
        public float Area { get; }


        public List<(RoadNode, EdgeSlot)> InteriorSlots =>
            _boundaryNodes.Zip(_boundaryNodeSlots, (f, s) => (Node: f, Slot: s)).ToList();

        public List<(RoadNode, EdgeSlot)> CandidateSlots => InteriorSlots.Where(
            s => !s.Item1.HasEdge(s.Item2)
        ).ToList();

        public Quarter(List<(RoadEdge, RoadNode)> traversal)
        {
            BoundaryEdges = traversal.Select(e => e.Item1).ToList();
            _boundaryEdgeSet = new HashSet<RoadEdge>(BoundaryEdges);
            _boundaryNodes = traversal.Select(e => e.Item2).ToList();
            _boundaryNodeSlots = ComputeSlots();

            Centroid = ComputeCentroid();
            Area = ComputeArea();
        }

        private List<EdgeSlot> ComputeSlots()
        {
            var slots = new List<EdgeSlot>();
            int count = BoundaryEdges.Count;

            for (int i = 0; i < count; i++)
            {
                var node = _boundaryNodes[i];
                var incomingEdge = BoundaryEdges[(i - 1 + count) % count];

                EdgeSlot arrivalSlot = node.SlotFor(incomingEdge);
                EdgeSlot inwardSlot = (EdgeSlot)(((int)arrivalSlot + 1) % 4);

                slots.Add(inwardSlot);
            }

            return slots;
        }

        private Vector2 ComputeCentroid()
        {
            Vector2 sum = Vector2.zero;
            foreach (var node in _boundaryNodes)
                sum += node.Position;

            return sum / _boundaryNodes.Count;
        }

        private float ComputeArea()
        {
            // Shoelace formula
            float area = 0f;
            for (int i = 0; i < _boundaryNodes.Count; i++)
            {
                var a = _boundaryNodes[i].Position;
                var b = _boundaryNodes[(i + 1) % _boundaryNodes.Count].Position;
                area += a.x * b.y - b.x * a.y;
            }
            return Mathf.Abs(area) * 0.5f;
        }

        // Computes the dominant grid axes of the quarter
        public void GetGridAxes(out Vector2 mainAxis, out Vector2 perpendicularAxis)
        {
            const float collinearThreshold = 15f;
            int count = BoundaryEdges.Count;

            float longestRun = 0f;
            mainAxis = Vector2.right;

            for (int i = 0; i < count; i++)
            {
                Vector2 runDir = (BoundaryEdges[i].B.Position - BoundaryEdges[i].A.Position).normalized;
                float runLength = Vector2.Distance(BoundaryEdges[i].A.Position, BoundaryEdges[i].B.Position);
                Vector2 dirSum = runDir;

                for (int j = 1; j < count; j++)
                {
                    var edge = BoundaryEdges[(i + j) % count];
                    Vector2 nextDir = (edge.B.Position - edge.A.Position).normalized;
                    float angle = Vector2.Angle(runDir, nextDir);

                    // Check parallel and anti-parallel
                    if (Mathf.Min(angle, 180f - angle) > collinearThreshold) break;

                    runLength += Vector2.Distance(edge.A.Position, edge.B.Position);
                    dirSum += nextDir;
                }

                if (runLength > longestRun)
                {
                    longestRun = runLength;
                    mainAxis = dirSum.normalized;
                }
            }

            perpendicularAxis = new Vector2(-mainAxis.y, mainAxis.x);
        }

        public bool Equals(Quarter? other)
        {
            if (other == null) return false;
            return _boundaryEdgeSet.SetEquals(other._boundaryEdgeSet);
        }


        public override bool Equals(object? obj) => Equals(obj as Quarter);

        public override int GetHashCode()
        {
            // XOR of node hash codes is order-independent
            int hash = 0;
            foreach (var edge in _boundaryEdgeSet)
            {
                hash ^= edge.GetHashCode();
            }
            return hash;
        }
    }
}