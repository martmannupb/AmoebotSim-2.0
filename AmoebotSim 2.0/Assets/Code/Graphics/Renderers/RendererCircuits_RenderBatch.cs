using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuits_RenderBatch
{

    // Data
    private List<Matrix4x4[]> circuitMatrices_Lines = new List<Matrix4x4[]>();
    private MaterialPropertyBlockData_Circuits propertyBlock_circuitMatrices_Lines = new MaterialPropertyBlockData_Circuits();
    private int currentIndex = 0;

    // Animations (only used if properties.animated = true)
    private List<Vector2[]> startPositions1 = new List<Vector2[]>();
    private List<Vector2[]> endPositions1 = new List<Vector2[]>();
    private List<Vector2[]> startPositions2 = new List<Vector2[]>();
    private List<Vector2[]> endPositions2 = new List<Vector2[]>();

    // Precalculated Data _____
    // Meshes
    private Mesh circuitQuad = Engine.Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
    const int maxArraySize = 1023;
    private Material lineMaterial;
    private float zOffset = 0f;
    private float lineWidth = 0f;

    // Settings _____
    public PropertyBlockData properties;

    /// <summary>
    /// An extendable struct that functions as the key for the mapping of particles to their render class.
    /// </summary>
    public struct PropertyBlockData
    {
        public enum LineType
        {
            InternalLine,
            ExternalLine,
            Bond
        }

        public Color color;
        public LineType lineType;
        public bool delayed;
        public bool beeping;
        public bool animated;

        public PropertyBlockData(Color color, LineType lineType, bool delayed, bool beeping, bool animated)
        {
            this.color = color;
            this.lineType = lineType;
            this.delayed = delayed;
            this.beeping = beeping;
            this.animated = animated;
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
        switch (properties.lineType)
        {
            case PropertyBlockData.LineType.InternalLine:
                lineMaterial = MaterialDatabase.material_circuit_line;
                break;
            case PropertyBlockData.LineType.ExternalLine:
                lineMaterial = SettingsGlobal.circuitBorderActive ? MaterialDatabase.material_circuit_lineConnector : MaterialDatabase.material_circuit_line;
                break;
            case PropertyBlockData.LineType.Bond:
                lineMaterial = MaterialDatabase.material_bond_line;
                break;
            default:
                break;
        }

        // Circle PropertyBlocks
        if(properties.lineType != PropertyBlockData.LineType.Bond) propertyBlock_circuitMatrices_Lines.ApplyColor(properties.color); // Bond Color stays
        if(properties.beeping)
        {
            propertyBlock_circuitMatrices_Lines.ApplyTexture(lineMaterial.GetTexture("_Texture2D"));
            lineMaterial = MaterialDatabase.material_circuit_beep;
            zOffset = -0.1f;
        }

        // Settings
        switch (properties.lineType)
        {
            case PropertyBlockData.LineType.InternalLine:
                lineWidth = RenderSystem.const_circuitLineWidth;
                break;
            case PropertyBlockData.LineType.ExternalLine:
                lineWidth = RenderSystem.const_circuitConnectorLineWidth;
                break;
            case PropertyBlockData.LineType.Bond:
                lineWidth = RenderSystem.const_bondsLineWidth;
                break;
            default:
                break;
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
        circuitMatrices_Lines[listNumber][listIndex] = CalculateLineMatrix(globalLineStartPos, globalLineEndPos);
        currentIndex++;
    }

    public void AddAnimatedLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, Vector2 globalLineStartPos2, Vector2 globalLineEndPos2)
    {
        if (currentIndex >= maxArraySize * circuitMatrices_Lines.Count)
        {
            // Add an Array
            circuitMatrices_Lines.Add(new Matrix4x4[maxArraySize]);
            startPositions1.Add(new Vector2[maxArraySize]);
            endPositions1.Add(new Vector2[maxArraySize]);
            startPositions2.Add(new Vector2[maxArraySize]);
            endPositions2.Add(new Vector2[maxArraySize]);
        }
        int listNumber = currentIndex / maxArraySize;
        int listIndex = currentIndex % maxArraySize;
        circuitMatrices_Lines[listNumber][listIndex] = CalculateLineMatrix(globalLineStartPos, globalLineEndPos);
        startPositions1[listNumber][listIndex] = globalLineStartPos;
        endPositions1[listNumber][listIndex] = globalLineEndPos;
        startPositions2[listNumber][listIndex] = globalLineStartPos2;
        endPositions2[listNumber][listIndex] = globalLineEndPos2;
        currentIndex++;
    }

    private Matrix4x4 CalculateLineMatrix(Vector2 posInitial, Vector2 posEnd)
    {
        Vector2 vec = posEnd - posInitial;
        float length = vec.magnitude;
        float z;
        if(properties.lineType == PropertyBlockData.LineType.Bond) z = RenderSystem.ZLayer_bonds;
        else z = RenderSystem.zLayer_circuits + zOffset;
        Quaternion q;
        q = Quaternion.FromToRotation(Vector2.right, vec);
        if (q.eulerAngles.y >= 179.999) q = Quaternion.Euler(0f, 0f, 180f); // Hotfix for wrong axis rotation for 180 degrees
        return Matrix4x4.TRS(new Vector3(posInitial.x, posInitial.y, z), q, new Vector3(length, lineWidth, 1f));
    }

    private void CalculateAnimationFrame()
    {
        float interpolationPercentage;
        if (properties.animated) interpolationPercentage = Engine.Library.InterpolationConstants.SmoothLerp(RenderSystem.animation_curAnimationPercentage);
        else interpolationPercentage = 1f;
        for (int i = 0; i < currentIndex; i++)
        {
            int listNumber = i / maxArraySize;
            int listIndex = i % maxArraySize;
            Vector2 posStart1 = startPositions1[listNumber][listIndex];
            Vector2 posEnd1 = endPositions1[listNumber][listIndex];
            Vector2 posStart2 = startPositions2[listNumber][listIndex];
            Vector2 posEnd2 = endPositions2[listNumber][listIndex];
            circuitMatrices_Lines[listNumber][listIndex] = CalculateLineMatrix(posStart1 + interpolationPercentage * (posStart2 - posStart1), posEnd2 + interpolationPercentage * (posEnd2 - posEnd1));
        }
    }



    /// <summary>
    /// Clears the matrices, so nothing gets rendered anymore. The lists can be filled with new data now.
    /// (Actually just sets the index to 0, so we dont draw anything anymore.)
    /// </summary>
    public void ClearMatrices()
    {
        currentIndex = 0;
    }

    private float lastBeepStartTime;

    public void ApplyUpdates(float animationStartTime, float beepStartTime)
    {
        if(properties.beeping)
        {
            // Beeping Animation
            float halfAnimationDuration = RenderSystem.data_circuitBeepDuration * 0.5f;
            propertyBlock_circuitMatrices_Lines.ApplyAlphaPercentagesToBlock(0f, 1f);
            propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(beepStartTime + halfAnimationDuration, halfAnimationDuration);
            //propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(animationStartTime, halfAnimationDuration);
            lastBeepStartTime = beepStartTime;
        }
        else
        {
            // Static / Moving Animation
            if (properties.delayed)
            {
                // Moving
                propertyBlock_circuitMatrices_Lines.ApplyAlphaPercentagesToBlock(0f, 1f); // Transparent to Visible
                propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(animationStartTime, RenderSystem.data_circuitAnimationDuration);
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
        // Visibility Check
        if(properties.lineType == PropertyBlockData.LineType.InternalLine || properties.lineType == PropertyBlockData.LineType.ExternalLine)
        {
            // Circuits
            if (RenderSystem.flag_showCircuitView == false) return;
        }
        else
        {
            // Bonds
            if (RenderSystem.flag_showBonds == false) return;
        }

        // Timestamp
        if (firstRenderFrame)
        {
            ApplyUpdates(RenderSystem.data_particleMovementFinishedTimestamp, RenderSystem.data_particleMovementFinishedTimestamp + RenderSystem.data_circuitAnimationDuration);
        }
        else if(properties.beeping && RenderSystem.data_circuitBeepRepeatOn && lastBeepStartTime + RenderSystem.data_circuitBeepDuration < Time.timeSinceLevelLoad)
        {
            // We show beeps and have shown the beep already
            // Repeat Beep
            ApplyUpdates(RenderSystem.data_particleMovementFinishedTimestamp, lastBeepStartTime + RenderSystem.data_circuitBeepRepeatDelay);
        }

        // Null Check
        if (currentIndex == 0) return;

        // Animations
        if(properties.animated) CalculateAnimationFrame();

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