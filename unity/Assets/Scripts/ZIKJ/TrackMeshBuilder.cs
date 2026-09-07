using System.Collections.Generic;
using UnityEngine;

// Generates the track floor mesh at runtime from WaypointCircuit geometry.
// Attach to an empty GameObject — it will add MeshFilter + MeshRenderer automatically.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TrackMeshBuilder : MonoBehaviour
{
    [Header("Visual")]
    public Material trackMaterial;    // drag a dark material here (e.g. Standard, dark grey)
    public int segments = 160;        // higher = smoother curves

    void Start() => Build();

    public void Build()
    {
        const float HW = WaypointCircuit.HalfWidth;

        var verts = new List<Vector3>();
        var tris  = new List<int>();
        var uvs   = new List<Vector2>();

        for (int i = 0; i <= segments; i++)
        {
            float t0   = (float)i       / segments;
            float tAdv = (float)(i + 1) / segments;

            Vector3 curr = WaypointCircuit.At(t0);
            Vector3 next = WaypointCircuit.At(tAdv);

            // Perpendicular in XZ plane (right-hand normal)
            Vector3 fwd   = new Vector3(next.x - curr.x, 0f, next.z - curr.z).normalized;
            Vector3 right = new Vector3(-fwd.z, 0f, fwd.x);

            verts.Add(curr - right * HW);           // inner edge
            verts.Add(curr + right * HW);           // outer edge
            uvs.Add(new Vector2(0f, (float)i / segments));
            uvs.Add(new Vector2(1f, (float)i / segments));
        }

        for (int i = 0; i < segments; i++)
        {
            int b = i * 2;
            tris.Add(b);     tris.Add(b + 2); tris.Add(b + 1);
            tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
        }

        var mesh = new Mesh { name = "Track" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        if (trackMaterial != null)
            GetComponent<MeshRenderer>().material = trackMaterial;
    }
}
