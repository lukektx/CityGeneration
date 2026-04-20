#nullable enable

using CityGenerator.Core.Quarters;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class RoadSegment
    {
        public Vector2 Start { get; }
        public Vector2 End { get; set; }
        public RoadType Type { get; }
        public Quarter? Quarter { get; }
        public RoadNode? StartNode { get; set; }
        public RoadNode? EndNode { get; set; }

        public Vector2 Direction => (End - Start).normalized;
        public float Length => Vector2.Distance(Start, End);
        public float Angle => Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

        public RoadSegment(
            Vector2 start,
            Vector2 end,
            RoadType type,
            RoadNode? startNode = null,
            Quarter? quarter = null
        )
        {
            Start = start;
            End = end;
            Type = type;
            StartNode = startNode;
            Quarter = quarter;
        }
    }
}