#nullable enable
using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Quarters
{
    public static class QuarterDetector
    {
        public static List<Quarter> FindQuartersForEdge(
            RoadEdge newEdge,
            HashSet<Quarter> knownQuarters,
            float minArea = 100f
        )
        {
            if (newEdge.Type != RoadType.Major) return new List<Quarter>();

            var newQuarters = new List<Quarter>();

            // Each half-edge traces a different face
            foreach (var startHalf in new[] { newEdge.HalfA, newEdge.HalfB })
            {
                var face = TraceFace(startHalf);
                if (face == null || face.Count < 3) continue;

                var quarter = new Quarter(face);

                // Check quarter winding
                if (quarter.IsClockwise) continue;

                // Filter degenerate quarters
                if (quarter.Area < minArea) continue;

                if (knownQuarters.Add(quarter))
                {
                    newQuarters.Add(quarter);
                }
            }

            return newQuarters;
        }

        /// <summary>
        /// Traces a face by following half-edge Next pointers.
        /// Returns null if the face is not closed or exceeds max steps.
        /// </summary>
        public static List<HalfEdge>? TraceFace(HalfEdge start, int maxSteps = 100)
        {
            var face = new List<HalfEdge>();
            face.Add(start);
            var current = start.Next;

            while (current != null && current != start && face.Count <= maxSteps)
            {
                face.Add(current);
                current = current.Next;
            }

            return face;
        }
    }
}