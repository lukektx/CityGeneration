#nullable enable

namespace CityGenerator.Core.Road
{
    public class HalfEdge
    {
        public RoadNode Origin { get; }
        public RoadEdge Edge { get; }
        public HalfEdge Twin { get; internal set; } = null!;
        public HalfEdge? Next { get; internal set; }

        public RoadNode Destination => Twin.Origin;

        public HalfEdge(RoadNode origin, RoadEdge edge)
        {
            Origin = origin;
            Edge = edge;
        }
    }
}