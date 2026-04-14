using System.Collections.Generic;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Rules
{
    [CreateAssetMenu(menuName = "City Generator/Rules/New York")]
    public class NewYorkRule : RoadRule
    {
        [Header("Grid Settings")]
        [Tooltip("Dominant grid orientation in degrees")]
        public float GridAngle = 0f;
        [Tooltip("Street angle deviation from grid in degrees")]
        public float AngleNoise = 3f;

        [Header("Block Settings")]

        public float BlockLength = 40f;
        public float BlockWidth = 25f;

        [Header("Street Settings")]
        public int HighwayBranchDelay = 3;
        public int StreetBranchDelay = 5;

        public override SegmentParameters ProposeNextSegment(SegmentContext ctx)
        {
            float snapped = SnapToGrid(ctx.Angle);
            return new SegmentParameters
            (
                // tiny noise keeps it organic
                angle: snapped + Random.Range(-AngleNoise, AngleNoise),
                length: ctx.Type == RoadType.Highway ? BlockLength : BlockWidth,
                type: ctx.Type,
                branchDelay: 0
            );
        }

        public override IEnumerable<SegmentParameters> ProposeBranches(SegmentContext ctx)
        {
            // Snap incoming angle first, then branch at 90 degrees from the snapped angle
            float snappedBaseAngle = SnapToGrid(ctx.Angle);

            yield return new SegmentParameters
            (
                angle: SnapToGrid(snappedBaseAngle + 90f),
                length: BlockWidth,
                type: RoadType.Street,
                branchDelay: ctx.Type == RoadType.Highway ? HighwayBranchDelay : StreetBranchDelay
            );
        }

        private float SnapToGrid(float angle)
        {
            // Snap to nearest multiple of 90 degrees, offset by gridAngle
            float relative = angle - GridAngle;
            float snapped = Mathf.Round(relative / 90f) * 90f;
            return snapped + GridAngle;
        }
    }
}