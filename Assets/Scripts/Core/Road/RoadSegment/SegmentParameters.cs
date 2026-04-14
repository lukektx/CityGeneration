#nullable enable

using CityGenerator.Core.Rules;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class SegmentParameters
    {
        public float Angle { get; }
        public float Length { get; }
        public int BranchDelay { get; }
        public RoadType Type { get; }
        // Nullable, when creating road, if null goes to default for road type
        public IRoadRule? Rule { get; }

        public SegmentParameters(
            float angle,
            float length,
            RoadType type,
            IRoadRule? rule = null,
            int branchDelay = 0
        )
        {
            Angle = angle;
            Length = length;
            Type = type;
            Rule = rule;
            BranchDelay = branchDelay;
        }

        public Vector2 EndPoint(Vector2 start)
        {
            float rad = Angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 end = start + dir * Length;
            return start + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * Length;
        }
    }
}