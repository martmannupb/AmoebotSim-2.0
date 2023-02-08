using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{
    public static class RenderBatch_MatrixCalculation
    {

        /// <summary>
        /// Creates a 2D line matrix.
        /// Used with a default quad mesh that has the pivot to the left.
        /// </summary>
        /// <param name="pos1">Line start point.</param>
        /// <param name="pos2">Line end point.</param>
        /// <param name="lineWdith">Width of the line.</param>
        /// <param name="z">Z Coordinate of the line.</param>
        /// <returns></returns>
        public static Matrix4x4 Line2D(Vector2 pos1, Vector2 pos2, float lineWdith, float z = 0f)
        {
            Vector2 vec = pos2 - pos1;
            float length = vec.magnitude;
            Quaternion q;
            q = Quaternion.FromToRotation(Vector2.right, vec);
            if (q.eulerAngles.y >= 179.999) q = Quaternion.Euler(0f, 0f, 180f); // Hotfix for wrong axis rotation for 180 degrees
            return Matrix4x4.TRS(new Vector3(pos1.x, pos1.y, z), q, new Vector3(length, lineWdith, 1f));
        }

    }

}