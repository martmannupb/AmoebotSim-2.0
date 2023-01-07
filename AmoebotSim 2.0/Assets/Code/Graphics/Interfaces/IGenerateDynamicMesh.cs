using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AS2.Visuals
{

    /// <summary>
    /// Interface that is implemented by classes that need to regenerate meshes when certain parameters change.
    /// </summary>
    public interface IGenerateDynamicMesh
    {

        /// <summary>
        /// Regenerates the meshes of this class.
        /// </summary>
        void RegenerateMeshes();

    }

}