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
        [field: Header("City Settings")]
        [field: SerializeField] public CityParameters Parameters { get; private set; } = null!;

        [field: Header("Road Visual Settings")]
        [field: SerializeField] public RoadVisualizer RoadVisualizer { get; private set; } = null!;

        [field: Header("Map Visual Settings")]
        [field: SerializeField] public MapVisualizer MapVisualizer { get; private set; } = null!;
        [field: SerializeField] public MapDisplay ShowMap { get; private set; } = MapDisplay.Population;

        [field: Header("Simulation Settings")]
        [field: Tooltip("If enabled, step through simulation instead of instantly computing")]
        [field: SerializeField] public bool StepThrough { get; private set; } = true;
        [field: SerializeField] public float StepDelay { get; private set; } = 0.02f;
        [field: SerializeField] public int StepsPerFrame { get; private set; } = 1;

        private RoadGenerator? _roadGenerator;

        void Start() => Regenerate();

        [ContextMenu("Regenerate")]
        public void Regenerate()
        {
            StopAllCoroutines();

            MapVisualizer.Show(ShowMap);

            RoadVisualizer.Clear();
            _roadGenerator = new RoadGenerator(Parameters);

            if (StepThrough)
                StartCoroutine(GenerateCoroutine());
            else
            {
                _roadGenerator.GenerateAll();
                RoadVisualizer.Refresh(_roadGenerator.Graph);
            }
        }

        private IEnumerator GenerateCoroutine()
        {
            while (!_roadGenerator!.IsComplete)
            {
                for (int i = 0; i < StepsPerFrame; i++)
                {
                    if (_roadGenerator.IsComplete) break;
                    _roadGenerator.GenerateNextSegment();
                }

                RoadVisualizer.Refresh(_roadGenerator.Graph);

                if (StepDelay > 0)
                    yield return new WaitForSeconds(StepDelay);
                else
                    yield return null;
            }
        }
    }
}