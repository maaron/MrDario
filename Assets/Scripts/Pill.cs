using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Pill : MonoBehaviour
{
    [SerializeField] PerHalf<VirusType> pillType;
    [SerializeField] PillRotation pillRotation;
    [SerializeField] PerHalf<HalfPill> halves;

    public PerHalf<VirusType> PillType
    {
        get => pillType;
        set
        {
            pillType = value;
            UpdateHalves();
        }
    }

    public PillRotation PillRotation
    {
        get => pillRotation;
        set
        {
            pillRotation = value;
            UpdateRotation();
        }
    }

    private void OnValidate()
    {
        UpdateHalves();
        UpdateRotation();
    }

    void UpdateHalves()
    {
        halves.Left.VirusType = pillType.Left;
        halves.Right.VirusType = pillType.Right;
    }

    void UpdateRotation()
    {
        transform.rotation = pillRotation.ToQuaternion();
    }
}
