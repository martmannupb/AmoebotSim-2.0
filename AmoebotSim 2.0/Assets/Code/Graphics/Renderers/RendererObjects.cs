using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AS2.Visuals
{

    /// <summary>
    /// Renders all objects in the system.
    /// </summary>
    public class RendererObjects
    {

        private List<ObjectGraphicsAdapter> objects = new List<ObjectGraphicsAdapter>();

        public RendererObjects()
        {

        }

        public void Render()
        {
            foreach (ObjectGraphicsAdapter obj in objects)
                obj.obj.Draw();
        }

        public void AddObject(ObjectGraphicsAdapter obj)
        {
            objects.Add(obj);
        }

        public void RemoveObject(ObjectGraphicsAdapter obj)
        {
            objects.Remove(obj);
        }
    }

} // namespace AS2.Visuals
