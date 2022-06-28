using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGraphicalData
{

    // References
    public Particle particle;
    private RendererParticles renderer;
    
    public ParticleGraphicalData(Particle particle, RendererParticles renderer)
    {
        this.particle = particle;
        this.renderer = renderer;
        Init();
    }

    private void Init()
    {
        // Create visual data
        renderer.Particle_Add(this);
        throw new System.NotImplementedException();
    }

    public void DeleteParticle()
    {
        HideParticle();
    }

    private void ShowParticle()
    {
        renderer.Particle_Connect(this);
    }

    private void HideParticle()
    {
        renderer.Particle_Disconnect(this);
    }

}
