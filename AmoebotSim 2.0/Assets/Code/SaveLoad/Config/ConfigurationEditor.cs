// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Script for the configuration editor GameObject.
    /// The custom Inspector content is implemented by the
    /// <see cref="AS2.ConfigurationEditorBehavior"/>.
    /// </summary>
    [ExecuteInEditMode]
    public class ConfigurationEditor : MonoBehaviour
    {
        /// <summary>
        /// The configuration data to be editable in the Inspector
        /// </summary>
        public ConfigData configData;
    }

} // namespace AS2
