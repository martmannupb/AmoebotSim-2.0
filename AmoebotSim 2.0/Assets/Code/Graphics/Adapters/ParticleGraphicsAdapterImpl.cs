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
    public PositionSnap state_cur = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, ParticleMovement.Contracted, 0f);
    public PositionSnap state_prev = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, ParticleMovement.Contracted, 0f);

    // Graphical Data
    public int graphics_listNumber = 0;
    public int graphics_listID = 0;

    public struct PositionSnap
    {
        public Vector2Int position1;
        public Vector2Int position2;
        public bool isExpanded;
        public int globalExpansionDir;
        public ParticleMovement movement;
        public float timestamp;

        public PositionSnap(Vector2Int p1, Vector2Int p2, bool isExpanded, int globalExpansionDir, ParticleMovement movement, float timestamp)
        {
            this.position1 = p1;
            this.position2 = p2;
            this.isExpanded = isExpanded;
            this.globalExpansionDir = globalExpansionDir;
            this.movement = movement;
            this.timestamp = timestamp;
        }

        public static bool operator ==(PositionSnap s1, PositionSnap s2)
        {
            return s1.position1 == s2.position1 &&
                s1.position2 == s2.position2 &&
                s1.isExpanded == s2.isExpanded &&
                s1.globalExpansionDir == s2.globalExpansionDir &&
                s1.movement == s2.movement &&
                s1.timestamp == s2.timestamp;
        }

        public static bool operator !=(PositionSnap s1, PositionSnap s2)
        {
            return s1.position1 != s2.position1 ||
                s1.position2 != s2.position2 ||
                s1.isExpanded != s2.isExpanded ||
                s1.globalExpansionDir != s2.globalExpansionDir ||
                s1.movement != s2.movement ||
                s1.timestamp != s2.timestamp;
        }

        public static bool IsPositionEqual(PositionSnap s1, PositionSnap s2)
        {
            return s1.position1 == s2.position1 && s1.position2 == s2.position2 && s1.isExpanded == true && s2.isExpanded == true ||
                s1.position1 == s2.position1 && s1.isExpanded == false && s2.isExpanded == false;
        }
    }

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
        state_prev = state_cur;
        // Current Data
        state_cur = new PositionSnap(particle.Head(), particle.Tail(), particle.IsExpanded(), particle.GetGlobalExpansionDir(), ParticleMovement.Contracted, Time.timeSinceLevelLoad);
        
        if(state_cur.isExpanded)
        {
            // Expanded
            if (state_prev.isExpanded) state_cur.movement = ParticleMovement.Expanded;
            else state_cur.movement = ParticleMovement.Expanding;
        }
        else
        {
            // Contracted
            if (state_prev.isExpanded)
            {
                state_cur.movement = ParticleMovement.Contracting;
                state_cur.position2 = state_prev.position2; // Tail to previous Tail
            }
            else state_cur.movement = ParticleMovement.Contracted;
        }
        // Update Matrix
        if(PositionSnap.IsPositionEqual(state_cur, state_prev) == false) renderer.UpdateMatrix(this);
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
