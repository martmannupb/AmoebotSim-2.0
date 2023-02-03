using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    public static class CollisionChecker
    {
        public static float debugDisplayTime = 20.0f;

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
            else if (otherMovement2 != Vector2Int.zero &&
                LineSegmentsIntersect(emStatic.start1, emStatic.end1, emOther.end1, emOther.end1 + otherMovement2))
            {
                // Collision
                collision = true;
            }

            if (collision)
            {
                DrawDebugLine(emStatic.start1, emStatic.end1, Color.red);
                DrawDebugLine(emOther.start1, emOther.end1, Color.green);
                DrawDebugLine(emOther.start1, emOther.start1 + otherMovement1, Color.blue, true);
                DrawDebugLine(emOther.end1, emOther.end1 + otherMovement2, Color.blue, true);
                Log.Debug("DETECTED COLLISION! (1 static)");
                return true;
            }

            return false;
        }

        public static bool EdgesCollideBothNonStatic(EdgeMovement em1, EdgeMovement em2)
        {
            // We always use the start point of the first edge as its origin

            // Compute the vector pointing from the first edge's start point to
            // its end point when the edge is expanded
            Vector2Int startToEnd = (em1.start1 != em1.end1 ? (em1.end1 - em1.start1) : (em1.end2 - em1.start2));

            // Construct the first virtual edge (going from the origin to the expanded position of the moving part)
            // and check if the relative movement of the other edge intersects it
            Vector2Int virtualMovement = em1.StartTranslation();
            Vector2Int otherMovement1 = em2.StartTranslation() - virtualMovement;
            Vector2Int otherMovement2 = em2.EndTranslation() - virtualMovement;

            bool collision1 = false;

            if (otherMovement1 != Vector2Int.zero && LineSegmentsIntersect(em1.start1, em1.start1 + startToEnd, em2.start1, em2.start1 + otherMovement1))
            {
                collision1 = true;
            }
            else if (otherMovement2 != Vector2Int.zero && LineSegmentsIntersect(em1.start1, em1.start1 + startToEnd, em2.end1, em2.end1 + otherMovement2))
            {
                collision1 = true;
            }

            // If there is no intersection, there cannot be a collision
            if (!collision1)
                return false;

            // If there is a collision, do the same from the point of view of the end
            virtualMovement = em1.EndTranslation();
            otherMovement1 = em2.StartTranslation() - virtualMovement;
            otherMovement2 = em2.EndTranslation() - virtualMovement;

            bool collision2 = false;

            if (otherMovement1 != Vector2Int.zero && LineSegmentsIntersect(em1.end1, em1.end1 - startToEnd, em2.start1, em2.start1 + otherMovement1))
            {
                collision2 = true;
            }
            else if (otherMovement2 != Vector2Int.zero && LineSegmentsIntersect(em1.end1, em1.end1 - startToEnd, em2.end1, em2.end1 + otherMovement2))
            {
                collision2 = true;
            }

            // Found a collision if there is an intersection here
            if (collision2)
            {
                // Current edge (in expanded state)
                DrawDebugLine(em1.start1, em1.start1 + startToEnd, Color.red);

                // Movement lines of the other edge relative to first edge's start
                Vector2Int em2_moveStart = em2.start1 + em2.StartTranslation() - em1.StartTranslation();
                Vector2Int em2_moveEnd = em2.end1 + em2.EndTranslation() - em1.StartTranslation();

                DrawDebugLine(em2.start1, em2.end1, Color.green);
                DrawDebugLine(em2_moveStart, em2_moveEnd, Color.green);
                DrawDebugLine(em2.start1, em2_moveStart, Color.blue, true);
                DrawDebugLine(em2.end1, em2_moveEnd, Color.blue, true);

                Log.Debug("DETECTED COLLISION! (2 non-static)");
                return true;
            }
            return false;
        }

        public static bool EdgesCollide(EdgeMovement em1, EdgeMovement em2)
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
                    DrawDebugLine(em1.start1, em1.end1, Color.red);
                    DrawDebugLine(em2.start1, em2.end1, Color.green);
                    DrawDebugLine(em2.start1, em2.start1 + em2_rel, Color.blue, true);
                    DrawDebugLine(em2.end1, em2.end1 + em2_rel, Color.blue, true);

                    Log.Debug("DETECTED COLLISION! (2 static)");
                    return true;
                }
            }
            // Case 2: One of the edges has only a translation movement
            else if (em1_tStart == em1_tEnd)
            {
                return EdgesCollideOneStatic(em1, em2);
            }
            else if (em2_tStart == em2_tEnd)
            {
                return EdgesCollideOneStatic(em2, em1);
            }
            // Case 3: Both edges have a contraction or expansion
            else
            {
                return EdgesCollideBothNonStatic(em1, em2);
            }

            return false;
        }

        private static void DrawDebugLine(Vector2Int p, Vector2Int q, Color color, bool arrow = false, float time = -1)
        {
            if (time < 0)
                time = debugDisplayTime;
            Vector3 start = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(p);
            Vector3 end = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(q);
            Debug.DrawLine(start, end, color, time, false);
            if (arrow)
            {
                Vector3 endToStart = start - end;
                Quaternion rot1 = Quaternion.Euler(0, 0, 15);
                Quaternion rot2 = Quaternion.Euler(0, 0, -15);
                Vector3 a1 = rot1 * endToStart * 0.1f;
                Vector3 a2 = rot2 * endToStart * 0.1f;
                Debug.DrawLine(end, end + a1, color, time, false);
                Debug.DrawLine(end, end + a2, color, time, false);
            }
        }
    }

} // namespace AS2.Sim
