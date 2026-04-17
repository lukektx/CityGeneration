#nullable enable

using Unity.Mathematics;
using UnityEngine.Splines;

namespace CityGenerator.Core.Road
{
    public class RoadEdge
    {
        public RoadNode A { get; }
        public RoadNode B { get; }
        public RoadType Type { get; }
        public Spline Spline { get; }

        public RoadNode Other(RoadNode node) => node == A ? B : A;

        public RoadEdge(RoadNode a, RoadNode b, RoadType type)
        {
            A = a;
            B = b;
            Type = type;
            Spline = new Spline
            {
                {
                    new BezierKnot(new float3(a.Position.x, 0, a.Position.y)),
                    TangentMode.Linear
                },
                {
                    new BezierKnot(new float3(b.Position.x, 0, b.Position.y)),
                    TangentMode.Linear
                }
            };
        }
    }
}