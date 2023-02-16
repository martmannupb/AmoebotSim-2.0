using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    // Copyright ©
    // Part of the personal code library of Tobias Maurer (tobias.maurer.it@web.de).
    // Usage by any current or previous members the University of paderborn and projects associated with the University or programmable matter is permitted.
    public abstract class RenderBatch<T>
    {

        // Init
        private bool initialized = false;

        // Data
        private List<Matrix4x4[]> listOfMatrixArrays = new List<Matrix4x4[]>();
        protected MaterialPropertyBlock materialPropertyBlock;
        protected int currentIndex = 0;
        protected const int maxArraySize = 1023;

        // Precalculated Data _____
        // Meshes
        protected Mesh mesh;
        protected Material material;
        protected int layer;

        // Settings _____
        public T properties;

        public RenderBatch() { }

        public RenderBatch(Mesh mesh, Material material, int layer) : this(default(T), mesh, material, layer) { }

        public RenderBatch(T properties, Mesh mesh, Material material, int layer)
        {
            Init(properties, mesh, material, layer);
        }

        /// <summary>
        /// This is either called by one of the long constructors of the base class or should be called manually by the constructor of the subclass if you use the empty base constructor.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="mesh"></param>
        /// <param name="material"></param>
        protected void Init(T properties, Mesh mesh, Material material, int layer)
        {
            this.properties = properties;
            this.material = material;
            if (mesh == null) this.mesh = Library.MeshConstants.getDefaultMeshQuad();
            else this.mesh = mesh;
            this.layer = layer;
            this.materialPropertyBlock = new MaterialPropertyBlock();

            initialized = true;

            SetUp();
        }

        protected abstract void SetUp();



        /// <summary>
        /// Adds one matrix to the system.
        /// </summary>
        /// <param name="matrix"></param>
        protected void AddMatrix(Matrix4x4 matrix)
        {
            if (currentIndex >= maxArraySize * listOfMatrixArrays.Count)
            {
                // Add an Array
                listOfMatrixArrays.Add(new Matrix4x4[maxArraySize]);
            }
            int listNumber = currentIndex / maxArraySize;
            int listIndex = currentIndex % maxArraySize;
            listOfMatrixArrays[listNumber][listIndex] = matrix;
            currentIndex++;
        }

        /// <summary>
        /// Sets the current index to the given index. Defines the last element (the count) to be displayed.
        /// </summary>
        /// <param name="currentIndex"></param>
        public void SetCurrentIndex(int currentIndex)
        {
            if(currentIndex <= maxArraySize * listOfMatrixArrays.Count)
            {
                this.currentIndex = currentIndex;
                if (this.currentIndex < 0) this.currentIndex = 0;
            }
        }

        public void DecrementCurrentIndex()
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = 0;
        }

        /// <summary>
        /// Clears the matrices, so nothing gets rendered anymore. The lists can be filled with new data now.
        /// (Actually just sets the index to 0, so we dont draw anything anymore.)
        /// </summary>
        public void ClearMatrices()
        {
            currentIndex = 0;
        }

        public void Draw()
        {
            if(initialized == false)
            {
                Log.Error("RenderBatch: You are trying to use a batch that has not been initialized correctly. Either use a long base constructor or call the Init method!");
                return;
            }
            if (currentIndex == 0) return;

            int listDrawAmount = ((currentIndex - 1) / maxArraySize) + 1;
            for (int i = 0; i < listDrawAmount; i++)
            {
                int count;
                if (i < listDrawAmount - 1) count = maxArraySize;
                else count = currentIndex % maxArraySize;

                Graphics.DrawMeshInstanced(mesh, 0, material, listOfMatrixArrays[i], count, materialPropertyBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false, layer);
            }
        }
    }

}