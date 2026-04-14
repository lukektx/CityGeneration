#nullable enable

using CityGenerator.Core.Parameters;
using UnityEngine;

namespace CityGenerator.Runtime.Visualization
{

    public class MapVisualizer : MonoBehaviour
    {
        [field: SerializeField] public CityParameters Parameters { get; private set; } = null!;
        [field: SerializeField] public Material MapMaterial { get; private set; } = null!;

        private GameObject? quad;

        public void Show(MapDisplay display)
        {
            if (quad != null) Destroy(quad);

            Texture2D? texture = display switch
            {
                MapDisplay.Population => Parameters.PopulationMap,
                MapDisplay.Water => Parameters.WaterMask,
                _ => null
            };

            if (texture == null) return;

            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.parent = transform;
            quad.transform.localPosition = new Vector3(0, -0.1f, 0);
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = new Vector3(
                Parameters.WorldSize,
                Parameters.WorldSize,
                1f
            );

            var mat = new Material(MapMaterial);
            mat.mainTexture = texture;
            quad.GetComponent<Renderer>().material = mat;
        }

        public void Hide()
        {
            if (quad != null) Destroy(quad);
        }
    }

    public enum MapDisplay { None, Population, Water }
}