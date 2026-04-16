#nullable enable
using UnityEngine;

namespace CityGenerator.Core.Maps
{
    public class RuntimeMap
    {
        private readonly float[,] data;
        private readonly Color[] pixels;
        private readonly int resolution;
        private readonly float worldSize;
        private readonly Texture2D texture;

        public Texture2D Texture => texture;
        public int Resolution => resolution;

        public RuntimeMap(Texture2D source, int resolution, float worldSize)
        {
            this.resolution = resolution;
            this.worldSize = worldSize;
            data = new float[resolution, resolution];
            pixels = new Color[resolution * resolution];
            texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float value = source.GetPixelBilinear(
                        x / (float)(resolution - 1),
                        y / (float)(resolution - 1)).r;

                    data[x, y] = value;
                    SetPixelGrayscale(x, y, value);
                }
            }

            UpdateTexture();
        }

        public float Sample(Vector2 worldPos)
        {
            Vector2Int idx = WorldToIndex(worldPos);
            if (!InBounds(idx)) return 0f;

            return data[idx.x, idx.y];
        }

        public void ReduceAround(Vector2 worldPos, float radius, float amount)
        {
            Vector2Int center = WorldToIndex(worldPos);
            int radiusInPixels = Mathf.CeilToInt(radius * resolution / worldSize);

            bool modified = false;

            for (int dx = -radiusInPixels; dx <= radiusInPixels; dx++)
            {
                for (int dy = -radiusInPixels; dy <= radiusInPixels; dy++)
                {
                    Vector2Int idx = new Vector2Int(center.x + dx, center.y + dy);
                    if (!InBounds(idx)) continue;

                    float dist = new Vector2(dx, dy).magnitude / radiusInPixels;
                    float falloff = Mathf.Exp(-dist * dist * 2f);
                    float newValue = Mathf.Max(0f, data[idx.x, idx.y] - amount * falloff);

                    data[idx.x, idx.y] = newValue;
                    SetPixelGrayscale(idx.x, idx.y, newValue);
                    modified = true;
                }
            }

            if (modified)
            {
                UpdateTexture();
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

        private void SetPixel(int x, int y, Color color)
        {
            pixels[y * resolution + x] = color;
        }

        private void SetPixel(Vector2Int pos, Color color)
        {
            SetPixel(pos.x, pos.y, color);
        }

        private void SetPixelGrayscale(int x, int y, float value)
        {
            SetPixel(x, y, new Color(value, value, value));
        }

        private void UpdateTexture()
        {
            texture.SetPixels(pixels);
            texture.Apply();
        }
    }
}