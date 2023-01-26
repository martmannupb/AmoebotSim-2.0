using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    public static class CollisionChecker
    {
        private static readonly float debugDisplayTime = 20.0f;

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

        public static bool SegmentsIntersect1D(float p1, float q1, float p2, float q2)
        {
            float min1 = Mathf.Min(p1, q1);
            float max1 = Mathf.Max(p1, q1);
            float min2 = Mathf.Min(p2, q2);
            float max2 = Mathf.Max(p2, q2);
            return (min1 <= min2 && min2 <= max1) || (min1 <= max2 && max2 <= max1)
                || (min2 <= min1 && min1 <= max2);
        }

        public static bool LineSegmentsIntersect(Vector2Int p1, Vector2Int q1, Vector2Int p2, Vector2Int q2)
        {
            // See https://www.dcs.gla.ac.uk/~pat/52233/slides/Geometry1x1.pdf
            int orientation1 = ComputeOrientationTwoSegments(p1, q1, p2);
            int orientation2 = ComputeOrientationTwoSegments(p1, q1, q2);
            int orientation3 = ComputeOrientationTwoSegments(p2, q2, p1);
            int orientation4 = ComputeOrientationTwoSegments(p2, q2, q1);
            // General case: The lines "split" each other by separating each other's points
            if (orientation1 != orientation2 && orientation3 != orientation4)
                return true;
            // Special case: The lines are collinear and their projections intersect on both axes
            if (orientation1 == 0 && orientation2 == 0 && orientation3 == 0 && orientation4 == 0)
            {
                if (SegmentsIntersect1D(p1.x, q1.x, p2.x, q2.x) && SegmentsIntersect1D(p1.y, q1.y, p2.y, q2.y))
                    return true;
            }
            return false;
        }

        public static bool EdgesCollideOneStatic(EdgeMovement emStatic, EdgeMovement emOther)
        {
            // Calculate the relative movement of both end points of the other edge
            Vector2Int staticMovement = emStatic.StartTranslation();
            Vector2Int otherMovement1 = emOther.StartTranslation() - staticMovement;
            Vector2Int otherMovement2 = emOther.EndTranslation() - staticMovement;

            bool collision = false;

            // For each end point of the other edge, check if its movement intersects the static edge
            if (otherMovement1 != Vector2Int.zero &&
                LineSegmentsIntersect(emStatic.start1, emStatic.end1, emOther.start1, emOther.start1 + otherMovement1))
            {
                // Collision
                collision = true;
            }
            if (otherMovement2 != Vector2Int.zero &&
                LineSegmentsIntersect(emStatic.start1, emStatic.end1, emOther.end1, emOther.end1 + otherMovement2))
            {
                // Collision
                collision = true;
            }

            if (collision)
            {
                Vector3 em1_start = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(emStatic.start1);
                Vector3 em1_end = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(emStatic.end1);
                Vector3 em2_start = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(emOther.start1);
                Vector3 em2_end = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(emOther.end1);
                Vector3 em2_moveStart = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(emOther.start1 + otherMovement1);
                Vector3 em2_moveEnd = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(emOther.end1 + otherMovement2);

                Debug.DrawLine(em1_start, em1_end, Color.red, debugDisplayTime, false);
                Debug.DrawLine(em2_start, em2_end, Color.green, debugDisplayTime, false);
                Debug.DrawLine(em2_start, em2_moveStart, Color.blue, debugDisplayTime, false);
                Debug.DrawLine(em2_end, em2_moveEnd, Color.blue, debugDisplayTime, false);
                Log.Debug("DETECTED COLLISION! (1 static)");
                return true;
            }

            return false;
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
                // Check if the movement vectors of edge 2 cross edge 1
                if (LineSegmentsIntersect(em1.start1, em1.end1, em2.start1, em2.start1 + em2_rel) ||
                    LineSegmentsIntersect(em1.start1, em1.end1, em2.end1, em2.end1 + em2_rel))
                {
                    Vector3 em1_start = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em1.start1);
                    Vector3 em1_end = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em1.end1);
                    Vector3 em2_start = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.start1);
                    Vector3 em2_end = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.end1);
                    Vector3 em2_moveStart = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.start1 + em2_rel);
                    Vector3 em2_moveEnd = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(em2.end1 + em2_rel);

                    Debug.DrawLine(em1_start, em1_end, Color.red, debugDisplayTime, false);
                    Debug.DrawLine(em2_start, em2_end, Color.green, debugDisplayTime, false);
                    Debug.DrawLine(em2_start, em2_moveStart, Color.blue, debugDisplayTime, false);
                    Debug.DrawLine(em2_end, em2_moveEnd, Color.blue, debugDisplayTime, false);
                    Log.Debug("DETECTED COLLISION! (2 static)");
                }
            }
            // Case 2: One of the edges has only a translation movement
            else if (em1_tStart == em1_tEnd)
            {
                EdgesCollideOneStatic(em1, em2);
            }
            else if (em2_tStart == em2_tEnd)
            {
                EdgesCollideOneStatic(em2, em1);
            }


            return false;
        }
    }

} // namespace AS2.Sim
