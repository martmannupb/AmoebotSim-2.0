// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// The base class of all particle rendering.
    /// Particles that are added to the system give continuous updates
    /// on their state over the <see cref="ParticleGraphicsAdapterImpl"/>
    /// class, while the system handles the necessary updates of the 
    /// visual data to perform the display on the screen.
    /// </summary>
    public class RendererParticles
    {

        public static RendererParticles instance;

        // Renderers
        // Particles ===============
        /// <summary>
        /// Collection of batches for the particle rendering
        /// (this is where the rendering of the base particles
        /// and pins happens).
        /// </summary>
        public Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch>();
        
        // Circuits + Bonds ===============
        /// <summary>
        /// Renderer for the circuits and bonds.
        /// </summary>
        public RendererCircuitsAndBonds circuitAndBondRenderer = new RendererCircuitsAndBonds();

        // Data _____
        // Particles
        /// <summary>
        /// Storage of all registered particles at this system.
        /// </summary>
        private Dictionary<IParticleState, ParticleGraphicsAdapterImpl> particleToParticleGraphicalDataMap = new Dictionary<IParticleState, ParticleGraphicsAdapterImpl>();

        public RendererParticles()
        {
            instance = this;
        }

        /// <summary>
        /// Adds the given particle to a <see cref="RendererParticles_RenderBatch"/>
        /// with the same properties (like color). If no such render batch exists
        /// yet, a new one is created.
        /// </summary>
        /// <param name="graphicalData">The graphics adapter belonging to the
        /// particle to be added.</param>
        /// <returns><c>true</c> if and only if the particle was added
        /// successfully.</returns>
        public bool Particle_Add(ParticleGraphicsAdapterImpl graphicalData)
        {
            RendererParticles_RenderBatch.PropertyBlockData block = new RendererParticles_RenderBatch.PropertyBlockData(graphicalData.graphics_color, graphicalData.particle.GetCircuitPinsPerSide());
            // Add particle to existing/new RenderBatch
            if (propertiesToRenderBatchMap.TryGetValue(block, out RendererParticles_RenderBatch batch))
            {
                // RenderBatch does already exist
                // Add particle to batch
                batch.Particle_Add(graphicalData);
            }
            else
            {
                // RenderBatch does not exist
                // Create RenderBatch, add particle
                RendererParticles_RenderBatch renderBatch = new RendererParticles_RenderBatch(block);
                propertiesToRenderBatchMap.Add(block, renderBatch);
                renderBatch.Particle_Add(graphicalData);
            }
            if(particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle) == false)
            {
                particleToParticleGraphicalDataMap.Add(graphicalData.particle, graphicalData);
            }
            return true;
        }

        /// <summary>
        /// Removes the given particle from the stored RenderBatch.
        /// </summary>
        /// <param name="graphicalData">The graphics adapter belonging to the
        /// particle to be removed.</param>
        public void Particle_Remove(ParticleGraphicsAdapterImpl graphicalData)
        {
            if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle))
            {
                particleToParticleGraphicalDataMap.Remove(graphicalData.particle);
                // Remove particle from old RenderBatch
                propertiesToRenderBatchMap[new RendererParticles_RenderBatch.PropertyBlockData(graphicalData.graphics_color, graphicalData.particle.GetCircuitPinsPerSide())].Particle_Remove(graphicalData);
            }
        }

        /// <summary>
        /// Updates the color of the particle, this means the
        /// <see cref="RendererParticles_RenderBatch"/> is changed
        /// if the new color is different from the old one.
        /// </summary>
        /// <param name="gd">The graphics adapter belonging to the
        /// particle that is changing colors.</param>
        /// <param name="oldColor">The old color of the particle.</param>
        /// <param name="color">The new color the particle should get.</param>
        /// <returns><c>true</c> if and only if the color of the given particle
        /// was changed.</returns>
        public bool Particle_UpdateColor(ParticleGraphicsAdapterImpl gd, Color oldColor, Color color)
        {
            if (oldColor == color) return false;

            // Remove particle from old RenderBatch
            propertiesToRenderBatchMap[new RendererParticles_RenderBatch.PropertyBlockData(oldColor, gd.particle.GetCircuitPinsPerSide())].Particle_Remove(gd);

            // Add particle to new RenderBatch
            gd.graphics_color = color;
            Particle_Add(gd);

            return true;
        }

        /// <summary>
        /// Renders all <see cref="RendererParticles_RenderBatch"/>es
        /// as well as the circuits and bonds.
        /// </summary>
        /// <param name="viewType">The view type that should be used
        /// to render the particles.</param>
        public void Render(ViewType viewType)
        {
            // Particles
            foreach (var item in propertiesToRenderBatchMap.Values)
            {
                item.Render(viewType);
            }
            // Circuits
            circuitAndBondRenderer.Render(viewType);
        }

        /// <summary>
        /// Changes the visibility setting of pins in the base
        /// hexagon or round hexagon view mode.
        /// </summary>
        /// <param name="visible">Whether the pins should be
        /// visible.</param>
        public void SetPinsVisible(bool visible)
        {
            foreach (var item in propertiesToRenderBatchMap.Values)
                item.SetPinsVisible(visible);
        }

    }

}
