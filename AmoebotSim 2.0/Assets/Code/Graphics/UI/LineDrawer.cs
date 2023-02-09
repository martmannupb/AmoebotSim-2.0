using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.UI
{

    public class LineDrawer : MonoBehaviour
    {
        private static LineDrawer instance;
        private List<GameObject> lines = new List<GameObject>();

        public static LineDrawer Instance
        {
            get { return instance; }
        }

        void Awake()
        {
            if (instance == null)
                instance = this;
        }

        public void AddLine()
        {
            // Create empty child GameObject
            GameObject empty = new GameObject("Line");
            empty.transform.SetParent(transform);
            // Add a LineRenderer component
            empty.AddComponent<LineRenderer>();
            LineRenderer lr = empty.GetComponent<LineRenderer>();
            // Set the material
            lr.material = MaterialDatabase.material_line;

            // Set a random color
            Gradient g = new Gradient();
            g.colorKeys = new GradientColorKey[] { new GradientColorKey(new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)), 0f) };
            lr.colorGradient = g;

            // Set the points
            float z = -2f;
            float length = 20f;
            float arrow_length = 0.4f;
            float arrow_start_offset = 0.01f;
            lr.positionCount = 4;
            lr.SetPositions(new Vector3[] {
                new Vector3(0, 0, z),
                new Vector3(length - arrow_length, 0, z),
                new Vector3(length - arrow_length + arrow_start_offset, 0, z),
                new Vector3(length, 0, z) });
            // Set width
            float arrow_width = 0.1f;
            float arrow_tip_width = arrow_width * 2;
            AnimationCurve ac = new AnimationCurve(new Keyframe[] {
                new Keyframe(0, arrow_width, 0, 0),
                new Keyframe((length - arrow_length) / length, arrow_width,
                    0, (arrow_tip_width - arrow_width) / (arrow_start_offset / length)),
                new Keyframe((length - arrow_length + arrow_start_offset) / length, arrow_tip_width,
                    (arrow_width - arrow_tip_width) / (arrow_start_offset / length),
                    -arrow_tip_width / (1.0f - (length - arrow_length + arrow_start_offset) / length)),
                new Keyframe(1, 0, -arrow_tip_width / (1.0f - (length - arrow_length + arrow_start_offset) / length), 0)
            });
            lr.widthCurve = ac;
            lines.Add(empty);
        }

        public void AddLine(Vector2Int start, Vector2Int end)
        {
            // Create empty child GameObject
            GameObject empty = new GameObject("Line");
            empty.transform.SetParent(transform);
            // Add a LineRenderer component
            empty.AddComponent<LineRenderer>();
            LineRenderer lr = empty.GetComponent<LineRenderer>();
            // Set the material
            lr.material = MaterialDatabase.material_line;

            // Set a random color
            Gradient g = new Gradient();
            g.colorKeys = new GradientColorKey[] { new GradientColorKey(new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)), 0f) };
            lr.colorGradient = g;

            // Set the points
            Vector2 p = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(start);
            Vector2 q = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(end);
            Vector2 to = q - p;
            float z = -2f;
            float length = Vector2.Distance(p, q);
            float arrow_length = 0.4f;
            float arrow_start_offset = 0.005f;
            float s1_offset = 1.0f - arrow_length / length;
            float s2_offset = 1.0f - (arrow_length - arrow_start_offset) / length;
            Vector2 s1 = p + s1_offset * to;
            Vector2 s2 = p + s2_offset * to;

            lr.positionCount = 4;

            lr.SetPositions(new Vector3[] {
                new Vector3(p.x, p.y, z),
                new Vector3(s1.x, s1.y, z),
                new Vector3(s2.x, s2.y, z),
                new Vector3(q.x, q.y, z) }
            );


            // Set width
            float arrow_width = 0.075f;
            float arrow_tip_width = 0.2f;
            float slope1 = (arrow_tip_width - arrow_width) / (arrow_start_offset / length);
            float slope2 = arrow_tip_width / (1.0f - s2_offset);
            AnimationCurve ac = new AnimationCurve(new Keyframe[] {
                new Keyframe(0, arrow_width, 0, 0),
                new Keyframe(s1_offset, arrow_width,
                    0, slope1),
                new Keyframe(s2_offset, arrow_tip_width,
                    slope1, -slope2),
                new Keyframe(1, 0, slope2, 0)
            });
            lr.widthCurve = ac;
            lines.Add(empty);
        }
    }

}
