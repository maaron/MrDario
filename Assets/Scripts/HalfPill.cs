using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[ExecuteInEditMode]
public class HalfPill : MonoBehaviour
{
    [SerializeField] VirusType virusType;
    [SerializeField] bool isDead;
    [SerializeField] PerVirusType<Material> Materials;
    [SerializeField] MeshRenderer ballRenderer, cylinderRenderer;
    [SerializeField] LineRenderer deadRenderer;

    public VirusType VirusType
    {
        get => virusType;
        set
        {
            virusType = value;
            UpdateMaterials();
        }
    }

    public bool IsDead
    {
        get => isDead;
        set
        {
            isDead = value;
            UpdateDeadness();
        }
    }
    
    private void OnValidate()
    {
        UpdateMaterials();
        UpdateDeadness();
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

            if (deadRenderer != null)
                deadRenderer.material = m;
        }
    }

    void UpdateDeadness()
    {
        ballRenderer.gameObject.SetActive(!isDead);
        cylinderRenderer.gameObject.SetActive(!isDead);
        deadRenderer.gameObject.SetActive(isDead);
    }
}
