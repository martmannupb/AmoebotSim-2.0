using System.Collections.Generic;
using System;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Helper methods for detecting collisions in joint movements.
    /// <para>
    /// Collision checks work by tracking the movements of all edges
    /// in the system. Edges are created by all bonds and expanded,
    /// expanding and contracting particles in the system.
    /// If any two edges that do not coincide in one point at the start
    /// or the end of the movement intersect during the movement, a
    /// collision occurs.
    /// </para>
    /// </summary>
    public static class CollisionChecker
    {

        /// <summary>
        /// Simple container for data required to draw lines that
        /// show a collision in the system.
        /// </summary>
        [Serializable]
        public struct DebugLine
        {
            /// <summary>
            /// The start point of the line.
            /// </summary>
            public Vector2Int p;
            /// <summary>
            /// The end point of the line.
            /// </summary>
            public Vector2Int q;
            /// <summary>
            /// The color of the line.
            /// </summary>
            public Color color;
            /// <summary>
            /// Determines whether the line should end with
            /// an arrow or not.
            /// </summary>
            public bool arrow;

            public DebugLine(Vector2Int p, Vector2Int q, Color color, bool arrow = false)
            {
                this.p = p;
                this.q = q;
                this.color = color;
                this.arrow = arrow;
            }
        }

        /// <summary>
        /// The number of seconds for which the collision visualization
        /// should be displayed.
        /// </summary>
        public static float debugDisplayTime = 20.0f;

        /// <summary>
        /// The debug lines drawn for the last detected collision.
        /// </summary>
        private static List<DebugLine> debugLines = new List<DebugLine>();

        /// <summary>
        /// Checks whether the two given edge movements share an end point
        /// at the start or the end of the movement.
        /// </summary>
        /// <param name="em1">The first edge movement.</param>
        /// <param name="em2">The second edge movement.</param>
        /// <returns><c>true</c> if and only if the two given edge
        /// movements share an end point at the start or the
        /// end of the movement.</returns>
        public static bool HaveSharedNode(EdgeMovement em1, EdgeMovement em2)
        {
            return em1.start1 == em2.start1 || em1.start1 == em2.end1 || em1.end1 == em2.start1 || em1.end1 == em2.end1 ||
                em1.start2 == em2.start2 || em1.start2 == em2.end2 || em1.end2 == em2.start2 || em1.end2 == em2.end2;
        }

        /// <summary>
        /// Calculates the orientation of the triangle formed by the
        /// three given points.
        /// <para>
        /// The orientation can be counter-clockwise, clockwise or
        /// collinear.
        /// </para>
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <param name="p3">The third point.</param>
        /// <returns><c>1</c> if the orientation is clockwise,
        /// <c>-1</c> if it is counter-clockwise and <c>0</c>
        /// if it is collinear.</returns>
        public static int ComputeOrientationTwoSegments(Vector2Int p1, Vector2Int p2, Vector2Int p3)
        {
            int product = (p2.y - p1.y) * (p3.x - p2.x) - (p3.y - p2.y) * (p2.x - p1.x);
            return product > 0 ? 1 : (product < 0 ? -1 : 0);
        }

        /// <summary>
        /// Checks if the two given one-dimensional line segments intersect.
        /// </summary>
        /// <param name="p1">The start of the first segment.</param>
        /// <param name="q1">The end of the first segment.</param>
        /// <param name="p2">The start of the second segment.</param>
        /// <param name="q2">The end of the second segment.</param>
        /// <returns><c>true</c> if and only if the two segments intersect.</returns>
        public static bool SegmentsIntersect1D(float p1, float q1, float p2, float q2)
        {
            float min1 = Mathf.Min(p1, q1);
            float max1 = Mathf.Max(p1, q1);
            float min2 = Mathf.Min(p2, q2);
            float max2 = Mathf.Max(p2, q2);
            return (min1 <= min2 && min2 <= max1) || (min1 <= max2 && max2 <= max1)
                || (min2 <= min1 && min1 <= max2);
        }

        /// <summary>
        /// Checks if the two given two-dimensional line segments intersect.
        /// <para>
        /// Uses the algorithm described in https://www.dcs.gla.ac.uk/~pat/52233/slides/Geometry1x1.pdf.
        /// </para>
        /// </summary>
        /// <param name="p1">The start point of the first segment.</param>
        /// <param name="q1">The end point of the first segment.</param>
        /// <param name="p2">The start point of the second segment.</param>
        /// <param name="q2">The end point of the second segment.</param>
        /// <returns><c>true</c> if and only if the two segments intersect.</returns>
        public static bool LineSegmentsIntersect(Vector2Int p1, Vector2Int q1, Vector2Int p2, Vector2Int q2)
        {
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

        /// <summary>
        /// Checks if the two given edges collide, given that the first
        /// edge is not a contraction or expansion.
        /// </summary>
        /// <param name="emStatic">The edge movement that is not a
        /// contraction or expansion.</param>
        /// <param name="emOther">The other edge movement, which might
        /// be a contraction or expansion.</param>
        /// <returns><c>true</c> if and only if the two edges collide.</returns>
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
                debugLines.Clear();
                debugLines.Add(new DebugLine(emStatic.start1, emStatic.end1, Color.red));
                debugLines.Add(new DebugLine(emOther.start1, emOther.end1, Color.green));
                debugLines.Add(new DebugLine(emOther.start1, emOther.start1 + otherMovement1, Color.blue, true));
                debugLines.Add(new DebugLine(emOther.end1, emOther.end1 + otherMovement2, Color.blue, true));
                DrawDebugLines(debugLines.ToArray());
                Log.Debug("DETECTED COLLISION! (1 static)");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the two given edges collide, where both edges can be
        /// contraction or expansion movements.
        /// </summary>
        /// <param name="em1">The first edge movement.</param>
        /// <param name="em2">The second edge movement.</param>
        /// <returns><c>true</c> if and only if the two given edges collide.</returns>
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
                debugLines.Clear();

                // Current edge (in expanded state)
                debugLines.Add(new DebugLine(em1.start1, em1.start1 + startToEnd, Color.red));

                // Movement lines of the other edge relative to first edge's start
                Vector2Int em2_moveStart = em2.start1 + em2.StartTranslation() - em1.StartTranslation();
                Vector2Int em2_moveEnd = em2.end1 + em2.EndTranslation() - em1.StartTranslation();

                debugLines.Add(new DebugLine(em2.start1, em2.end1, Color.green));
                debugLines.Add(new DebugLine(em2_moveStart, em2_moveEnd, Color.green));
                debugLines.Add(new DebugLine(em2.start1, em2_moveStart, Color.blue, true));
                debugLines.Add(new DebugLine(em2.end1, em2_moveEnd, Color.blue, true));
                DrawDebugLines(debugLines.ToArray());

                Log.Debug("DETECTED COLLISION! (2 non-static)");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the two given edge movements collide.
        /// This is the most general of the collision check methods.
        /// It selects the most appropriate helper procedure to apply
        /// to the two edges.
        /// </summary>
        /// <param name="em1">The first edge movement.</param>
        /// <param name="em2">The second edge movement.</param>
        /// <returns><c>true</c> if and only if the two edges collide.</returns>
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
                    debugLines.Clear();
                    debugLines.Add(new DebugLine(em1.start1, em1.end1, Color.red));
                    debugLines.Add(new DebugLine(em2.start1, em2.end1, Color.green));
                    debugLines.Add(new DebugLine(em2.start1, em2.start1 + em2_rel, Color.blue, true));
                    debugLines.Add(new DebugLine(em2.end1, em2.end1 + em2_rel, Color.blue, true));
                    DrawDebugLines(debugLines.ToArray());

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

        /// <summary>
        /// Returns the list of debug lines generated for the
        /// last detected collision. These lines can be used to
        /// display where the collision was detected.
        /// </summary>
        /// <returns>An array containing information for drawing
        /// lines that show where the last collision was detected.</returns>
        public static DebugLine[] GetDebugLines()
        {
            return debugLines.ToArray();
        }

        /// <summary>
        /// Draws the given debug lines onto the screen.
        /// <para>
        /// Note that this only works in the Unity Editor and the
        /// "Gizmos" toggle in the Game View must be activated for
        /// the lines to be visible.
        /// </para>
        /// </summary>
        /// <param name="lines">The lines to be drawn. Should be
        /// the return value of <see cref="GetDebugLines"/>.</param>
        /// <param name="time">The time in seconds for which the
        /// lines should be displayed. A negative value will lead to
        /// <see cref="debugDisplayTime"/> being used.</param>
        public static void DrawDebugLines(DebugLine[] lines, float time = -1)
        {
            foreach (DebugLine line in lines)
                DrawDebugLine(line, time);
        }

        /// <summary>
        /// Helper for drawing debug lines.
        /// </summary>
        /// <param name="p">The start point of the line.</param>
        /// <param name="q">The end point of the line.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="arrow">Determines whether the line should have an arrow.</param>
        /// <param name="time">The time for which the line should be displayed.
        /// Negative values lead to <see cref="debugDisplayTime"/> being used.</param>
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

        /// <summary>
        /// Helper for drawing debug lines.
        /// </summary>
        /// <param name="line">The line to be drawn.</param>
        /// <param name="time">The time for which the line should be displayed.
        /// Negative values lead to <see cref="debugDisplayTime"/> being used.</param>
        private static void DrawDebugLine(DebugLine line, float time = -1)
        {
            DrawDebugLine(line.p, line.q, line.color, line.arrow, time);
        }
    }

} // namespace AS2.Sim
