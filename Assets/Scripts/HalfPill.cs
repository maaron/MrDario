using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[ExecuteInEditMode]
public class HalfPill : MonoBehaviour
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
    [SerializeField] MeshRenderer ballRenderer, cylinderRenderer;

    private void OnValidate()
    {
        UpdateMaterials();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    void UpdateMaterials()
    {
        var m = Materials[virusType];
        if (m != null)
        {
            if (ballRenderer != null)
                ballRenderer.material = m;

            if (cylinderRenderer != null)
                cylinderRenderer.material = m;
        }
    }
}
