#nullable enable

using CityGenerator.Core.Rules;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class RoadSegment
    {
        public Vector2 Start { get; }
        public Vector2 End { get; set; }
        public RoadType Type { get; }
        // Depth from starting node
        public int Depth { get; }
        // Countdown before this segment activates
        public int BranchDelay { get; set; }
        public IRoadRule Rule { get; }
        // Null if not connected
        public RoadNode? StartNode { get; set; }
        public RoadNode? EndNode { get; set; }

        public bool WasPruned { get; set; } = false;

        public Vector2 Direction => (End - Start).normalized;
        public float Length => Vector2.Distance(Start, End);
        public float Angle => Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

        public RoadSegment(
            Vector2 start,
            Vector2 end,
            RoadType type,
            int depth,
            int branchDelay,
            IRoadRule rule,
            RoadNode? startNode = null
        )
        {
            Start = start;
            End = end;
            Type = type;
            Depth = depth;
            BranchDelay = branchDelay;
            Rule = rule;
            StartNode = startNode;
        }
    }
}