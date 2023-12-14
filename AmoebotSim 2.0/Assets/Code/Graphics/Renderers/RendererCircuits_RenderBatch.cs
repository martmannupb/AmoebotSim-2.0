using System.Collections;
using System.Collections.Generic;
using AS2.UI;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Renderer for the circuits. Each instance of this class renders circuit
    /// lines with the same properties (like color, type, etc.) with Unity's
    /// instanced drawing. The class handles circuit lines inside of particles,
    /// circuit connections between the particles and bonds. Each instance only
    /// renders one type of line.
    /// </summary>
    public class RendererCircuits_RenderBatch : IGenerateDynamicMesh
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
        private Mesh circuitQuad;
        const int maxArraySize = 1023;
        private Material lineMaterial;
        private float zOffset = 0f;
        private float lineWidth = 0f;

        // Settings _____
        public PropertyBlockData properties;

        /// <summary>
        /// An extendable struct that functions as the key for the
        /// mapping of particles to their render class.
        /// </summary>
        public struct PropertyBlockData
        {
            /// <summary>
            /// The type of line that is being drawn.
            /// </summary>
            public enum LineType
            {
                /// <summary>
                /// Circuit connection lines inside of a particle.
                /// </summary>
                InternalLine,
                /// <summary>
                /// Circuit lines connecting particles.
                /// </summary>
                ExternalLine,
                /// <summary>
                /// Bonds in the hexagonal view mode.
                /// </summary>
                BondHexagonal,
                /// <summary>
                /// Bonds in the graph view mode.
                /// </summary>
                BondCircular
            }

            /// <summary>
            /// Simulation state for which the lines should be drawn.
            /// </summary>
            public enum ActiveState
            {
                /// <summary>
                /// Simulation is running.
                /// </summary>
                SimActive,
                /// <summary>
                /// Simulation is paused.
                /// </summary>
                SimPaused,
                /// <summary>
                /// Simulation is running or paused
                /// (used when both are rendered the same).
                /// </summary>
                SimActiveOrPaused
            }

            public Color color;
            public LineType lineType;
            /// <summary>
            /// <c>true</c> if this line should appear after a delay because
            /// the particle is performing a movement.
            /// </summary>
            public bool delayed;
            /// <summary>
            /// Whether this line belongs to a circuit that is currently beeping.
            /// </summary>
            public bool beeping;
            /// <summary>
            /// <c>true</c> if the start and end points of this line are
            /// animated separately and outside of the shader. This is only
            /// the case for moving bonds.
            /// </summary>
            public bool animationUpdatedManually;
            /// <summary>
            /// The global offset by which this line should move during the
            /// animation phase (uses the shader animation).
            /// </summary>
            public Vector2 animationOffset;
            /// <summary>
            /// Determines in which state of the simulator this line
            /// should be visible.
            /// </summary>
            public ActiveState activeState;

            public PropertyBlockData(Color color, LineType lineType, bool delayed, bool beeping, bool animationUpdatedManually) : this(color, lineType, delayed, beeping, animationUpdatedManually, Vector2.zero) { }
            public PropertyBlockData(Color color, LineType lineType, bool delayed, bool beeping, bool animationUpdatedManually, Vector2 animationOffset, ActiveState activeState = ActiveState.SimActiveOrPaused)
            {
                this.color = color;
                this.lineType = lineType;
                this.delayed = delayed;
                this.beeping = beeping;
                this.animationUpdatedManually = animationUpdatedManually;
                this.animationOffset = animationOffset;
                this.activeState = activeState;
            }
        }

        public RendererCircuits_RenderBatch(PropertyBlockData properties)
        {
            this.properties = properties;

            Init();
        }

        /// <summary>
        /// Initializes the materials based on the circuit render batch type.
        /// Also sets certain parameters for the property blocks and general
        /// settings of this class.
        /// </summary>
        public void Init()
        {
            // Set Material
            switch (properties.lineType)
            {
                case PropertyBlockData.LineType.InternalLine:
                    lineMaterial = MaterialDatabase.material_circuit_line_movement;
                    break;
                case PropertyBlockData.LineType.ExternalLine:
                    lineMaterial = RenderSystem.flag_circuitBorderActive ? MaterialDatabase.material_circuit_lineConnector_movement : MaterialDatabase.material_circuit_line_movement;
                    break;
                case PropertyBlockData.LineType.BondHexagonal:
                    lineMaterial = MaterialDatabase.material_bond_lineHexagonal_movement;
                    break;
                case PropertyBlockData.LineType.BondCircular:
                    lineMaterial = MaterialDatabase.material_bond_lineCircular_movement;
                    break;
                default:
                    break;
            }
            
            // Set Colors
            if (properties.lineType != PropertyBlockData.LineType.BondHexagonal && properties.lineType != PropertyBlockData.LineType.BondCircular)
            {
                if (properties.beeping && properties.activeState == PropertyBlockData.ActiveState.SimPaused)
                {
                    // Special Case: Paused Beeps
                    propertyBlock_circuitMatrices_Lines.ApplyColor(Color.white);
                    propertyBlock_circuitMatrices_Lines.ApplyColorSecondary(properties.color);
                }
                else if (properties.beeping && properties.activeState == PropertyBlockData.ActiveState.SimActive)
                {
                    // This is a beep flash highlight, which should always be white
                    propertyBlock_circuitMatrices_Lines.ApplyColor(Color.white);
                }
                else
                    propertyBlock_circuitMatrices_Lines.ApplyColor(properties.color); // Circuit Color stays
            }
            // Beeping Properties
            if (properties.beeping)
            {
                switch (properties.activeState)
                {
                    case PropertyBlockData.ActiveState.SimActive:
                    case PropertyBlockData.ActiveState.SimActiveOrPaused:
                        // Running Mode
                        propertyBlock_circuitMatrices_Lines.ApplyTexture(lineMaterial.GetTexture("_Texture2D"));
                        lineMaterial = MaterialDatabase.material_circuit_beep;
                        zOffset = -0.1f;
                        break;
                    case PropertyBlockData.ActiveState.SimPaused:
                        // Paused Mode
                        //propertyBlock_circuitMatrices_Lines.ApplyTexture(lineMaterial.GetTexture("_Texture2D")); // change this
                        lineMaterial = MaterialDatabase.material_circuit_beepPaused;
                        zOffset = -0.2f;
                        break;
                    default:
                        break;
                }
                
            }
            propertyBlock_circuitMatrices_Lines.ApplyMovementOffset(properties.animationOffset);

            // Line Width
            switch (properties.lineType)
            {
                case PropertyBlockData.LineType.InternalLine:
                    lineWidth = RenderSystem.const_circuitLineWidth;
                    break;
                case PropertyBlockData.LineType.ExternalLine:
                    lineWidth = RenderSystem.const_circuitConnectorLineWidth;
                    break;
                case PropertyBlockData.LineType.BondHexagonal:
                    lineWidth = RenderSystem.const_bondsLineWidthHex;
                    break;
                case PropertyBlockData.LineType.BondCircular:
                    lineWidth = RenderSystem.const_bondsLineWidthCirc;
                    break;
                default:
                    break;
            }
            // Special case: Use wider lines for beeping circuits when the simulation is paused
            if (properties.beeping && properties.activeState == PropertyBlockData.ActiveState.SimPaused)
                lineWidth = RenderSystem.const_circuitConnectorLineWidth;

            // Generate Mesh
            RegenerateMeshes();
        }

        public void RegenerateMeshes()
        {
            circuitQuad = Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
            if (RenderSystem.const_mesh_useManualBoundingBoxRadius)
                circuitQuad = Library.MeshConstants.AddManualBoundsToMesh(circuitQuad, Vector3.zero, RenderSystem.const_mesh_boundingBoxRadius, true);
        }

        /// <summary>
        /// Adds a line from A to B.
        /// </summary>
        /// <param name="globalLineStartPos">The global start position A of the line.</param>
        /// <param name="globalLineEndPos">The global end position B of the line.</param>
        /// <returns>The batch index of the new line in this batch.</returns>
        public RenderBatchIndex AddLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos)
        {
            if (currentIndex >= maxArraySize * circuitMatrices_Lines.Count)
            {
                // Add an Array
                circuitMatrices_Lines.Add(new Matrix4x4[maxArraySize]);
            }
            int listNumber = currentIndex / maxArraySize;
            int listIndex = currentIndex % maxArraySize;
            circuitMatrices_Lines[listNumber][listIndex] = CalculateLineMatrix(globalLineStartPos, globalLineEndPos);
            currentIndex++;
            return new RenderBatchIndex(listNumber, listIndex);
        }

        /// <summary>
        /// Updates a line from A to B.
        /// </summary>
        /// <param name="globalLineStartPos">The start position A of the line.</param>
        /// <param name="globalLineEndPos">The end position B of the line.</param>
        /// <param name="index">The index of the line to update.</param>
        public void UpdateLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, RenderBatchIndex index)
        {
            if (index.isValid == false || index.listNumber < 0 || index.listNumber >= circuitMatrices_Lines.Count || index.listIndex < 0 || index.listIndex >= circuitMatrices_Lines[index.listNumber].Length)
            {
                Log.Error("Circuit Render Batch: UpdateLine: Array out of bounds.");
                return;
            }
            circuitMatrices_Lines[index.listNumber][index.listIndex] = CalculateLineMatrix(globalLineStartPos, globalLineEndPos);
        }

        /// <summary>
        /// Adds a animated line from A to B which can move to the
        /// line from A' to B'.
        /// </summary>
        /// <param name="globalLineStartPos">The start position A of the line at the beginning of the animation.</param>
        /// <param name="globalLineEndPos">The end position B of the line at the beginning of the animation.</param>
        /// <param name="globalLineStartPos2">The start position A' of the line at the end of the animation.</param>
        /// <param name="globalLineEndPos2">The end position B' of the line at the end of the animation.</param>
        public void AddManuallyUpdatedLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, Vector2 globalLineStartPos2, Vector2 globalLineEndPos2)
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

        /// <summary>
        /// Calculates the line matrix from the positional information about the line.
        /// We basically take a standard quad and transform it so that it forms a line.
        /// The pivot of the quad must be at the center of the quad's left side.
        /// </summary>
        /// <param name="posStart">The start position of the line.</param>
        /// <param name="posEnd">The end position of the line.</param>
        /// <returns>A transformation, rotation, scaling matrix for the new line.</returns>
        private Matrix4x4 CalculateLineMatrix(Vector2 posStart, Vector2 posEnd)
        {
            Vector2 vec = posEnd - posStart;
            float length = vec.magnitude;
            float z;
            if (properties.lineType == PropertyBlockData.LineType.BondHexagonal || properties.lineType == PropertyBlockData.LineType.BondCircular)
                z = RenderSystem.zLayer_bonds;
            else
                z = RenderSystem.zLayer_circuits + zOffset;
            Quaternion q = Quaternion.FromToRotation(Vector2.right, vec);
            if (q.eulerAngles.y >= 179.999)
                q = AmoebotFunctions.rotation_180; // Hotfix for wrong axis rotation for 180 degrees
            return Matrix4x4.TRS(new Vector3(posStart.x, posStart.y, z), q, new Vector3(length, lineWidth, 1f));
        }

        /// <summary>
        /// Calculates the interpolated line matrices for a point in time of an animation.
        /// This should done every frame if the animations are running.
        /// <para>
        /// DEPRECATED: Try to replace this with the new shader animations. This is more performant and straightforward.
        /// </para>
        /// </summary>
        private void CalculateAnimationFrame()
        {
            float interpolationPercentage;
            if (properties.animationUpdatedManually && RenderSystem.animationsOn)
                interpolationPercentage = Library.InterpolationConstants.SmoothLerp(RenderSystem.animation_curAnimationPercentage);
            else
                interpolationPercentage = 1f;
            for (int i = 0; i < currentIndex; i++)
            {
                int listNumber = i / maxArraySize;
                int listIndex = i % maxArraySize;
                Vector2 posStart1 = startPositions1[listNumber][listIndex];
                Vector2 posEnd1 = endPositions1[listNumber][listIndex];
                Vector2 posStart2 = startPositions2[listNumber][listIndex];
                Vector2 posEnd2 = endPositions2[listNumber][listIndex];
                circuitMatrices_Lines[listNumber][listIndex] = CalculateLineMatrix(posStart1 + interpolationPercentage * (posStart2 - posStart1), posEnd1 + interpolationPercentage * (posEnd2 - posEnd1));
            }
        }


        /// <summary>
        /// Clears the matrices, so nothing gets rendered anymore.
        /// The lists can be filled with new data now.
        /// (Actually just sets the index to 0, so we don't draw anything anymore.)
        /// </summary>
        public void ClearMatrices()
        {
            currentIndex = 0;
        }

        private float lastBeepStartTime;

        /// <summary>
        /// Applies the new data for the timing of animations and beeps.
        /// </summary>
        /// <param name="animationStartTime">Start time of the animation.</param>
        /// <param name="beepStartTime">Start time of the beeps.</param>
        public void ApplyUpdates(float animationStartTime, float beepStartTime)
        {
            if (properties.beeping && properties.activeState != PropertyBlockData.ActiveState.SimPaused)
            {
                // Beeping Animation
                float halfAnimationDuration = RenderSystem.data_circuitBeepDuration * 0.5f;
                // The shader will animate from alpha 0 to 1 and back to 0, starting half
                // the duration before the animation trigger time
                propertyBlock_circuitMatrices_Lines.ApplyAlphaPercentagesToBlock(0f, 1f);
                propertyBlock_circuitMatrices_Lines.ApplyAnimationTimestamp(beepStartTime + halfAnimationDuration, halfAnimationDuration);
                lastBeepStartTime = beepStartTime;
            }
            else
            {
                // Static / Moving Animation (and special beeping paused case)
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

        /// <summary>
        /// Applies the timestamps for the movement offsets.
        /// </summary>
        /// <param name="movementStartTime">Start time of the animation.</param>
        /// <param name="movementDuration">Duration of the animation.</param>
        public void ApplyMovementTimestamps(float movementStartTime, float movementDuration)
        {
            propertyBlock_circuitMatrices_Lines.ApplyMovementTimestamp(movementStartTime, movementDuration);
        }

        /// <summary>
        /// Draws the circuits to the screen based on current settings.
        /// </summary>
        /// <param name="type">The view type of the system. Useful for deciding
        /// what should be shown.</param>
        /// <param name="firstRenderFrame">Set to <c>true</c> if a new round has
        /// just begun, so that the new timing settings can be applied.</param>
        public void Render(ViewType type, bool firstRenderFrame)
        {
            // Visibility Check
            bool simRunning = AmoebotSimulator.instance != null && AmoebotSimulator.instance.running;
            switch (properties.activeState)
            {
                case PropertyBlockData.ActiveState.SimActive:
                    if (simRunning == false) return;
                    break;
                case PropertyBlockData.ActiveState.SimPaused:
                    if (simRunning) return;
                    break;
                case PropertyBlockData.ActiveState.SimActiveOrPaused:
                    // Show in both states
                    break;
                default:
                    break;
            }
            switch (type)
            {
                case ViewType.Hexagonal:
                case ViewType.HexagonalCirc:
                    if (properties.lineType == PropertyBlockData.LineType.BondCircular) return;
                    if (properties.lineType == PropertyBlockData.LineType.InternalLine || properties.lineType == PropertyBlockData.LineType.ExternalLine)
                    {
                        // Circuits
                        if (RenderSystem.flag_showCircuitView == false) return;
                    }
                    else
                    {
                        // Bonds
                        if (RenderSystem.flag_showBonds == false) return;
                    }
                    break;
                case ViewType.Circular:
                    if (properties.lineType != PropertyBlockData.LineType.BondCircular || RenderSystem.flag_showBonds == false) return;
                    break;
                default:
                    break;
            }
        

            // Timestamp
            if (firstRenderFrame)
            {
                ApplyUpdates(RenderSystem.data_particleMovementFinishedTimestamp, RenderSystem.data_particleMovementFinishedTimestamp + RenderSystem.data_circuitAnimationDuration);
                ApplyMovementTimestamps(RenderSystem.animation_animationTriggerTimestamp, RenderSystem.data_hexagonalAnimationDuration);
            }
            else if (properties.beeping && properties.activeState != PropertyBlockData.ActiveState.SimPaused &&  RenderSystem.data_circuitBeepRepeatOn && lastBeepStartTime + RenderSystem.data_circuitBeepDuration < Time.timeSinceLevelLoad)
            {
                // We show beeps and have shown the beep already
                // Repeat Beep
                ApplyUpdates(RenderSystem.data_particleMovementFinishedTimestamp, lastBeepStartTime + RenderSystem.data_circuitBeepRepeatDelay);
            }

            // Null Check
            if (currentIndex == 0) return;

            // Animations
            if (properties.animationUpdatedManually) CalculateAnimationFrame();

            int listDrawAmount = ((currentIndex - 1) / maxArraySize) + 1;
            for (int i = 0; i < listDrawAmount; i++)
            {
                int count;
                if (i < listDrawAmount - 1) count = maxArraySize;
                else count = currentIndex % maxArraySize;

                UnityEngine.Graphics.DrawMeshInstanced(circuitQuad, 0, lineMaterial, circuitMatrices_Lines[i], count, propertyBlock_circuitMatrices_Lines.propertyBlock);
            }
        }
    }

}