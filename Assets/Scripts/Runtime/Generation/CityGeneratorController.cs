#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Generation;
using CityGenerator.Core.Parameters;
using CityGenerator.Core.Road;
using CityGenerator.Runtime.Visualization;
using UnityEngine;

namespace CityGenerator.Runtime
{
    public class CityGeneratorController : MonoBehaviour
    {
        [Header("City Settings")]
        [SerializeField] private CityParameters _parameters = null!;

        [Header("Road Visual Settings")]
        [SerializeField] private RoadVisualizer RoadVisualizer = null!;

        [Header("Map Visual Settings")]
        [SerializeField] private MapVisualizer _mapVisualizer = null!;
        [SerializeField] private MapType _displayMaps = MapType.Water;
        [SerializeField] private List<MapDisplayOptions> _mapDisplayOptions = new();

        private StreetExpander? _expander;
        public RoadGraph? Graph => _expander?.Graph;
        private CityParameters _parameterInstance = null!;
        public CityParameters Parameters => _parameterInstance;

        public bool Paused { get; set; }

        void Awake()
        {
            _parameterInstance = Instantiate(_parameters);
        }

        void Start()
        {
            Regenerate();
        }

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            StopAllCoroutines();

            RoadVisualizer.Clear();
            _expander = new StreetExpander(_parameterInstance);
            _expander.OnSegmentProposed += RoadVisualizer.OnSegmentProposed;

            if (_parameterInstance.StepThrough)
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
                if (Paused)
                {
                    yield return null;
                }

                else
                {
                    for (int i = 0; i < _parameterInstance.StepsPerFrame; i++)
                    {
                        if (_expander.IsComplete) break;
                        _expander.ExpandNext();
                    }

                    RoadVisualizer.Refresh(_expander.Graph);

                    if (_parameterInstance.StepDelay > 0)
                        yield return new WaitForSeconds(_parameterInstance.StepDelay);
                    else
                        yield return null;
                }
            }
        }

        private void DisplayMap(MapType mapType)
        {
            _mapVisualizer.ShowTexture(GetMapTexture(mapType), GetDisplayOptions(mapType), _parameterInstance.WorldSize);
        }

        private Texture2D? GetMapTexture(MapType mapType)
        {
            Texture2D? texture = mapType switch
            {
                MapType.Population => _parameterInstance.PopulationMap,
                MapType.Water => _parameterInstance.WaterMask,
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