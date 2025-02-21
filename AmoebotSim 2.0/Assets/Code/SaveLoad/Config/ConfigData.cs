// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Container for all settings that can be modified in the
    /// configuration file. This class is serializable, so it
    /// can be translated to and from JSON easily.
    /// </summary>
    [Serializable]
    public class ConfigData
    {
        /// <summary>
        /// Container for all configuration settings that can also
        /// be changed in the Settings Panel at runtime.
        /// </summary>
        [Serializable]
        public class SettingsMenu
        {
            [Tooltip("Whether movement animations should be played.")]
            public bool movementAnimationsOn = true;

            [Tooltip("Whether the compass overlay should be displayed using arrows or direction names.")]
            public bool drawCompassOverlayAsArrows = true;

            [Tooltip("Whether the connections between the pins of neighboring particles should have black borders.")]
            public bool drawCircuitBorder = true;

            [Tooltip("Whether a colored ring should be displayed around particle heads and tails in the triangular grid view.")]
            public bool drawParticleRing = true;

            [Tooltip("Whether the application should run in fullscreen mode (does not work in the Editor).")]
            public bool fullscreen = false;

            [Tooltip("Whether tooltips should be displayed.")]
            public bool showTooltips = true;

            [Tooltip("The probability of beeps and messages not being received on a partition set.")]
            [Range(0f, 1f)]
            public float beepFailureProbability = 0f;
        }

        /// <summary>
        /// Container for all configuration settings that cannot
        /// be changed in the Settings Panel at runtime.
        /// </summary>
        [Serializable]
        public class AdditionalConfiguration
        {
            [Tooltip("The border color of hexagonal and round particles. Should not be too bright. Adjust the shader border threshold value to avoid a too sharp transition between the center and border colors.")]
            public Color particleBorderColor = new Color(0.15f, 0.15f, 0.15f);
            [Tooltip("Color brightness threshold seen as a particle's interior by the shader. Used to avoid a sharp transition between the particle's center and its border as well as bleeding of the fill color. For bright border colors, a higher value is usually better.")]
            [Range(0f, 1f)]
            public float shaderBorderThreshold = 0.2f;
            
            [Tooltip("The color of beep origin highlights.")]
            public Color beepOriginColor = new Color(222f / 255f, 222f / 255f, 222f / 255f, 1f);
            [Tooltip("The color of beep reception highlights.")]
            public Color beepReceiveColor = new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);
            [Tooltip("The color of beep fault highlights.")]
            public Color faultyBeepColor = new Color(255f / 255f, 95f / 255f, 64f / 255f, 1f);

            [Tooltip("Default colors used for circuits. When circuit colors are assigned automatically, the colors are picked from this list in a round-robin fashion. The default colors are the tertiary colors: Amber, Vermillion, Magenta, Violet, Teal and Chartreuse.")]
            public Color[] circuitColors = new Color[] {
                // Tertiary colors
                // Amber, Vermillion, Magenta, Violet, Teal, Chartreuse
                new Color(255f / 255f, 191f / 255f, 0f / 255f),
                new Color(227f / 255f, 66f / 255f, 52f / 255f),
                new Color(255f / 255f, 0f / 255f, 255f / 255f),
                new Color(143f / 255f, 0f / 255f, 255f / 255f),
                new Color(0f / 255f, 128f / 255f, 128f / 255f),
                new Color(127f / 255f, 255f / 255f, 0f / 255f)
            };

            [Tooltip("Additional colors for general use available in the static ColorData class. By default, these are the fixed particle colors that are already available individually (Particle_Black, Particle_Yellow etc.).")]
            public Color[] additionalColors = new Color[] {
                // Default particle colors:
                // Black, Yellow, Orange, Red, Purple, Blue,
                // BlueDark, Aqua, Green
                new Color(45f / 255f, 45f / 255f, 45f / 255f, 1f),
                new Color(202f / 255f, 192f / 255f, 91f / 255f, 1f),
                new Color(202f / 255f, 155f / 255f, 91f / 255f, 1f),
                new Color(202f / 255f, 95f / 255f, 91f / 255f, 1f),
                new Color(202f / 255f, 91f / 255f, 168f / 255f, 1f),
                new Color(91f / 255f, 175f / 255f, 202f / 255f, 1f),
                new Color(91f / 255f, 123f / 255f, 202f / 255f, 1f),
                new Color(91f / 255f, 202f / 255f, 154f / 255f, 1f),
                new Color(111f / 255f, 202f / 255f, 91f / 255f, 1f)
            };
        }

        [Tooltip("Options that can be adjusted in the Settings Panel at runtime.")]
        public SettingsMenu settingsMenu;
        [Tooltip("Additional options that cannot be changed at runtime.")]
        public AdditionalConfiguration additionalConfiguration;

        /// <summary>
        /// Creates a configuration with all default values.
        /// </summary>
        /// <returns>A new configuration object where each
        /// option has the default value.</returns>
        public static ConfigData GetDefault()
        {
            ConfigData data = new ConfigData();
            data.settingsMenu = new SettingsMenu();
            data.additionalConfiguration = new AdditionalConfiguration();
            return data;
        }
    }

} // namespace AS2
