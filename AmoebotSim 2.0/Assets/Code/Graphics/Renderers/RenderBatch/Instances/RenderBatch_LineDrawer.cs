using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    public class RenderBatch_LineDrawer : RenderBatch<RenderBatch_LineDrawer.LineProperties>
    {
        public struct LineProperties
        {
            public Color color;
            public float width;
            public float z;

            public LineProperties(Color color, float width, float z)
            {
                this.color = color;
                this.width = width;
                this.z = z;
            }
        }

        public RenderBatch_LineDrawer(LineProperties properties) : base(properties, Library.MeshConstants.getDefaultMeshQuad(1f, 0f, new Vector2(0f, 0.5f)), MaterialDatabase.material_line, 0) { }

        protected override void SetUp()
        {
            // Apply default color
            SetColor(properties.color);
        }

        public void AddLine(Vector2 pos1, Vector2 pos2)
        {
            AddMatrix(RenderBatch_MatrixCalculation.Line2D(pos1, pos2, properties.width, properties.z));
        }

        public void SetColor(Color color)
        {
            materialPropertyBlock.SetColor("_InputColor", color);
        }
    }

}