using System.Collections.Generic;
using CityGenerator.Core.Parameters;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Rules
{
    [CreateAssetMenu(menuName = "City Generator/Rules/Basic")]
    public class BasicRule : RoadRule
    {
        [Header("Street Settings")]
        [SerializeField] private float _streetBranchingNoise = 10f;
        [SerializeField] private float _streetBranchingProbability = 0.6f;
        [SerializeField] private int _minHighwayBranchDelay = 2;
        [SerializeField] private int _maxHighwayBranchDelay = 4;

        [SerializeField] private int _minStreetBranchDelay = 1;
        [SerializeField] private int _maxStreetBranchDelay = 2;

        [Header("Highway Settings")]
        [SerializeField] private float _highwayRayRadius = 100f;
        [SerializeField] private float _highwayAngleRange = 45f;
        [SerializeField] private int _highwayRayCount = 16;
        [SerializeField] private int _highwayRaySamples = 6;
        // [SerializeField] private float _forwardBiasStrength = 2f;

        [Header("Highway Branching")]
        [SerializeField] private int _highwayBranchCount = 2;
        [SerializeField] private int _highwayBranchDelay = 5;
        [SerializeField] private float _minBranchPopulationThreshold = 0.1f;


        public override SegmentParameters ProposeNextSegment(SegmentContext ctx)
        {
            float length = ctx.Type == RoadType.Highway
                ? ctx.Parameters.HighwayLength
                : ctx.Parameters.StreetLength;

            float bestAngle = ctx.Angle;

            if (ctx.Type == RoadType.Highway)
            {
                bestAngle = FindPopulationPeak(ctx);
            }
            else
            {
                // Streets just continue forward with slight noise
                bestAngle = ctx.Angle + RandomStreetAngleNoise();
            }

            return new SegmentParameters
            (
                angle: bestAngle,
                length: length,
                type: ctx.Type
            );
        }

        public override IEnumerable<SegmentParameters> ProposeBranches(SegmentContext ctx)
        {
            if (ctx.Type == RoadType.Highway)
            {
                // Spawn additional highway branches toward other population peaks
                for (int i = 0; i < _highwayBranchCount; i++)
                {
                    // Each branch searches a different angular sector
                    float sectorAngle = ctx.Angle + (i + 1) * (360f / (_highwayBranchCount + 1));
                    float? bestAngle = FindPopulationPeakInSector(ctx, sectorAngle, 60f);

                    // Found some valid angle in range
                    if (bestAngle.HasValue)
                    {
                        yield return new SegmentParameters(
                            angle: bestAngle.Value,
                            length: ctx.Parameters.HighwayLength,
                            type: RoadType.Highway,
                            branchDelay: _highwayBranchDelay
                        );
                    }
                }

                // Propose street branch
                yield return new SegmentParameters(
                    angle: ctx.Angle + 90f,
                    length: ctx.Parameters.StreetLength,
                    type: RoadType.Street,
                    branchDelay: Random.Range(_minHighwayBranchDelay, _maxHighwayBranchDelay + 1)
                );
            }
            else
            {
                // Streets branch at roughly 90 degrees occasionally
                if (Random.value <= _streetBranchingProbability)
                {
                    yield return new SegmentParameters
                    (
                        angle: ctx.Angle + 90f,
                        length: ctx.Parameters.StreetLength,
                        type: RoadType.Street,
                        branchDelay: Random.Range(_minStreetBranchDelay, _maxStreetBranchDelay + 1)
                    );
                }
            }
        }

        private float? FindPopulationPeakInSector(SegmentContext ctx, float sectorCenter, float sectorWidth)
        {
            // minimum threshold, return null if nothing found
            float bestScore = _minBranchPopulationThreshold;
            float? bestAngle = null;

            for (int i = 0; i < _highwayRayCount; i++)
            {
                float t = i / (float)(_highwayRayCount - 1);
                float angle = sectorCenter + Mathf.Lerp(-sectorWidth, sectorWidth, t);
                float score = ScoreRay(ctx.Position, angle, ctx.Parameters);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestAngle = angle;
                }
            }

            return bestAngle;
        }

        private float FindPopulationPeak(SegmentContext ctx)
        {
            float bestScore = -1f;
            float bestAngle = ctx.Angle;

            for (int i = 0; i < _highwayRayCount; i++)
            {
                float t = i / (float)(_highwayRayCount - 1);
                float angle = ctx.Angle + Mathf.Lerp(-_highwayAngleRange, _highwayAngleRange, t);
                float score = ScoreRay(ctx.Position, angle, ctx.Parameters);

                // Add forward bias as bonus rather than multiplier
                // float deviation = Mathf.Abs(Mathf.DeltaAngle(ctx.Angle, angle));
                // score += _forwardBiasStrength * (1f - deviation / _highwayAngleRange);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestAngle = angle;
                }
            }

            return bestAngle;
        }

        private float ScoreRay(Vector2 origin, float angleDeg, CityParameters parameters)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float score = 0f;

            for (int i = 1; i <= _highwayRaySamples; i++)
            {
                float t = i / (float)_highwayRaySamples;
                Vector2 samplePos = origin + dir * (_highwayRayRadius * t);

                // Zero score for illegal positions instead of just ignoring them
                if (!parameters.IsLegalPosition(samplePos))
                {
                    score = 0f;  // water kills this entire ray
                    break;
                }

                // closer samples weighted more
                float weight = 1f - t;
                score += parameters.SamplePopulation(samplePos) * weight;
            }

            return score;
        }

        private float RandomStreetAngleNoise()
        {
            return Random.Range(-_streetBranchingNoise, _streetBranchingNoise);
        }
    }
}