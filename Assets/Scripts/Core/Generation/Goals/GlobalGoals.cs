#nullable enable

using System.Collections.Generic;
using CityGenerator.Core.Maps;
using CityGenerator.Core.Parameters;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Generation
{
    public static class GlobalGoals
    {
        public static List<RoadSegment> Apply(
            RoadSegment segment,
            CityParameters cityParameters,
            RuntimeMap populationMap
        )
        {
            var results = new List<RoadSegment>();
            var ctx = BuildContext(segment, cityParameters, populationMap);

            // Streets stop growing in unpopulated areas
            if (segment.Type == RoadType.Street)
            {
                float pop = populationMap.Sample(segment.End);
                if (pop < cityParameters.MinStreetPopulation)
                    return results; // empty - no successors
            }

            // Propose the continuing segment
            var nextParams = segment.Rule.ProposeNextSegment(ctx);
            results.Add(ParamsToSegment(segment, nextParams, cityParameters));

            // Propose branches
            foreach (var branchParams in segment.Rule.ProposeBranches(ctx))
            {
                results.Add(ParamsToSegment(segment, branchParams, cityParameters));
            }

            return results;
        }

        private static SegmentContext BuildContext(
            RoadSegment segment,
            CityParameters cityParameters,
            RuntimeMap populationMap
        )
        {
            return new SegmentContext
            (
                position: segment.End,
                angle: segment.Angle,
                depth: segment.Depth,
                type: segment.Type,
                parameters: cityParameters,
                populationMap: populationMap
            );
        }

        private static RoadSegment ParamsToSegment(
            RoadSegment parent,
            SegmentParameters segmentParameters,
            CityParameters cityParameters
        )
        {
            Vector2 newEnd = segmentParameters.EndPoint(parent.End);
            return new RoadSegment
            (
                start: parent.End,
                end: newEnd,
                type: segmentParameters.Type,
                depth: parent.Depth + 1,
                branchDelay: segmentParameters.BranchDelay,
                rule: segmentParameters.Rule ?? cityParameters.GetRuleForType(segmentParameters.Type)
            );
        }
    }
}