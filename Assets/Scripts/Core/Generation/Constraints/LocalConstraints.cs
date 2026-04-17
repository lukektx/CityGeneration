#nullable enable

using CityGenerator.Core.Parameters;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Generation
{
    public static class LocalConstraints
    {
        private const float LINE_INTERSECTION_EPSILON = 0.001f;

        public static ConstraintResult Apply(RoadSegment segment, RoadGraph graph, CityParameters parameters)
        {
            RoadEdge? splitEdge = null;

            // 1. Check if endpoint is legal - try to fix if not
            if (!parameters.IsLegalPosition(segment.End))
            {
                if (!TryFixIllegalEnd(segment, parameters))
                    return ConstraintResult.Failed;
            }

            // 2. Check for intersections with existing edges
            float closestIntersectionDist = float.MaxValue;
            Vector2 closestIntersection = Vector2.zero;
            RoadEdge? intersectedEdge = null;

            foreach (var edge in graph.Edges)
            {
                if (TryFindIntersection(segment, edge, out Vector2 intersection))
                {
                    float dist = Vector2.Distance(segment.Start, intersection);
                    if (dist < closestIntersectionDist)
                    {
                        closestIntersectionDist = dist;
                        closestIntersection = intersection;
                        intersectedEdge = edge;
                    }
                }
            }

            if (intersectedEdge != null)
            {
                segment.End = closestIntersection;
                splitEdge = intersectedEdge;
            }

            // 3. Snap street endpoints to nearby nodes, highways only snap on intersections
            RoadNode? nearby = FindNearbyNode(segment.End, graph, parameters.SnapDistance);
            if (nearby != null)
            {
                segment.End = nearby.Position;
                segment.EndNode = nearby;
            }

            // 4. Snap to nearby edge if no node was found
            if (segment.EndNode == null)
            {
                if (TrySnapToEdge(segment, graph, parameters, out Vector2 snapPoint, out RoadEdge? snapEdge))
                {
                    segment.End = snapPoint;
                    splitEdge = snapEdge;
                }
            }

            // 5. Minimum length check
            if (!ValidDistance(segment.Start, segment.End, parameters))
            {
                return ConstraintResult.Failed;
            }

            // 6. Apply edge split if needed
            if (splitEdge != null)
            {
                RoadNode splitNode = graph.SplitEdge(splitEdge, segment.End);
                segment.EndNode = splitNode;
            }

            return ConstraintResult.Succeed;
        }

        private static bool ValidDistance(Vector2 start, Vector2 end, CityParameters parameters)
        {
            return Vector2.Distance(start, end) >= parameters.MinStreetLength;
        }

        private static bool TrySnapToEdge(
            RoadSegment segment,
            RoadGraph graph,
            CityParameters parameters,
            out Vector2 snapPoint,
            out RoadEdge? snapEdge
        )
        {
            float closestDist = parameters.SnapDistance;
            snapEdge = null;
            snapPoint = Vector2.zero;

            foreach (var edge in graph.Edges)
            {
                // Skip edges connected to our start node
                if (edge.A == segment.StartNode || edge.B == segment.StartNode)
                    continue;

                Vector2 closest = ClosestPointOnSegment(
                    segment.End,
                    edge.A.Position,
                    edge.B.Position
                );

                float dist = Vector2.Distance(segment.End, closest);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    snapEdge = edge;
                    snapPoint = closest;
                }
            }

            return snapEdge != null;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(point - a, ab) / Vector2.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            return a + t * ab;
        }

        private static bool TryFixIllegalEnd(RoadSegment segment, CityParameters parameters)
        {
            // Try rotating
            float originalAngle = segment.Angle;
            for (int i = 1; i <= parameters.MaxRotationAttempts; i++)
            {
                foreach (int sign in new[] { 1, -1 })
                {
                    float testAngle = (originalAngle + sign * i * parameters.RotationStep) * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(testAngle), Mathf.Sin(testAngle));
                    Vector2 testEnd = segment.Start + dir * segment.Length;
                    if (parameters.IsLegalPosition(testEnd))
                    {
                        segment.End = testEnd;
                        return true;
                    }
                }
            }

            // Try pruning length
            Vector2 direction = segment.Direction;
            for (float factor = 0.9f; factor >= parameters.MinLengthFactor; factor -= 0.1f)
            {
                Vector2 testEnd = segment.Start + direction * (segment.Length * factor);
                if (parameters.IsLegalPosition(testEnd))
                {
                    segment.End = testEnd;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindIntersection(RoadSegment segment, RoadEdge edge, out Vector2 intersection)
        {
            // Don't intersect with edges that are connected to our start node
            if (edge.A == segment.StartNode || edge.B == segment.StartNode)
            {
                intersection = Vector2.zero;
                return false;
            }

            return LineIntersection(
                segment.Start, segment.End,
                edge.A.Position, edge.B.Position,
                out intersection
            );
        }

        private static RoadNode? FindNearbyNode(Vector2 pos, RoadGraph graph, float radius)
        {
            RoadNode? closest = null;
            float closestDist = radius;

            foreach (var node in graph.Nodes)
            {
                float dist = Vector2.Distance(pos, node.Position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = node;
                }
            }

            return closest;
        }

        private static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
        {
            intersection = Vector2.zero;

            float d1x = p2.x - p1.x, d1y = p2.y - p1.y;
            float d2x = p4.x - p3.x, d2y = p4.y - p3.y;
            float denom = d1x * d2y - d1y * d2x;

            // Ensure not parallel
            if (Mathf.Abs(denom) < LINE_INTERSECTION_EPSILON) return false;

            float t = ((p3.x - p1.x) * d2y - (p3.y - p1.y) * d2x) / denom;
            float u = ((p3.x - p1.x) * d1y - (p3.y - p1.y) * d1x) / denom;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                intersection = p1 + new Vector2(d1x, d1y) * t;
                return true;
            }

            return false;
        }
    }
}