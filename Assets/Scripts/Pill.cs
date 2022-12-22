using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Pill : MonoBehaviour
{
    [SerializeField] PillRotation pillRotation;
    [SerializeField] PerHalf<HalfPill> halves;

    public HalfPill Left => halves.Left;
    public HalfPill Right => halves.Right;

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
        UpdateRotation();
    }

    void UpdateRotation()
    {
        transform.rotation = pillRotation.ToQuaternion();
    }
}
