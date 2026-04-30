#nullable enable

using System.Collections.Generic;
using CityGenerator.Core.Road;

namespace CityGenerator.Core.Traffic
{
    public class Trip
    {
        public RoadNode Start { get; }
        public RoadNode End { get; }
        public IReadOnlyList<RoadNode> Path { get; }

        public Trip(RoadNode start, RoadNode end, List<RoadNode> path)
        {
            Start = start;
            End = end;
            Path = path;
        }
    }
}