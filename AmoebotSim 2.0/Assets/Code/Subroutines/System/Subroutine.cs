using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;


namespace AS2.Subroutines
{

    /// <summary>
    /// Abstract base class for subroutines.
    /// <para>
    /// A subroutine is an object that encapsulates an algorithm
    /// so that it can be reused easily. Subroutine objects must
    /// be instantiated in the algorithm's constructor, after
    /// the attributes have been created, because they create their
    /// own attributes and register them in the particle.
    /// Typically, a subroutine has an initialization method that
    /// sets up the computation, after which its activation methods
    /// can be called in each round to run the algorithm.
    /// </para>
    /// </summary>
    public abstract class Subroutine
    {
        /// <summary>
        /// The particle to which this subroutine belongs.
        /// </summary>
        protected Particle particle;

        /// <summary>
        /// The algorithm instance to which this subroutine belongs.
        /// </summary>
        protected ParticleAlgorithm algo;

        public Subroutine(Particle p)
        {
            if (!p.inConstructor)
            {
                throw new SimulatorStateException("Cannot create subroutine outside of constructor.");
            }
            particle = p;
            algo = p.algorithm;
        }

        /// <summary>
        /// The movement activation method of the subroutine.
        /// Not all subroutines require this method to be called.
        /// </summary>
        public virtual void ActivateMove() { }

        /// <summary>
        /// The beep activation method of the subroutine.
        /// Not all subroutines require this method to be called.
        /// </summary>
        public virtual void ActivateBeep() { }

        /// <summary>
        /// Finds an attribute name that is not taken yet
        /// by appending a number to the end of the given name.
        /// </summary>
        /// <param name="name">The base name of the attribute.</param>
        /// <returns>Either <paramref name="name"/> if the name is not
        /// taken yet, or <paramref name="name"/> with an appended
        /// number to avoid duplicate attribute names.</returns>
        protected string FindValidAttributeName(string name)
        {
            if (particle.TryGetAttributeByName(name) == null)
                return name;

            int i;
            for (i = 0; i < 1000; i++)
            {
                string n = name + "_" + i.ToString();
                if (particle.TryGetAttributeByName(n) == null)
                    return n;
            }
            throw new SimulatorStateException("Could not find valid attribute name with base name '" + name + "' (exceeded maximum amount of naming attempts)");
        }
    }

} // namespace AS2.Subroutines
