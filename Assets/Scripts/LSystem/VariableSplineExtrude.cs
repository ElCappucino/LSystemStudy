using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VariableSplineExtrude : MonoBehaviour
{
    public SplineContainer splineContainer;

    [Header("Extrude Settings")]
    public int sides = 8;
    public int segmentsPerUnit = 8;

    [Header("Radius Per Knot")]
    public Dictionary<Spline, List<float>> splineRadii = new Dictionary<Spline, List<float>>();

    Mesh mesh;

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "VariableSplineExtrude";
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        if (splineContainer == null)
            return;

        GenerateMesh();
    }

    List<float> GetSplineRadii(Spline spline)
    {
        if (!splineRadii.ContainsKey(spline))
        {
            splineRadii[spline] = new List<float>();
        }

        return splineRadii[spline];
    }

    void SyncRadiusCount(Spline spline)
    {
        var radii = GetSplineRadii(spline);

        while (radii.Count < spline.Count)
            radii.Add(0.1f);

        while (radii.Count > spline.Count)
            radii.RemoveAt(radii.Count - 1);
    }

    float GetRadius(float t, Spline spline)
    {
        var radii = GetSplineRadii(spline);

        int knotCount = spline.Count;

        float scaled = t * (knotCount - 1);
        int i0 = Mathf.FloorToInt(scaled);
        int i1 = Mathf.Clamp(i0 + 1, 0, knotCount - 1);

        float localT = scaled - i0;

        return Mathf.Lerp(radii[i0], radii[i1], localT);
    }

    void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexOffset = 0;

        foreach (var spline in splineContainer.Splines)
        {
            if (spline.Count < 2)
                continue;

            SyncRadiusCount(spline);

            float length = spline.GetLength();
            int segments = Mathf.Max(2, Mathf.RoundToInt(length * segmentsPerUnit));

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;

                spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);

                Vector3 position = pos;
                Vector3 forward = math.normalize((Vector3)tangent);
                Vector3 right = Vector3.Cross(up, forward).normalized;
                Vector3 realUp = Vector3.Cross(forward, right);

                float radius = GetRadius(t, spline);

                for (int j = 0; j < sides; j++)
                {
                    float angle = j * Mathf.PI * 2f / sides;

                    Vector3 offset =
                        Mathf.Cos(angle) * right * radius +
                        Mathf.Sin(angle) * realUp * radius;

                    vertices.Add(position + offset);
                }
            }

            for (int i = 0; i < segments; i++)
            {
                int ringStart = vertexOffset + i * sides;
                int nextRing = vertexOffset + (i + 1) * sides;

                for (int j = 0; j < sides; j++)
                {
                    int nextSide = (j + 1) % sides;

                    int a = ringStart + j;
                    int b = ringStart + nextSide;
                    int c = nextRing + j;
                    int d = nextRing + nextSide;

                    // ONLY add the outer faces. 
                    // This stops the flickering and the "shining" normal bug.
                    AddQuad(triangles, a, b, c, d, false);
                }
            }

            vertexOffset += (segments + 1) * sides;
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
    }

    // Helper to handle double-sided quads
    void AddQuad(List<int> tris, int a, int b, int c, int d, bool reverse)
    {
        if (reverse)
        {
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(b); tris.Add(c); tris.Add(d);
        }
        else
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(b); tris.Add(d); tris.Add(c);
        }
    }
}