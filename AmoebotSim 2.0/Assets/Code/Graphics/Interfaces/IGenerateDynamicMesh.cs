// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AS2.Visuals
{

    /// <summary>
    /// Interface that is implemented by classes that need to
    /// regenerate meshes when certain parameters change.
    /// </summary>
    public interface IGenerateDynamicMesh
    {

        /// <summary>
        /// Regenerates the meshes of this class.
        /// </summary>
        void RegenerateMeshes();
    }

}
