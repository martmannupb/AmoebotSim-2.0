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

        // Standard Circuit Colors
        public static Color[] Circuit_Colors = new Color[] {
            // Tertiary colors
            // Amber, Vermillion, Magenta, Violet, Teal, Chartreuse
            new Color(255f / 255f, 191f / 255f, 0f / 255f),
            new Color(227f / 255f, 66f / 255f, 52f / 255f),
            new Color(255f / 255f, 0f / 255f, 255f / 255f),
            new Color(143f / 255f, 0f / 255f, 255f / 255f),
            new Color(0f / 255f, 128f / 255f, 128f / 255f),
            new Color(127f / 255f, 255f / 255f, 0f / 255f)
        };

        // Other Colors
        public static Color beepOrigin = new Color(222f / 255f, 222f / 255f, 222f / 255f, 1f);
        public static Color beepReceive = new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);
        public static Color faultyBeep = new Color(255f / 255f, 95f / 255f, 64f / 255f, 1f);

        public static Color particleBorderColor = new Color(0.15f, 0.15f, 0.15f);

        private static Color defaultHexBGColor = new Color(121f / 255f, 121f / 255f, 121f / 255f, 1f);

        public static Color ConvertColorToHexBGColor(Color color)
        {
            return new Color((defaultHexBGColor.r + color.r) / 2f, (defaultHexBGColor.g + color.g) / 2f, (defaultHexBGColor.b + color.b) / 2f, (defaultHexBGColor.a + color.a) / 2f);
        }


    }

} // namespace AS2
