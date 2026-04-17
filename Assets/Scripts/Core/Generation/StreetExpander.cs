#nullable enable

using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Maps;
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

            var startNode = graph.AddNode(startPos);
            var endNode = graph.AddNode(endPos);

            graph.AddEdge(startNode, endNode, EdgeSlot.Base, EdgeSlot.Base, RoadType.Major);

            // Start node has invalid EdgeSlot so dont use it
            //unfinishedMajor.Add(startNode);
            unfinishedMajor.Add(endNode);
        }

        private void ExpandNode(RoadNode node, RoadType type)
        {
            if (node.IsFinished)
            {
                Debug.Log($"Node at {node.Position} is finished");
                RemoveFromUnfinished(node);
                return;
            }

            float length = (type == RoadType.Major)
                ? parameters.MajorStreetLength
                : parameters.MinorStreetLengthLong;

            float angleDeviation = (type == RoadType.Major)
                ? parameters.MaxMajorAngleDeviation
                : parameters.MaxMinorAngleDeviation;

            // Get base expansion angle from valence rules
            var expansionData = GetExpansionAngle(node);
            if (!expansionData.HasValue)
            {
                Debug.Log($"Node at {node.Position} can't get expansionData");
                node.IsFinished = true;
                RemoveFromUnfinished(node);
                return;
            }

            (float baseAngle, EdgeSlot fromSlot) = expansionData.Value;

            // Apply Gaussian angle noise
            float angle = baseAngle + RandomAngleDeviation(angleDeviation);

            float rad = angle * Mathf.Deg2Rad;
            Vector2 newDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 newPos = node.Position + newDirection * length;

            // Build proposed segment for LocalConstraints
            var segment = new RoadSegment(
                start: node.Position,
                end: newPos,
                type: type,
                startNode: node
            );

            bool success = CommitSegment(segment, fromSlot);
            if (!success)
            {
                node.FailedExpansions++;
                Debug.Log($"Node at {node.Position} failed expansion count {node.FailedExpansions}");

                if (node.FailedExpansions >= parameters.MaxExpansionFailures)
                {
                    node.IsFinished = true;
                    RemoveFromUnfinished(node);
                }
            }
        }

        private void FillQuarter(Quarter quarter)
        {
            quarter.GetGridAxes(out Vector2 mainAxis, out _);
            float gridAngle = Mathf.Atan2(mainAxis.y, mainAxis.x) * Mathf.Rad2Deg;

            RoadNode seedNode = graph.AddNode(quarter.Centroid);
            seedNode.InitialAngle = gridAngle;
            unfinishedMinor.Add(seedNode);
        }

        // private void FillQuarter(Quarter quarter)
        // {
        //     quarter.GetGridAxes(out Vector2 mainAxis, out Vector2 perpAxis);

        //     foreach (var (node, inwardSlot) in quarter.CandidateSlots)
        //     {
        //         if (node.IsFinished || node.HasEdge(inwardSlot)) continue;

        //         float inwardAngle = node.BaseAngle + (int)inwardSlot * 90f;
        //         float inwardRad = inwardAngle * Mathf.Deg2Rad;
        //         Vector2 inwardDir = new Vector2(Mathf.Cos(inwardRad), Mathf.Sin(inwardRad));

        //         Vector2 chosenAxis = Vector2.Dot(mainAxis, inwardDir) < 0 ? -mainAxis : mainAxis;
        //         float gridAngle = Mathf.Atan2(chosenAxis.y, chosenAxis.x) * Mathf.Rad2Deg;
        //         float rad = gridAngle * Mathf.Deg2Rad;
        //         Vector2 newPos = node.Position +
        //             new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * parameters.MinorStreetLengthLong;

        //         var segment = new RoadSegment(
        //             start: node.Position,
        //             end: newPos,
        //             type: RoadType.Minor,
        //             startNode: node
        //         );

        //         if (CommitSegment(segment, inwardSlot)) return;
        //     }
        // }

        private bool CommitSegment(RoadSegment segment, EdgeSlot fromSlot)
        {
            var result = LocalConstraints.Apply(segment, graph, parameters);
            if (result == ConstraintResult.Failed)
            {
                Debug.Log("Failed local constraints");
                return false;
            }

            bool toNodeExisted = segment.EndNode != null;
            RoadNode toNode = segment.EndNode ?? graph.AddNode(segment.End);
            EdgeSlot toSlot = toNodeExisted ? GetArrivalSlot(toNode, segment.StartNode!) : EdgeSlot.Base;

            // If slot is already filled, don't commit road
            if (toNodeExisted && toNode.HasEdge(toSlot))
            {
                Debug.Log("Desired slot is already full");
                return false;
            }

            var edge = graph.AddEdge(segment.StartNode!, toNode, fromSlot, toSlot, segment.Type);
            if (edge == null)
            {
                Debug.Log("Graph cannot add edge");
                return false;
            }

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

            // Just check for quarters on all major road expansions
            // Prevents missing them when roads are split
            if (segment.Type == RoadType.Major)
            {
                CheckForNewQuarters(edge);
            }
            return true;
        }

        // Finds best aligned slot to use for connecting to existing node
        private EdgeSlot GetArrivalSlot(RoadNode toNode, RoadNode fromNode)
        {
            Vector2 arrivalDir = (toNode.Position - fromNode.Position).normalized;

            EdgeSlot bestSlot = EdgeSlot.Base;
            float bestAlignment = float.MinValue;

            foreach (EdgeSlot slot in System.Enum.GetValues(typeof(EdgeSlot)))
            {
                float slotAngle = (toNode.BaseAngle + slot switch
                {
                    EdgeSlot.Base => 180f,
                    EdgeSlot.CCW => -90f,
                    EdgeSlot.Opposite => 0f,
                    EdgeSlot.CW => 90f,
                    _ => 0f
                }) * Mathf.Deg2Rad;

                Vector2 slotDir = new Vector2(Mathf.Cos(slotAngle), Mathf.Sin(slotAngle));
                float alignment = Vector2.Dot(arrivalDir, slotDir);

                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    bestSlot = slot;
                }
            }

            return bestSlot;
        }

        private void CheckForNewQuarters(RoadEdge newEdge)
        {
            var newQuarters = QuarterDetector.FindQuartersForEdge(newEdge, graph, knownQuarters);
            foreach (var quarter in newQuarters)
            {
                pendingQuarters.Enqueue(quarter);
                Debug.Log($"Formed quarter with {quarter.Nodes.Count} nodes and Centroid {quarter.Centroid} with {quarter.CandidateSlots.Count} candidate slots");
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

        private (float angle, EdgeSlot slot)? GetExpansionAngle(RoadNode node)
        {
            return node.Valence switch
            {
                // Continue in growth direction
                0 => (node.BaseAngle, EdgeSlot.Base),
                1 => (node.BaseAngle, EdgeSlot.Opposite),

                2 => ValenceTwoAngle(node),

                // Find largest angular gap
                3 => ValenceThreeAngle(node),

                _ => null
            };
        }

        private static (float, EdgeSlot) ValenceTwoAngle(RoadNode node)
        {
            EdgeSlot choice;

            if (!node.HasEdge(EdgeSlot.CCW) && !node.HasEdge(EdgeSlot.CW))
            {
                choice = Random.value > 0.5f ? EdgeSlot.CCW : EdgeSlot.CW;
            }
            else
            {
                choice = node.HasEdge(EdgeSlot.CCW) ? EdgeSlot.CW : EdgeSlot.CCW;
            }

            return choice == EdgeSlot.CCW
                ? (node.BaseAngle + 90f, EdgeSlot.CCW)
                : (node.BaseAngle - 90f, EdgeSlot.CW);
        }

        // Picks expansion angle opposite the one used before
        private static (float, EdgeSlot) ValenceThreeAngle(RoadNode node)
        {
            if (!node.HasEdge(EdgeSlot.CCW) || !node.HasEdge(EdgeSlot.CW))
            {
                return node.HasEdge(EdgeSlot.CCW)
                    ? (node.BaseAngle - 90f, EdgeSlot.CW)
                    : (node.BaseAngle + 90f, EdgeSlot.CCW);
            }

            // fallback for irregular nodes
            return GetRemainingAngle(node);
        }

        private static (float, EdgeSlot) GetRemainingAngle(RoadNode node)
        {
            foreach (EdgeSlot slot in System.Enum.GetValues(typeof(EdgeSlot)))
            {
                if (slot == EdgeSlot.Base) continue;
                if (node.GetEdge(slot) == null)
                {
                    float angle = node.BaseAngle + slot switch
                    {
                        EdgeSlot.CCW => 90f,
                        EdgeSlot.Opposite => 180f,
                        EdgeSlot.CW => -90f,
                        _ => 0f
                    };
                    return (angle, slot);
                }
            }
            // Shouldn't reach here if valence check is correct
            return (node.BaseAngle, EdgeSlot.Opposite);
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