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
            bool animated = RenderSystem.animationsOn;
            float animationOffset = Library.InterpolationConstants.SmoothLerp(RenderSystem.animation_curAnimationPercentage);
            foreach (ObjectGraphicsAdapter obj in objects)
            {
                if (obj.mesh != null)
                {
                    Matrix4x4 matrix = obj.CalculateMatrix(animated, animationOffset);
                    Graphics.DrawMesh(obj.mesh, matrix, objectMat, 0, Camera.current, 0, obj.propertyBlock.propertyBlock);
                }
            }
        }

        /// <summary>
        /// Adds the given object to the render system.
        /// </summary>
        /// <param name="obj">The object adapter to add.</param>
        public void AddObject(ObjectGraphicsAdapter obj)
        {
            UpdateObjectColor(obj);
            objects.Add(obj);
        }

        /// <summary>
        /// Removes the given object from the render system.
        /// </summary>
        /// <param name="obj">The object adapter to remove.</param>
        public void RemoveObject(ObjectGraphicsAdapter obj)
        {
            objects.Remove(obj);
        }

        /// <summary>
        /// Updates the given object's property block to match
        /// the object's color.
        /// </summary>
        /// <param name="obj">The object adapter whose color to update.</param>
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
