#nullable enable
using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Road;

namespace CityGenerator.Core.Traffic
{
    public class TrafficSimulator
    {
        private readonly NavigationGraph _navigationGraph;
        private readonly TrafficParameters _parameters;
        private readonly Dictionary<RoadNode, IntersectionQueue> _queues = new();
        private readonly Dictionary<HalfEdge, List<TrafficAgent>> _halfEdgeOccupancy = new();
        private readonly List<TrafficAgent> _agents = new();

        public IReadOnlyList<TrafficAgent> Agents => _agents;

        public IReadOnlyList<TrafficAgent> GetAgentsOnHalfEdge(HalfEdge he)
        {
            _halfEdgeOccupancy.TryGetValue(he, out var list);
            return list ?? (IReadOnlyList<TrafficAgent>)System.Array.Empty<TrafficAgent>();
        }

        public TrafficSimulator(RoadGraph graph, NavigationGraph navigationGraph, TrafficParameters parameters)
        {
            _navigationGraph = navigationGraph;
            _parameters = parameters;

            // Pre-build queues for all nodes
            foreach (var node in graph.Nodes)
            {
                _queues[node] = new IntersectionQueue(node, _parameters);
            }
        }

        /// <summary>
        /// Spawns a trip between two random nodes. Returns null if no path exists.
        /// </summary>
        public TrafficAgent? SpawnTrip(RoadNode start, RoadNode end)
        {
            var path = _navigationGraph.FindPath(start, end);
            if (path == null || path.Count < 2) return null;

            var trip = new Trip(start, end, path);
            var agent = new TrafficAgent(trip, _queues, _parameters)
            {
                WorldPosition = start.Position
            };
            _agents.Add(agent);
            return agent;
        }

        public void Tick(float dt)
        {
            // Tick intersection queues first so agents cleared this frame can move
            foreach (var q in _queues.Values)
            {
                q.Tick(dt);
            }

            UpdateOccupancy();

            // Tick agents
            foreach (var agent in _agents)
            {
                agent.Tick(dt, this);
            }

            // Remove finished agents
            _agents.RemoveAll(a => a.State == AgentState.Finished);
        }

        public void Clear() => _agents.Clear();

        private void UpdateOccupancy()
        {
            foreach (var list in _halfEdgeOccupancy.Values) list.Clear();

            foreach (var agent in _agents)
            {
                if (agent.State == AgentState.Finished) continue;
                var he = agent.CurrentHalfEdge;

                if (he == null) continue;

                if (!_halfEdgeOccupancy.TryGetValue(he, out var list))
                {
                    list = new List<TrafficAgent>();
                    _halfEdgeOccupancy[he] = list;
                }
                list.Add(agent);
            }
        }
    }
}