#nullable enable

using System;
using UnityEngine;

namespace CityGenerator.Core.Parameters
{
    [CreateAssetMenu(fileName = "CityParameters", menuName = "City Generator/City Parameters")]
    public class CityParameters : ScriptableObject
    {
        [Header("World Settings")]
        public float WorldSize = 500f;

        [Header("Simulation Settings")]
        [Tooltip("If enabled, step through simulation instead of instantly computing")]
        public bool StepThrough = true;
        public float StepDelay = 0.02f;
        public int StepsPerFrame = 1;

        [Header("Generation Settings")]
        public int MaxSegments = 2000;
        public int HighwaySeedCount = 3;
        public int MaxExpansionFailures = 3;
        [Range(0, 1)]
        [Tooltip("Target ratio of Valence 4 to Valence 2 nodes (0 means no intersections, 1 means more intersections)")]
        public float TargetBranchRatio = 0.5f;

        [Header("Growth Centers")]
        public Vector2[] GrowthCenters = { Vector2.zero };
        public float GrowthFocusFactor = 2f;

        [Header("Major Street Settings")]
        public float MajorStreetLength = 60f;
        public float MaxMajorAngleDeviation = 15f;

        [Header("Minor Street Settings")]
        public float MinorStreetLength = 40f;
        public float MaxMinorAngleDeviation = 0f;

        [Header("Legality")]
        public float SnapDistance = 25f;
        public float MaxRotationAttempts = 6;
        public float RotationStep = 15f;
        public float MinLengthFactor = 0.4f;
        public float MinRoadAngle = 30f;

        [Header("Population Reduction Settings")]
        public int PopulationMapResolution = 128;
        public float HighwayReductionRadius = 80f;
        public float HighwayReductionAmount = 0.6f;
        public float StreetReductionRadius = 30f;
        public float StreetReductionAmount = 0.3f;
        public float MinStreetPopulation = 0.05f;

        [Header("Maps")]
        public Texture2D PopulationMap = null!;
        public Texture2D WaterMask = null!;


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
    }
}