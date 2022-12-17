using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Virus : MonoBehaviour
{
    [SerializeField] VirusType virusType;
    [SerializeField] PerVirusType<GameObject> viruses;

    public VirusType VirusType
    {
        get => virusType;
        set
        {
            virusType = value;
            UpdateVirusType();
        }
    }

    private void OnValidate()
    {
        UpdateVirusType();
    }

    void UpdateVirusType()
    {
        viruses.Visit((t, v) => v.gameObject.SetActive(t == virusType));
    }
}
