// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// This class dynamically creates textures at runtime. For example, we take
    /// textures like hexagons as input and print a variable number of pins onto it.
    /// The class also uses the generated textures to create materials.
    /// </summary>
    public static class TextureCreator
    {

        // Textures
        // Pin Border Textures
        public static Texture2D pinBorderTextureEmpty;

        public static Dictionary<int, Texture2D> pinBorderTextures3Pins1 = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> pinBorderTextures3Pins2 = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> pinBorderTextures5Pins1 = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> pinBorderTextures5Pins2 = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> pinBorderCircTextures3Pins1 = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> pinBorderCircTextures3Pins2 = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> pinBorderCircTextures5Pins1 = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> pinBorderCircTextures5Pins2 = new Dictionary<int, Texture2D>();
        // Hexagon Textures
        public static Dictionary<int, Texture2D> hexagonTextures = new Dictionary<int, Texture2D>();
        public static Dictionary<int, Texture2D> hexagonCircTextures = new Dictionary<int, Texture2D>();

        // Materials
        public static Dictionary<int, Material> pinBorderHexMaterials = new Dictionary<int, Material>();
        public static Dictionary<int, Material> pinBorderHexCircMaterials = new Dictionary<int, Material>();

        public static Dictionary<int, Material> hexagonMaterials = new Dictionary<int, Material>();
        public static Dictionary<int, Material> hexagonCircMaterials = new Dictionary<int, Material>();

        private static Texture2D pinTexture = Resources.Load<Texture2D>(FilePaths.path_textures + "PinTex");
        private static Texture2D transTexture = Resources.Load<Texture2D>(FilePaths.path_textures + "TransparentPixel");
        private static Texture2D hexagonTexture = Resources.Load<Texture2D>("Images/Hexagons/HQ/Hexagon1_1024");
        private static Texture2D hexagonCircTexture = Resources.Load<Texture2D>("Images/Hexagons/HQ Soft/HexagonCircle");

        /// <summary>
        /// Creates the material with the generated texture for the pins
        /// with the invisible hexagon. Read the method doc to
        /// <see cref="GetPinBorderTexture(int, bool, bool, int, bool, ViewType)"/>
        /// to gain more info.
        /// </summary>
        /// <param name="pinsPerSide">The amount of pins per side.</param>
        /// <param name="viewType">The view type, Hexagonal or HexagonalCirc, not Circular!</param>
        /// <returns>A material that only renders pins, only while the
        /// particle is not moving.</returns>
        public static Material GetPinBorderMaterial(int pinsPerSide, ViewType viewType)
        {
            if (viewType == ViewType.Hexagonal && pinBorderHexMaterials.TryGetValue(pinsPerSide, out Material m1))
                return m1;
            if (viewType == ViewType.HexagonalCirc && pinBorderHexCircMaterials.TryGetValue(pinsPerSide, out Material m2))
                return m2;

            // Create Material
            Material hexMat = MaterialDatabase.material_hexagonal_particleCombined;
            Material mat = new Material(hexMat.shader);
            mat.CopyPropertiesFromMaterial(hexMat);
            // We have to set a new color threshold in case the border color is different
            mat.SetFloat("_BorderColorThreshold", Config.ConfigData.additionalConfiguration.shaderBorderThreshold);
            //Texture2D borderTex1 = GetPinBorderTextureEmpty();
            //Texture2D borderTex2 = GetPinBorderTextureEmpty();
            Texture2D borderTex100P = GetPinBorderTexture(pinsPerSide, true, false, 0, false, viewType);
            Texture2D borderTex100P2 = GetPinBorderTexture(pinsPerSide, true, false, 3, true, viewType);
            //mat.SetTexture("_TextureHexagon", borderTex1);
            //mat.SetTexture("_TextureHexagon2", borderTex2);
            mat.SetTexture("_TextureHexagon", transTexture);
            mat.SetTexture("_TextureHexagon2", transTexture);
            mat.SetTexture("_TextureHexagon100P", borderTex100P);
            mat.SetTexture("_TextureHexagon100P2", borderTex100P2);
            mat.SetTexture("_TextureHexagonConnector", transTexture);
            // Update render queue to be the same as for pins
            mat.renderQueue = RenderSystem.renderQueue_pins;

            // Add Material to Data
            if (viewType == ViewType.Hexagonal) pinBorderHexMaterials.Add(pinsPerSide, mat);
            if (viewType == ViewType.HexagonalCirc) pinBorderHexCircMaterials.Add(pinsPerSide, mat);

            // Return
            return mat;
        }

        /// <summary>
        /// Creates the material with the generated texture for the pins
        /// with the visible hexagon. Read the method doc of
        /// <see cref="GetHexagonBaseTextureWithPins(int, ViewType)"/>
        /// to gain more info.
        /// </summary>
        /// <param name="pinsPerSide">The amount of pins per side.</param>
        /// <param name="viewType">The view type, Hexagonal or HexagonalCirc, not Circular!</param>
        /// <returns>The material used to draw particles with
        /// <paramref name="pinsPerSide"/> pins in the hexagonal view type
        /// <paramref name="viewType"/>.</returns>
        public static Material GetHexagonWithPinsMaterial(int pinsPerSide, ViewType viewType)
        {
            if (viewType == ViewType.Hexagonal && hexagonMaterials.TryGetValue(pinsPerSide, out Material m1))
                return m1;
            if (viewType == ViewType.HexagonalCirc && hexagonCircMaterials.TryGetValue(pinsPerSide, out Material m2))
                return m2;

            // Create Material
            Material hexMat = MaterialDatabase.material_hexagonal_particleCombined;
            Material mat = new Material(hexMat.shader);
            mat.CopyPropertiesFromMaterial(hexMat);
            // We have to set a new color threshold in case the border color is different
            mat.SetFloat("_BorderColorThreshold", Config.ConfigData.additionalConfiguration.shaderBorderThreshold);
            Texture2D hexTex1 = GetHexagonBaseTextureWithPins(pinsPerSide, viewType);
            mat.SetTexture("_TextureHexagon", hexTex1);
            mat.SetTexture("_TextureHexagon2", hexTex1);
            mat.SetTexture("_TextureHexagon100P", hexTex1);
            mat.SetTexture("_TextureHexagon100P2", hexTex1);
            //mat.SetTexture("_TextureHexagonConnector", transTexture);

            // Add Material to Data
            if (viewType == ViewType.Hexagonal) hexagonMaterials.Add(pinsPerSide, mat);
            else if (viewType == ViewType.HexagonalCirc) hexagonCircMaterials.Add(pinsPerSide, mat);

            // Return
            return mat;
        }
        
        /// <summary>
        /// This thing creates a texture from a transparent texture and dots which represent the pins.
        /// The pins are merged on top of the original transparent texture to get a texture with pins.
        /// This is done to lay this texture with a mesh on top of the original hexagon, so we can fit
        /// circuits in between the two meshes and give the impression that all is one connected component.
        /// </summary>
        /// <param name="pinsPerSide">The amount of pins per side.</param>
        /// <param name="omitSide">Whether one side's pins should be omitted.</param>
        /// <param name="omit3Pins">Whether three pins should be omitted (the
        /// <paramref name="omittedSide"/> and the two neighboring sides).</param>
        /// <param name="omittedSide">The side of the omitted pin/s.</param>
        /// <param name="isTex1">Whether this is texture 1 of the 2 possible texture positions in the
        /// final shader. One of the two textures is used for the stationary part and one for the
        /// moving part during an animation.</param>
        /// <param name="viewType">The type of the base hexagon texture. Circular and hexagonal forms
        /// have different pin positions. Accepted inputs: Hexagonal or HexagonalCirc, but not Circular!</param>
        /// <returns>A transparent texture with the specified pins.</returns>
        private static Texture2D GetPinBorderTexture(int pinsPerSide, bool omitSide, bool omit3Pins, int omittedSide, bool isTex1, ViewType viewType)
        {
            if(viewType == ViewType.Hexagonal)
            {
                if (isTex1 && omit3Pins && pinBorderTextures3Pins1.TryGetValue(pinsPerSide, out Texture2D t1))
                    return t1;
                if (isTex1 && !omit3Pins && pinBorderTextures5Pins1.TryGetValue(pinsPerSide, out Texture2D t2))
                    return t2;
                if (!isTex1 && omit3Pins && pinBorderTextures3Pins2.TryGetValue(pinsPerSide, out Texture2D t3))
                    return t3;
                if (!isTex1 && !omit3Pins && pinBorderTextures5Pins2.TryGetValue(pinsPerSide, out Texture2D t4))
                    return t4;
            }
            else if(viewType == ViewType.HexagonalCirc)
            {
                if (isTex1 && omit3Pins && pinBorderCircTextures3Pins1.TryGetValue(pinsPerSide, out Texture2D t1))
                    return t1;
                if (isTex1 && !omit3Pins && pinBorderCircTextures5Pins1.TryGetValue(pinsPerSide, out Texture2D t2))
                    return t2;
                if (!isTex1 && omit3Pins && pinBorderCircTextures3Pins2.TryGetValue(pinsPerSide, out Texture2D t3))
                    return t3;
                if (!isTex1 && !omit3Pins && pinBorderCircTextures5Pins2.TryGetValue(pinsPerSide, out Texture2D t4))
                    return t4;
            }

            // Create Texture
            Texture2D tex = new Texture2D(1024, 1024);
            tex.wrapMode = TextureWrapMode.Clamp;
            //tex.filterMode = ;

            int tex_width = tex.width;
            int tex_height = tex.height;
            int pin_tex_width = pinTexture.width;
            int pin_tex_height = pinTexture.height;

            // Metadata
            Vector2Int texCenterPixel = new Vector2Int(tex_width / 2, tex_height / 2);

            // Get the pixel data for faster reading and writing
            Color[] texPixels = tex.GetPixels();
            Color[] pinPixels = pinTexture.GetPixels();

            // Make Tex Transparent
            Color colorTransparent = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < tex_height; y++)
            {
                int yy = y * tex_width;
                for (int x = 0; x < tex_width; x++)
                {
                    texPixels[x + yy] = colorTransparent;
                }
            }

            // Add Pins to Texture
            // First collect the pin positions
            // Calc center pos of this pin relative from texture center
            List<Vector2Int> pinStartPositions = new List<Vector2Int>();
            Vector2 relPosTopRight = new Vector2(AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2);
            Vector2 relPosBottomRight = new Vector2(AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2);
            for (int j = 0; j < pinsPerSide; j++)
            {
                Vector2 relPosPinRight = Vector2.zero;
                if (viewType == ViewType.Hexagonal)
                {
                    // Hexagonal Particles (we take the right side as reference)
                    Vector2 relDistBottomToTop = relPosTopRight - relPosBottomRight;
                    if (pinsPerSide == 1) relPosPinRight = relPosBottomRight + 0.5f * relDistBottomToTop;
                    else
                    {
                        Vector2 relStep = relDistBottomToTop / (pinsPerSide + 1);
                        relPosPinRight = relPosBottomRight + (j + 1) * relStep;
                    }
                }
                else if (viewType == ViewType.HexagonalCirc)
                {
                    // Circular Particles with Circuits (we have a circle and work with angles and the distance to the center)
                    float distanceToCenter = AmoebotFunctions.hexRadiusMinor;
                    relPosPinRight = new Vector2(distanceToCenter, 0f);
                    if (pinsPerSide > 1)
                    {
                        float angleStep = 60f / (pinsPerSide + 1);
                        float angle = -30f + (j + 1) * angleStep;
                        relPosPinRight = Quaternion.Euler(new Vector3(0f, 0f, angle)) * relPosPinRight;
                    }
                }
                // Use relPosPinRight to calculate absolute positions
                for (int k = 0; k < 6; k++)
                {
                    bool isOmitted;
                    if (omit3Pins) isOmitted = k == omittedSide || ((k + 6 - 1) % 6) == omittedSide || ((k + 1) % 6) == omittedSide;
                    else isOmitted = k == omittedSide;
                    if (omitSide == false || !isOmitted)
                    {
                        Vector2 relPosRotated = Quaternion.Euler(new Vector3(0f, 0f, 60f * k)) * relPosPinRight;
                        Vector2 absPosRotated = texCenterPixel + new Vector2(0.5f * relPosRotated.x * tex_width, 0.5f * relPosRotated.y * tex_height);
                        Vector2 startPosF = absPosRotated - new Vector2(pin_tex_width / 2.0f, pin_tex_height / 2.0f);
                        Vector2Int startPos = new Vector2Int(Mathf.RoundToInt(startPosF.x), Mathf.RoundToInt(startPosF.y));

                        pinStartPositions.Add(startPos);
                    }
                }
            }

            // Now add the pins to the texture
            for (int y = 0; y < pin_tex_height; y++)
            {
                int yy = y * pin_tex_width;
                for (int x = 0; x < pin_tex_width; x++)
                {
                    Color pinTexturePixel = pinPixels[x + yy];
                    if (pinTexturePixel.a > 0.1f)
                    {
                        foreach (Vector2Int startPos in pinStartPositions)
                        {
                            int texPos_x = startPos.x + x;
                            int texPos_y = startPos.y + y;
                            if (texPos_x >= 0 && texPos_x < tex_width && texPos_y >= 0 && texPos_y < tex_height)
                                texPixels[texPos_x + texPos_y * tex_width] = pinTexturePixel;
                        }
                    }
                }
            }

            // Apply
            tex.SetPixels(texPixels);
            tex.Apply();

            // Add Texture to Data
            if (viewType == ViewType.Hexagonal)
            {
                if (isTex1 && omit3Pins) pinBorderTextures3Pins1.Add(pinsPerSide, tex);
                if (isTex1 && !omit3Pins) pinBorderTextures5Pins1.Add(pinsPerSide, tex);
                if (!isTex1 && omit3Pins) pinBorderTextures3Pins2.Add(pinsPerSide, tex);
                if (!isTex1 && !omit3Pins) pinBorderTextures5Pins2.Add(pinsPerSide, tex);
            }
            else if (viewType == ViewType.HexagonalCirc)
            {
                if (isTex1 && omit3Pins) pinBorderCircTextures3Pins1.Add(pinsPerSide, tex);
                if (isTex1 && !omit3Pins) pinBorderCircTextures5Pins1.Add(pinsPerSide, tex);
                if (!isTex1 && omit3Pins) pinBorderCircTextures3Pins2.Add(pinsPerSide, tex);
                if (!isTex1 && !omit3Pins) pinBorderCircTextures5Pins2.Add(pinsPerSide, tex);
            }

            // Return
            return tex;
        }

        private static Texture2D GetPinBorderTextureEmpty()
        {
            if (pinBorderTextureEmpty != null) return pinBorderTextureEmpty;

            // Create Texture
            Texture2D tex = new Texture2D(1024, 1024);
            tex.wrapMode = TextureWrapMode.Clamp;
            //tex.filterMode = ;

            // Make Tex Transparent
            Color colorTransparent = new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, colorTransparent);
                }
            }

            // Apply
            tex.Apply();

            pinBorderTextureEmpty = tex;

            // Return
            return tex;
        }

        /// <summary>
        /// Creates a texture from one of the base hexagon textures
        /// and dots which represent the pins.
        /// The pins are merged on top of the original hexagon to get a figure with pins.
        /// </summary>
        /// <param name="pinsPerSide">The amount of pins per side.</param>
        /// <param name="viewType">The view type, e.g. the standard hexagon or the circular view.
        /// Please only use Hexagonal and HexagonalCirc here (the hexagonal base grid view),
        /// Circular (the graph view) is not meant to work.</param>
        /// <returns>A texture containing a hexagon or circle based on
        /// <paramref name="viewType"/> with <paramref name="pinsPerSide"/> pins
        /// on each of its six edges.</returns>
        private static Texture2D GetHexagonBaseTextureWithPins(int pinsPerSide, ViewType viewType)
        {
            // Create Texture
            Texture2D tex = new Texture2D(1024, 1024);
            tex.wrapMode = TextureWrapMode.Clamp;

            //tex.filterMode = ;

            int tex_width = tex.width;
            int tex_height = tex.height;
            int pin_tex_width = pinTexture.width;
            int pin_tex_height = pinTexture.height;

            // Metadata
            Vector2Int texCenterPixel = new Vector2Int(tex_width / 2, tex_height / 2);

            // Get the pixel data for faster reading and writing
            Color[] texColors = tex.GetPixels();
            Color[] hexColors = viewType == ViewType.Hexagonal ? hexagonTexture.GetPixels() : hexagonCircTexture.GetPixels();
            Color[] pinColors = pinTexture.GetPixels();

            // Fill Tex with Hexagon/HexagonCirc Tex
            for (int y = 0; y < tex_height; y++)
            {
                int yy = y * tex_width;
                for (int x = 0; x < tex_width; x++)
                {
                    Color pixel = hexColors[x + yy];

                    // Replace black border with custom color
                    float alpha = pixel.a;
                    Color gray = ColorData.particleBorderColor;
                    pixel = (1.0f - pixel.grayscale) * gray + pixel.grayscale * pixel;
                    pixel.a = alpha;
                    texColors[x + yy] = pixel;
                }
            }

            // Add Pins to Texture
            // First collect the pin positions
            // Calc center pos of right pin relative from texture center
            List<Vector2Int> pinStartPositions = new List<Vector2Int>();
            Vector2 relPosTopRight = new Vector2(AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2);
            Vector2 relPosBottomRight = new Vector2(AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2);
            for (int j = 0; j < pinsPerSide; j++)
            {
                Vector2 relPosPinRight = Vector2.zero;
                if (viewType == ViewType.Hexagonal)
                {
                    // Hexagonal Particles (we take the right side as reference)
                    Vector2 relDistBottomToTop = relPosTopRight - relPosBottomRight;
                    if (pinsPerSide == 1) relPosPinRight = relPosBottomRight + 0.5f * relDistBottomToTop;
                    else
                    {
                        Vector2 relStep = relDistBottomToTop / (pinsPerSide + 1);
                        relPosPinRight = relPosBottomRight + (j + 1) * relStep;
                    }
                }
                else if (viewType == ViewType.HexagonalCirc)
                {
                    // Circular Particles with Circuits (we have a circle and work with angles and the distance to the center)
                    float distanceToCenter = AmoebotFunctions.hexRadiusMinor;
                    relPosPinRight = new Vector2(distanceToCenter, 0f);
                    if (pinsPerSide > 1)
                    {
                        float angleStep = 60f / (pinsPerSide + 1);
                        float angle = -30f + (j + 1) * angleStep;
                        relPosPinRight = Quaternion.Euler(new Vector3(0f, 0f, angle)) * relPosPinRight;
                    }
                }
                // Use relPosPinRight to calculate absolute positions
                for (int k = 0; k < 6; k++)
                {
                    Vector2 relPosRotated = Quaternion.Euler(new Vector3(0f, 0f, 60f * k)) * relPosPinRight;
                    Vector2 absPosRotated = texCenterPixel + new Vector2(0.5f * relPosRotated.x * tex_width, 0.5f * relPosRotated.y * tex_height);
                    Vector2 startPosF = absPosRotated - new Vector2(pin_tex_width / 2.0f, pin_tex_height / 2.0f);
                    Vector2Int startPos = new Vector2Int(Mathf.RoundToInt(startPosF.x), Mathf.RoundToInt(startPosF.y));
                    pinStartPositions.Add(startPos);
                }
            }

            // Now add the pins to the texture
            for (int y = 0; y < pin_tex_height; y++)
            {
                int yy = y * pin_tex_width;
                for (int x = 0; x < pin_tex_width; x++)
                {
                    //Color colorPin = pinTexture.GetPixel(x, y);
                    Color colorPin = pinColors[x + yy];
                    foreach (Vector2Int startPos in pinStartPositions)
                    {
                        int texPos_x = startPos.x + x;
                        int texPos_y = startPos.y + y;
                        if (texPos_x >= 0 && texPos_x < tex_width && texPos_y >= 0 && texPos_y < tex_height)
                        {
                            // Merge Textures (based on overlay alpha)
                            // Get color to merge with
                            Color colorBase = texColors[texPos_x + texPos_y * tex_width];
                            // Calculate final color (interpolate between colors)
                            Color colorNew = new Color(Mathf.Lerp(colorBase.r, colorPin.r, colorPin.a), Mathf.Lerp(colorBase.g, colorPin.g, colorPin.a), Mathf.Lerp(colorBase.b, colorPin.b, colorPin.a), colorBase.a + (1f - colorBase.a) * colorPin.a);
                            // Set color
                            texColors[texPos_x + texPos_y * tex_width] = colorNew;
                        }
                    }
                }
            }

            // Apply
            tex.SetPixels(texColors);
            tex.Apply();

            // Add Texture to Data
            if (viewType == ViewType.Hexagonal && hexagonTextures.ContainsKey(pinsPerSide) == false) hexagonTextures.Add(pinsPerSide, tex);
            if (viewType == ViewType.HexagonalCirc && hexagonCircTextures.ContainsKey(pinsPerSide) == false) hexagonCircTextures.Add(pinsPerSide, tex);

            // Return
            return tex;
        }

    }

} // namespace AS2.Visuals
