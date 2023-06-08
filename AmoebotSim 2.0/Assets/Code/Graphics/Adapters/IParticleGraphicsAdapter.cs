using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Serves as the bridge between the particles of the simulator
    /// and the render system. All particles have an instance of an
    /// implementation of this interface. Used to pass visual
    /// information to the graphical system and draw the particle
    /// with circuits and bonds to the screen.
    /// </summary>
    public interface IParticleGraphicsAdapter
    {

        // General Functions _______________

        /// <summary>
        /// Adds and initializes the graphics of the particle.
        /// Call this each time a new particle has been added
        /// to the render system.
        /// Afterwards use <see cref="Update(ParticleMovementState)"/>
        /// repeatedly to update the particle visuals.
        /// </summary>
        /// <param name="movementState">The current movement state
        /// of the particle to be added.</param>
        void AddParticle(ParticleMovementState movementState);

        /// <summary>
        /// Updates the particle graphics. This is applied and
        /// shown directly in the next render cycle. Call it once
        /// per round, even if the particle has not moved.
        /// <para>
        /// Example: A particle has expanded. Call
        /// <see cref="Update(ParticleMovementState)"/> to update the visuals.
        /// </para>
        /// </summary>
        /// <param name="movementState">The updated movement state
        /// of the particle.</param>
        void Update(ParticleMovementState movementState);

        /// <summary>
        /// Like <see cref="Update(ParticleMovementState)"/>, but forces
        /// the update to be applied visually without an animation, even
        /// if the particle positions are the same as in the previous frame.
        /// </summary>
        void UpdateReset();

        /// <summary>
        /// Like <see cref="Update(ParticleMovementState)"/>, but forces
        /// the update to be applied visually without an animation, even
        /// if the particle positions are the same as in the previous frame.
        /// </summary>
        /// <param name="movementState">The new movement state of the
        /// particle.</param>
        void UpdateReset(ParticleMovementState movementState);

        /// <summary>
        /// Renders a particle bond for this round. Only call this on one of
        /// the two connected particles.
        /// Call Order: <see cref="Update(ParticleMovementState)"/> methods
        /// of all particles, then all <see cref="BondUpdate(ParticleBondGraphicState)"/>
        /// methods so that all bonds are rendered.
        /// </summary>
        /// <param name="bondState">Data about the current state of the bond.</param>
        void BondUpdate(ParticleBondGraphicState bondState);

        /// <summary>
        /// Pushes an update for the pin configuration of the particle and
        /// the immediate connections to neighboring particles.
        /// </summary>
        /// <param name="state">Data about the current state of the
        /// particle's pin configuration and neighbor connections.</param>
        void CircuitUpdate(ParticlePinGraphicState state);

        /// <summary>
        /// Complements <see cref="HideParticle"/>. Shows the particle.
        /// Calling this method is not necessary if the particle is never
        /// hidden. By default the particle is visible once it is added
        /// and updated.
        /// </summary>
        void ShowParticle();

        /// <summary>
        /// Complements <see cref="ShowParticle"/>. Hides the particle.
        /// Call this if you want to hide the particle.
        /// </summary>
        void HideParticle();

        /// <summary>
        /// Removes the particle from the render system.
        /// Example: If you want to load a different setup of particles,
        /// all particles should be removed.
        /// </summary>
        void RemoveParticle();

        // Visualization _______________

        /// <summary>
        /// Call this when you want to update the particle color.
        /// This only needs to be called when the color which has
        /// automatically been set when the particle was added to the
        /// rendering system should be changed in some way.
        /// </summary>
        /// <param name="color">The new color of the particle.</param>
        void SetParticleColor(Color color);

        /// <summary>
        /// Call this when you want to remove the particle color.
        /// It goes back to default afterwards. This only needs to
        /// be called when the color which has been set manually
        /// should be removed.
        /// </summary>
        void ClearParticleColor();
    }

}
