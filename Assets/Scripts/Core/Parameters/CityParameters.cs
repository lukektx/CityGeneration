#nullable enable

using System;
using CityGenerator.Core.Road;
using CityGenerator.Core.Rules;
using UnityEngine;

namespace CityGenerator.Core.Parameters
{
    [CreateAssetMenu(fileName = "CityParameters", menuName = "City Generator/City Parameters")]
    public class CityParameters : ScriptableObject
    {
        [field: Header("World Settings")]
        [field: SerializeField] public float WorldSize { get; private set; } = 500f;

        [field: Header("Generation Settings")]
        [field: SerializeField] public int MaxSegments { get; private set; } = 500;
        [field: SerializeField] public int MaxSeedingSamples { get; private set; } = 20;

        [field: Header("Highway Settings")]
        [field: SerializeField] public float HighwayLength { get; private set; } = 60f;
        [field: SerializeField] public int MaxHighwayDepth { get; private set; } = 100;

        [field: Header("Street Settings")]
        [field: SerializeField] public float StreetLength { get; private set; } = 30f;
        [field: SerializeField] public int MaxStreetDepth { get; private set; } = 20;
        [field: SerializeField] public float MinStreetPopulation { get; private set; } = 0.05f;

        [field: Header("Constraint Settings")]
        [field: SerializeField] public float SnapDistance { get; private set; } = 15f;
        [field: SerializeField] public float MaxRotationAttempts { get; private set; } = 6;
        [field: SerializeField] public float RotationStep { get; private set; } = 15f;
        [field: Tooltip("Minimum angle difference between roads with the same start node")]
        [field: SerializeField] public float MinBranchAngle { get; private set; } = 30f;
        [field: Tooltip("Minimum percentage of length for a road to be")]
        [field: SerializeField] public float MinLengthFactor { get; private set; } = 0.4f;

        [field: Header("Population Reduction")]
        [field: SerializeField] public int PopulationMapResolution { get; private set; } = 128;
        [field: SerializeField] public float HighwayReductionRadius { get; private set; } = 80f;
        [field: SerializeField] public float HighwayReductionAmount { get; private set; } = 0.6f;
        [field: SerializeField] public float StreetReductionRadius { get; private set; } = 30f;
        [field: SerializeField] public float StreetReductionAmount { get; private set; } = 0.1f;

        [Header("Default Rules")]
        [SerializeField] private RoadRule? _highwayRule;
        [SerializeField] private RoadRule? _streetRule;

        [field: Header("Input Maps")]
        [field: SerializeField] public Texture2D? PopulationMap { get; private set; }
        [field: SerializeField] public Texture2D? WaterMask { get; private set; }


        public float SamplePopulation(Vector2 worldPos)
        {
            if (PopulationMap == null) return 0.5f;

            Vector2 uv = WorldToUV(worldPos);
            return PopulationMap.GetPixelBilinear(uv.x, uv.y).r;
        }

        public bool IsLegalPosition(Vector2 worldPos)
        {
            if (WaterMask == null) return true;

            Vector2 uv = WorldToUV(worldPos);
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return false;
            // Convention is white = water (illegal), black = land (legal)
            return WaterMask.GetPixelBilinear(uv.x, uv.y).r < 0.5f;
        }

        private Vector2 WorldToUV(Vector2 worldPos)
        {
            return new Vector2(
                (worldPos.x / WorldSize) + 0.5f,
                (worldPos.y / WorldSize) + 0.5f
            );
        }

        public IRoadRule GetRuleForType(RoadType type)
        {
            IRoadRule? rule = type == RoadType.Highway ? _highwayRule : _streetRule;
            if (rule == null)
            {
                string paramName = type == RoadType.Highway ? "HighwayRule" : "StreetRule";
                throw new ArgumentNullException(paramName, $"[CityParameters] Rule {paramName} is null");
            }

            return rule;
        }

        public int GetMaxDepthForType(RoadType type)
        {
            return type == RoadType.Highway
                ? MaxHighwayDepth
                : MaxStreetDepth;
        }
    }
}