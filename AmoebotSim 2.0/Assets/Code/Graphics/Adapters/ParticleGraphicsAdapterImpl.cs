using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGraphicsAdapterImpl : IParticleGraphicsAdapter
{

    // References
    public Particle particle;
    private RendererParticles renderer;

    // Data
    public Vector2Int stored_position1;
    public Vector2Int stored_position2;
    public bool stored_isExpanded = false;
    public int stored_expansionDir = -1;

    // Graphical Data
    public int graphics_listNumber = 0;
    public int graphics_listID = 0;

    public ParticleGraphicsAdapterImpl(Particle particle, RendererParticles renderer)
    {
        this.particle = particle;
        this.renderer = renderer;
    }

    public void AddParticle()
    {
        renderer.Particle_Add(this);
    }

    public void Update()
    {
        renderer.UpdateMatrix(this);
    }

    public void ShowParticle()
    {
        throw new System.NotImplementedException();
    }

    public void HideParticle()
    {
        throw new System.NotImplementedException();
    }

    public void RemoveParticle()
    {
        throw new System.NotImplementedException();
    }
}
