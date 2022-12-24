using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class TorusWedge : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] float majorRadius = 1;
    [SerializeField] float minorRadius = 0.25f;
    [SerializeField, Range(3, 512)] int majorSegments = 4;
    [SerializeField, Range(3, 512)] int minorSegments = 4;
    [SerializeField, Range(0, 360)] float wedgeAngleDegrees = 90;

    private Mesh mesh = null;
    private bool needsUpdate = true;

    private void Awake()
    {
        CheckMesh();
    }

    void Start()
    {
        needsUpdate = true;
    }

    private void OnValidate()
    {
        needsUpdate = true;
    }

    IEnumerable<float> Linspace(float start, float end, int count)
    {
        int safeCount = Math.Max(count, 2);

        for (int i = 0; i < safeCount; i++)
        {
            float alpha = i / (safeCount - 1);
            yield return alpha * end + (1 - alpha) * start;
        }
    }

    void Update()
    {
        // This is only needed for one case that I can tell.  If you use "revert all" in the
        // editor for a prefab instance that has a TorusWedge, it will remove the MeshFilter
        // added dynamically by this class.  This check ensures that it is recreated, in which
        // case needsUpdate will also be set.
        CheckMesh();

        if (needsUpdate)
        {
            needsUpdate = false;

            mesh.Clear();
            var indices = new List<int>();

            var vquery =
                from i in Enumerable.Range(0, majorSegments + 1)
                from j in Enumerable.Range(0, minorSegments)
                let minorAngle = j * 2 * Mathf.PI / minorSegments
                let majorAngle = i * wedgeAngleDegrees / majorSegments
                let rot = Quaternion.AngleAxis(majorAngle, new Vector3(0, 0, 1))
                let xy = new Vector3(
                    majorRadius + minorRadius * Mathf.Cos(minorAngle),
                    0,
                    minorRadius * Mathf.Sin(minorAngle))
                select rot * xy;

            var vertices = vquery.ToList();

            for (int i = 0; i < majorSegments; i++)
            {
                for (int j = 0; j < minorSegments; j++)
                {
                    var jnext = (j + 1) % minorSegments;
                    var inext = i + 1;

                    indices.Add(minorSegments * i + j);
                    indices.Add(minorSegments * inext + j);
                    indices.Add(minorSegments * inext + jnext);

                    indices.Add(minorSegments * i + j);
                    indices.Add(minorSegments * inext + jnext);
                    indices.Add(minorSegments * i + jnext);
                }
            }

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
    }

    private void CheckMesh()
    {
        // The MeshFilter must be added at runtime with HideAndDontSave in order to avoid
        // unneccessary prefab overrides of the generated mesh.  Unfortunately, setting
        // HideAndDontSave only on the mesh doesn't work.  When used within a prefab, containing
        // prefabs/scenes will generate an associated override.
        if (GetComponent<MeshFilter>() == null)
        {
            mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            var filter = GetComponent<MeshFilter>();
            if (filter == null) filter = this.AddComponent<MeshFilter>();
            filter.hideFlags = HideFlags.HideAndDontSave;
            filter.mesh = mesh;
            needsUpdate = true;
        }
    }
}
