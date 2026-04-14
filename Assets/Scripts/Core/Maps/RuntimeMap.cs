#nullable enable
using UnityEngine;

namespace CityGenerator.Core.Maps
{
    public class RuntimeMap
    {
        private readonly float[,] density;
        private readonly int resolution;
        private readonly float worldSize;

        public RuntimeMap(Texture2D source, int resolution, float worldSize)
        {
            this.resolution = resolution;
            this.worldSize = worldSize;
            density = new float[resolution, resolution];

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    density[x, y] = source.GetPixelBilinear(
                        x / (float)(resolution - 1),
                        y / (float)(resolution - 1)).r;
                }
            }
        }

        public float Sample(Vector2 worldPos)
        {
            Vector2Int idx = WorldToIndex(worldPos);
            if (!InBounds(idx)) return 0f;

            return density[idx.x, idx.y];
        }

        public void ReduceAround(Vector2 worldPos, float radius, float amount)
        {
            Vector2Int center = WorldToIndex(worldPos);
            int radiusInPixels = Mathf.CeilToInt(radius * resolution / worldSize);

            for (int dx = -radiusInPixels; dx <= radiusInPixels; dx++)
            {
                for (int dy = -radiusInPixels; dy <= radiusInPixels; dy++)
                {
                    Vector2Int idx = new Vector2Int(center.x + dx, center.y + dy);
                    if (!InBounds(idx)) continue;

                    float dist = new Vector2(dx, dy).magnitude / radiusInPixels;
                    float falloff = Mathf.Exp(-dist * dist * 2f);
                    density[idx.x, idx.y] = Mathf.Max(0f, density[idx.x, idx.y] - amount * falloff);
                }
            }
        }

        private Vector2Int WorldToIndex(Vector2 worldPos)
        {
            float u = (worldPos.x / worldSize) + 0.5f;
            float v = (worldPos.y / worldSize) + 0.5f;
            return new Vector2Int(
                Mathf.RoundToInt(u * (resolution - 1)),
                Mathf.RoundToInt(v * (resolution - 1))
            );
        }

        private bool InBounds(Vector2Int idx) =>
            idx.x >= 0 && idx.x < resolution &&
            idx.y >= 0 && idx.y < resolution;
    }
}