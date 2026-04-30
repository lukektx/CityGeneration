#nullable enable

using System.Collections.Generic;
using System.Linq;
using CityGenerator.Core.Road;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace CityGenerator.Runtime.Visualization
{
    public class RoadVisualizer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Material HighwayMaterial = null!;
        [SerializeField] private Material StreetMaterial = null!;
        [SerializeField] private Color _proposedSegmentSuccessColor = Color.green;
        [SerializeField] private Color _proposedSegmentFailureColor = Color.red;

        [SerializeField] private float HighwayWidth = 1.5f;
        [SerializeField] private float StreetWidth = 0.5f;

        [Header("Spline Settings")]
        [SerializeField] private bool splineMode = false;
        [SerializeField] private int highwaySplineSamples = 10;
        [SerializeField] private int streetSplineSamples = 4;

        private MeshFilter? highwayFilter;
        private MeshFilter? streetFilter;
        private LineRenderer? _proposedSegmentRenderer;
        private Material? _proposedMinorSegmentMaterial;
        private Material? _proposedMajorSegmentMaterial;

        private const float SPLINE_TANGENT_EPSILON = 0.0001f;

        private void Awake()
        {
            highwayFilter = CreateMeshObject("Highways", HighwayMaterial);
            streetFilter = CreateMeshObject("Streets", StreetMaterial, -0.1f);

            InitProposedSegment();
        }

        private MeshFilter CreateMeshObject(string name, Material material, float? height = null)
        {
            var go = new GameObject(name);
            go.transform.parent = transform;
            if (height.HasValue)
            {
                go.transform.position = new Vector3(0, height.Value, 0);
            }
            go.AddComponent<MeshRenderer>().material = material;
            return go.AddComponent<MeshFilter>();
        }

        public void Refresh(RoadGraph graph)
        {
            BuildMesh(
                highwayFilter!,
                graph.Edges.Where(e => e.Type == RoadType.Major),
                HighwayWidth,
                highwaySplineSamples
            );

            BuildMesh(
                streetFilter!,
                graph.Edges.Where(e => e.Type == RoadType.Minor),
                StreetWidth,
                streetSplineSamples
            );
        }

        public void Clear()
        {
            if (highwayFilter != null) highwayFilter.mesh = new Mesh();
            if (streetFilter != null) streetFilter.mesh = new Mesh();
        }

        public void OnSegmentProposed(RoadSegment segment, bool accepted)
        {
            if (_proposedSegmentRenderer == null) return;

            Material? material = segment.Type == RoadType.Major
                ? _proposedMajorSegmentMaterial
                : _proposedMinorSegmentMaterial;
            if (material == null) return;

            material.color = accepted ? _proposedSegmentSuccessColor : _proposedSegmentFailureColor
            ;
            _proposedSegmentRenderer.material = material;
            float width = segment.Type == RoadType.Major ? HighwayWidth : StreetWidth;
            _proposedSegmentRenderer.startWidth = width;
            _proposedSegmentRenderer.endWidth = width;

            _proposedSegmentRenderer.SetPosition(0, new Vector3(segment.Start.x, 0, segment.Start.y));
            _proposedSegmentRenderer.SetPosition(1, new Vector3(segment.End.x, 0, segment.End.y));
            _proposedSegmentRenderer.enabled = true;
        }

        private void InitProposedSegment()
        {
            var go = new GameObject("ProposedSegment");
            go.transform.SetParent(transform);
            _proposedSegmentRenderer = go.AddComponent<LineRenderer>();
            _proposedSegmentRenderer.positionCount = 2;
            _proposedSegmentRenderer.enabled = false;

            _proposedMajorSegmentMaterial = new Material(HighwayMaterial);
            _proposedMinorSegmentMaterial = new Material(StreetMaterial);
        }

        private void BuildMesh
        (
            MeshFilter filter,
            IEnumerable<RoadEdge> edges,
            float width,
            int samples
        )
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();

            foreach (var edge in edges)
            {
                // more samples = smoother curve, straight lines only need 2

                for (int i = 0; i < samples - 1; i++)
                {
                    Vector3 a, b, perp;

                    if (splineMode)
                    {
                        float t0 = i / (float)(samples - 1);
                        float t1 = (i + 1) / (float)(samples - 1);

                        SplineUtility.Evaluate(edge.Spline, t0,
                            out float3 pos0, out float3 tan0, out float3 _);
                        SplineUtility.Evaluate(edge.Spline, t1,
                            out float3 pos1, out float3 tan1, out float3 _);

                        Debug.Log($"Edge: from={pos0} to={pos1} tan={tan0} lengthsq={math.lengthsq(tan0)}");

                        // Skip degenerate segments with zero tangent
                        if (math.lengthsq(tan0) < SPLINE_TANGENT_EPSILON) continue;

                        a = pos0;
                        b = pos1;
                        Vector3 dir = math.normalize(tan0);
                        perp = new Vector3(-dir.z, 0, dir.x) * (width * 0.5f);
                    }
                    else
                    {
                        a = new Vector3(edge.A.Position.x, 0, edge.A.Position.y);
                        b = new Vector3(edge.B.Position.x, 0, edge.B.Position.y);
                        Vector3 dir = (b - a).normalized;
                        if (dir == Vector3.zero) continue;
                        perp = new Vector3(-dir.z, 0, dir.x) * (width * 0.5f);
                    }

                    int baseIdx = verts.Count;
                    verts.Add(a - perp);
                    verts.Add(a + perp);
                    verts.Add(b + perp);
                    verts.Add(b - perp);

                    tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
                    tris.Add(baseIdx); tris.Add(baseIdx + 2); tris.Add(baseIdx + 3);
                }
            }

            // BuildJunctionCaps(verts, tris, graph, width);

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            // mesh.RecalculateNormals();
            filter.mesh = mesh;
        }
    }
}