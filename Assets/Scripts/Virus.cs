using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Virus : MonoBehaviour
{
    [SerializeField] VirusType virusType;
    [SerializeField] PerVirusType<GameObject> viruses;
    [SerializeField] LineRenderer deadRenderer;
    [SerializeField] PerVirusType<Material> Materials;
    [SerializeField] bool isDead;

    public VirusType VirusType
    {
        get => virusType;
        set
        {
            virusType = value;
            UpdateVirusType();
        }
    }

    public bool IsDead
    {
        get => isDead;
        set
        {
            isDead = value;
            UpdateVirusType();
        }
    }

    private void OnValidate()
    {
        UpdateVirusType();
    }

    void UpdateVirusType()
    {
        viruses.Visit((t, v) => v.gameObject.SetActive(t == virusType && isDead == false));
        deadRenderer.gameObject.SetActive(isDead);
        deadRenderer.material = Materials[virusType];
    }
}
