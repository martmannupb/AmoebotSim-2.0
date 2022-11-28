using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuitsAndBonds
{

    // Instances
    public RendererCircuits_Instance[] renderInstances = new RendererCircuits_Instance[] { new RendererCircuits_Instance(), new RendererCircuits_Instance() };
    private int updateInstance = 0;
    private int drawnInstance {
        get {
            return (updateInstance - 1 + renderInstances.Length) % renderInstances.Length;
        }
    }

    public void AddCircuits(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap)
    {
        renderInstances[updateInstance].AddCircuits(state, snap);
    }

    public void AddBond(ParticleBondGraphicState bondState)
    {
        renderInstances[updateInstance].AddBond(bondState);
    }

    /// <summary>
    /// Reinits the batches in the instances. Helpful in case settings have been changed.
    /// </summary>
    public void ReinitBatches()
    {
        foreach (var instance in renderInstances)
        {
            instance.ReinitBatches();
        }
    }

    public void SwitchInstances()
    {
        // Clear old Instance
        renderInstances[drawnInstance].Clear();
        // Switch + Notify Instance
        updateInstance = (updateInstance + 1) % renderInstances.Length;
    }

    public void Render(ViewType type)
    {
        renderInstances[drawnInstance].Render(type);
    }

}