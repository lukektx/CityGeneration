#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator.Core.Road
{
    public class RoadNode
    {
        public Vector2 Position { get; }
        public IReadOnlyList<RoadEdge> Edges => edges;
        private readonly List<RoadEdge> edges = new();

        public RoadNode(Vector2 position)
        {
            Position = position;
        }

        internal void AddEdge(RoadEdge edge) => edges.Add(edge);
        internal void RemoveEdge(RoadEdge edge) => edges.Remove(edge);
    }
}