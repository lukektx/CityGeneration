#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator.Runtime.Visualization
{

    public class MapVisualizer : MonoBehaviour
    {
        [SerializeField] private Material MapMaterial = null!;

        private Dictionary<Texture2D, GameObject> quads = new();

        public void ShowTextures(List<(Texture2D?, MapDisplayOptions?)> textures, float worldSize)
        {
            foreach ((Texture2D? texture, MapDisplayOptions? options) in textures)
            {
                ShowTexture(texture, options, worldSize);
            }
        }

        public void ShowTexture(
            Texture2D? texture,
            MapDisplayOptions? displayOptions,
            float worldSize
        )
        {
            if (texture == null) return;
            Debug.Log($"[MapVisualizer] Displaying map texture {texture?.name} color {(displayOptions != null ? displayOptions.Color : "")}");

            GameObject? quad;
            quads.TryGetValue(texture!, out quad);

            if (quad == null)
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.parent = transform;
                quad.transform.localPosition = new Vector3(0, -0.1f, 0);
                quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
                quad.transform.localScale = new Vector3(worldSize, worldSize, 1f);

                var mat = new Material(MapMaterial)
                {
                    mainTexture = texture,
                };

                if (displayOptions != null)
                {
                    mat.color = displayOptions.Color;
                }
                quad.GetComponent<Renderer>().material = mat;
            }

            // Always update texture reference, handles both first call and refresh
            quad.GetComponent<Renderer>().material.mainTexture = texture;
        }

        public void Hide()
        {
            foreach (GameObject quad in quads.Values)
            {
                if (quad != null) Destroy(quad);
            }
        }
    }
}