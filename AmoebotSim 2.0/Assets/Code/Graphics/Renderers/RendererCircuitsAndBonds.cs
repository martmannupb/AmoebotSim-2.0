using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// The renderer for the circuits and bonds.
    /// In 2 instances which alternate in drawing to the screen, we update
    /// the graphical data and draw the instance once all data is ready and
    /// the new round should be displayed.
    /// After the displayed data is not shown anymore, everything is discarded
    /// and built up again. We make sure that no matrices are ever discarded
    /// and do not get garbage collection issues.
    /// <para>
    /// (The idea with the instances is used to possibly update data over
    /// multiple frames and still show the old instance, so the performance
    /// does not drop when a new round is calculated. This system has not yet
    /// been implemented completely since the idea came later, so we still
    /// build everything in one round, which is performant enough for a
    /// certain amount of particles. The particle rendering and render loop
    /// steering would also need to be updated to fully make use of the new
    /// system.)
    /// </para>
    /// </summary>
    public class RendererCircuitsAndBonds
    {

        // Instances
        /// <summary>
        /// The array of render instances. Usually has size 2.
        /// </summary>
        public RendererCircuits_Instance[] renderInstances = new RendererCircuits_Instance[] { new RendererCircuits_Instance(), new RendererCircuits_Instance() };
        /// <summary>
        /// The index of the instance that is currently being updated (and not drawn).
        /// </summary>
        private int updateInstance = 0;
        /// <summary>
        /// The index of the instance that is currently being drawn.
        /// </summary>
        private int drawnInstance
        {
            get
            {
                return (updateInstance - 1 + renderInstances.Length) % renderInstances.Length;
            }
        }

        /// <summary>
        /// Adds the graphical circuit data of a single particle to the system.
        /// </summary>
        /// <param name="state">The graphical data of the particle's pin configuration.</param>
        /// <param name="snap">The position data of the particle.</param>
        public void AddCircuits(ParticleGraphicsAdapterImpl particle, ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, PartitionSetViewType pSetViewType)
        {
            renderInstances[updateInstance].AddCircuits(particle, state, snap, pSetViewType);
        }

        /// <summary>
        /// Adds the graphical data of a single bond.
        /// </summary>
        /// <param name="bondState">Visual information of the bond.</param>
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

        /// <summary>
        /// Switches the instances, so that the currently built instance is
        /// now drawn and the currently rendered instance is cleared and
        /// can be rebuilt.
        /// </summary>
        public void SwitchInstances()
        {
            // Clear old Instance
            renderInstances[drawnInstance].Clear();
            // Switch + Notify Instance
            updateInstance = (updateInstance + 1) % renderInstances.Length;
        }

        /// <summary>
        /// Gets the instance that is currently drawing.
        /// </summary>
        /// <returns>The currently drawing instance.</returns>
        public RendererCircuits_Instance GetCurrentInstance()
        {
            return renderInstances[drawnInstance];
        }

        /// <summary>
        /// Renders the current instance.
        /// </summary>
        /// <param name="type">The visualization mode that is currently
        /// used to render the particle system.</param>
        public void Render(ViewType type)
        {
            renderInstances[drawnInstance].Render(type);
        }

    }

}