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
    /// Implementation of <see cref="IParticleGraphicsAdapter"/>.
    /// </summary>
    public class ParticleGraphicsAdapterImpl : IParticleGraphicsAdapter
    {

        // References
        public IParticleState particle;
        private RendererParticles renderer;

        // Defaults
        private static Color defColor = new Color(111f / 255f, 202f / 255f, 91f / 255f, 1f);

        // Data
        // Current Round
        public PositionSnap state_cur = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, true, ParticleMovement.Contracted, ParticleJointMovementState.None, 0f);
        public PositionSnap state_prev = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, true, ParticleMovement.Contracted, ParticleJointMovementState.None, 0f);

        // Graphical Data
        public bool graphics_isRegistered = false;
        public int graphics_listNumber = 0;
        public int graphics_listID = 0;
        public int graphics_globalID = 0;
        public RendererParticles_RenderBatch graphics_colorRenderer;
        public Color graphics_color;

        /// <summary>
        /// Possible movement phases of a particle.
        /// </summary>
        public enum ParticleMovement
        {
            Contracted,
            Expanded,
            Expanding,
            Contracting
        }

        /// <summary>
        /// A snap of a position that is used to determine the current state of the particle.
        /// </summary>
        public struct PositionSnap
        {
            public Vector2Int position1;        // Head
            public Vector2Int position2;        // Tail
            public bool isExpanded;
            public int globalExpansionOrContractionDir;
            public bool noAnimation;
            public ParticleMovement movement;
            public ParticleJointMovementState jointMovementState;
            public float timestamp;

            public PositionSnap(Vector2Int p1, Vector2Int p2, bool isExpanded, int globalExpansionOrContractionDir, bool noAnimation, ParticleMovement movement, ParticleJointMovementState jointMovementState, float timestamp)
            {
                this.position1 = p1;
                this.position2 = p2;
                this.isExpanded = isExpanded;
                this.globalExpansionOrContractionDir = globalExpansionOrContractionDir;
                this.noAnimation = noAnimation;
                this.movement = movement;
                this.jointMovementState = jointMovementState;
                this.timestamp = timestamp;
            }

            public static bool operator ==(PositionSnap s1, PositionSnap s2)
            {
                return s1.position1 == s2.position1 &&
                    s1.position2 == s2.position2 &&
                    s1.isExpanded == s2.isExpanded &&
                    s1.globalExpansionOrContractionDir == s2.globalExpansionOrContractionDir &&
                    s1.noAnimation == s2.noAnimation &&
                    s1.movement == s2.movement &&
                    s1.jointMovementState == s2.jointMovementState &&
                    s1.timestamp == s2.timestamp;
            }

            public static bool operator !=(PositionSnap s1, PositionSnap s2)
            {
                return s1.position1 != s2.position1 ||
                    s1.position2 != s2.position2 ||
                    s1.isExpanded != s2.isExpanded ||
                    s1.globalExpansionOrContractionDir != s2.globalExpansionOrContractionDir ||
                    s1.noAnimation != s2.noAnimation ||
                    s1.movement != s2.movement ||
                    s1.jointMovementState != s2.jointMovementState ||
                    s1.timestamp != s2.timestamp;
            }

            public override bool Equals(object s)
            {
                return s is PositionSnap && (PositionSnap)s == this;
            }

            /// <summary>
            /// Compares the positions and expansion states of
            /// the two given snapshots.
            /// </summary>
            /// <param name="s1">The first snapshot to compare.</param>
            /// <param name="s2">The second snapshot to compare.</param>
            /// <returns><c>true</c> if and only if the position and
            /// expansion state of the two given snapshots are equal.
            /// Note that if the snapshots are contracted, their tail
            /// positions might still differ.</returns>
            public static bool IsPositionEqual(PositionSnap s1, PositionSnap s2)
            {
                return s1.position1 == s2.position1 && s1.position2 == s2.position2 && s1.isExpanded == true && s2.isExpanded == true ||
                    s1.position1 == s2.position1 && s1.isExpanded == false && s2.isExpanded == false;
            }

            /// <summary>
            /// Dummy value to avoid warnings. Please do not use!
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return 42;
            }

            /// <summary>
            /// Dummy value to avoid warnings. Please do not use!
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (isExpanded) return "Head: " + position1 + ", Tail: " + position2;
                else return "Head/Tail: " + position1;
            }
        }

        public ParticleGraphicsAdapterImpl(IParticleState particle, RendererParticles renderer)
        {
            this.particle = particle;
            this.renderer = renderer;
        }

        /// <summary>
        /// Adds the particle to the renderer. Only needs to be called once to register a particle.
        /// </summary>
        /// <param name="movementState">The current movement state of the new particle.</param>
        public void AddParticle(ParticleMovementState movementState)
        {
            // Check if already added to the system
            if (graphics_isRegistered)
            {
                Log.Error("Error: Particle has already been added!");
            }

            // Add Particle Text UI
            WorldSpaceUIHandler.instance.AddParticleTextUI(particle, state_cur.position1);
            // Register Particle
            graphics_isRegistered = true;
            if (particle.IsParticleColorSet())
                graphics_color = particle.GetParticleColor();
            else
                graphics_color = defColor;
            renderer.Particle_Add(this);
            Update(true, movementState, true);
        }

        /// <summary>
        /// Removes a particle from the renderer. Only needs to be called once
        /// when the particle is removed from the system.
        /// </summary>
        public void RemoveParticle()
        {
            renderer.Particle_Remove(this);
            // Remove Particle Text UI
            WorldSpaceUIHandler.instance.RemoveParticleTextUI(particle);
        }

        public void Update(ParticleMovementState movementState)
        {
            Update(false, movementState, false);
        }

        private void Update(bool forceRenderUpdate, ParticleMovementState movementState, bool noAnimation)
        {
            // Previous Data
            state_prev = state_cur;
            // Current Data
            state_cur = new PositionSnap(movementState.posHead, movementState.posTail, movementState.isExpanded, movementState.expansionOrContractionDir, noAnimation, ParticleMovement.Contracted, noAnimation == false ? movementState.jointMovement : ParticleJointMovementState.None, Time.timeSinceLevelLoad);

            // Get Expanded State
            if (state_cur.isExpanded)
            {
                // Expanded
                if (state_prev.isExpanded || noAnimation)
                    state_cur.movement = ParticleMovement.Expanded;
                else
                    state_cur.movement = ParticleMovement.Expanding;
            }
            else
            {
                // Contracted
                if (state_prev.isExpanded == false || noAnimation)
                {
                    state_cur.movement = ParticleMovement.Contracted;
                }
                else
                {
                    state_cur.movement = ParticleMovement.Contracting;
                    state_cur.position2 = state_prev.position2; // Tail to previous Tail
                }
            }

            // Update Matrix
            if (PositionSnap.IsPositionEqual(state_cur, state_prev) == false
                || state_prev.movement == ParticleMovement.Contracting
                || state_prev.movement == ParticleMovement.Expanding
                || state_prev.jointMovementState.isJointMovement
                || state_cur.jointMovementState.isJointMovement
                || forceRenderUpdate) graphics_colorRenderer.UpdateMatrix(this, false);
        }

        private void Update(bool forceRenderUpdate, ParticleJointMovementState jointMovementState, bool noAnimation)
        {
            // Previous Data
            state_prev = state_cur;
            // Current Data
            state_cur = new PositionSnap(particle.Head(), particle.Tail(), particle.IsExpanded(), particle.GlobalHeadDirectionInt(), noAnimation, ParticleMovement.Contracted, noAnimation == false ? jointMovementState : ParticleJointMovementState.None, Time.timeSinceLevelLoad);

            // Get Expanded State
            if (state_cur.isExpanded)
            {
                // Expanded
                if (state_prev.isExpanded || noAnimation)
                    state_cur.movement = ParticleMovement.Expanded;
                else
                    state_cur.movement = ParticleMovement.Expanding;
            }
            else
            {
                // Contracted
                if (state_prev.isExpanded == false || noAnimation)
                {
                    state_cur.movement = ParticleMovement.Contracted;
                }
                else
                {
                    state_cur.movement = ParticleMovement.Contracting;
                    state_cur.position2 = state_prev.position2; // Tail to previous Tail
                }
            }

            // Update Matrix
            if (PositionSnap.IsPositionEqual(state_cur, state_prev) == false
                || state_prev.movement == ParticleMovement.Contracting
                || state_prev.movement == ParticleMovement.Expanding
                || state_prev.jointMovementState.isJointMovement
                || state_cur.jointMovementState.isJointMovement
                || forceRenderUpdate) graphics_colorRenderer.UpdateMatrix(this, false); //renderer.UpdateMatrix(this);
        }

        public void UpdateReset()
        {
            Update(true, ParticleJointMovementState.None, true);
        }

        public void UpdateReset(ParticleMovementState movementState)
        {
            Update(true, movementState, true);
        }

        public void BondUpdate(ParticleBondGraphicState bondState)
        {
            renderer.circuitAndBondRenderer.AddBond(bondState);
        }

        public void CircuitUpdate(ParticlePinGraphicState state)
        {
            renderer.circuitAndBondRenderer.AddCircuits(this, state, state_cur, RenderSystem.flag_partitionSetViewType);
        }

        public void SetParticleColor(Color color)
        {
            if (graphics_isRegistered) renderer.Particle_UpdateColor(this, graphics_color, color);
        }

        public void ClearParticleColor()
        {
            SetParticleColor(defColor);
        }

        public void ShowParticle()
        {
            throw new System.NotImplementedException();
        }

        public void HideParticle()
        {
            throw new System.NotImplementedException();
        }

    }

}
