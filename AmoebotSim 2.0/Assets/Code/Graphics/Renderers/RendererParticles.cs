using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererParticles
{

    // Circuits ===============
    public RendererCircuits circuitRenderer = new RendererCircuits();

    // Particles ===============
    // Render Batch
    public Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch>();

    // Data _____
    // Particles
    private Dictionary<IParticleState, ParticleGraphicsAdapterImpl> particleToParticleGraphicalDataMap = new Dictionary<IParticleState, ParticleGraphicsAdapterImpl>();

    /// <summary>
    /// Adds the particle to a new RenderBatch with the same properties (like color).
    /// </summary>
    /// <param name="graphicalData"></param>
    /// <returns></returns>
    public bool Particle_Add(ParticleGraphicsAdapterImpl graphicalData)
    {
        RendererParticles_RenderBatch.PropertyBlockData block = new RendererParticles_RenderBatch.PropertyBlockData(graphicalData.graphics_color);
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
        propertiesToRenderBatchMap[new RendererParticles_RenderBatch.PropertyBlockData(graphicalData.graphics_color)].Particle_Remove(graphicalData);
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
        propertiesToRenderBatchMap[new RendererParticles_RenderBatch.PropertyBlockData(oldColor)].Particle_Remove(gd);

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
        foreach (var item in propertiesToRenderBatchMap.Values)
        {
            item.Render(viewType);
        }
    }

}