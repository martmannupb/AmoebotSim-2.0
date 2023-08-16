using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Visuals
{

    /// <summary>
    /// Representation of a <see cref="AS2.Sim.ParticleObject"/> in the render system.
    /// Stores all graphics-specific data of the object.
    /// </summary>
    public class ObjectGraphicsAdapter
    {
        public ParticleObject obj;
        public RendererObjects renderer;

        private bool isRegistered = false;

        public ObjectGraphicsAdapter(ParticleObject obj, RendererObjects renderer)
        {
            this.obj = obj;
            this.renderer = renderer;
        }

        public void AddObject()
        {
            if (isRegistered)
            {
                Log.Error("Object graphics adapter already registered.");
                return;
            }

            isRegistered = true;
            renderer.AddObject(this);
        }

        public void RemoveObject()
        {
            if (!isRegistered)
            {
                Log.Error("Cannot remove object graphics adapter that is not registered.");
                return;
            }

            isRegistered = false;
            renderer.RemoveObject(this);
        }
    }

} // namespace AS2.Visuals
