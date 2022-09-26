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
    public PositionSnap state_cur = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, ParticleMovement.Contracted, ParticleJointMovementState.None, 0f);
    public PositionSnap state_prev = new PositionSnap(Vector2Int.zero, Vector2Int.zero, false, -1, ParticleMovement.Contracted, ParticleJointMovementState.None, 0f);

    // Graphical Data
    public bool graphics_isRegistered = false;
    public int graphics_listNumber = 0;
    public int graphics_listID = 0;
    public int graphics_globalID = 0;
    public RendererParticles_RenderBatch graphics_colorRenderer;
    public Color graphics_color;

    /// <summary>
    /// A snap of a position that is used to determine the current state of the particle.
    /// </summary>
    public struct PositionSnap
    {
        public Vector2Int position1;
        public Vector2Int position2;
        public bool isExpanded;
        public int globalExpansionDir;
        public ParticleMovement movement;
        public ParticleJointMovementState jointMovementState;
        public float timestamp;

        public PositionSnap(Vector2Int p1, Vector2Int p2, bool isExpanded, int globalExpansionDir, ParticleMovement movement, ParticleJointMovementState jointMovementState, float timestamp)
        {
            this.position1 = p1;
            this.position2 = p2;
            this.isExpanded = isExpanded;
            this.globalExpansionDir = globalExpansionDir;
            this.movement = movement;
            this.jointMovementState = jointMovementState;
            this.timestamp = timestamp;
        }

        public static bool operator ==(PositionSnap s1, PositionSnap s2)
        {
            return s1.position1 == s2.position1 &&
                s1.position2 == s2.position2 &&
                s1.isExpanded == s2.isExpanded &&
                s1.globalExpansionDir == s2.globalExpansionDir &&
                s1.movement == s2.movement &&
                s1.jointMovementState == s2.jointMovementState &&
                s1.timestamp == s2.timestamp;
        }

        public static bool operator !=(PositionSnap s1, PositionSnap s2)
        {
            return s1.position1 != s2.position1 ||
                s1.position2 != s2.position2 ||
                s1.isExpanded != s2.isExpanded ||
                s1.globalExpansionDir != s2.globalExpansionDir ||
                s1.movement != s2.movement ||
                s1.jointMovementState != s2.jointMovementState ||
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
            if (isExpanded) return "Expanded Particle: Head: " + position1 + ", Tail: " + position2;
            else return "Contracted Particle: Head: " + position1;
        }
    }

    public ParticleGraphicsAdapterImpl(IParticleState particle, RendererParticles renderer)
    {
        this.particle = particle;
        this.renderer = renderer;
    }

    /// <summary>
    /// Adds the particle to the renderer. Only needs to be called once to register a particle.
    /// </summary>
    public void AddParticle()
    {
        // Check if already added to the system
        if(graphics_isRegistered)
        {
            Log.Error("Error: Particle has already been added!");
        }

        graphics_isRegistered = true;
        if (particle.IsParticleColorSet()) graphics_color = particle.GetParticleColor();
        else graphics_color = defColor;
        renderer.Particle_Add(this);
        Update(true, ParticleJointMovementState.None, true);
    }

    /// <summary>
    /// Removes a particle from the renderer. Only needs to be called once when the particle is removed from the system.
    /// </summary>
    public void RemoveParticle()
    {
        renderer.Particle_Remove(this);
    }

    public void Update()
    {
        Update(false, ParticleJointMovementState.None, false);
    }

    public void Update(ParticleJointMovementState jointMovementState)
    {
        if(((Particle)particle).id == 24)
        {
            Log.Debug("Here");
            Log.Debug("Is joint: " + jointMovementState.isJointMovement);
        }
        Update(false, jointMovementState, false);
    }
    
    private void Update(bool forceRenderUpdate, ParticleJointMovementState jointMovementState, bool noAnimation)
    {
        // Previous Data
        state_prev = state_cur;
        // Current Data
        state_cur = new PositionSnap(particle.Head(), particle.Tail(), particle.IsExpanded(), particle.GlobalHeadDirectionInt(), ParticleMovement.Contracted, noAnimation == false ? jointMovementState : ParticleJointMovementState.None, Time.timeSinceLevelLoad);

        // Get Expanded State
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
            || forceRenderUpdate) graphics_colorRenderer.UpdateMatrix(this, false); //renderer.UpdateMatrix(this);
    }

    public void UpdateReset()
    {
        Update(true, ParticleJointMovementState.None, true);
    }

    public void BondUpdate(ParticleBondGraphicState bondState)
    {
        //renderer.
    }

    public void CircuitUpdate(ParticlePinGraphicState state)
    {
        renderer.circuitRenderer.AddCircuits(state, state_cur);
    }

    public void SetParticleColor(Color color)
    {
        if(graphics_isRegistered) renderer.Particle_UpdateColor(this, graphics_color, color);
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

    public enum ParticleMovement
    {
        Contracted,
        Expanded,
        Expanding,
        Contracting 
    }
}
