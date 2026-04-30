#nullable enable
using UnityEngine;

namespace CityGenerator.Core.Traffic
{
    [CreateAssetMenu(fileName = "TrafficParameters", menuName = "City Generator/Traffic Parameters")]
    public class TrafficParameters : ScriptableObject
    {
        [Header("Agent Settings")]
        public float BaseSpeed = 8f;
        public float MajorRoadMultiplier = 1f;
        public float MinorRoadMultiplier = 0.4f;
        public float LaneOffset = 0.8f;
        public bool RightSideDrive = true;
        public int LaneMultiplier => RightSideDrive ? 1 : -1;

        [Header("Follow Settings")]
        public float StopDistanceFromIntersection = 2f;
        public float FollowDistance = 1.5f;

        [Header("Turning Settings")]
        public float TurnSpeed = 0.8f;
        public float TurnTangentScale = 0.4f;

        [Header("Intersection Settings")]
        public float ReleaseInterval = 1.0f;
        public int IntersectionValence = 3;

        [Header("Spawning Settings")]
        public int TripsToSpawn = 20;
        public float RespawnDelay = 2f;

        [Header("Car Colors")]
        public int CarBodyMaterialIndex = 0;
        public Color[] CarColors = new Color[]
        {
            new Color(0.8f, 0.1f, 0.1f),
            new Color(0.1f, 0.2f, 0.8f),
            new Color(0.9f, 0.9f, 0.9f),
            new Color(0.1f, 0.1f, 0.1f),
            new Color(0.2f, 0.6f, 0.2f),
            new Color(0.9f, 0.6f, 0.1f),
        };
    }
}