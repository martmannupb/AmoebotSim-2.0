// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using AS2.UI;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// The renderer for the base particles and pins.
    /// Stores many matrices in which the data for the
    /// mesh instancing is stored and rendered with
    /// <c>UnityEngine.Graphics.DrawMeshInstanced(Mesh, int, Material, List{Matrix4x4})</c>.
    /// </summary>
    public class RendererParticles_RenderBatch
    {
    
        // Data _____
        // Particles
        private Dictionary<IParticleState, ParticleGraphicsAdapterImpl> particleToParticleGraphicalDataMap = new Dictionary<IParticleState, ParticleGraphicsAdapterImpl>();
        private List<ParticleGraphicsAdapterImpl> graphicalDataList = new List<ParticleGraphicsAdapterImpl>();
        // Graphics
        // Matrices
        private List<Matrix4x4[]> particleMatricesCircle_Contracted = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesCircle_Expanded = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesCircle_Expanding = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesCircle_Contracting = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesPins_Contracted = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesPins_Expanded = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesPins_Expanding = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesPins_Contracting = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Contracted = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Expanded = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Expanding = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Contracting = new List<Matrix4x4[]>();
        // Joint Movements
        private List<Vector3[]> particlePositionOffsets_jointMovementsInv = new List<Vector3[]>();
        private List<ParticleGraphicsAdapterImpl[]> particleReferences = new List<ParticleGraphicsAdapterImpl[]>();
        // PropertyBlocks
        private MaterialPropertyBlockData_Particles propertyBlock_circle_contracted = new MaterialPropertyBlockData_Particles();
        private MaterialPropertyBlockData_Particles propertyBlock_circle_expanded = new MaterialPropertyBlockData_Particles();
        private MaterialPropertyBlockData_Particles propertyBlock_circle_expanding = new MaterialPropertyBlockData_Particles();
        private MaterialPropertyBlockData_Particles propertyBlock_circle_contracting = new MaterialPropertyBlockData_Particles();
        private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_contracted = new MaterialPropertyBlockData_Particles();
        private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_expanded = new MaterialPropertyBlockData_Particles();
        private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_expanding = new MaterialPropertyBlockData_Particles();
        private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_contracting = new MaterialPropertyBlockData_Particles();
        // Materials
        private Material circuitHexPinMaterial;
        private Material circuitHexCircPinMaterial;
        private Material hexagonWithPinsMaterial;
        private Material hexagonCircWithPinsMaterial;


        // Precalculated Data _____
        // Meshes
        private Mesh mesh_circle_particle = MeshCreator_CircularView.GetMesh_ParticleOptimized();
        private Mesh mesh_circle_particleConnector = MeshCreator_CircularView.GetMesh_ParticleConnector();
        private Mesh mesh_hex_particle = MeshCreator_HexagonalView.GetMesh_MergingExpansionHexagon();
        // Defaults
        //Matrix4x4 matrixTRS_zero = Matrix4x4.TRS(new Vector3(float.MaxValue / 2f, float.MaxValue / 2f, 0f), Quaternion.identity, Vector3.zero);
        Matrix4x4 matrixTRS_zero = Matrix4x4.TRS(new Vector3(0f, 0f, -100f), Quaternion.identity, Vector3.zero);
        const int maxArraySize = 1023;

        // Dynamic Data _____
        private float curAnimationLength = 0f;
        private float jmInterpolation = 0f;

        // Settings _____
        public PropertyBlockData properties;

        /// <summary>
        /// An extendable struct that functions as the key for
        /// the mapping of particles to their render class.
        /// Particles with identical properties can be
        /// rendered in the same batch.
        /// </summary>
        public struct PropertyBlockData
        {
            /// <summary>
            /// The color of the rendered particle.
            /// </summary>
            public Color color;
            /// <summary>
            /// The number of pins on each side of the particle.
            /// </summary>
            public int pinsPerSide;

            public PropertyBlockData(Color color, int pinsPerMesh)
            {
                this.color = color;
                this.pinsPerSide = pinsPerMesh;
            }
        }

        public RendererParticles_RenderBatch(PropertyBlockData properties)
        {
            this.properties = properties;

            Init();
        }

        /// <summary>
        /// Initializes all the property blocks and materials.
        /// </summary>
        public void Init()
        {
            // PropertyBlocks
            propertyBlock_circle_contracted.ApplyColor(properties.color);
            propertyBlock_circle_expanded.ApplyColor(properties.color);
            propertyBlock_circle_expanding.ApplyColor(properties.color);
            propertyBlock_circle_contracting.ApplyColor(properties.color);
            propertyBlock_circle_contracted.ApplyUpdatedValues(false, 0, 0f, 0f, Vector3.right);
            propertyBlock_circle_expanded.ApplyUpdatedValues(true, 0, 1f, 1f, Vector3.right);
            propertyBlock_circle_expanding.ApplyUpdatedValues(true, 0, 0f, 1f, Vector3.right);
            propertyBlock_circle_contracting.ApplyUpdatedValues(true, 0, 1f, 0f, Vector3.right);
            propertyBlock_circle_connector_contracted.ApplyConnectorValues(0f, 0f, Vector3.right, Vector3.left);
            propertyBlock_circle_connector_expanded.ApplyConnectorValues(1f, 1f, Vector3.right, Vector3.left);
            propertyBlock_circle_connector_expanding.ApplyConnectorValues(0f, 1f, Vector3.right, Vector3.left);
            propertyBlock_circle_connector_contracting.ApplyConnectorValues(1f, 0f, Vector3.right, Vector3.left);

            // Circuit Pins
            // Generate Material
            circuitHexPinMaterial = TextureCreator.GetPinBorderMaterial(properties.pinsPerSide, ViewType.Hexagonal);
            circuitHexCircPinMaterial = TextureCreator.GetPinBorderMaterial(properties.pinsPerSide, ViewType.HexagonalCirc);
            // If circuits are currently not visible, get material for 0 pins
            int numPins = RenderSystem.flag_showCircuitView ? properties.pinsPerSide : 0;
            hexagonWithPinsMaterial = TextureCreator.GetHexagonWithPinsMaterial(numPins, ViewType.Hexagonal);
            hexagonCircWithPinsMaterial = TextureCreator.GetHexagonWithPinsMaterial(numPins, ViewType.HexagonalCirc);
        }

        /// <summary>
        /// Adds a particle to the render batch.
        /// </summary>
        /// <param name="graphicalData">The graphical data belonging to
        /// the particle that should be added.</param>
        /// <returns><c>true</c> if and only if the particle
        /// was added to this render batch.</returns>
        public bool Particle_Add(ParticleGraphicsAdapterImpl graphicalData)
        {
            if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle)) return false;

            if ((particleToParticleGraphicalDataMap.Count % maxArraySize) == 0)
            {
                // Create Matrix Arrays
                particleMatricesCircle_Contracted.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesCircle_Expanded.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesCircle_Expanding.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesCircle_Contracting.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesPins_Contracted.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesPins_Expanded.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesPins_Expanding.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesPins_Contracting.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesCircle_ConnectionMatrices_Contracted.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesCircle_ConnectionMatrices_Expanded.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesCircle_ConnectionMatrices_Expanding.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particleMatricesCircle_ConnectionMatrices_Contracting.Add(Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
                particlePositionOffsets_jointMovementsInv.Add(new Vector3[maxArraySize]);
                particleReferences.Add(new ParticleGraphicsAdapterImpl[maxArraySize]);
            }

            // References
            graphicalData.graphics_listNumber = particleMatricesCircle_Contracted.Count - 1;
            graphicalData.graphics_listID = particleToParticleGraphicalDataMap.Count % maxArraySize;
            graphicalData.graphics_globalID = particleToParticleGraphicalDataMap.Count;
            graphicalData.graphics_color = properties.color;
            graphicalData.graphics_colorRenderer = this;

            // Register Particle
            particleToParticleGraphicalDataMap.Add(graphicalData.particle, graphicalData);
            graphicalDataList.Add(graphicalData);
            UpdateMatrix(graphicalData, false);

            return true;
        }

        /// <summary>
        /// Removes a particle from the render batch. The last element
        /// in the last array takes its position afterwards, so that we
        /// still have valid arrays for the
        /// <c>UnityEngine.Graphics.DrawMeshInstanced(Mesh, int, Material, List{Matrix4x4})</c>
        /// method.
        /// </summary>
        /// <param name="graphicalData">The graphics adapter belonging to
        /// the particle that should be removed.</param>
        /// <returns><c>true</c> if and only if the particle
        /// was removed from this render batch.</returns>
        public bool Particle_Remove(ParticleGraphicsAdapterImpl graphicalData)
        {
            if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle) == false) return false;

            // Unset Reference
            graphicalData.graphics_colorRenderer = null;

            // Move last particle to the place of this particle
            ParticleGraphicsAdapterImpl lastParticleGraphicalData = graphicalDataList[graphicalDataList.Count - 1];
            int movedParticleIndex = graphicalDataList.IndexOf(graphicalData);
            graphicalDataList[movedParticleIndex] = lastParticleGraphicalData;
            // Update References
            int original_listNumber = lastParticleGraphicalData.graphics_listNumber;
            int original_listID = lastParticleGraphicalData.graphics_listID;
            lastParticleGraphicalData.graphics_listNumber = movedParticleIndex / maxArraySize;
            lastParticleGraphicalData.graphics_listID = movedParticleIndex % maxArraySize;
            lastParticleGraphicalData.graphics_globalID = movedParticleIndex;
            // Copy Matrices of lastParticleGraphicalData from original index to moved index
            CutAndCopyMatrices(original_listNumber, original_listID, lastParticleGraphicalData.graphics_listNumber, lastParticleGraphicalData.graphics_listID);

            // Unregister Particle
            graphicalDataList.RemoveAt(graphicalDataList.Count - 1);
            particleToParticleGraphicalDataMap.Remove(graphicalData.particle);
            // Lists became smaller, possibly delete list entry
            if (graphicalDataList.Count % maxArraySize == 0)
            {
                // Particle was only element in the last list
                // Delete list
                int indexOfLastList = particleMatricesCircle_Contracted.Count - 1;

                particleMatricesCircle_Contracted.RemoveAt(indexOfLastList);
                particleMatricesCircle_Expanded.RemoveAt(indexOfLastList);
                particleMatricesCircle_Expanding.RemoveAt(indexOfLastList);
                particleMatricesCircle_Contracting.RemoveAt(indexOfLastList);
                particleMatricesPins_Contracted.RemoveAt(indexOfLastList);
                particleMatricesPins_Expanded.RemoveAt(indexOfLastList);
                particleMatricesPins_Expanding.RemoveAt(indexOfLastList);
                particleMatricesPins_Contracting.RemoveAt(indexOfLastList);
                particleMatricesCircle_ConnectionMatrices_Contracted.RemoveAt(indexOfLastList);
                particleMatricesCircle_ConnectionMatrices_Expanded.RemoveAt(indexOfLastList);
                particleMatricesCircle_ConnectionMatrices_Expanding.RemoveAt(indexOfLastList);
                particleMatricesCircle_ConnectionMatrices_Contracting.RemoveAt(indexOfLastList);
                particlePositionOffsets_jointMovementsInv.RemoveAt(indexOfLastList);
                particleReferences.RemoveAt(indexOfLastList);

                // ..
            }

            return true;
        }

        /// <summary>
        /// Copies the values from a specific matrix id
        /// and position to another matrix id and position.
        /// Used for reordering matrix positions.
        /// </summary>
        /// <param name="orig_ListNumber">Current list index of the matrices.</param>
        /// <param name="orig_ListID">Current array index of the matrices.</param>
        /// <param name="moved_ListNumber">New list index of the matrices.</param>
        /// <param name="moved_ListID">New array index of the matrices.</param>
        private void CutAndCopyMatrices(int orig_ListNumber, int orig_ListID, int moved_ListNumber, int moved_ListID)
        {
            // 1. Move Matrices
            particleMatricesCircle_Contracted[moved_ListNumber][moved_ListID] = particleMatricesCircle_Contracted[orig_ListNumber][orig_ListID];
            particleMatricesCircle_Expanded[moved_ListNumber][moved_ListID] = particleMatricesCircle_Expanded[orig_ListNumber][orig_ListID];
            particleMatricesCircle_Expanding[moved_ListNumber][moved_ListID] = particleMatricesCircle_Expanding[orig_ListNumber][orig_ListID];
            particleMatricesCircle_Contracting[moved_ListNumber][moved_ListID] = particleMatricesCircle_Contracting[orig_ListNumber][orig_ListID];
            particleMatricesPins_Contracted[moved_ListNumber][moved_ListID] = particleMatricesPins_Contracted[orig_ListNumber][orig_ListID];
            particleMatricesPins_Expanded[moved_ListNumber][moved_ListID] = particleMatricesPins_Expanded[orig_ListNumber][orig_ListID];
            particleMatricesPins_Expanding[moved_ListNumber][moved_ListID] = particleMatricesPins_Expanding[orig_ListNumber][orig_ListID];
            particleMatricesPins_Contracting[moved_ListNumber][moved_ListID] = particleMatricesPins_Contracting[orig_ListNumber][orig_ListID];
            particleMatricesCircle_ConnectionMatrices_Contracted[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Contracted[orig_ListNumber][orig_ListID];
            particleMatricesCircle_ConnectionMatrices_Expanded[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Expanded[orig_ListNumber][orig_ListID];
            particleMatricesCircle_ConnectionMatrices_Expanding[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Expanding[orig_ListNumber][orig_ListID];
            particleMatricesCircle_ConnectionMatrices_Contracting[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Contracting[orig_ListNumber][orig_ListID];
            particlePositionOffsets_jointMovementsInv[moved_ListNumber][moved_ListID] = particlePositionOffsets_jointMovementsInv[orig_ListNumber][orig_ListID];
            particleReferences[moved_ListNumber][moved_ListID] = particleReferences[orig_ListNumber][orig_ListID];

            // 2. Reset Matrices
            ResetMatrices(orig_ListNumber, orig_ListID);
        }

        /// <summary>
        /// Zeroes the values at a specific matrix id and position.
        /// If this was rendered, nothing would show up
        /// (but at the position (0,0) the mesh would still be
        /// drawn and have performance impact).
        /// </summary>
        /// <param name="listNumber">The list index of the matrices to reset.</param>
        /// <param name="listID">The array index of the matrices to reset.</param>
        private void ResetMatrices(int listNumber, int listID)
        {
            particleMatricesCircle_Contracted[listNumber][listID] = matrixTRS_zero;
            particleMatricesCircle_Expanded[listNumber][listID] = matrixTRS_zero;
            particleMatricesCircle_Expanding[listNumber][listID] = matrixTRS_zero;
            particleMatricesCircle_Contracting[listNumber][listID] = matrixTRS_zero;
            particleMatricesPins_Contracted[listNumber][listID] = matrixTRS_zero;
            particleMatricesPins_Expanded[listNumber][listID] = matrixTRS_zero;
            particleMatricesPins_Expanding[listNumber][listID] = matrixTRS_zero;
            particleMatricesPins_Contracting[listNumber][listID] = matrixTRS_zero;
            particleMatricesCircle_ConnectionMatrices_Contracted[listNumber][listID] = matrixTRS_zero;
            particleMatricesCircle_ConnectionMatrices_Expanded[listNumber][listID] = matrixTRS_zero;
            particleMatricesCircle_ConnectionMatrices_Expanding[listNumber][listID] = matrixTRS_zero;
            particleMatricesCircle_ConnectionMatrices_Contracting[listNumber][listID] = matrixTRS_zero;
            particlePositionOffsets_jointMovementsInv[listNumber][listID] = Vector3.zero;
            particleReferences[listNumber][listID] = null;
        }

        /// <summary>
        /// Updates the matrices for a specific particle. Usually done
        /// once per round (if <paramref name="executeJointMovement"/> is <c>false</c>).
        /// This method is also used for continuously updating the joint movements each
        /// frame (if <paramref name="executeJointMovement"/> is set to <c>true</c>).
        /// </summary>
        /// <param name="gd">The graphics adapter blonging to the particle to update.</param>
        /// <param name="executeJointMovement">Whether this is the execution of a joint
        /// movement which is done repeatedly.
        /// (this might seem a little odd, but the system was added later and seemed to be easily implementable it this way)</param>
        public void UpdateMatrix(ParticleGraphicsAdapterImpl gd, bool executeJointMovement)
        {
            // World positions of the particle's head and tail
            Vector3 particle_position1world = AmoebotFunctions.GridToWorldPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles);
            Vector3 particle_position2world = AmoebotFunctions.GridToWorldPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles);
            // Pin positions are the same but with different Z layer
            Vector3 pin_position1world = new Vector3(particle_position1world.x, particle_position1world.y, RenderSystem.zLayer_pins);
            Vector3 pin_position2world = new Vector3(particle_position2world.x, particle_position2world.y, RenderSystem.zLayer_pins);

            // Determine joint movement offset vector
            if (executeJointMovement)
            {
                // Compute joint movement offset by interpolation (if animation is on)
                // Stored offset points from end position to start position
                Vector3 interpolatedOffset;
                if (RenderSystem.animationsOn)
                    interpolatedOffset = (1f - jmInterpolation) * particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID];
                else
                    interpolatedOffset = 0f * particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID];
                // Add offset to particle and pin positions
                particle_position1world += interpolatedOffset;
                particle_position2world += interpolatedOffset;
                pin_position1world = new Vector3(particle_position1world.x, particle_position1world.y, RenderSystem.zLayer_pins);
                pin_position2world = new Vector3(particle_position2world.x, particle_position2world.y, RenderSystem.zLayer_pins);
            }
            else
            {
                // Reset Matrices
                ResetMatrices(gd.graphics_listNumber, gd.graphics_listID);
                particleReferences[gd.graphics_listNumber][gd.graphics_listID] = gd;

                // Joint Movements
                if (gd.state_cur.jointMovementState.isJointMovement)
                {
                    // Calculate joint movement position offset
                    // Points from end position to start position
                    Vector2Int jointMovementPositionOffsetInv = new Vector2Int(-gd.state_cur.jointMovementState.jointMovementOffset.x, -gd.state_cur.jointMovementState.jointMovementOffset.y);
                    // Convert to world coordinates and store the offset
                    Vector3 absPositionOffsetInv = AmoebotFunctions.GridToWorldPositionVector2(jointMovementPositionOffsetInv);
                    particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID] = absPositionOffsetInv;
                }
                else
                {
                    particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID] = Vector3.zero;
                }
            }
        
            // Calculate Rotation
            Quaternion rotation = Quaternion.identity;
            if (gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanded || gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding)
            {
                rotation = Quaternion.Euler(0f, 0f, 60f * gd.state_cur.globalExpansionOrContractionDir) * Quaternion.identity;
            }
            else if (gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting)
            {
                // Apply Rotation
                rotation = Quaternion.Euler(0f, 0f, (60f * gd.state_cur.globalExpansionOrContractionDir) % 360f) * Quaternion.identity;
            }

            // Ignore contracting and expanding movement if animations are off
            ParticleGraphicsAdapterImpl.ParticleMovement movement = gd.state_cur.movement;
            if (RenderSystem.animationsOn == false)
            {
                if (movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting)
                    movement = ParticleGraphicsAdapterImpl.ParticleMovement.Contracted;
                else if (movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding)
                    movement = ParticleGraphicsAdapterImpl.ParticleMovement.Expanded;
            }

            // Update Matrices
            switch (movement)
            {
                case ParticleGraphicsAdapterImpl.ParticleMovement.Contracted:
                    particleMatricesCircle_Contracted[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position1world, rotation, Vector3.one);
                    particleMatricesPins_Contracted[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position1world, rotation, Vector3.one);
                    WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position1world);
                    break;
                case ParticleGraphicsAdapterImpl.ParticleMovement.Expanded:
                    particleMatricesCircle_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world, rotation, Vector3.one);
                    particleMatricesPins_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position2world, rotation, Vector3.one);
                    particleMatricesCircle_ConnectionMatrices_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world + new Vector3(0f, 0f, 0.1f), rotation, Vector3.one);
                    WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position2world);
                    break;
                case ParticleGraphicsAdapterImpl.ParticleMovement.Expanding:
                    particleMatricesCircle_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world, rotation, Vector3.one);
                    particleMatricesPins_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position2world, rotation, Vector3.one);
                    particleMatricesCircle_ConnectionMatrices_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world + new Vector3(0f, 0f, 0.1f), rotation, Vector3.one);
                    WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position2world);
                    break;
                case ParticleGraphicsAdapterImpl.ParticleMovement.Contracting:
                    particleMatricesCircle_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position1world, rotation, Vector3.one);
                    particleMatricesPins_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position1world, rotation, Vector3.one);
                    particleMatricesCircle_ConnectionMatrices_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position1world + new Vector3(0f, 0f, 0.1f), rotation, Vector3.one);
                    WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position1world);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Render loop of the particles. Calculates all timings
        /// of movements and draws the particles on the screen.
        /// </summary>
        /// <param name="viewType">The view type that should be used
        /// to visualize the particles.</param>
        public void Render(ViewType viewType)
        {
            // 1. Update properties
            curAnimationLength = Mathf.Clamp(RenderSystem.data_hexagonalAnimationDuration, 0f, Time.fixedDeltaTime);
            if (RenderSystem.flag_particleRoundOver)
            {
                RenderSystem.animation_animationTriggerTimestamp = Time.timeSinceLevelLoad;
                // Update PropertyBlocks timestamps (for animations)
                propertyBlock_circle_contracted.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                propertyBlock_circle_expanded.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                propertyBlock_circle_expanding.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                propertyBlock_circle_contracting.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                propertyBlock_circle_connector_contracted.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                propertyBlock_circle_connector_expanded.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                propertyBlock_circle_connector_expanding.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                propertyBlock_circle_connector_contracting.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
                RenderSystem.data_particleMovementFinishedTimestamp = RenderSystem.animation_animationTriggerTimestamp + curAnimationLength;
            }

            // 2. Update joint movement positions
            // Update interpolation value
            jmInterpolation = Library.InterpolationConstants.SmoothLerp(RenderSystem.animation_curAnimationPercentage);
            // Update all JM positions
            for (int i = 0; i < particlePositionOffsets_jointMovementsInv.Count; i++)
            {
                Vector3[] offsets = particlePositionOffsets_jointMovementsInv[i];
                for (int j = 0; j < offsets.Length; j++)
                {
                    Vector3 offset = offsets[j];
                    if (offset != Vector3.zero)
                    {
                        // We have a joint movement
                        // Interpolate
                        UpdateMatrix(particleReferences[i][j], true);
                    }
                }
            }

            // 3. Render
            switch (viewType)
            {
                case ViewType.Hexagonal:
                case ViewType.HexagonalCirc:
                    Render_Hexagonal(viewType);
                    break;
                case ViewType.Circular:
                    Render_Circular();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Hexagonal particle drawing. Part of the render loop.
        /// </summary>
        /// <param name="viewType">The view type used to
        /// visualize the system. The only recognized values
        /// are <see cref="ViewType.Hexagonal"/> and
        /// <see cref="ViewType.HexagonalCirc"/>.</param>
        private void Render_Hexagonal(ViewType viewType)
        {
            for (int i = 0; i < particleMatricesCircle_Contracted.Count; i++)
            {
                // Find out how many matrices are used in the array
                int arrayLength;
                if (i == particleMatricesCircle_Contracted.Count - 1)
                    arrayLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
                else
                    arrayLength = maxArraySize;

                // Particles (previous mat: MaterialDatabase.material_hexagonal_particleCombined)
                Material mat = viewType == ViewType.Hexagonal ? hexagonWithPinsMaterial : hexagonCircWithPinsMaterial;
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Contracted[i], arrayLength, propertyBlock_circle_contracted.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Expanded[i], arrayLength, propertyBlock_circle_expanded.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Expanding[i], arrayLength, propertyBlock_circle_expanding.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Contracting[i], arrayLength, propertyBlock_circle_contracting.propertyBlock);
                // Pins
                if (RenderSystem.flag_showCircuitView)
                {
                    Material matCirc = viewType == ViewType.Hexagonal ? circuitHexPinMaterial : circuitHexCircPinMaterial;
                    Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Contracted[i], arrayLength, propertyBlock_circle_contracted.propertyBlock);
                    Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Expanded[i], arrayLength, propertyBlock_circle_expanded.propertyBlock);
                    Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Expanding[i], arrayLength, propertyBlock_circle_expanding.propertyBlock);
                    Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Contracting[i], arrayLength, propertyBlock_circle_contracting.propertyBlock);
                }
            }
        }

        /// <summary>
        /// Circular particle drawing. Part of the render loop.
        /// </summary>
        private void Render_Circular()
        {
            // Outer Ring visible?
            if(RenderSystem.flag_showCircuitViewOuterRing && propertyBlock_circle_contracted.GetCurrentOuterCircleWidthPercentage() != 1f
                || RenderSystem.flag_showCircuitViewOuterRing == false && propertyBlock_circle_contracted.GetCurrentOuterCircleWidthPercentage() != 0f)
            {
                float val = RenderSystem.flag_showCircuitViewOuterRing ? 1f : 0f;
                propertyBlock_circle_contracted.ApplyOuterCircleWidthPercentage(val);
                propertyBlock_circle_expanded.ApplyOuterCircleWidthPercentage(val);
                propertyBlock_circle_expanding.ApplyOuterCircleWidthPercentage(val);
                propertyBlock_circle_contracting.ApplyOuterCircleWidthPercentage(val);
            }

            // Connectors
            for (int i = 0; i < particleMatricesCircle_Contracted.Count; i++)
            {
                // Find out how many matrices are used in the array
                int arrayLength;
                if (i == particleMatricesCircle_Contracted.Count - 1) arrayLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
                else arrayLength = maxArraySize;

                // Particle Connectors
                Graphics.DrawMeshInstanced(mesh_circle_particleConnector, 0, MaterialDatabase.material_circular_particleCompleteConnector, particleMatricesCircle_ConnectionMatrices_Expanded[i], arrayLength, propertyBlock_circle_connector_expanded.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_circle_particleConnector, 0, MaterialDatabase.material_circular_particleCompleteConnector, particleMatricesCircle_ConnectionMatrices_Expanding[i], arrayLength, propertyBlock_circle_connector_expanding.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_circle_particleConnector, 0, MaterialDatabase.material_circular_particleCompleteConnector, particleMatricesCircle_ConnectionMatrices_Contracting[i], arrayLength, propertyBlock_circle_connector_contracting.propertyBlock);
            }

            // Particles
            for (int i = 0; i < particleMatricesCircle_Contracted.Count; i++)
            {
                int arrayLength;
                if (i == particleMatricesCircle_Contracted.Count - 1) arrayLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
                else arrayLength = maxArraySize;

                // Particles
                Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Contracted[i], arrayLength, propertyBlock_circle_contracted.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Expanded[i], arrayLength, propertyBlock_circle_expanded.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Expanding[i], arrayLength, propertyBlock_circle_expanding.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Contracting[i], arrayLength, propertyBlock_circle_contracting.propertyBlock);
            }
        }

        /// <summary>
        /// Changes the visibility setting of pins in the base
        /// hexagon or round hexagon view mode.
        /// </summary>
        /// <param name="visible">Whether the pins should be
        /// visible.</param>
        public void SetPinsVisible(bool visible)
        {
            // If pins should not be shown, simply use a material for 0 pins
            int numPins = visible ? properties.pinsPerSide : 0;

            hexagonWithPinsMaterial = TextureCreator.GetHexagonWithPinsMaterial(numPins, ViewType.Hexagonal);
            hexagonCircWithPinsMaterial = TextureCreator.GetHexagonWithPinsMaterial(numPins, ViewType.HexagonalCirc);
            
        }

    }

}
