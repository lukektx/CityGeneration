#nullable enable

using System.Collections;
using CityGenerator.Core.Generation;
using CityGenerator.Core.Parameters;
using CityGenerator.Runtime.Visualization;
using UnityEngine;

namespace CityGenerator.Runtime
{
    public class CityGeneratorController : MonoBehaviour
    {
        [Header("City Settings")]
        [SerializeField] private CityParameters Parameters = null!;

        [Header("Road Visual Settings")]
        [SerializeField] private RoadVisualizer RoadVisualizer = null!;

        [Header("Map Visual Settings")]
        [SerializeField] private MapVisualizer _mapVisualizer = null!;
        [SerializeField] private MapDisplay _displayMap = MapDisplay.Population;

        [Header("Simulation Settings")]
        [Tooltip("If enabled, step through simulation instead of instantly computing")]
        [SerializeField] private bool _stepThrough = true;
        [SerializeField] private float _stepDelay = 0.02f;
        [SerializeField] private int _stepsPerFrame = 1;

        private StreetExpander? _expander;

        void Start() => Regenerate();

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            StopAllCoroutines();

            RoadVisualizer.Clear();
            _expander = new StreetExpander(Parameters);

            if (_stepThrough)
            {
                StartCoroutine(GenerateCoroutine());
            }
            else
            {
                _expander.ExpandAll();
                RoadVisualizer.Refresh(_expander.Graph);
            }

            _mapVisualizer.ShowTexture(GetMapTexture(), Parameters);
        }

        private IEnumerator GenerateCoroutine()
        {
            while (!_expander!.IsComplete)
            {
                for (int i = 0; i < _stepsPerFrame; i++)
                {
                    if (_expander.IsComplete) break;
                    _expander.ExpandNext();
                }

                RoadVisualizer.Refresh(_expander.Graph);

                if (_stepDelay > 0)
                    yield return new WaitForSeconds(_stepDelay);
                else
                    yield return null;
            }
        }

        private Texture2D? GetMapTexture()
        {
            Texture2D? texture = _displayMap switch
            {
                MapDisplay.Population => Parameters.PopulationMap,
                MapDisplay.Water => Parameters.WaterMask,
                // Disabling for now as it doesn't seem to be needed in simulation method
                //MapDisplay.LivePopulation => _roadGenerator?.RuntimePopulationMap.Texture,
                _ => null
            };

            if (texture == null)
            {
                Debug.LogWarning($"Map texture for type {_displayMap} is null");
            }
            //Debug.Log($"Showing map texture {texture?.name}");

            return texture;
        }
    }
}