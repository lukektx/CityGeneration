#nullable enable
using System.Collections.Generic;
using CityGenerator.Core.Road;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Splines;

namespace CityGenerator.Core.Traffic
{
    public class TrafficAgent
    {
        public Trip Trip { get; }
        public AgentState State { get; private set; } = AgentState.Moving;
        public Vector2 TravelDirection { get; private set; }

        public float EdgeT { get; private set; }
        public int PathIndex { get; private set; }

        public RoadNode CurrentFrom => Trip.Path[PathIndex];
        public RoadNode? CurrentTo => PathIndex + 1 < Trip.Path.Count
            ? Trip.Path[PathIndex + 1]
            : null;

        // World-space interpolated position
        public Vector2 WorldPosition { get; set; }

        private readonly Dictionary<RoadNode, IntersectionQueue> _queues;
        private readonly TrafficParameters _parameters;

        private HalfEdge? _currentHalfEdge;

        // Turning spline data
        private bool _isTurning;
        private float _turnT;
        private BezierCurve _turnCurve;

        public TrafficAgent(
            Trip trip,
            Dictionary<RoadNode, IntersectionQueue> queues,
            TrafficParameters parameters
        )
        {
            Trip = trip;
            _queues = queues;
            _parameters = parameters;
        }

        public void ClearIntersection()
        {
            if (State == AgentState.WaitingAtIntersection)
            {
                State = AgentState.ClearedToAdvance;
            }
        }

        public void Tick(float dt, TrafficSimulator sim)
        {
            if (PathIndex >= Trip.Path.Count - 1)
            {
                State = AgentState.Finished;
                return;
            }
            if (State == AgentState.Finished) return;

            // Handle turn animation
            if (_isTurning)
            {
                _turnT += _parameters.TurnSpeed * dt;
                _turnT = Mathf.Clamp01(_turnT);

                var pos = CurveUtility.EvaluatePosition(_turnCurve, _turnT);
                WorldPosition = new Vector2(pos.x, pos.z);

                // Orient along tangent
                var tan = CurveUtility.EvaluateTangent(_turnCurve, _turnT);
                var tan2d = new Vector2(tan.x, tan.z);
                if (tan2d.sqrMagnitude > 0.0001f)
                {
                    TravelDirection = tan2d.normalized;
                }

                if (_turnT >= 1f)
                {
                    _isTurning = false;
                    PathIndex++;
                    _currentHalfEdge = null;
                    if (PathIndex >= Trip.Path.Count - 1) { State = AgentState.Finished; return; }
                    EdgeT = _parameters.StopDistanceFromIntersection / CurrentHalfEdge!.Edge.Length;
                    State = AgentState.Moving;
                }
                return;
            }

            if (State == AgentState.WaitingAtIntersection) return;

            if (State == AgentState.ClearedToAdvance)
            {
                BeginTurn();
                return;
            }

            if (PathIndex >= Trip.Path.Count - 1) { State = AgentState.Finished; return; }

            if (CurrentHalfEdge == null) return;
            var edge = CurrentHalfEdge.Edge;
            float speed = _parameters.BaseSpeed * (edge.Type == RoadType.Major ? 1f : 0.4f);
            float edgeLen = edge.Length;

            float stopT = Mathf.Clamp01(1f - (_parameters.StopDistanceFromIntersection / edgeLen));
            float? leadT = GetLeadingCarT(sim);
            float followT = leadT.HasValue ? leadT.Value - (_parameters.FollowDistance / edgeLen) : 1f;

            bool nextIsIntersection = _queues.TryGetValue(CurrentTo!, out var queue) && queue.IsIntersection;

            float targetT = nextIsIntersection
                ? Mathf.Min(stopT, followT)
                : Mathf.Min(1f, followT);
            targetT = Mathf.Clamp01(targetT);

            if (EdgeT < targetT)
            {
                EdgeT += (speed * dt) / edgeLen;
                EdgeT = Mathf.Min(EdgeT, targetT);
            }

            // Reached stop line
            if (nextIsIntersection && EdgeT >= stopT && State == AgentState.Moving)
            {
                State = AgentState.WaitingAtIntersection;
                queue!.Enqueue(this);
            }

            // Reached end of non-intersection edge
            if (!nextIsIntersection && EdgeT >= 1f)
            {
                AdvanceToNextEdge();
                if (PathIndex >= Trip.Path.Count - 1) State = AgentState.Finished;
            }

            // Update position with lane offset
            if (CurrentTo == null) return;
            var from = CurrentFrom.Position;
            var to = CurrentTo!.Position;
            var dir = (to - from).normalized;
            var right = new Vector2(dir.y, -dir.x);
            var laneOffset = right * _parameters.LaneOffset * _parameters.LaneMultiplier;
            WorldPosition = Vector2.Lerp(from, to, EdgeT) + laneOffset;
            TravelDirection = dir;
        }

        private void AdvanceToNextEdge()
        {
            PathIndex++;
            EdgeT = 0f;
            _currentHalfEdge = null;
        }

        private float? GetLeadingCarT(TrafficSimulator sim)
        {
            float? nearest = null;
            if (CurrentHalfEdge == null) return null;

            foreach (var other in sim.GetAgentsOnHalfEdge(CurrentHalfEdge))
            {
                if (other == this) continue;
                if (other.EdgeT > EdgeT)
                {
                    if (nearest == null || other.EdgeT < nearest.Value)
                        nearest = other.EdgeT;
                }
            }
            return nearest;
        }

        public void BeginTurn()
        {
            // Need at least PathIndex+2 to exist to have somewhere to turn to
            if (PathIndex + 2 >= Trip.Path.Count)
            {
                _isTurning = false;
                // Skip past intersection to end
                PathIndex += 2;
                State = AgentState.Finished;
                return;
            }

            var stopPos = new float3(WorldPosition.x, 0f, WorldPosition.y);

            var intersectionNode = CurrentTo!;
            var inDir = (intersectionNode.Position - CurrentFrom.Position).normalized;

            var nextTo = Trip.Path[PathIndex + 2]; // node after intersection
            var outDir = (nextTo.Position - intersectionNode.Position).normalized;
            var outRight = new Vector2(outDir.y, -outDir.x);

            var entryPt = intersectionNode.Position
                + outDir * _parameters.StopDistanceFromIntersection
                + outRight * _parameters.LaneOffset * _parameters.LaneMultiplier;
            var entryPos = new float3(entryPt.x, 0f, entryPt.y);

            float dist = Vector2.Distance(WorldPosition, entryPt);
            float tScale = dist * _parameters.TurnTangentScale;

            var p1 = stopPos + new float3(inDir.x, 0f, inDir.y) * tScale;
            var p2 = entryPos - new float3(outDir.x, 0f, outDir.y) * tScale;

            _turnCurve = new BezierCurve(stopPos, p1, p2, entryPos);
            _isTurning = true;
            _turnT = 0f;
        }

        public HalfEdge? CurrentHalfEdge
        {
            get
            {
                if (_currentHalfEdge == null && CurrentTo != null) _currentHalfEdge = FindHalfEdge(CurrentFrom, CurrentTo);
                return _currentHalfEdge;
            }
        }

        private static HalfEdge FindHalfEdge(RoadNode from, RoadNode to)
        {
            foreach (var he in from.OutgoingEdges)
                if (he.Destination == to) return he;
            throw new System.Exception($"No half-edge from {from} to {to}");
        }

        public RoadEdge? CurrentEdge => CurrentHalfEdge?.Edge;
    }
}