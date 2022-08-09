using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuits_RenderBatch
{

    // Settings _____
    public PropertyBlockData properties;

    /// <summary>
    /// An extendable struct that functions as the key for the mapping of particles to their render class.
    /// </summary>
    public struct PropertyBlockData
    {
        public Color color;
        public bool hasDelay;

        public PropertyBlockData(Color color, bool hasDelay)
        {
            this.color = color;
            this.hasDelay = hasDelay;
        }
    }

}