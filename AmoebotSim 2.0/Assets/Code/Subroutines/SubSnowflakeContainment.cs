using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.ShapeContainment;

namespace AS2.Subroutines.SnowflakeContainment
{

    /// <summary>
    /// Container class storing all information required by the containment check
    /// for snowflake shapes.
    /// </summary>
    public class SnowflakeInfo
    {
        /// <summary>
        /// The list of the snowflake's dependency tree's nodes,
        /// in topological order. The arm lengths must be indices
        /// in the array of occurring lengths instead of actual lengths.
        /// </summary>
        public ShapeContainer.DTreeNode[] nodes;

        /// <summary>
        /// The occurring arm lengths in ascending order.
        /// </summary>
        public int[] armLengths;

        /// <summary>
        /// Binary strings of the occurring arm lengths.
        /// </summary>
        public string[] armLengthsStr;

        /// <summary>
        /// The number of bits in the longest arm length string.
        /// </summary>
        public int longestParameter;
    }

    /// <summary>
    /// Containment check procedure for snowflake shapes.
    /// </summary>
    public class SubSnowflakeContainment : Subroutine
    {

        SnowflakeInfo snowflakeInfo;

        public SubSnowflakeContainment(Particle p, SnowflakeInfo snowflakeInfo) : base(p)
        {
            this.snowflakeInfo = snowflakeInfo;
        }
    }

} // namespace AS2.Subroutines.SnowflakeContainment
