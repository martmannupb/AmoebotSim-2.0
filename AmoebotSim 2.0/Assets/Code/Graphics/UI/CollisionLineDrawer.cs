using System.Collections.Generic;
using UnityEngine;

namespace AS2.UI
{

    /// <summary>
    /// Utility class for drawing lines and arrows that indicate
    /// collisions to the screen.
    /// <para>
    /// One instance of this script is attached to an empty GameObject
    /// in the Simulator Scene. This instance can be accessed as a
    /// singleton object and it maintains a pool of GameObjects that
    /// contain LineRenderers. Arrows are created by setting the
    /// width curve of a line renderer appropriately.
    /// </para>
    /// </summary>
    public class CollisionLineDrawer : MonoBehaviour
    {
        // The singleton instance
        private static CollisionLineDrawer instance;

        // Pooling
        private List<GameObject> activeLines = new List<GameObject>();
        private Stack<GameObject> pool = new Stack<GameObject>();

        // Timing
        private bool onTimer = false;
        private float timerStart = 0f;
        private float timerDuration = 0f;

        /*
         * Line shape parameters
        */
        private static float lineWidth = 0.15f;                 // Width of basic lines
        private static float arrowLineWidth = 0.065f;           // Width of lines with arrow heads
        private static float arrowHeadWidth = 0.2f;             // Maximal width of arrow heads
        private static float arrowHeadLength = 0.4f;            // Absolute length of arrow heads
        private static float arrowStartOffset = 0.005f;         // Absolute distance between line and arrow head along the line
        private static float zLine = -1f;                       // Z coordinate for basic lines
        private static float zArrow = -2f;                      // Z coordinate for lines with arrow heads

        /// <summary>
        /// The singleton instance of this class.
        /// </summary>
        public static CollisionLineDrawer Instance
        {
            get { return instance; }
        }

        // Awake is called once before all Start() methods.
        void Awake()
        {
            // Establish the singleton instance (should only happen once because we only
            // have one instance)
            if (instance == null)
                instance = this;
        }

        void Update()
        {
            // Check if timer has expired
            if (onTimer && Time.realtimeSinceStartup - timerStart >= timerDuration)
            {
                Clear();
            }
        }

        /// <summary>
        /// Creates a new GameObject with an attached LineRenderer and
        /// sets up the LineRenderer's material and number of cap vertices.
        /// </summary>
        /// <returns>The instantiated GameObject.</returns>
        private GameObject InstantiateLine()
        {
            // Create new child GameObject
            GameObject obj = new GameObject("Line");
            obj.transform.SetParent(transform);
            // Add a LineRenderer component
            obj.AddComponent<LineRenderer>();
            // Set the material
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            lr.material = MaterialDatabase.material_line;
            // Give the end points more vertices to make them rounder
            lr.numCapVertices = 6;
            return obj;
        }

        /// <summary>
        /// Gets a GameObject with a LineRenderer from the pool, if
        /// one is available, or creates a new one otherwise.
        /// </summary>
        /// <returns>A LineRenderer GameObject that may still be
        /// set up for drawing a line.</returns>
        private GameObject GetLine()
        {
            if (pool.Count > 0)
                return pool.Pop();
            else
                return InstantiateLine();
        }

        /// <summary>
        /// Hides all current lines and resets the timer.
        /// </summary>
        public void Clear()
        {
            foreach (GameObject line in activeLines)
            {
                line.SetActive(false);
                pool.Push(line);
            }
            activeLines.Clear();
            // Reset timer in case the drawer was cleared manually before the time ran out
            onTimer = false;
        }

        /// <summary>
        /// Displays a new line to the set of lines that are shown.
        /// </summary>
        /// <param name="start">The grid coordinates of the line's start point.</param>
        /// <param name="end">The grid coordinates of the line's end point.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="arrow">Whether the line should have an arrow head or not.</param>
        public void AddLine(Vector2 start, Vector2 end, Color color, bool arrow = false)
        {
            // Obtain GameObject with LineRenderer
            GameObject obj = GetLine();
            LineRenderer lr = obj.GetComponent<LineRenderer>();

            // Setup the color
            Gradient g = new Gradient();
            g.colorKeys = new GradientColorKey[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) };
            lr.colorGradient = g;

            // Compute the world coordinates of the start and end points
            Vector2 startWorld = AmoebotFunctions.GridToWorldPositionVector2(start);
            Vector2 endWorld = AmoebotFunctions.GridToWorldPositionVector2(end);

            // Setup the line renderer for the given type of line
            if (arrow)
                SetupArrowLine(startWorld, endWorld, lr);
            else
                SetupLine(startWorld, endWorld, lr);

            // Activate the line
            activeLines.Add(obj);
            obj.SetActive(true);
        }

        /// <summary>
        /// Sets up a timer that clears all lines after the given duration.
        /// </summary>
        /// <param name="duration">The time in seconds after which all lines
        /// should be hidden.</param>
        public void SetTimer(float duration)
        {
            onTimer = true;
            timerStart = Time.realtimeSinceStartup;
            timerDuration = duration;
        }

        /// <summary>
        /// Sets up the given LineRenderer to display a basic line.
        /// </summary>
        /// <param name="start">The world coordinates of the line's start point.</param>
        /// <param name="end">The world coordinates of the line's end point.</param>
        /// <param name="lr">The LineRenderer that should display the line.</param>
        private void SetupLine(Vector2 start, Vector2 end, LineRenderer lr)
        {
            // Set the points
            lr.positionCount = 2;
            lr.SetPositions(new Vector3[] {
                new Vector3(start.x, start.y, zLine),
                new Vector3(end.x, end.y, zLine)
            });

            // Set width
            AnimationCurve ac = new AnimationCurve(new Keyframe[] {
                new Keyframe(0, lineWidth, 0, 0),
                new Keyframe(1, lineWidth, 0, 0)
            });
            lr.widthCurve = ac;
        }

        /// <summary>
        /// Sets up the given LineRenderer to display a line with an arrow head.
        /// </summary>
        /// <param name="start">The world coordinates of the line's start point.</param>
        /// <param name="end">The world coordinates of the line's end point.</param>
        /// <param name="lr">The LineRenderer that should display the line.</param>
        private void SetupArrowLine(Vector2 start, Vector2 end, LineRenderer lr)
        {
            // Set the points
            Vector2 to = end - start;
            float length = to.magnitude;
            float s1_offset = 1.0f - arrowHeadLength / length;
            float s2_offset = 1.0f - (arrowHeadLength - arrowStartOffset) / length;
            Vector2 s1 = start + s1_offset * to;
            Vector2 s2 = start + s2_offset * to;

            lr.positionCount = 4;

            lr.SetPositions(new Vector3[] {
                new Vector3(start.x, start.y, zArrow),
                new Vector3(s1.x, s1.y, zArrow),
                new Vector3(s2.x, s2.y, zArrow),
                new Vector3(end.x, end.y, zArrow) }
            );

            // Set width
            float slope1 = (arrowHeadWidth - arrowLineWidth) / (arrowStartOffset / length);
            float slope2 = arrowHeadWidth / (1.0f - s2_offset);
            AnimationCurve ac = new AnimationCurve(new Keyframe[] {
                new Keyframe(0, arrowLineWidth, 0, 0),
                new Keyframe(s1_offset, arrowLineWidth, 0, slope1),
                new Keyframe(s2_offset, arrowHeadWidth, slope1, -slope2),
                new Keyframe(1, 0, slope2, 0)
            });
            lr.widthCurve = ac;
        }
    }

}
