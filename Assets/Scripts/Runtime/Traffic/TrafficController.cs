#nullable enable
using System.Collections;
using System.Collections.Generic;
using CityGenerator.Core.Road;
using CityGenerator.Core.Traffic;
using UnityEngine;

namespace CityGenerator.Runtime
{
    public enum CityMode { Generation, Simulation }

    public class TrafficController : MonoBehaviour
    {
        [SerializeField] private CityGeneratorController _cityController = null!;
        [SerializeField] private GameObject _carPrefab = null!;
        [SerializeField] private ParticleSystem _arrivalParticlePrefab = null!;

        [Header("Simulation settings")]
        [SerializeField] private TrafficParameters _parameters = null!;

        public CityMode Mode { get; private set; } = CityMode.Generation;

        private TrafficSimulator? _simulator;
        private NavigationGraph? _navigationGraph;
        private readonly Dictionary<TrafficAgent, Transform> _agentVisuals = new();
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public bool Paused { get; set; }


        public void EnterSimulation()
        {
            if (Mode == CityMode.Simulation) return;
            Mode = CityMode.Simulation;
            _cityController.Paused = true;
            Paused = false;

            var graph = _cityController.Graph;
            if (graph == null)
            {
                Debug.LogWarning("No graph to simulate on.");
                return;
            }

            _navigationGraph = new NavigationGraph(graph);
            _simulator = new TrafficSimulator(graph, _navigationGraph, _parameters);

            StartCoroutine(SpawnLoop(graph));
        }

        public void EnterGeneration()
        {
            if (Mode == CityMode.Generation) return;
            Mode = CityMode.Generation;
            _cityController.Paused = false;
            Paused = true;

            StopAllCoroutines();
            ClearVisuals();
            _simulator?.Clear();
            _simulator = null;
        }


        private IEnumerator SpawnLoop(RoadGraph graph)
        {
            var nodes = graph.Nodes;
            if (nodes.Count < 2) yield break;

            while (Mode == CityMode.Simulation && !Paused)
            {
                if (_simulator!.Agents.Count < _parameters.TripsToSpawn)
                {
                    var (a, b) = PickDistinctNodes(nodes);
                    var agent = _simulator.SpawnTrip(a, b);
                    if (agent != null) CreateVisual(agent);
                }
                yield return new WaitForSeconds(_parameters.RespawnDelay);
            }
        }

        private static (RoadNode, RoadNode) PickDistinctNodes(IReadOnlyList<RoadNode> nodes)
        {
            var a = nodes[Random.Range(0, nodes.Count)];
            RoadNode b;
            do
            {
                b = nodes[Random.Range(0, nodes.Count)];
            } while (b == a);

            return (a, b);
        }


        private void Update()
        {
            if (_simulator == null || Mode != CityMode.Simulation || Paused) return;

            _simulator.Tick(Time.deltaTime);
            SyncVisuals();
        }

        private void CreateVisual(TrafficAgent agent)
        {
            // Convert 2D city position to 3D world pos
            var go = Instantiate(_carPrefab, ToWorld(agent.WorldPosition), Quaternion.identity);

            // Change color of the car randomly
            if (_parameters.CarColors.Length > 0)
            {
                var renderer = go.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var color = _parameters.CarColors[Random.Range(0, _parameters.CarColors.Length)];
                    var mpb = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(mpb, _parameters.CarBodyMaterialIndex);
                    mpb.SetColor(BaseColor, color);
                    renderer.SetPropertyBlock(mpb, _parameters.CarBodyMaterialIndex);
                }
            }

            _agentVisuals[agent] = go.transform;
        }

        private void SyncVisuals()
        {
            var finished = new List<TrafficAgent>();

            foreach (var (agent, t) in _agentVisuals)
            {
                if (agent.State == AgentState.Finished)
                {
                    finished.Add(agent);
                    continue;
                }
                t.position = ToWorld(agent.WorldPosition);

                // Face direction of travel
                var dir3 = new Vector3(agent.TravelDirection.x, 0f, agent.TravelDirection.y);
                if (dir3.sqrMagnitude > 0.001f)
                {
                    t.rotation = Quaternion.LookRotation(dir3, Vector3.up);
                }
            }

            foreach (var a in finished)
            {
                if (_agentVisuals.TryGetValue(a, out var t))
                {
                    SpawnArrivalParticles(t.position);
                    Destroy(t.gameObject);
                }

                _agentVisuals.Remove(a);
            }
        }

        private void SpawnArrivalParticles(Vector3 position)
        {
            var burst = Instantiate(_arrivalParticlePrefab, position, Quaternion.identity);
            burst.Play();
        }

        private void ClearVisuals()
        {
            foreach (var t in _agentVisuals.Values)
                if (t != null) Destroy(t.gameObject);
            _agentVisuals.Clear();
        }

        private static Vector3 ToWorld(Vector2 p) => new Vector3(p.x, 0f, p.y);
    }
}