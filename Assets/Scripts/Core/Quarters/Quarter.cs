#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Quarters
{
    public class Quarter : IEquatable<Quarter>
    {
        public IReadOnlyList<HalfEdge> BoundaryHalfEdges { get; }
        public IReadOnlyList<RoadEdge> BoundaryEdges { get; }
        public IReadOnlyList<RoadNode> Nodes { get; }

        private readonly HashSet<RoadEdge> _boundaryEdgeSet;

        public Vector2 Centroid { get; }
        public bool IsClockwise { get; }
        public float Area { get; }

        public Vector2 MainAxis { get; }
        public Vector2 PerpindicularAxis { get; }

        public Quarter(List<HalfEdge> faceHalfEdges)
        {
            BoundaryHalfEdges = faceHalfEdges;
            BoundaryEdges = faceHalfEdges.Select(he => he.Edge).ToList();
            Nodes = faceHalfEdges.Select(he => he.Origin).ToList();

            _boundaryEdgeSet = new HashSet<RoadEdge>(BoundaryEdges);

            GetGridAxes(out Vector2 mainAxis, out Vector2 perpendicularAxis);
            MainAxis = mainAxis;
            PerpindicularAxis = perpendicularAxis;

            Centroid = ComputeCentroid();

            float signedArea = ComputeSignedArea();
            IsClockwise = signedArea > 0f;
            Area = Mathf.Abs(signedArea);
        }

        public bool IsPointInside(Vector2 point)
        {
            bool inside = false;
            int j = Nodes.Count - 1;
            for (int i = 0; i < Nodes.Count; i++)
            {
                var a = Nodes[i].Position;
                var b = Nodes[j].Position;
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        private Vector2 ComputeCentroid()
        {
            Vector2 sum = Vector2.zero;
            foreach (var node in Nodes)
            {
                sum += node.Position;
            }

            return sum / Nodes.Count;
        }

        private float ComputeSignedArea()
        {
            // Shoelace formula
            float area = 0f;
            for (int i = 0; i < Nodes.Count; i++)
            {
                var a = Nodes[i].Position;
                var b = Nodes[(i + 1) % Nodes.Count].Position;
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }

        // Computes the dominant grid axes of the quarter
        private void GetGridAxes(out Vector2 mainAxis, out Vector2 perpendicularAxis)
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