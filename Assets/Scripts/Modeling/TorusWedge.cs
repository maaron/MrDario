using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Modeling
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    public class TorusWedge : ProceduralMesh
    {
        [SerializeField] float majorRadius = 1;
        [SerializeField] float minorRadius = 0.25f;
        [SerializeField, Range(3, 512)] int majorSegments = 4;
        [SerializeField, Range(3, 512)] int minorSegments = 4;
        [SerializeField, Range(0, 360)] float wedgeAngleDegrees = 90;

        public override (Vector3[], int[]) GenerateMesh()
        {
            return Modeling.Surface.TriangulateHorizontalCylinder(
                new Vector2Int(majorSegments, minorSegments),
                uv =>
                {
                    var majorAngleDeg = uv.x * wedgeAngleDegrees;
                    var minorAngle = uv.y * 2 * Mathf.PI;
                    var rot = Quaternion.AngleAxis(majorAngleDeg, new Vector3(0, 0, 1));
                    return rot * new Vector3(
                        majorRadius + minorRadius * Mathf.Cos(minorAngle),
                        0,
                        minorRadius * Mathf.Sin(minorAngle));
                });
        }
    }
}