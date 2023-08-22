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

        private Dictionary<Color, MaterialPropertyBlockData_Objects> propertyBlocks = new Dictionary<Color, MaterialPropertyBlockData_Objects>();

        private List<ObjectGraphicsAdapter> objects = new List<ObjectGraphicsAdapter>();

        private Material objectMat = MaterialDatabase.material_object_base;

        public RendererObjects()
        {

        }

        public void Render()
        {
            foreach (ObjectGraphicsAdapter obj in objects)
            {
                if (obj.mesh != null)
                {
                    Graphics.DrawMesh(obj.mesh,
                        Matrix4x4.TRS(
                            AmoebotFunctions.GridToWorldPositionVector3(obj.obj.Position.x, obj.obj.Position.y, RenderSystem.zLayer_objects), Quaternion.identity, Vector3.one),
                        objectMat, 0, Camera.current, 0, obj.propertyBlock.propertyBlock);
                }
            }
        }

        public void AddObject(ObjectGraphicsAdapter obj)
        {
            UpdateObjectColor(obj);
            objects.Add(obj);
        }

        public void RemoveObject(ObjectGraphicsAdapter obj)
        {
            objects.Remove(obj);
        }

        public void UpdateObjectColor(ObjectGraphicsAdapter obj)
        {
            if (obj.propertyBlock == null || obj.propertyBlock.Color != obj.Color)
            {
                // Find or generate property block
                if (propertyBlocks.TryGetValue(obj.Color, out MaterialPropertyBlockData_Objects prop))
                    obj.propertyBlock = prop;
                else
                {
                    MaterialPropertyBlockData_Objects newProp = new MaterialPropertyBlockData_Objects();
                    newProp.ApplyColor(obj.Color);
                    propertyBlocks[obj.Color] = newProp;
                    obj.propertyBlock = newProp;
                }
            }
        }
    }

} // namespace AS2.Visuals
