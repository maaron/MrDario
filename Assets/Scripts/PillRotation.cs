using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum PillRotation { Zero, Quarter, Half, ThreeQuarter }

public static class PillRotationExtensions
{
    public static Quaternion ToQuaternion(this PillRotation pr)
    {
        return Quaternion.AngleAxis((int)pr * 90, Vector3.back);
    }
}