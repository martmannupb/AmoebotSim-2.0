using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IParticleGraphicsAdapter
{

    // General Functions _______________

    /// <summary>
    /// Adds and initializes the graphics of the particle. Call this each time a new particle has been added to the RenderSystem.
    /// Afterwards use Update() repeatedly to update the particle visuals.
    /// DEPRECATED! (use AddParticle(ParticleMovementState movementState) instead)
    /// </summary>
    void AddParticle();

    /// <summary>
    /// Adds and initializes the graphics of the particle. Call this each time a new particle has been added to the RenderSystem.
    /// Afterwards use Update() repeatedly to update the particle visuals.
    /// </summary>
    /// <param name="movementState"></param>
    void AddParticle(ParticleMovementState movementState);

    /// <summary>
    /// Updates the particle graphics. This is applied and shown directly in the next render cycle. Call it once per round, even if the particle has not moved.
    /// Example: A particle has expanded. Call Update() to update the visuals.
    /// (replaces the old Update() and Update(ParticleJointMovementState jointMovementState) methods)
    /// </summary>
    /// <param name="movementState"></param>
    void Update(ParticleMovementState movementState);

    /// <summary>
    /// Like Update(), but forces the update to be applied visually without an animation, even if the particle positions are the same as in the previous frame.
    /// </summary>
    void UpdateReset();

    /// <summary>
    /// Renders a particle bond for this round. Only call this on one of the two connected particles.
    /// Call Order: Update() methods of all particles, then all UpdateBond() methods so that all bonds are rendered.
    /// </summary>
    /// <param name="bondState">Data about the current state of the bond.</param>
    void BondUpdate(ParticleBondGraphicState bondState);

    /// <summary>
    /// Pushes an Update for the internal circuits of the particle and the immediate connections to neighboring particles.
    /// </summary>
    /// <param name="data"></param>
    void CircuitUpdate(ParticlePinGraphicState state);

    /// <summary>
    /// Complements HideParticle(). Shows the particle.
    /// Calling this method is not necessary if the particle is never hidden. By default the particle is visible once it is added and updated.
    /// </summary>
    void ShowParticle();

    /// <summary>
    /// Complements ShowParticle(). Hides the particle.
    /// Call this if you want to hide the particle.
    /// </summary>
    void HideParticle();

    /// <summary>
    /// Removes the particle from the RenderSystem.
    /// Example: If you want to load a different setup of particles, all particles should be deleted.
    /// </summary>
    void RemoveParticle();

    // Visualization _______________

    /// <summary>
    /// Call this when you want to update the particle color.
    /// This only needs to be called when the color which has automatically been set when the particle was added to the rendering system should be changed in some way.
    /// </summary>
    void SetParticleColor(Color color);

    /// <summary>
    /// Call this when you want to remove the particle color. It goes back to default afterwards.
    /// This only needs to be called when the color which has been set manually should be removed.
    /// </summary>
    void ClearParticleColor();
}
