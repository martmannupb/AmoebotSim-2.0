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

    public static class ColorData
    {

        // Standard Particle Colors
        public static Color Particle_Black = new Color(45f / 255f, 45f / 255f, 45f / 255f, 1f);
        public static Color Particle_Yellow = new Color(202f / 255f, 192f / 255f, 91f / 255f, 1f);
        public static Color Particle_Orange = new Color(202f / 255f, 155f / 255f, 91f / 255f, 1f);
        public static Color Particle_Red = new Color(202f / 255f, 95f / 255f, 91f / 255f, 1f);
        public static Color Particle_Purple = new Color(202f / 255f, 91f / 255f, 168f / 255f, 1f);
        public static Color Particle_Blue = new Color(91f / 255f, 175f / 255f, 202f / 255f, 1f);
        public static Color Particle_BlueDark = new Color(91f / 255f, 123f / 255f, 202f / 255f, 1f);
        public static Color Particle_Aqua = new Color(91f / 255f, 202f / 255f, 154f / 255f, 1f);
        public static Color Particle_Green = new Color(111f / 255f, 202f / 255f, 91f / 255f, 1f);

        // Additional Colors
        /// <summary>
        /// Additional custom colors to choose from. These are specified in the configuration
        /// file and can be used for particles, objects, circuits, etc.
        /// </summary>
        public static Color[] Additional_Colors = Config.ConfigData.additionalConfiguration.additionalColors;

        // Standard Circuit Colors
        public static Color[] Circuit_Colors = Config.ConfigData.additionalConfiguration.circuitColors;

        // Other Colors
        public static Color beepOrigin = Config.ConfigData.additionalConfiguration.beepOriginColor;
        public static Color beepReceive = Config.ConfigData.additionalConfiguration.beepReceiveColor;
        public static Color faultyBeep = Config.ConfigData.additionalConfiguration.faultyBeepColor;

        public static Color particleBorderColor = Config.ConfigData.additionalConfiguration.particleBorderColor;
    }

} // namespace AS2
