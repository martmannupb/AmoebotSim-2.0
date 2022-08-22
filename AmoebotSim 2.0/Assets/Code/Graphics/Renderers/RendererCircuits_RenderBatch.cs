using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuits_RenderBatch
{

    // Data
    private List<Matrix4x4[]> circuitMatrices_Lines = new List<Matrix4x4[]>();
    private MaterialPropertyBlockData_Circuits propertyBlock_circuitMatrices_Lines = new MaterialPropertyBlockData_Circuits();
    private int currentIndex = 0;

    // Precalculated Data _____
    // Meshes
    private Mesh circuitQuad = Engine.Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
    const int maxArraySize = 1023;
    private Material lineMaterial;
    private float zOffset = 0f;

    // Settings _____
    public PropertyBlockData properties;

    /// <summary>
    /// An extendable struct that functions as the key for the mapping of particles to their render class.
    /// </summary>
    public struct PropertyBlockData
    {
        public Color color;
        public bool isExternalConnectorLine;
        public bool moving;
        public bool beeping;

        public PropertyBlockData(Color color, bool isExternalConnectorLine, bool moving, bool beeping)
        {
            this.color = color;
            this.isExternalConnectorLine = isExternalConnectorLine;
            this.moving = moving;
            this.beeping = beeping;
        }
    }

    public RendererCircuits_RenderBatch(PropertyBlockData properties)
    {
        this.properties = properties;

        Init();
    }

    public void Init()
    {
        // Set Material
        if (properties.isExternalConnectorLine) lineMaterial = MaterialDatabase.material_circuit_lineConnector;
        else lineMaterial = MaterialDatabase.material_circuit_line;

        // Circle PropertyBlocks
        propertyBlock_circuitMatrices_Lines.ApplyColor(properties.color);
        if(properties.beeping)
        {
            propertyBlock_circuitMatrices_Lines.ApplyTexture(lineMaterial.GetTexture("_Texture2D"));
            lineMaterial = MaterialDatabase.material_circuit_beep;
            zOffset = -0.1f;
        }
    }

    public void AddLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos)
    {
        if(currentIndex >= maxArraySize * circuitMatrices_Lines.Count)
        {
            // Add an Array
            circuitMatrices_Lines.Add(new Matrix4x4[maxArraySize]);
        }
        int listNumber = currentIndex / maxArraySize;
        int listIndex = currentIndex % maxArraySize;
        float lineWidth = properties.isExternalConnectorLine ? RenderSystem.const_circuitConnectorLineWidth : RenderSystem.const_circuitLineWidth;
        Matrix4x4 matrix = CalculateLineMatrix(globalLineStartPos, globalLineEndPos, lineWidth);
        circuitMatrices_Lines[listNumber][listIndex] = matrix;
        currentIndex++;
    }

    private Matrix4x4 CalculateLineMatrix(Vector2 posInitial, Vector2 posEnd, float width)
    {
        Vector2 vec = posEnd - posInitial;
        float length = vec.magnitude;
        Quaternion q;
        q = Quaternion.FromToRotation(Vector2.right, vec);
        if (q.eulerAngles.y >= 179.99) q = Quaternion.Euler(0f, 0f, 180f); // Hotfix for wrong axis rotation for 180 degrees
        return Matrix4x4.TRS(new Vector3(posInitial.x, posInitial.y, RenderSystem.zLayer_circuits + zOffset), q, new Vector3(length, width, 1f));
    }

    /// <summary>
    /// Clears the matrices, so nothing gets rendered anymore. The lists can be filled with new data now.
    /// (Actually just sets the index to 0, so we dont draw anything anymore.)
    /// </summary>
    public void ClearMatrices()
    {
        currentIndex = 0;
    }

    public void ApplyUpdates(float animationStartTime, float beepStartTime)
    {
        if(properties.beeping)
        {
            // Beeping Animation
            float halfAnimationDuration = RenderSystem.const_circuitBeepDuration * 0.5f;
            propertyBlock_circuitMatrices_Lines.ApplyAlphaPercentagesToBlock(0f, 1f);
            //propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(beepStartTime + halfAnimationDuration, halfAnimationDuration);
            propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(animationStartTime, halfAnimationDuration);
        }
        else
        {
            // Static / Moving Animation
            if (properties.moving)
            {
                // Moving
                propertyBlock_circuitMatrices_Lines.ApplyAlphaPercentagesToBlock(0f, 1f); // Transparent to Visible
                propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(animationStartTime, RenderSystem.const_circuitAnimationDuration);
            }
            else
            {
                // Not Moving
                propertyBlock_circuitMatrices_Lines.ApplyAlphaPercentagesToBlock(1f, 1f); // Visible
                propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(-1f, 0.01f);
            }
        }
    }

    public void Render(bool firstRenderFrame)
    {
        // Timestamp
        if (firstRenderFrame)
        {
            ApplyUpdates(RenderSystem.data_particleMovementFinishedTimestamp, RenderSystem.data_particleMovementFinishedTimestamp + RenderSystem.const_circuitAnimationDuration);
        }

        // Null Check
        if (currentIndex == 0) return;

        int listDrawAmount = ((currentIndex - 1) / maxArraySize) + 1;
        for (int i = 0; i < listDrawAmount; i++)
        {
            int count;
            if (i < listDrawAmount - 1) count = maxArraySize;
            else count = currentIndex % maxArraySize;

            Graphics.DrawMeshInstanced(circuitQuad, 0, lineMaterial, circuitMatrices_Lines[i], count, propertyBlock_circuitMatrices_Lines.propertyBlock);
        }
    }

}