#nullable enable

using CityGenerator.Core.Parameters;
using UnityEngine;

namespace CityGenerator.Runtime.Visualization
{

    public class MapVisualizer : MonoBehaviour
    {
        [SerializeField] private Material MapMaterial = null!;
        [SerializeField] private Color baseColor = Color.white;

        private GameObject? quad;

        public void ShowTexture(Texture2D? texture, CityParameters parameters)
        {
            if (texture == null) return;

            if (quad == null)
            {
                quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.parent = transform;
                quad.transform.localPosition = new Vector3(0, -0.1f, 0);
                quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
                quad.transform.localScale = new Vector3(
                    parameters.WorldSize,
                    parameters.WorldSize,
                    1f
                );

                var mat = new Material(MapMaterial)
                {
                    mainTexture = texture,
                    color = baseColor
                };
                quad.GetComponent<Renderer>().material = mat;
            }

            // Always update texture reference, handles both first call and refresh
            quad.GetComponent<Renderer>().material.mainTexture = texture;
        }

        public void Hide()
        {
            if (quad != null) Destroy(quad);
        }
    }

    public enum MapDisplay { None, Population, Water, LivePopulation }
}