#nullable enable

using System;
using CityGenerator.Core.Road;
using UnityEngine;

namespace CityGenerator.Core.Parameters
{
    [CreateAssetMenu(fileName = "CityParameters", menuName = "City Generator/City Parameters")]
    public class CityParameters : ScriptableObject
    {
        [Header("World Settings")]
        public float WorldSize = 500f;

        [Header("Generation Settings")]
        public int MaxSegments = 2000;
        public int HighwaySeedCount = 3;
        public int MaxExpansionFailures = 3;
        public float MinStreetLength = 10f;

        [Header("Growth Centers")]
        public Vector2[] GrowthCenters = { Vector2.zero };
        public float GrowthFocusFactor = 2f;

        [Header("Major Street Settings")]
        public float MajorStreetLength = 60f;
        public float MaxMajorAngleDeviation = 15f;

        [Header("Minor Street Settings")]
        public float MinorStreetLengthLong = 40f;
        public float MinorStreetLengthShort = 25f;
        public float MaxMinorAngleDeviation = 0f;

        [Header("Legality")]
        public float SnapDistance = 25f;
        public float MaxRotationAttempts = 6;
        public float RotationStep = 15f;
        public float MinLengthFactor = 0.4f;

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