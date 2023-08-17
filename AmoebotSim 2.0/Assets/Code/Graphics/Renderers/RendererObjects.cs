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
            {
                if (obj.mesh == null)
                    obj.obj.Draw();
                else
                {
                    Material m = MaterialDatabase.material_circular_bgLines;
                    Graphics.DrawMesh(obj.mesh, Matrix4x4.TRS(AmoebotFunctions.GridToWorldPositionVector3(obj.obj.Position), Quaternion.identity, Vector3.one), m, 0);
                }
            }
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
