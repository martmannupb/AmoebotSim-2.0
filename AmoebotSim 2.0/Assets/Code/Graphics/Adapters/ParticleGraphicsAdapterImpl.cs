using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGraphicsAdapterImpl : IParticleGraphicsAdapter
{

    // References
    public IParticleState particle;
    private RendererParticles renderer;

    // Data
    // Current Round
    public Vector2Int cur_position1;
    public Vector2Int cur_position2;
    public bool cur_isExpanded = false;
    public int cur_globalExpansionDir = -1;
    public ParticleMovement cur_movement = ParticleMovement.Contracted;
    // Previous Round
    public Vector2Int prev_position1;
    public Vector2Int prev_position2;
    public bool prev_isExpanded = false;
    public int prev_globalExpansionDir = -1;
    public ParticleMovement prev_movement = ParticleMovement.Contracted;
    // Movement

    // Graphical Data
    public int graphics_listNumber = 0;
    public int graphics_listID = 0;

    public ParticleGraphicsAdapterImpl(IParticleState particle, RendererParticles renderer)
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
        // Previous Data
        prev_position1 = cur_position1;
        prev_position2 = cur_position2;
        prev_isExpanded = cur_isExpanded;
        prev_globalExpansionDir = cur_globalExpansionDir;
        // Current Data
        cur_position1 = particle.Head();
        cur_position2 = particle.Tail();
        cur_isExpanded = particle.IsExpanded();
        cur_globalExpansionDir = particle.GetGlobalExpansionDir();
        if(cur_isExpanded)
        {
            // Expanded
            if (prev_isExpanded) cur_movement = ParticleMovement.Expanded;
            else cur_movement = ParticleMovement.Expanding;
        }
        else
        {
            // Contracted
            if (prev_isExpanded) cur_movement = ParticleMovement.Contracting;
            else cur_movement = ParticleMovement.Contracted;
        }
        // Update Matrix
        if(cur_movement != prev_movement) renderer.UpdateMatrix(this);
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

    public enum ParticleMovement
    {
        Contracted,
        Expanded,
        Expanding,
        Contracting 
    }
}
