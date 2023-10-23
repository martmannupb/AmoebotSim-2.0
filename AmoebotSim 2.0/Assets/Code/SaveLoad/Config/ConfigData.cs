using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AS2
{

    [Serializable]
    public class ConfigData
    {
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

        [Serializable]
        public class AdditionalConfiguration
        {
            [Tooltip("The border color of hexagonal and round particles. Should not be too bright. Adjust the shader border threshold value to avoid a too sharp transition between the center and border colors.")]
            public Color particleBorderColor = new Color(0.15f, 0.15f, 0.15f);
            [Tooltip("Color brightness threshold seen as a particle's interior by the shader. Used to avoid a sharp transition between the particle's center and its border as well as bleeding of the fill color. For bright border colors, a higher value is usually better.")]
            [Range(0f, 1f)]
            public float shaderBorderThreshold = 0.3f;

            [Tooltip("The color of beep origin highlights.")]
            public Color beepOriginColor = new Color(222f / 255f, 222f / 255f, 222f / 255f, 1f);
            [Tooltip("The color of beep reception highlights.")]
            public Color beepReceiveColor = new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);
            [Tooltip("The color of beep fault highlights.")]
            public Color faultyBeepColor = new Color(255f / 255f, 95f / 255f, 64f / 255f, 1f);
        }

        [Tooltip("Options that can be adjusted in the Settings Panel at runtime.")]
        public SettingsMenu settingsMenu;
        [Tooltip("Additional options that cannot be changed at runtime.")]
        public AdditionalConfiguration additionalConfiguration;
    }

} // namespace AS2
