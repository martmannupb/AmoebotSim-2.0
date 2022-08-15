using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuits
{

    // Instances
    public RendererCircuits_Instance[] renderInstances = new RendererCircuits_Instance[] { new RendererCircuits_Instance(), new RendererCircuits_Instance() };
    private int updateInstance = 0;
    private int drawnInstance {
        get {
            return 1 - updateInstance;
        }
    }

    public void AddCircuits(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap)
    {
        renderInstances[updateInstance].AddCircuits(state, snap);
    }

    public void ApplyUpdates(float animationStartTime, float animationDuration)
    {
        // Switch + Notify Instance
        updateInstance = 1 - updateInstance;
        renderInstances[drawnInstance].ApplyUpdates(animationStartTime, animationDuration);
        // Clear old Instance
        renderInstances[updateInstance].Clear();
    }

    public void Render()
    {
        renderInstances[drawnInstance].Render();
    }

}