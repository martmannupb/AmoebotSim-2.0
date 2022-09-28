using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuitPins_RenderBatch
{

    // Data
    private List<Matrix4x4[]> circuitMatrices_Pins = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> circuitMatrices_PinConnectors = new List<Matrix4x4[]>();
    private MaterialPropertyBlockData_Circuits propertyBlock_circuitMatrices_Pins = new MaterialPropertyBlockData_Circuits();
    private MaterialPropertyBlockData_Circuits propertyBlock_circuitMatrices_PinConnectors = new MaterialPropertyBlockData_Circuits();
    private int currentIndex = 0;
    private int currentIndex_connectors = 0;

    // Precalculated Data _____
    // Meshes
    private Mesh pinQuad = Engine.Library.MeshConstants.getDefaultMeshQuad();
    const int maxArraySize = 1023;
    private Material pinMaterial;
    private float zOffset = 0f;

    // Settings _____
    public PropertyBlockData properties;

    /// <summary>
    /// An extendable struct that functions as the key for the mapping of particles to their render class.
    /// </summary>
    public struct PropertyBlockData
    {
        public Color color;
        public bool delayed;
        public bool beeping;

        public PropertyBlockData(Color color, bool delayed, bool beeping)
        {
            this.color = color;
            this.delayed = delayed;
            this.beeping = beeping;
        }
    }

    public RendererCircuitPins_RenderBatch(PropertyBlockData properties)
    {
        this.properties = properties;

        Init();
    }

    public void Init()
    {
        // Set Material
        if (properties.beeping) pinMaterial = MaterialDatabase.material_circuit_pin;
        else pinMaterial = MaterialDatabase.material_circuit_pin;

        // PropertyBlocks
        if (properties.beeping)
        {
            propertyBlock_circuitMatrices_Pins.ApplyColor(ColorData.beepOrigin);
            propertyBlock_circuitMatrices_PinConnectors.ApplyColor(properties.color);
            zOffset = -0.1f;
        }
        else
        {
            propertyBlock_circuitMatrices_Pins.ApplyColor(Color.black);
            propertyBlock_circuitMatrices_PinConnectors.ApplyColor(properties.color);
        }
    }

    public void AddPin(Vector2 pinPos, bool singletonPin)
    {
        if(currentIndex >= maxArraySize * circuitMatrices_Pins.Count)
        {
            // Add an Array
            circuitMatrices_Pins.Add(new Matrix4x4[maxArraySize]);
        }
        int listNumber = currentIndex / maxArraySize;
        int listIndex = currentIndex % maxArraySize;
        Matrix4x4 matrix = CalculatePinMatrix(pinPos, singletonPin);
        circuitMatrices_Pins[listNumber][listIndex] = matrix;
        currentIndex++;
    }

    public void AddConnectorPin(Vector2 pinPos)
    {
        if (currentIndex_connectors >= maxArraySize * circuitMatrices_PinConnectors.Count)
        {
            // Add an Array
            circuitMatrices_PinConnectors.Add(new Matrix4x4[maxArraySize]);
        }
        int listNumber = currentIndex_connectors / maxArraySize;
        int listIndex = currentIndex_connectors % maxArraySize;
        Matrix4x4 matrix = CalculatePinConnectorMatrix(pinPos);
        circuitMatrices_PinConnectors[listNumber][listIndex] = matrix;
        currentIndex_connectors++;
    }

    private Matrix4x4 CalculatePinMatrix(Vector2 pinPos, bool isSingletonPin)
    {
        // Calc Pin Size
        float pinSize = isSingletonPin ? RenderSystem.const_circuitSingletonPinSize : RenderSystem.const_circuitPinSize;
        if (properties.beeping) pinSize *= RenderSystem.const_circuitPinBeepSizePercentage;
        // Calc Matrix
        return Matrix4x4.TRS(new Vector3(pinPos.x, pinPos.y, RenderSystem.zLayer_pins + zOffset), Quaternion.identity, new Vector3(pinSize, pinSize, 1f));
    }

    private Matrix4x4 CalculatePinConnectorMatrix(Vector2 pinPos)
    {
        return Matrix4x4.TRS(new Vector3(pinPos.x, pinPos.y, RenderSystem.zLayer_pins), Quaternion.identity, new Vector3(RenderSystem.const_circuitPinConnectorSize, RenderSystem.const_circuitPinConnectorSize, 1f));
    }

    /// <summary>
    /// Clears the matrices, so nothing gets rendered anymore. The lists can be filled with new data now.
    /// (Actually just sets the index to 0, so we dont draw anything anymore.)
    /// </summary>
    public void ClearMatrices()
    {
        currentIndex = 0;
        currentIndex_connectors = 0;
    }

    public void ApplyUpdates(float animationStartTime)
    {
        if (properties.delayed)
        {
            // Moving
            propertyBlock_circuitMatrices_Pins.ApplyAlphaPercentagesToBlock(0f, 1f); // Transparent to Visible
            propertyBlock_circuitMatrices_PinConnectors.ApplyAlphaPercentagesToBlock(0f, 1f); // Transparent to Visible
            propertyBlock_circuitMatrices_Pins.ApplyAnimationTimestamp(animationStartTime, RenderSystem.data_circuitAnimationDuration);
            propertyBlock_circuitMatrices_PinConnectors.ApplyAnimationTimestamp(animationStartTime, RenderSystem.data_circuitAnimationDuration);
        }
        else
        {
            // Not Moving
            propertyBlock_circuitMatrices_Pins.ApplyAlphaPercentagesToBlock(1f, 1f); // Visible
            propertyBlock_circuitMatrices_PinConnectors.ApplyAlphaPercentagesToBlock(1f, 1f); // Visible
            propertyBlock_circuitMatrices_Pins.ApplyAnimationTimestamp(-1f, 0.01f);
            propertyBlock_circuitMatrices_PinConnectors.ApplyAnimationTimestamp(-1f, 0.01f);
        }
    }

    public void Render(bool firstRenderFrame)
    {
        if (RenderSystem.flag_showCircuitView == false) return;

        // Timestamp
        if (firstRenderFrame)
        {
            ApplyUpdates(RenderSystem.data_particleMovementFinishedTimestamp);
        }

        // Pins
        int listDrawAmount = ((currentIndex - 1) / maxArraySize) + 1;
        if(currentIndex > 0)
        {
            for (int i = 0; i < listDrawAmount; i++)
            {
                int count;
                if (i < listDrawAmount - 1) count = maxArraySize;
                else count = currentIndex % maxArraySize;
                Graphics.DrawMeshInstanced(pinQuad, 0, pinMaterial, circuitMatrices_Pins[i], count, propertyBlock_circuitMatrices_Pins.propertyBlock);
            }
        }

        // Pin Connectors
        int listDrawAmount_connectors = ((currentIndex_connectors - 1) / maxArraySize) + 1;
        if(currentIndex_connectors > 0)
        {
            for (int i = 0; i < listDrawAmount_connectors; i++)
            {
                int count;
                if (i < listDrawAmount_connectors - 1) count = maxArraySize;
                else count = currentIndex_connectors % maxArraySize;

                Graphics.DrawMeshInstanced(pinQuad, 0, pinMaterial, circuitMatrices_PinConnectors[i], count, propertyBlock_circuitMatrices_PinConnectors.propertyBlock);
            }
        }
    }

}

/**
 * - Bugfixing
 * - Beeps
 * - UI (Parameter Editing)
 *     - Content
 * 
 * - Todo _____
 * - UI (Param Editing, Deleting, Adding)
 * - Load/save
 * - Performance (Time Checks, Threading)
 * - Strong Connections
 * - Screenshots
 * - Runtime Scripting
 * - Docs
 * - 
 **/