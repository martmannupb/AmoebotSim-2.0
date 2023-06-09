using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{
    /// <summary>
    /// Helper for instanced drawing of matrices.
    /// Contains a dynamically extendable list of matrices and lets you add matrices that should be drawn.
    /// Automatically adds new matrix arrays if the max. count of instanced drawing is reached.
    /// Once the structure is built, created lists stay in the system, so no new objects are created and the
    /// system works with maximal efficiency.
    /// </summary>
    public class InstancedDrawer
    {

        private List<Matrix4x4[]> matricesList = new List<Matrix4x4[]>();
        private int count = 0;      // The total number of managed matrices
        private int Capacity {
            get {
                return matricesList.Count * 1024;
            }
        }

        /// <summary>
        /// Adds a matrix to the system.
        /// </summary>
        /// <param name="matrix">The matrix to be added.</param>
        public void AddMatrix(Matrix4x4 matrix)
        {
            if (Capacity < count + 1) matricesList.Add(new Matrix4x4[1024]);
            // Calc Index
            int listNumber = count / 1024;
            int listID = count % 1024;
            // Add Matrix
            matricesList[listNumber][listID] = matrix;
            // Incr
            count++;
        }

        /// <summary>
        /// Clears the drawn matrices.
        /// Internally, only the counter is set to 0 and the matrices will be overridden when you add new matrices.
        /// </summary>
        public void ClearMatrices()
        {
            count = 0;
        }

        /// <summary>
        /// Draws the matrices stored in this class.
        /// </summary>
        /// <param name="mesh">The mesh to use.</param>
        /// <param name="mat">The material to use.</param>
        /// <param name="matPropBlock">A material property block. Can be omitted.</param>
        public void Draw(Mesh mesh, Material mat, MaterialPropertyBlock matPropBlock = null)
        {
            int listDrawAmount = count / 1024 + 1;
            for (int i = 0; i < listDrawAmount; i++)
            {
                int idDrawAmount = i == listDrawAmount - 1 ? count % 1024 : 1024;
                Graphics.DrawMeshInstanced(mesh, 0, mat, matricesList[i], idDrawAmount, matPropBlock);
            }
        }
    }
}
