#nullable enable

using Unity.Mathematics;
using UnityEngine.Splines;

namespace CityGenerator.Core.Road
{
    public class RoadEdge
    {
        public RoadNode From { get; }
        public RoadNode To { get; }
        public RoadType Type { get; }
        public Spline Spline { get; }

        public RoadEdge(RoadNode from, RoadNode to, RoadType type)
        {
            From = from;
            To = to;
            Type = type;
            Spline = new Spline
            {
                {
                    new BezierKnot(new float3(from.Position.x, 0, from.Position.y)),
                    TangentMode.Linear
                },
                {
                    new BezierKnot(new float3(to.Position.x, 0, to.Position.y)),
                    TangentMode.Linear
                }
            };
        }
    }
}