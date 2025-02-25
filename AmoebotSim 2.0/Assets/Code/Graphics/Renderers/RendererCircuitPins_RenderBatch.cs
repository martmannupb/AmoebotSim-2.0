// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Renderer for the circuit pins. Each instance of this class renders pins
    /// with the same properties (like color, type, etc.) with Unity's instanced drawing.
    /// </summary>
    public class RendererCircuitPins_RenderBatch : IGenerateDynamicMesh
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
        private Mesh pinQuad;
        const int maxArraySize = 1023;
        private Material pinMaterial;
        private float zOffset = 0f;

        // Settings _____
        public PropertyBlockData properties;

        /// <summary>
        /// An extendable struct that functions as the key for
        /// the mapping of particles to their render class.
        /// </summary>
        public struct PropertyBlockData
        {
            public Color color;
            public bool singleton;
            public bool delayed;
            public bool beeping;
            public bool beepOrigin;
            public bool faulty;
            public bool connector;
            public Vector2 animationOffset;

            public PropertyBlockData(Color color, bool delayed, bool singleton, bool beeping, bool beepOrigin, bool faulty, bool connector) : this(color, delayed, singleton, beeping, beepOrigin, faulty, connector, Vector2.zero) { }
            public PropertyBlockData(Color color, bool delayed, bool singleton, bool beeping, bool beepOrigin, bool faulty, bool connector, Vector2 animationOffset)
            {
                this.color = color;
                this.singleton = singleton;
                this.delayed = delayed;
                this.beeping = beeping;
                this.beepOrigin = beepOrigin;
                this.faulty = faulty;
                this.connector = connector;
                this.animationOffset = animationOffset;
            }
        }

        public RendererCircuitPins_RenderBatch(PropertyBlockData properties)
        {
            this.properties = properties;

            Init();
        }

        /// <summary>
        /// Initializes the materials and settings of this class.
        /// </summary>
        public void Init()
        {
            // Set Material
            if (!properties.connector)
            {
                // This material expects two colors to fill the center and the
                // border of a circle. We can use this to make solid circles by
                // setting both to the same color
                pinMaterial = MaterialDatabase.material_circuit_pin_movement_border;
            }
            else
            {
                pinMaterial = MaterialDatabase.material_circuit_pin_movement;
                // Render the connector on the same level as the circuit lines
                // TODO: Set correct zOffset
                pinMaterial.renderQueue = RenderSystem.renderQueue_circuits;
            }

            // PropertyBlocks
            if (properties.connector)
            {
                propertyBlock_circuitMatrices_PinConnectors.ApplyColor(properties.color);
            }
            else if (properties.beepOrigin)
            {
                // Beep origin is always filled and singleton
                propertyBlock_circuitMatrices_Pins.ApplyColor(ColorData.beepOrigin);
                propertyBlock_circuitMatrices_Pins.ApplyColorSecondary(ColorData.beepOrigin);

                // Render the beep origin in front of the pin / partition set handle
                zOffset = -0.1f;
                pinMaterial.renderQueue = RenderSystem.renderQueue_pinBeeps;
            }
            else if (properties.faulty)
            {
                propertyBlock_circuitMatrices_Pins.ApplyColor(ColorData.faultyBeep);
                if (properties.singleton)
                {
                    propertyBlock_circuitMatrices_Pins.ApplyColorSecondary(ColorData.faultyBeep);
                    // Render the fault highlight in front of the pin
                    // but behind the beep origin highlight
                    zOffset = -0.05f;
                    pinMaterial.renderQueue = RenderSystem.renderQueue_pinFault;
                }
            }
            else if (properties.beeping)
            {
                propertyBlock_circuitMatrices_Pins.ApplyColor(ColorData.beepReceive);
                if (properties.singleton)
                {
                    propertyBlock_circuitMatrices_Pins.ApplyColorSecondary(ColorData.beepReceive);
                    // Render the receiving highlight in front of the pin
                    // but behind the beep origin highlight
                    zOffset = -0.05f;
                    pinMaterial.renderQueue = RenderSystem.renderQueue_pinFault;
                }
            }
            else
            {
                propertyBlock_circuitMatrices_Pins.ApplyColor(Color.black);
            }
            propertyBlock_circuitMatrices_Pins.ApplyMovementOffset(properties.animationOffset);
            propertyBlock_circuitMatrices_PinConnectors.ApplyMovementOffset(properties.animationOffset);

            // Generate Mesh
            RegenerateMeshes();
        }

        public void RegenerateMeshes()
        {
            pinQuad = Library.MeshConstants.getDefaultMeshQuad();
            if (RenderSystem.const_mesh_useManualBoundingBoxRadius)
                pinQuad = Library.MeshConstants.AddManualBoundsToMesh(pinQuad, Vector3.zero, RenderSystem.const_mesh_boundingBoxRadius, true);
        }

        /// <summary>
        /// Adds a pin.
        /// </summary>
        /// <param name="pinPos">The global pin center position.</param>
        /// <param name="singletonPin">Whether this is a singleton pin.
        /// Singleton pins have their own pin size.</param>
        /// <returns>The index of the new pin's matrix in this batch.</returns>
        public RenderBatchIndex AddPin(Vector2 pinPos, bool singletonPin)
        {
            if (currentIndex >= maxArraySize * circuitMatrices_Pins.Count)
            {
                // Add an Array
                circuitMatrices_Pins.Add(new Matrix4x4[maxArraySize]);
            }
            int listNumber = currentIndex / maxArraySize;
            int listIndex = currentIndex % maxArraySize;
            Matrix4x4 matrix = CalculatePinMatrix(pinPos, singletonPin);
            circuitMatrices_Pins[listNumber][listIndex] = matrix;
            currentIndex++;
            return new RenderBatchIndex(listNumber, listIndex);
        }

        /// <summary>
        /// Updates a pin.
        /// </summary>
        /// <param name="pinPos">The global pin center position.</param>
        /// <param name="singletonPin">Whether this is a singleton pin.
        /// Singleton pins have their own pin size.</param>
        /// <param name="index">The index of the pin to update.</param>
        public void UpdatePin(Vector2 pinPos, bool singletonPin, RenderBatchIndex index)
        {
            if(index.isValid == false || index.listNumber < 0 || index.listNumber >= circuitMatrices_Pins.Count || index.listIndex < 0 || index.listIndex >= circuitMatrices_Pins[index.listNumber].Length)
            {
                Log.Error("Pin Render Batch: UpdatePin: Array out of bounds.");
                return;
            }
            Matrix4x4 matrix = CalculatePinMatrix(pinPos, singletonPin);
            circuitMatrices_Pins[index.listNumber][index.listIndex] = matrix;
        }

        /// <summary>
        /// Adds a connector pin.
        /// A connector pin is an internal pin in expanded particles that
        /// connects the two sides of the particle. It has the width and color
        /// of the circuit lines, so that two lines that go to the same point
        /// do not have visibly sharp edges and seem like a connected structure.
        /// </summary>
        /// <param name="pinPos">The global pin center position.</param>
        public RenderBatchIndex AddConnectorPin(Vector2 pinPos)
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
            return new RenderBatchIndex(listNumber, listIndex);
        }

        /// <summary>
        /// Updates a connector pin.
        /// </summary>
        /// <param name="pinPos">The new global pin center position.</param>
        /// <param name="index">The batch index of the pin to update.</param>
        public void UpdateConnectorPin(Vector2 pinPos, RenderBatchIndex index)
        {
            if (index.isValid == false || index.listNumber < 0 || index.listNumber >= circuitMatrices_PinConnectors.Count || index.listIndex < 0 || index.listIndex >= circuitMatrices_PinConnectors[index.listNumber].Length)
            {
                Log.Error("Pin Render Batch: UpdateConnectorPin: Array out of bounds.");
                return;
            }
            Matrix4x4 matrix = CalculatePinConnectorMatrix(pinPos);
            circuitMatrices_PinConnectors[index.listNumber][index.listIndex] = matrix;
        }

        /// <summary>
        /// Calculates the pin matrix from the positional information about the pin.
        /// We basically take a standard quad with a pin texture and transform it so
        /// that it forms a pin of the right size.
        /// </summary>
        /// <param name="pinPos">The global position of the pin.</param>
        /// <param name="isSingletonPin">Whether this is a singleton pin. Singleton
        /// pins have their own pin size.</param>
        /// <returns>A translation, rotation and scaling matrix for a pin mesh
        /// with the given properties.</returns>
        private Matrix4x4 CalculatePinMatrix(Vector2 pinPos, bool isSingletonPin)
        {
            // Calc Pin Size
            float pinSize = isSingletonPin ? RenderSystem.const_circuitSingletonPinSize : RenderSystem.const_circuitPinSize;
            if (properties.beepOrigin)
                pinSize *= RenderSystem.const_circuitPinBeepOriginSizePercentage;
            else if (properties.singleton && (properties.beeping || properties.faulty))
                pinSize *= RenderSystem.const_circuitPinBeepHighlightSizePercentage;
            // Calc Matrix
            return Matrix4x4.TRS(new Vector3(pinPos.x, pinPos.y, RenderSystem.zLayer_pins + zOffset), Quaternion.identity, new Vector3(pinSize, pinSize, 1f));
        }

        /// <summary>
        /// Calculates the pin connector matrix from the positional information
        /// about the pin connector. We basically take a standard quad with a pin
        /// texture and transform it so that it forms a pin of the right size.
        /// </summary>
        /// <param name="pinPos">The global position of the pin center.</param>
        /// <returns>A translation, rotation and scaling matrix for a pin mesh
        /// with the given properties.</returns>
        private Matrix4x4 CalculatePinConnectorMatrix(Vector2 pinPos)
        {
            return Matrix4x4.TRS(new Vector3(pinPos.x, pinPos.y, RenderSystem.zLayer_circuits), Quaternion.identity, new Vector3(RenderSystem.const_circuitPinConnectorSize, RenderSystem.const_circuitPinConnectorSize, 1f));
        }

        /// <summary>
        /// Clears the matrices, so nothing gets rendered anymore.
        /// The lists can be filled with new data now.
        /// (Actually just sets the index to 0, so we don't draw
        /// anything anymore.)
        /// </summary>
        public void ClearMatrices()
        {
            currentIndex = 0;
            currentIndex_connectors = 0;
        }

        /// <summary>
        /// Applies the updates for the animations.
        /// </summary>
        /// <param name="animationStartTime">The start time of the animation.</param>
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

        /// <summary>
        /// Applies the timestamps for the movement offsets.
        /// </summary>
        /// <param name="movementStartTime">Start time of the animation.</param>
        /// <param name="movementDuration">Duration of the animation.</param>
        public void ApplyMovementTimestamps(float movementStartTime, float movementDuration)
        {
            propertyBlock_circuitMatrices_Pins.ApplyMovementTimestamp(movementStartTime, movementDuration);
            propertyBlock_circuitMatrices_PinConnectors.ApplyMovementTimestamp(movementStartTime, movementDuration);
        }

        /// <summary>
        /// Renders the pins.
        /// </summary>
        /// <param name="type">The current view type. Useful for deciding what to show.</param>
        /// <param name="firstRenderFrame">If this is the first frame of the new round.</param>
        public void Render(ViewType type, bool firstRenderFrame)
        {
            if (type == ViewType.Circular) return;
            if (RenderSystem.flag_showCircuitView == false) return;

            // Timestamp
            if (firstRenderFrame)
            {
                ApplyUpdates(RenderSystem.data_particleMovementFinishedTimestamp);
                ApplyMovementTimestamps(RenderSystem.animation_animationTriggerTimestamp, RenderSystem.data_hexagonalAnimationDuration);
            }

            // Pins
            int listDrawAmount = ((currentIndex - 1) / maxArraySize) + 1;
            if (currentIndex > 0)
            {
                for (int i = 0; i < listDrawAmount; i++)
                {
                    int count;
                    if (i < listDrawAmount - 1)
                        count = maxArraySize;
                    else
                        count = currentIndex % maxArraySize;
                    UnityEngine.Graphics.DrawMeshInstanced(pinQuad, 0, pinMaterial, circuitMatrices_Pins[i], count, propertyBlock_circuitMatrices_Pins.propertyBlock);
                }
            }

            // Pin Connectors
            int listDrawAmount_connectors = ((currentIndex_connectors - 1) / maxArraySize) + 1;
            if (currentIndex_connectors > 0)
            {
                for (int i = 0; i < listDrawAmount_connectors; i++)
                {
                    int count;
                    if (i < listDrawAmount_connectors - 1)
                        count = maxArraySize;
                    else
                        count = currentIndex_connectors % maxArraySize;
                    UnityEngine.Graphics.DrawMeshInstanced(pinQuad, 0, pinMaterial, circuitMatrices_PinConnectors[i], count, propertyBlock_circuitMatrices_PinConnectors.propertyBlock);
                }
            }
        }
    }

}