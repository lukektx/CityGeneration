#nullable enable
using System.Collections.Generic;
using CityGenerator.Core.Road;

namespace CityGenerator.Core.Traffic
{
    public class IntersectionQueue
    {
        public RoadNode Node { get; }

        private readonly Queue<TrafficAgent> _waiting = new();
        private readonly TrafficParameters _parameters = null!;
        private TrafficAgent? _active; // currently crossing
        private float _timer;
        private bool _serving;

        public IntersectionQueue(RoadNode node, TrafficParameters parameters)
        {
            Node = node;
            _parameters = parameters;
        }

        public bool IsIntersection => Node.OutgoingEdges.Count >= _parameters.IntersectionValence;

        public void Enqueue(TrafficAgent agent) => _waiting.Enqueue(agent);

        public void Tick(float dt)
        {
            if (!IsIntersection) return;

            // Release active agent after interval
            if (_serving)
            {
                if (_active == null || _active.State == AgentState.Finished)
                {
                    _active = null;
                    _serving = false;
                    _timer = 0f;
                }
                else
                {
                    _timer += dt;
                    if (_timer >= _parameters.ReleaseInterval)
                    {
                        _active.ClearIntersection();
                        _active = null;
                        _serving = false;
                        _timer = 0f;
                    }
                }
            }

            // Pick next if slot is free
            if (!_serving && _waiting.Count > 0)
            {
                // Skip any agents that already finished while waiting
                while (_waiting.Count > 0)
                {
                    var candidate = _waiting.Dequeue();
                    if (candidate.State != AgentState.Finished)
                    {
                        _active = candidate;
                        _serving = true;
                        _timer = 0f;
                        break;
                    }
                }
            }
        }
    }
}