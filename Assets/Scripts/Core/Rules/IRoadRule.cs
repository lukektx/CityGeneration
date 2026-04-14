using System.Collections.Generic;
using CityGenerator.Core.Road;

namespace CityGenerator.Core.Rules
{
    public interface IRoadRule
    {
        SegmentParameters ProposeNextSegment(SegmentContext context);
        IEnumerable<SegmentParameters> ProposeBranches(SegmentContext context);
    }
}