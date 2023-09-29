using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;


namespace AS2.Subroutines
{

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

        public virtual void ActivateMove() { }

        public virtual void ActivateBeep() { }

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
