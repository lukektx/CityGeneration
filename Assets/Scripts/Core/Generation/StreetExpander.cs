#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Parameters;
using CityGenerator.Core.Quarters;
using CityGenerator.Core.Road;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CityGenerator.Core.Generation
{
    public class StreetExpander
    {
        public event Action<RoadSegment, bool>? OnSegmentProposed;
        private readonly CityParameters parameters;
        private readonly RoadGraph graph = new();

        // Detected quarters waiting to be filled
        private readonly Queue<Quarter> pendingQuarters = new();
        private readonly HashSet<Quarter> knownQuarters = new();

        public RoadGraph Graph => graph;
        public bool IsComplete =>
            graph.Edges.Count >= parameters.MaxSegments ||
            (
                graph.UnfinishedCount == 0 &&
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
                Debug.Log($"Generation complete: edges={graph.Edges.Count}/{parameters.MaxSegments} | unfinishedMajor={graph.UnfinishedMajorCount} | unfinishedMinor={graph.UnfinishedMinorCount} | pendingQuarters={pendingQuarters.Count}");
                return false;
            }

            Debug.Log($"Expanding: edges={graph.Edges.Count}/{parameters.MaxSegments} | unfinishedMajor={graph.UnfinishedMajorCount} | unfinishedMinor={graph.UnfinishedMinorCount} | pendingQuarters={pendingQuarters.Count}");

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
            RoadNode? node = SampleNextMinorNode();
            if (node != null)
            {
                Debug.Log($"Expanding minor node {node.Position}");
                ExpandNode(node, RoadType.Minor);
                return true;
            }

            // Otherwise expand a major node
            node = SampleNextMajorNode();
            if (node != null)
            {
                Debug.Log($"Expanding major node {node.Position} valence {node.Valence} current valence ratio {graph.MajorValenceRatio}");
                ExpandNode(node, RoadType.Major);
                return true;
            }
            else
            {
                Debug.Log("No major nodes to expand");
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
            // Create seeded major roads from given growth centers 
            Vector2[] centers = parameters.GrowthCenters;

            foreach (Vector2 center in centers)
            {
                Vector2 endPos = center + Vector2.right * parameters.MajorStreetLength;

                var startNode = graph.AddNode(center, RoadType.Major);
                var endNode = graph.AddNode(endPos, RoadType.Major);

                graph.AddEdge(startNode, endNode, RoadType.Major);
            }
        }

        private bool ExpandQuarterNode(RoadNode node, float angle, Quarter quarter)
        {
            return ExpandNodeAtAngle(node, RoadType.Minor, angle, quarter: quarter);
        }

        private bool ExpandNode(RoadNode node, RoadType type)
        {
            float? expansionAngle = GetExpansionAngle(node);
            if (!expansionAngle.HasValue)
            {
                graph.FinishNode(node);
                return false;
            }

            return ExpandNodeAtAngle(node, type, expansionAngle.Value);
        }

        private bool ExpandNodeAtAngle(RoadNode node, RoadType type, float angle, Quarter? quarter = null)
        {
            float length = (type == RoadType.Major)
                ? parameters.MajorStreetLength
                : parameters.MinorStreetLength;

            float angleDeviation = (type == RoadType.Major)
                ? parameters.MaxMajorAngleDeviation
                : parameters.MaxMinorAngleDeviation;

            angle += RandomAngleDeviation(angleDeviation);


            float rad = angle * Mathf.Deg2Rad;
            Vector2 newDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 newPos = node.Position + newDirection * length;

            //Debug.Log($"{node.Position} Expansion angle after deviation {angle} segment from {node.Position} -> {newPos}");

            var segment = new RoadSegment(
                start: node.Position,
                end: newPos,
                type: type,
                startNode: node,
                quarter: quarter
            );

            // Validate with quarter requirements
            if (!QuarterValidation(segment)) return false;

            bool success = CommitSegment(segment);
            OnSegmentProposed?.Invoke(segment, success);
            if (!success)
            {
                FailedExpansion(node);
                return false;
            }

            return true;
        }

        private void FailedExpansion(RoadNode node)
        {
            node.FailedExpansions++;
            if (node.FailedExpansions >= parameters.MaxExpansionFailures)
            {
                graph.FinishNode(node);
            }
        }

        private bool QuarterValidation(RoadSegment segment)
        {
            Vector2 midpoint = (segment.Start + segment.End) * 0.5f;
            // Check if in proper quarter
            if (segment.Quarter != null && !segment.Quarter.IsPointInside(midpoint))
            {
                return false;
            }
            // If not in quarter, make sure it doesnt go into existing quarter
            else if (segment.Type == RoadType.Major)
            {
                foreach (var quarter in knownQuarters)
                {
                    if (quarter.IsPointInside(midpoint))
                    {
                        FailedExpansion(segment.StartNode!);
                        return false;
                    }
                }
            }

            return true;
        }

        private void FillQuarter(Quarter quarter)
        {
            // HashSet<RoadNode> interiorNodes = graph.Nodes.Where(node => quarter.IsPointInside(node.Position)).ToHashSet();
            // interiorNodes.UnionWith(quarter.Nodes.ToHashSet());
            HashSet<RoadNode> boundaryNodes = quarter.Nodes.ToHashSet();

            foreach (RoadNode node in boundaryNodes)
            {
                foreach (float angle in quarter.GetGridAngles())
                {
                    // Stop after seeding one segment
                    if (ExpandQuarterNode(node, angle, quarter)) return;
                }
            }

            RoadNode seedNode = graph.AddNode(quarter.Centroid, RoadType.Minor);
            seedNode.InitialAngle = Mathf.Atan2(quarter.MainAxis.y, quarter.MainAxis.x) * Mathf.Rad2Deg;

        }

        private bool CommitSegment(RoadSegment segment)
        {
            // Test LocalConstraints to apply global rules
            var result = LocalConstraints.Apply(segment, graph, parameters);
            if (result == ConstraintResult.Failed) return false;

            RoadNode toNode = segment.EndNode ?? graph.AddNode(segment.End, segment.Type);

            // Add edge to the graph
            var edge = graph.AddEdge(segment.StartNode!, toNode, segment.Type);

            // Check for new quarters when adding major roads
            if (segment.Type == RoadType.Major)
            {
                CheckForNewQuarters(edge);
            }

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

        private RoadNode? SampleNextMinorNode()
        {
            RoadNode? sample = null;
            if (graph.HasUnfinishedMinorNodes)
            {
                sample = SampleNode(graph.UnfinishedMinorNodes);
            }

            return sample;
        }

        private RoadNode? SampleNextMajorNode()
        {
            // Valence 0 always has highest priority (new unconnected nodes)
            RoadNode? sample;
            if (TrySampleMajorValence(0, out sample)) return sample;

            // Valence 3 has next priority (expand to full junction)
            if (TrySampleMajorValence(3, out sample)) return sample;
            float currentRatio = graph.MajorValenceRatio;

            // Check if we need more valence 2 or 4 based on parameters
            bool needMoreBranches = currentRatio < parameters.TargetBranchRatio;

            if (needMoreBranches)
            {
                if (TrySampleMajorValence(2, out sample)) return sample;
                if (TrySampleMajorValence(1, out sample)) return sample;
            }
            else
            {
                if (TrySampleMajorValence(1, out sample)) return sample;
                if (TrySampleMajorValence(2, out sample)) return sample;
            }

            return null;
        }

        private bool TrySampleMajorValence(int valence, out RoadNode? sample)
        {
            sample = null;
            if (graph.HasUnfinishedMajorNodes(valence))
            {
                sample = SampleNode(graph.UnfinishedMajorNodes(valence));
                return true;
            }

            return false;
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
            return angle;
        }

        private static float? ValenceThreeAngle(RoadNode node)
        {
            if (node.BaseEdge == null) return null;

            float[] candidates = {
                node.BaseAngle + 90f,
                node.BaseAngle + 180f,
                node.BaseAngle + 270f
            };

            float bestAngle = 0f;
            float bestMinDelta = -1f;

            foreach (float candidate in candidates)
            {
                float minDelta = node.OutgoingEdges
                    .Min(he => Mathf.Abs(Mathf.DeltaAngle(candidate, node.GetAngle(he))));

                if (minDelta > bestMinDelta)
                {
                    bestMinDelta = minDelta;
                    bestAngle = candidate;
                }
            }

            return bestAngle;
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