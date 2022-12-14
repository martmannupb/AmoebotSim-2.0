using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The base class of all particle rendering.
/// Particles that are added to the system give continouus updates on their state over the ParticleGraphicsAdapterImpl class, while the system handles the necessary updates of the 
/// visual data to perform the display on the screen.
/// </summary>
public class RendererParticles
{

    public static RendererParticles instance;

    // Renderers
    // Particles ===============
    /// <summary>
    /// Collection of batches for the particle rendering (this is where the rendering of the base particles and pins happens)
    /// </summary>
    public Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch>();
    // Circuits + Bonds ===============
    /// <summary>
    /// Renderer for the circuits and bonds
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
    /// Adds the particle to a new RenderBatch with the same properties (like color).
    /// </summary>
    /// <param name="graphicalData"></param>
    /// <returns></returns>
    public bool Particle_Add(ParticleGraphicsAdapterImpl graphicalData)
    {
        RendererParticles_RenderBatch.PropertyBlockData block = new RendererParticles_RenderBatch.PropertyBlockData(graphicalData.graphics_color, graphicalData.particle.GetCircuitPinsPerSide());
        // Add particle to existing/new RenderBatch
        if (propertiesToRenderBatchMap.ContainsKey(block))
        {
            // RenderBatch does already exist
            // Add particle to batch
            propertiesToRenderBatchMap[block].Particle_Add(graphicalData);
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
    /// Removes the particle from the stored RenderBatch.
    /// </summary>
    /// <param name="graphicalData"></param>
    public void Particle_Remove(ParticleGraphicsAdapterImpl graphicalData)
    {
        if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle)) particleToParticleGraphicalDataMap.Remove(graphicalData.particle);

        // Remove particle from old RenderBatch
        propertiesToRenderBatchMap[new RendererParticles_RenderBatch.PropertyBlockData(graphicalData.graphics_color, graphicalData.particle.GetCircuitPinsPerSide())].Particle_Remove(graphicalData);
    }

    /// <summary>
    /// Updates the color of the particle, this means the RenderBatch is changed if the new color is different from the old one.
    /// </summary>
    /// <param name="gd"></param>
    /// <param name="oldColor"></param>
    /// <param name="color"></param>
    /// <returns></returns>
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
    /// Renders all RenderBatches.
    /// </summary>
    /// <param name="viewType"></param>
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

}