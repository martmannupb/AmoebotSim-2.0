using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    public static class CollisionChecker
    {
        public static bool showDebug = true;

        public static bool HaveSharedNode(EdgeMovement em1, EdgeMovement em2)
        {
            return em1.start1 == em2.start1 || em1.start1 == em2.end1 || em1.end1 == em2.start1 || em1.end1 == em2.end1 ||
                em1.start2 == em2.start2 || em1.start2 == em2.end2 || em1.end2 == em2.start2 || em1.end2 == em2.end2;
        }

        public static int ComputeOrientationTwoSegments(Vector2Int p1, Vector2Int p2, Vector2Int p3)
        {
            int product = (p2.y - p1.y) * (p3.x - p2.x) - (p3.y - p2.y) * (p2.x - p1.x);
            return product > 0 ? 1 : (product < 0 ? -1 : 0);
        }

        public static bool EdgesCollide(EdgeMovement em1, EdgeMovement em2, bool debug = false)
        {
            // If the two edges share a node before or after the movement,
            // then they belong to the same particle and cannot collide
            if (HaveSharedNode(em1, em2))
                return false;

            // Find the translation vectors of each edge's end points
            Vector2Int em1_tStart = em1.StartTranslation();
            Vector2Int em1_tEnd = em1.EndTranslation();
            Vector2Int em2_tStart = em2.StartTranslation();
            Vector2Int em2_tEnd = em2.EndTranslation();

            // Case 1: Both edges only have a translation movement
            if (em1_tStart == em1_tEnd && em2_tStart == em2_tEnd)
            {
                // Calculate the movement of edge 2 relative to edge 1
                Vector2Int em2_rel = em2_tStart - em1_tStart;

                // If there is no relative movement: No collision
                if (em2_rel == Vector2Int.zero)
                    return false;

                // There is relative movement
                // Check if it crosses edge 1

                if (debug || showDebug)
                {
                    Vector3 em1_start = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em1.start1);
                    Vector3 em1_end = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em1.end1);
                    Vector3 em2_start = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.start1);
                    Vector3 em2_end = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.end1);
                    Vector3 em2_moveStart = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.start1 + em2_rel);
                    Vector3 em2_moveEnd = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.end1 + em2_rel);

                    Debug.DrawLine(em1_start, em1_end, Color.red, 10f, false);
                    Debug.DrawLine(em2_start, em2_end, Color.green, 10f, false);
                    Debug.DrawLine(em2_start, em2_moveStart, Color.blue, 10f, false);
                    Debug.DrawLine(em2_end, em2_moveEnd, Color.blue, 10f, false);
                    showDebug = false;
                }
            }

            return false;
        }
    }

} // namespace AS2.Sim
