using System.Collections;
using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2.Visuals
{
    /// <summary>
    /// Stores the location of a rendered object (like a matrix)
    /// in a batch structure. A batch is usually a list of
    /// arrays such that each array can be rendered in a single
    /// batch.
    /// </summary>
    public struct RenderBatchIndex
    {
        /// <summary>
        /// The list index (major index).
        /// </summary>
        public int listNumber;
        /// <summary>
        /// The array index (minor index).
        /// </summary>
        public int listIndex;
        /// <summary>
        /// Whether the index is still valid.
        /// </summary>
        public bool isValid;

        public RenderBatchIndex(int listNumber, int listIndex)
        {
            this.listNumber = listNumber;
            this.listIndex = listIndex;
            this.isValid = true;
        }

        /// <summary>
        /// Checks whether the index is still valid.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return isValid;
        }

        /// <summary>
        /// Invalidates the index.
        /// </summary>
        public void Discard()
        {
            isValid = false; ;
        }
    }

}