#nullable enable
using System.Collections.Generic;
using CityGenerator.Core.Road;
using CityGenerator.Core.Rules;
using UnityEngine;

public abstract class RoadRule : ScriptableObject, IRoadRule
{
    public abstract SegmentParameters ProposeNextSegment(SegmentContext context);
    public abstract IEnumerable<SegmentParameters> ProposeBranches(SegmentContext context);
}