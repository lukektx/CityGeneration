#nullable enable

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace CityGenerator.Core.Road
{
    public class RoadEdge
    {
        public RoadNode A { get; }
        public RoadNode B { get; }
        // A to B half edge
        public HalfEdge HalfA { get; }
        // B to A half edge
        public HalfEdge HalfB { get; }
        public RoadType Type { get; }
        public Spline Spline { get; }

        public float Length { get; }

        public RoadNode Other(RoadNode node) => node == A ? B : A;

        public HalfEdge GetHalfFrom(RoadNode node) => node == A ? HalfA : HalfB;
        public HalfEdge GetHalfTo(RoadNode node) => node == A ? HalfB : HalfA;

        public RoadEdge(RoadNode a, RoadNode b, RoadType type)
        {
            A = a;
            B = b;

            Length = Vector2.Distance(a.Position, b.Position);

            HalfA = new HalfEdge(a, this);
            HalfB = new HalfEdge(b, this);
            HalfA.Twin = HalfB;
            HalfB.Twin = HalfA;

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