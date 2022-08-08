using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGraphicsAdapterImpl : IParticleGraphicsAdapter
{

    // References
    public IParticleState particle;
    private RendererParticles renderer;

    // Defaults
    private static Color defColor = MaterialDatabase.material_circular_particle.GetColor("_InputColor");

    // Data
    // Current Round
    public PositionSnap state_cur = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, ParticleMovement.Contracted, 0f);
    public PositionSnap state_prev = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, ParticleMovement.Contracted, 0f);

    // Graphical Data
    public int graphics_listNumber = 0;
    public int graphics_listID = 0;
    public int graphics_globalID = 0;
    public RendererParticles_RenderBatch graphics_colorRenderer;
    public Color graphics_color;

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

        public override bool Equals(object s)
        {
            return s is PositionSnap && (PositionSnap)s == this;
        }

        public static bool IsPositionEqual(PositionSnap s1, PositionSnap s2)
        {
            return s1.position1 == s2.position1 && s1.position2 == s2.position2 && s1.isExpanded == true && s2.isExpanded == true ||
                s1.position1 == s2.position1 && s1.isExpanded == false && s2.isExpanded == false;
        }

        /// <summary>
        /// 42, the answer to everything.
        /// Dummy value to avoid warnings.. Please do not use!
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return 42;
        }

        /// <summary>
        /// Dummy value to avoid warnings.. Please do not use!
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "This is a PositionSnap. Just deal with it.";
        }
    }

    public ParticleGraphicsAdapterImpl(IParticleState particle, RendererParticles renderer)
    {
        this.particle = particle;
        this.renderer = renderer;
    }

    public void AddParticle()
    {
        if (particle.IsParticleColorSet()) graphics_color = particle.GetParticleColor();
        else graphics_color = defColor;
        renderer.Particle_Add(this);
        Update(true, true);
    }

    public void Update()
    {
        Update(false, false);
    }

    private void Update(bool forceRenderUpdate, bool noAnimation)
    {
        // Previous Data
        state_prev = state_cur;
        // Current Data
        state_cur = new PositionSnap(particle.Head(), particle.Tail(), particle.IsExpanded(), particle.GlobalHeadDirection(), ParticleMovement.Contracted, Time.timeSinceLevelLoad);

        if (state_cur.isExpanded)
        {
            // Expanded
            if (state_prev.isExpanded || noAnimation) state_cur.movement = ParticleMovement.Expanded;
            else state_cur.movement = ParticleMovement.Expanding;
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
            || forceRenderUpdate) graphics_colorRenderer.UpdateMatrix(this); //renderer.UpdateMatrix(this);
    }

    public void UpdateReset()
    {
        Update(true, true);
    }

    public void SetParticleColor(Color color)
    {
        renderer.Particle_UpdateColor(this, graphics_color, color);
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

    public void RemoveParticle()
    {
        renderer.Particle_Remove(this);
    }


    public enum ParticleMovement
    {
        Contracted,
        Expanded,
        Expanding,
        Contracting 
    }
}
