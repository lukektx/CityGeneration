#nullable enable

using CityGenerator.Core.Maps;
using CityGenerator.Core.Parameters;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class SegmentContext
    {
        public Vector2 Position { get; set; }
        public float Angle { get; set; }
        public int Depth { get; set; }
        public RoadType Type { get; set; }
        public CityParameters Parameters { get; set; }
        public RuntimeMap PopulationMap { get; }

        public SegmentContext(
            Vector2 position,
            float angle,
            int depth,
            RoadType type,
            CityParameters parameters,
            RuntimeMap populationMap
        )
        {
            Position = position;
            Angle = angle;
            Depth = depth;
            Type = type;
            Parameters = parameters;
            PopulationMap = populationMap;
        }
    }
}