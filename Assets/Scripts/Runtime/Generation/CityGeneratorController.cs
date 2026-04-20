#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private MapType _displayMaps = MapType.Background;
        [SerializeField] private List<MapDisplayOptions> _mapDisplayOptions = new();

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
            _expander.OnSegmentProposed += RoadVisualizer.OnSegmentProposed;

            if (_stepThrough)
            {
                StartCoroutine(GenerateCoroutine());
            }
            else
            {
                _expander.ExpandAll();
                RoadVisualizer.Refresh(_expander.Graph);
            }

            var mapTypes = Enum.GetValues(typeof(MapType)).Cast<MapType>().ToList();
            mapTypes.OrderBy(type => _mapDisplayOptions.Find(opt => opt.Type == type).priority);
            foreach (MapType type in mapTypes)
            {
                if (_displayMaps.HasFlag(type))
                {
                    DisplayMap(type);
                }
            }
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

        private void DisplayMap(MapType mapType)
        {
            _mapVisualizer.ShowTexture(GetMapTexture(mapType), GetDisplayOptions(mapType), Parameters.WorldSize);
        }

        private Texture2D? GetMapTexture(MapType mapType)
        {
            Texture2D? texture = mapType switch
            {
                MapType.Population => Parameters.PopulationMap,
                MapType.Water => Parameters.WaterMask,
                MapType.Background => Texture2D.whiteTexture,
                // Disabling for now as it doesn't seem to be needed in simulation method
                //MapDisplay.LivePopulation => _roadGenerator?.RuntimePopulationMap.Texture,
                _ => null
            };

            if (texture == null)
            {
                Debug.LogWarning($"Map texture for type {mapType} is null");
            }
            //Debug.Log($"Showing map texture {texture?.name}");

            return texture;
        }

        private MapDisplayOptions? GetDisplayOptions(MapType mapType)
        {
            return _mapDisplayOptions.Find(pair => pair.Type == mapType);
        }
    }
}