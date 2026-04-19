#nullable enable

using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Parameters;
using CityGenerator.Core.Quarters;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Generation
{
    public class StreetExpander
    {
        private readonly CityParameters parameters;
        private readonly RoadGraph graph = new();

        // Unfinished nodes available for expansion
        private readonly HashSet<RoadNode> unfinishedMajor = new();
        private readonly HashSet<RoadNode> unfinishedMinor = new();

        // Detected quarters waiting to be filled
        private readonly Queue<Quarter> pendingQuarters = new();
        private readonly HashSet<Quarter> knownQuarters = new();

        public RoadGraph Graph => graph;
        public bool IsComplete =>
            graph.Edges.Count >= parameters.MaxSegments ||
            (
                unfinishedMajor.Count == 0 &&
                unfinishedMinor.Count == 0 &&
                pendingQuarters.Count == 0
            );
        public StreetExpander(CityParameters parameters)
        {
            this.parameters = parameters;
            Seed();
        }

        public bool ExpandNext()
        {
            if (IsComplete)
            {
                Debug.Log($"Generation complete: edges={graph.Edges.Count}/{parameters.MaxSegments} | unfinishedMajor={unfinishedMajor.Count} | unfinishedMinor={unfinishedMinor.Count} | pendingQuarters={pendingQuarters.Count}");
                return false;
            }

            Debug.Log($"Expanding: edges={graph.Edges.Count}/{parameters.MaxSegments} | unfinishedMajor={unfinishedMajor.Count} | unfinishedMinor={unfinishedMinor.Count} | pendingQuarters={pendingQuarters.Count}");

            // Fill pending quarters by adding their nodes
            if (pendingQuarters.Count > 0)
            {
                var quarter = pendingQuarters.Dequeue();
                Debug.Log($"Filling quarter at {quarter.Centroid}");
                FillQuarter(quarter);
                return true;
            }

            // If there are pending quarters to fill, do that first
            // Process minor nodes before major to fill quarters as they form
            if (unfinishedMinor.Count > 0)
            {
                var node = SampleNode(unfinishedMinor);
                Debug.Log($"Expanding minor node {node.Position}");
                ExpandNode(node, RoadType.Minor);
                return true;
            }

            // Otherwise expand a major node
            if (unfinishedMajor.Count > 0)
            {
                var node = SampleNode(unfinishedMajor);
                Debug.Log($"Expanding major node {node.Position}");
                ExpandNode(node, RoadType.Major);
                return true;
            }

            return false;
        }

        public void ExpandAll()
        {
            while (!IsComplete)
                ExpandNext();
        }

        private void Seed()
        {
            // Place initial segment at highest population point
            // pointing in an arbitrary direction - growth emerges naturally
            Vector2 startPos = FindHighestPopulationPoint();

            Vector2 endPos = startPos + Vector2.right * parameters.MajorStreetLength;

            var startNode = graph.AddNode(startPos, RoadType.Major);
            var endNode = graph.AddNode(endPos, RoadType.Major);

            graph.AddEdge(startNode, endNode, RoadType.Major);

            // Start node has invalid EdgeSlot so dont use it
            //unfinishedMajor.Add(startNode);
            unfinishedMajor.Add(endNode);
        }

        private void ExpandNode(RoadNode node, RoadType type)
        {
            if (node.IsFinished)
            {
                RemoveFromUnfinished(node);
                return;
            }

            float length = (type == RoadType.Major)
                ? parameters.MajorStreetLength
                : parameters.MinorStreetLength;

            float angleDeviation = (type == RoadType.Major)
                ? parameters.MaxMajorAngleDeviation
                : parameters.MaxMinorAngleDeviation;

            float? expansionAngle = GetExpansionAngle(node);
            if (!expansionAngle.HasValue)
            {
                node.IsFinished = true;
                RemoveFromUnfinished(node);
                return;
            }

            float angle = expansionAngle.Value + RandomAngleDeviation(angleDeviation);


            float rad = angle * Mathf.Deg2Rad;
            Vector2 newDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 newPos = node.Position + newDirection * length;

            Debug.Log($"{node.Position} Expansion angle after deviation {angle} segment from {node.Position} -> {newPos}");

            var segment = new RoadSegment(
                start: node.Position,
                end: newPos,
                type: type,
                startNode: node
            );

            bool success = CommitSegment(segment);
            if (!success)
            {
                node.FailedExpansions++;
                if (node.FailedExpansions >= parameters.MaxExpansionFailures)
                {
                    node.IsFinished = true;
                    RemoveFromUnfinished(node);
                }
            }
            else
            {
                Debug.Log($"Success commiting segment from {segment.Start} -> {segment.End} angle {segment.Angle}");
            }
        }

        private void FillQuarter(Quarter quarter)
        {
            Vector2 mainAxis = quarter.MainAxis;
            float gridAngle = Mathf.Atan2(mainAxis.y, mainAxis.x) * Mathf.Rad2Deg;

            RoadNode seedNode = graph.AddNode(quarter.Centroid, RoadType.Minor);
            seedNode.InitialAngle = gridAngle;
            unfinishedMinor.Add(seedNode);
        }

        private bool CommitSegment(RoadSegment segment)
        {
            var result = LocalConstraints.Apply(segment, graph, parameters);
            if (result == ConstraintResult.Failed) return false;

            bool toNodeExisted = segment.EndNode != null;
            RoadNode toNode = segment.EndNode ?? graph.AddNode(segment.End, segment.Type);

            var edge = graph.AddEdge(segment.StartNode!, toNode, segment.Type);
            if (edge == null) return false;

            if (segment.StartNode!.Valence >= 4)
            {
                segment.StartNode.IsFinished = true;
                RemoveFromUnfinished(segment.StartNode);
            }

            if (!toNode.IsFinished && !toNodeExisted)
            {
                if (segment.Type == RoadType.Major)
                    unfinishedMajor.Add(toNode);
                else
                    unfinishedMinor.Add(toNode);
            }

            if (segment.Type == RoadType.Major)
                CheckForNewQuarters(edge);

            return true;
        }

        private void CheckForNewQuarters(RoadEdge newEdge)
        {
            var newQuarters = QuarterDetector.FindQuartersForEdge(newEdge, knownQuarters);
            foreach (var quarter in newQuarters)
            {
                pendingQuarters.Enqueue(quarter);
                Debug.Log($"Formed quarter with {quarter.Nodes.Count} nodes and Centroid {quarter.Centroid}");
                Debug.Log($"Quarter detected with {quarter.Nodes.Count} nodes: {string.Join(" -> ", quarter.Nodes.Select(n => n.Position))}");
                Debug.Log($"Quarter edges: {string.Join(", ", quarter.BoundaryEdges.Select(e => $"{e.A.Position}<->{e.B.Position}"))}");
            }
        }

        private RoadNode SampleNode(HashSet<RoadNode> candidates)
        {
            // Weight by distance from nearest growth center
            float totalWeight = 0f;
            var nodes = candidates.ToList();
            var weights = new float[nodes.Count];

            for (int i = 0; i < nodes.Count; i++)
            {
                float minDist = parameters.GrowthCenters
                    .Min(c => Vector2.Distance(nodes[i].Position, c));
                // e^(-f * dist^2) from the paper
                weights[i] = Mathf.Exp(
                    -parameters.GrowthFocusFactor * minDist * minDist /
                    (parameters.WorldSize * parameters.WorldSize)
                );
                totalWeight += weights[i];
            }

            float sample = Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < nodes.Count; i++)
            {
                cumulative += weights[i];
                if (cumulative >= sample)
                {
                    return nodes[i];
                }
            }

            return nodes[nodes.Count - 1];
        }

        /// <summary>
        /// Returns the angle to expand in, or null if the node can't expand.
        /// Uses the largest angular gap between existing edges.
        /// </summary>
        private float? GetExpansionAngle(RoadNode node)
        {
            float? angle = node.Valence switch
            {
                0 => node.BaseAngle,
                1 => node.BaseAngle + 180f,
                2 => Random.value > 0.5f ? node.BaseAngle + 90f : node.BaseAngle - 90f,
                3 => ValenceThreeAngle(node),
                _ => null
            };
            Debug.Log($"GetExpansionAngle: pos={node.Position} valence={node.Valence} baseAngle={node.BaseAngle:F1} expansionAngle={angle} outgoing=[{string.Join(", ", node.OutgoingEdges.Select(he => $"{node.GetAngle(he):F1}→{he.Destination.Position}"))}]");
            return angle;
        }

        private static float ValenceThreeAngle(RoadNode node)
        {
            float ccwAngle = node.BaseAngle + 90f;
            float cwAngle = node.BaseAngle - 90f;

            // See which one has an existing edge closer to it
            bool ccwTaken = false;
            foreach (var he in node.OutgoingEdges)
            {
                float angle = node.GetAngle(he);
                if (Mathf.Abs(Mathf.DeltaAngle(angle, ccwAngle)) < 45f)
                {
                    ccwTaken = true;
                    break;
                }
            }

            return ccwTaken ? cwAngle : ccwAngle;
        }

        /// <summary>
        /// Finds the center of the largest angular gap between existing edges.
        /// </summary>
        private static float? GetLargestGapAngle(RoadNode node)
        {
            if (node.Valence < 2) return null;

            var angles = new List<float>();
            foreach (var he in node.OutgoingEdges)
            {
                float angle = node.GetAngle(he);
                angles.Add(angle);
            }
            angles.Sort();

            float bestGapSize = 0f;
            float bestGapCenter = 0f;

            for (int i = 0; i < angles.Count; i++)
            {
                float a = angles[i];
                float b = angles[(i + 1) % angles.Count];
                float gap = b - a;
                if (gap <= 0f) gap += 360f;

                if (gap > bestGapSize)
                {
                    bestGapSize = gap;
                    bestGapCenter = a + gap * 0.5f;
                }
            }

            if (bestGapCenter > 180f) bestGapCenter -= 360f;
            if (bestGapCenter < -180f) bestGapCenter += 360f;

            Debug.Log($"LargestGap at {node.Position} | valence={node.Valence} | angles=[{string.Join(", ", angles.Select(a => a.ToString("F1")))}] | bestGap={bestGapSize:F1} | bestCenter={bestGapCenter:F1}");

            return bestGapCenter;
        }

        private void RemoveFromUnfinished(RoadNode node)
        {
            unfinishedMajor.Remove(node);
            unfinishedMinor.Remove(node);
        }

        // TODO add runtime population map to change with street expansion as required
        // private void ReducePopulation(RoadSegment segment)
        // {
        //     bool isHighway = segment.Type == RoadType.Highway;
        //     Vector2 midpoint = Vector2.Lerp(segment.Start, segment.End, 0.5f);
        //     populationMap.ReduceAround(
        //         midpoint,
        //         isHighway ? parameters.HighwayReductionRadius : parameters.StreetReductionRadius,
        //         isHighway ? parameters.HighwayReductionAmount : parameters.StreetReductionAmount
        //     );
        // }

        private static float RandomAngleDeviation(float angleDev)
        {
            float stdev = angleDev / 3;
            float gaussianSample = SampleGaussian(0, stdev);
            return Mathf.Clamp(gaussianSample, -angleDev, angleDev);
        }

        private static float SampleGaussian(float mean, float stdDev)
        {
            // Box-Muller transform
            float u1 = 1f - Random.value;
            float u2 = 1f - Random.value;
            float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        // Find highest population for seeding
        // Samples from runtime population map, influenced by road population reduction
        private Vector2 FindHighestPopulationPoint()
        {
            float bestScore = -1f;
            Vector2 bestPos = Vector2.zero;
            int searchSamples = 20;

            for (int x = 0; x < searchSamples; x++)
            {
                for (int y = 0; y < searchSamples; y++)
                {
                    float tx = x / (float)(searchSamples - 1);
                    float ty = y / (float)(searchSamples - 1);
                    Vector2 pos = new Vector2(
                        (tx - 0.5f) * parameters.WorldSize,
                        (ty - 0.5f) * parameters.WorldSize
                    );

                    if (!parameters.IsLegalPosition(pos)) continue;

                    float score = parameters.SamplePopulation(pos);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPos = pos;
                    }
                }
            }

            return bestPos;
        }
    }
}