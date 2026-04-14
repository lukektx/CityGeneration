#nullable enable

using System;
using System.Collections.Generic;
using CityGenerator.Core.Maps;
using CityGenerator.Core.Parameters;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Generation
{

    public class RoadGenerator
    {
        private readonly CityParameters parameters;
        private readonly RoadGraph graph = new();
        private readonly Queue<RoadSegment> pending = new();
        private readonly RuntimeMap populationMap;

        public RoadGraph Graph => graph;
        public bool IsComplete => pending.Count == 0 ||
                                  graph.Edges.Count >= parameters.MaxSegments;

        public RoadGenerator(CityParameters parameters)
        {
            if (parameters.PopulationMap == null)
            {
                throw new ArgumentNullException("PopulationMap");
            }

            this.parameters = parameters;
            this.populationMap = new RuntimeMap(
                parameters.PopulationMap,
                parameters.PopulationMapResolution,
                parameters.WorldSize
            );
            Seed();
        }

        public bool GenerateNextSegment()
        {
            if (IsComplete) return false;

            var segment = pending.Dequeue();

            if (segment.BranchDelay > 0)
            {
                segment.BranchDelay--;
                pending.Enqueue(segment);
                return true;
            }

            var result = LocalConstraints.Apply(segment, graph, parameters);
            if (result == ConstraintResult.Failed) return true;

            RoadNode fromNode = segment.StartNode ?? graph.AddNode(segment.Start);
            RoadNode toNode = segment.EndNode ?? graph.AddNode(segment.End);
            if (fromNode == toNode) return true;

            graph.AddEdge(fromNode, toNode, segment.Type);



            if (segment.Depth < parameters.GetMaxDepthForType(segment.Type) && !segment.WasPruned)
            {
                var successors = GlobalGoals.Apply(segment, parameters, populationMap);
                foreach (var s in successors)
                {
                    s.StartNode = toNode;
                    pending.Enqueue(s);
                }
            }

            return true;
        }

        public void GenerateAll()
        {
            while (!IsComplete)
            {
                GenerateNextSegment();
            }
        }

        private void ReducePopulationAlongSegment(RoadSegment segment)
        {
            bool isHighway = segment.Type == RoadType.Highway;
            float radius = isHighway ?
                parameters.HighwayReductionRadius : parameters.StreetReductionRadius;
            float amount = isHighway ?
                parameters.HighwayReductionAmount : parameters.StreetReductionAmount;

            // Single reduction at midpoint, radius covers the whole segment
            Vector2 midpoint = Vector2.Lerp(segment.Start, segment.End, 0.5f);
            populationMap.ReduceAround(midpoint, radius, amount);
        }

        private void Seed()
        {
            Vector2 startPos = FindHighestPopulationPoint();
            var centerNode = graph.AddNode(startPos);

            foreach (float angle in new[] { 0f, 90f })
            {

                float rad = angle * Mathf.Deg2Rad;
                Vector2 end = startPos +
                    new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) *
                    parameters.HighwayLength;

                pending.Enqueue(new RoadSegment(
                    start: startPos,
                    end: end,
                    type: RoadType.Highway,
                    depth: 0,
                    branchDelay: 0,
                    rule: parameters.GetRuleForType(RoadType.Highway),
                    startNode: centerNode
                ));
            }
        }

        private Vector2 FindHighestPopulationPoint()
        {
            float bestScore = -1f;
            Vector2 bestPos = Vector2.zero;

            int samples = parameters.MaxSeedingSamples;

            for (int x = 0; x < parameters.MaxSeedingSamples; x++)
            {
                for (int y = 0; y < samples; y++)
                {
                    float tx = x / (float)(samples - 1);
                    float ty = y / (float)(samples - 1);
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