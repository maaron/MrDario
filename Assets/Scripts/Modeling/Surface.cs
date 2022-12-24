using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Modeling
{
    public static class Surface
    {
        public static T[] MapQuadYMajor<T>(Vector2Int segments, Func<Vector2, T> f)
        {
            int nx = segments.x + 1;
            int ny = segments.y + 1;
            var vertices = new T[nx * ny];

            float sx = 1.0f / segments.x;
            float sy = 1.0f / segments.y;
            int k = 0;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    vertices[k++] = f(new Vector2(i * sx, j * sy));
                }
            }

            return vertices;
        }

        public static int[] QuadTrianglesClockwise(Vector2Int segments)
        {
            int k = 0;
            int ny = segments.y + 1;
            var indices = new int[6 * segments.x * segments.y];

            for (int i = 0; i < segments.x; i++)
            {
                for (int j = 0; j < segments.y; j++)
                {
                    int inext = i + 1;
                    int jnext = j + 1;

                    indices[k++] = ny * i + j;
                    indices[k++] = ny * inext + j;
                    indices[k++] = ny * inext + jnext;
                    indices[k++] = ny * i + j;
                    indices[k++] = ny * inext + jnext;
                    indices[k++] = ny * i + jnext;
                }
            }

            return indices;
        }

        public static T[] MapVerticalCylinderYMajor<T>(Vector2Int segments, Func<Vector2, T> f)
        {
            int nx = segments.x;
            int ny = segments.y + 1;
            var vertices = new T[nx * ny];

            float sx = 1.0f / segments.x;
            float sy = 1.0f / segments.y;
            int k = 0;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    vertices[k++] = f(new Vector2(i * sx, j * sy));
                }
            }

            return vertices;
        }

        public static int[] VerticalCylinderTrianglesClockwise(Vector2Int segments)
        {
            int k = 0;
            int ny = segments.y + 1;
            var indices = new int[6 * segments.x * segments.y];

            for (int i = 0; i < segments.x; i++)
            {
                for (int j = 0; j < segments.y; j++)
                {
                    int inext = (i + 1) % segments.x;
                    int jnext = j + 1;

                    indices[k++] = ny * i + j;
                    indices[k++] = ny * inext + j;
                    indices[k++] = ny * inext + jnext;
                    indices[k++] = ny * i + j;
                    indices[k++] = ny * inext + jnext;
                    indices[k++] = ny * i + jnext;
                }
            }

            return indices;
        }

        public static T[] MapHorizontalCylinderYMajor<T>(Vector2Int segments, Func<Vector2, T> f)
        {
            int nx = segments.x + 1;
            int ny = segments.y;
            var vertices = new T[nx * ny];

            float sx = 1.0f / segments.x;
            float sy = 1.0f / segments.y;
            int k = 0;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    vertices[k++] = f(new Vector2(i * sx, j * sy));
                }
            }

            return vertices;
        }

        public static int[] HorizontalCylinderTrianglesClockwise(Vector2Int segments)
        {
            int k = 0;
            int ny = segments.y;
            var indices = new int[6 * segments.x * segments.y];

            for (int i = 0; i < segments.x; i++)
            {
                for (int j = 0; j < segments.y; j++)
                {
                    int inext = i + 1;
                    int jnext = (j + 1) % ny;

                    indices[k++] = ny * i + j;
                    indices[k++] = ny * inext + j;
                    indices[k++] = ny * inext + jnext;
                    indices[k++] = ny * i + j;
                    indices[k++] = ny * inext + jnext;
                    indices[k++] = ny * i + jnext;
                }
            }

            return indices;
        }

        public static T[] MapHorizontalTorusYMajor<T>(Vector2Int segments, Func<Vector2, T> f)
        {
            int nx = segments.x;
            int ny = segments.y;
            var vertices = new T[nx * ny];

            float sx = 1.0f / segments.x;
            float sy = 1.0f / segments.y;
            int k = 0;

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    vertices[k++] = f(new Vector2(i * sx, j * sy));
                }
            }

            return vertices;
        }

        public static int[] HorizontalTorusTrianglesClockwise(Vector2Int segments)
        {
            int k = 0;
            var indices = new int[6 * segments.x * segments.y];

            for (int i = 0; i < segments.x; i++)
            {
                for (int j = 0; j < segments.y; j++)
                {
                    int inext = (i + 1) % segments.x;
                    int jnext = (j + 1) % segments.y;

                    indices[k++] = segments.y * i + j;
                    indices[k++] = segments.y * inext + j;
                    indices[k++] = segments.y * inext + jnext;
                    indices[k++] = segments.y * i + j;
                    indices[k++] = segments.y * inext + jnext;
                    indices[k++] = segments.y * i + jnext;
                }
            }

            return indices;
        }

        // Lots of different variants of a plane embedding in 3D space:
        //  - quad: A grid
        //  - vertical/horizontal cylinder: quad with top/bottom or left/right sides joined together
        //  - l/r/u/d cone: cylinder with one end joined
        //  - h/v capsule: cylinder with both ends joined
        //  - santa bag: quad with perimeter joined
        //  - v/h torus: cylinder with ends joined
        // In general, can consider joining edges of the quad in different ways.

        public static (Vector3[], int[]) Triangulate(Vector2Int segments, Func<Vector2, Vector3> embed) =>
            (
                MapQuadYMajor<Vector3>(segments, embed), 
                QuadTrianglesClockwise(segments)
            );

        public static (Vector3[], int[]) TriangulateVerticalCylinder(Vector2Int segments, Func<Vector2, Vector3> embed) =>
            (
                MapVerticalCylinderYMajor<Vector3>(segments, embed),
                VerticalCylinderTrianglesClockwise(segments)
            );

        public static (Vector3[], int[]) TriangulateHorizontalCylinder(Vector2Int segments, Func<Vector2, Vector3> embed) =>
            (
                MapHorizontalCylinderYMajor<Vector3>(segments, embed),
                HorizontalCylinderTrianglesClockwise(segments)
            );

        public static (Vector3[], int[]) TriangulateHorizontalTorus(Vector2Int segments, Func<Vector2, Vector3> embed) =>
            (
                MapHorizontalTorusYMajor(segments, embed),
                HorizontalTorusTrianglesClockwise(segments)
            );
    }
}
