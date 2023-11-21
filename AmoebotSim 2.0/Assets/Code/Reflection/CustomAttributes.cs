using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// A method attribute identifying a static method as a
    /// status info method. Static methods of algorithm classes
    /// that are marked with this attribute will be represented by
    /// buttons in the Particle Panel in Simulation Mode. The button
    /// belonging to each method will be marked with the given name.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class StatusInfoAttribute : System.Attribute
    {
        /// <summary>
        /// The display name of the status info method.
        /// </summary>
        public string name;

        /// <summary>
        /// The tooltip to be displayed for the status info button.
        /// </summary>
        public string tooltip;

        /// <summary>
        /// Whether this info method should be called automatically at the end of each round.
        /// </summary>
        public bool autocall;

        /// <summary>
        /// Marks this static method as a status info method.
        /// The method must have two parameters: The first must have
        /// type <see cref="AS2.Sim.ParticleSystem"/> and gives access
        /// to the entire particle system and the second must have
        /// type <see cref="AS2.Sim.Particle"/> and gives access to
        /// the currently selected particle.
        /// </summary>
        /// <param name="name">The label of the button in the
        /// Particle Panel.</param>
        /// <param name="tooltip">The (optional) tooltip to be displayed
        /// for the button.</param>
        /// <param name="autocall">Whether the status info method should be
        /// called automatically at the end of each round by default. This
        /// behavior can be changed using the toggles in the Particle Panel.</param>
        public StatusInfoAttribute(string name, string tooltip = null, bool autocall = false)
        {
            this.name = name;
            this.tooltip = tooltip;
            this.autocall = autocall;
        }
    }

} // namespace AS2
