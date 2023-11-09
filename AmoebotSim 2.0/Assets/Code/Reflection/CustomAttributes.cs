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
        public string name;

        public StatusInfoAttribute(string name)
        {
            this.name = name;
        }
    }

} // namespace AS2
