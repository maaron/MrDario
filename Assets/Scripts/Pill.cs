using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Pill : MonoBehaviour
{
    [SerializeField] PerHalf<VirusType> pillType;

    public PerHalf<VirusType> PillType
    {
        get => pillType;
        set
        {
            pillType = value;
            UpdateHalves();
        }
    }

    [SerializeField] PerHalf<HalfPill> halves;

    private void OnValidate()
    {
        UpdateHalves();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    void UpdateHalves()
    {
        halves.Left.VirusType = pillType.Left;
        halves.Right.VirusType = pillType.Right;
    }
}
