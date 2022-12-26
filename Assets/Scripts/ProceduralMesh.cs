using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public abstract class ProceduralMesh : MonoBehaviour
{
    private Mesh mesh = null;
    private bool needsUpdate = true;

    public abstract (Vector3[], int[]) GenerateMesh();

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

            (var vertices, var indices) = GenerateMesh();

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
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;
        }

        if (GetComponent<MeshFilter>() == null)
        {
            var filter = this.AddComponent<MeshFilter>();
            filter.hideFlags = HideFlags.HideAndDontSave;
            filter.mesh = mesh;
            needsUpdate = true;
        }
    }
}
