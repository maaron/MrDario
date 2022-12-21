using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[ExecuteInEditMode]
public class BrokenPill : MonoBehaviour
{
    [SerializeField] VirusType virusType;
    public VirusType VirusType
    {
        get => virusType;
        set
        {
            virusType = value;
            UpdateMaterials();
        }
    }

    [SerializeField] PerVirusType<Material> Materials;
    [SerializeField] MeshRenderer ballRenderer;

    private void OnValidate()
    {
        UpdateMaterials();
    }

    void UpdateMaterials()
    {
        var m = Materials[virusType];
        if (m != null)
        {
            if (ballRenderer != null)
                ballRenderer.material = m;
        }
    }
}
