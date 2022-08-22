using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuitPins_RenderBatch
{

    // Data
    private List<Matrix4x4[]> circuitMatrices_Pins = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> circuitMatrices_PinConnectors = new List<Matrix4x4[]>();
    private MaterialPropertyBlockData_Circuits propertyBlock_circuitMatrices_Pins = new MaterialPropertyBlockData_Circuits();
    private int currentIndex = 0;
    private int currentIndex_connectors = 0;

    // Precalculated Data _____
    // Meshes
    private Mesh pinQuad = Engine.Library.MeshConstants.getDefaultMeshQuad();
    const int maxArraySize = 1023;

    // Settings _____
    public PropertyBlockData properties;

    /// <summary>
    /// An extendable struct that functions as the key for the mapping of particles to their render class.
    /// </summary>
    public struct PropertyBlockData
    {
        public Color color;
        public bool moving;

        public PropertyBlockData(Color color, bool moving)
        {
            this.color = color;
            this.moving = moving;
        }
    }

    public RendererCircuitPins_RenderBatch(PropertyBlockData properties)
    {
        this.properties = properties;

        Init();
    }

    public void Init()
    {
        // Circle PropertyBlocks
        //propertyBlock_circuitMatrices_Lines.ApplyColor(properties.color);
        
        //propertyBlock_circle_contracted.ApplyColor(properties.color);
        //propertyBlock_circle_expanded.ApplyColor(properties.color);
        //propertyBlock_circle_expanding.ApplyColor(properties.color);
        //propertyBlock_circle_contracting.ApplyColor(properties.color);
        //propertyBlock_circle_contracted.ApplyUpdatedValues(false, 0, 0f, 0f, Vector3.right);
        //propertyBlock_circle_expanded.ApplyUpdatedValues(true, 0, 1f, 1f, Vector3.right);
        //propertyBlock_circle_expanding.ApplyUpdatedValues(true, 0, 0f, 1f, Vector3.right);
        //propertyBlock_circle_contracting.ApplyUpdatedValues(true, 0, 1f, 0f, Vector3.right);
        //propertyBlock_circle_connector_contracted.ApplyConnectorValues(0f, 0f, Vector3.right, Vector3.left);
        //propertyBlock_circle_connector_expanded.ApplyConnectorValues(1f, 1f, Vector3.right, Vector3.left);
        //propertyBlock_circle_connector_expanding.ApplyConnectorValues(0f, 1f, Vector3.right, Vector3.left);
        //propertyBlock_circle_connector_contracting.ApplyConnectorValues(1f, 0f, Vector3.right, Vector3.left);

        // Hexagon PropertyBlocks
        // ..
    }

    public void AddPin(Vector2 pinPos)
    {
        if(currentIndex >= maxArraySize * circuitMatrices_Pins.Count)
        {
            // Add an Array
            circuitMatrices_Pins.Add(new Matrix4x4[maxArraySize]);
        }
        int listNumber = currentIndex / maxArraySize;
        int listIndex = currentIndex % maxArraySize;
        Matrix4x4 matrix = CalculatePinMatrix(pinPos);
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

    private Matrix4x4 CalculatePinMatrix(Vector2 pinPos)
    {
        return Matrix4x4.TRS(new Vector3(pinPos.x, pinPos.y, RenderSystem.zLayer_pins), Quaternion.identity, new Vector3(RenderSystem.const_circuitPinSize, RenderSystem.const_circuitPinSize, 1f));
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
        if (properties.moving)
        {
            // Moving
            propertyBlock_circuitMatrices_Pins.ApplyAlphaPercentagesToBlock(0f, 1f); // Transparent to Visible
            propertyBlock_circuitMatrices_Pins.ApplyAnimationTimestamp(animationStartTime, RenderSystem.const_circuitAnimationDuration);
        }
        else
        {
            // Not Moving
            propertyBlock_circuitMatrices_Pins.ApplyAlphaPercentagesToBlock(1f, 1f); // Visible
            propertyBlock_circuitMatrices_Pins.ApplyAnimationTimestamp(-1f, 0.01f);
        }
    }

    public void Render(bool firstRenderFrame)
    {
        // Timestamp
        if(firstRenderFrame)
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
                Graphics.DrawMeshInstanced(pinQuad, 0, MaterialDatabase.material_circuit_pin, circuitMatrices_Pins[i], count, propertyBlock_circuitMatrices_Pins.propertyBlock);
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

                Log.Debug("Count: "+count+", ");
                Graphics.DrawMeshInstanced(pinQuad, 0, MaterialDatabase.material_circuit_pin, circuitMatrices_PinConnectors[i], count, propertyBlock_circuitMatrices_Pins.propertyBlock);
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