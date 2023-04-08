using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// This monstrosity dynamically creates textures at runtime. For example, we take textures like hexagons as input and print a generic number of pins onto it.
    /// The class wants to confuse you. Don't let it confuse you!
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
        private static Texture2D hexagonTexture = Resources.Load<Texture2D>("Images/Hexagons/HQ Soft/HexagonSoft1_1024");
        private static Texture2D hexagonCircTexture = Resources.Load<Texture2D>("Images/Hexagons/HQ Soft/HexagonCircleSoft");

        /// <summary>
        /// Creates the material with the generated texture for the pins with the invisible hexagon. Read the method doc to GetPinBorderTexture to gain more info.
        /// </summary>
        /// <param name="pinsPerSide">The amount of pins per side.</param>
        /// <param name="viewType">The view type, Hexagonal or HexagonalCirc, not Circular!</param>
        /// <returns></returns>
        public static Material GetPinBorderMaterial(int pinsPerSide, ViewType viewType)
        {
            if (viewType == ViewType.Hexagonal && pinBorderHexMaterials.ContainsKey(pinsPerSide)) return pinBorderHexMaterials[pinsPerSide];
            if (viewType == ViewType.HexagonalCirc && pinBorderHexCircMaterials.ContainsKey(pinsPerSide)) return pinBorderHexCircMaterials[pinsPerSide];

            // Create Material
            Material hexMat = MaterialDatabase.material_hexagonal_particleCombined;
            Material mat = new Material(hexMat.shader);
            mat.CopyPropertiesFromMaterial(hexMat);
            Texture2D borderTex1 = GetPinBorderTexture(pinsPerSide, true, true, 0, false, viewType);
            Texture2D borderTex2 = GetPinBorderTexture(pinsPerSide, true, true, 3, true, viewType);
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
            if (viewType == ViewType.Hexagonal && hexagonMaterials.ContainsKey(pinsPerSide)) return hexagonMaterials[pinsPerSide];
            if (viewType == ViewType.HexagonalCirc && hexagonCircMaterials.ContainsKey(pinsPerSide)) return hexagonCircMaterials[pinsPerSide];

            // Create Material
            Material hexMat = MaterialDatabase.material_hexagonal_particleCombined;
            Material mat = new Material(hexMat.shader);
            mat.CopyPropertiesFromMaterial(hexMat);
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
        /// This thing creates a texture from one of the base hexagon textures and dots which represent the pins.
        /// The pins are merged on top of the original hexagon to get a figure with pins.
        /// </summary>
        /// <param name="pinsPerSide">The amount of pins per side.</param>
        /// <param name="viewType">The view type, e.g. the standard hexagon or the circular view.
        /// Please only use Hexagonal and HexagonalCirc here (the hexagonal base grid view), Circular (the graph view) is not meant to work.</param>
        /// <returns></returns>
        /// <summary>
        /// This thing creates a texture from a transparent texture and dots which represent the pins.
        /// The pins are merged on top of the original transparent texture to get a texture with pins.
        /// This is done to lay this texture with a mesh on top of the original hexagon, so we can fit circuits in between the two meshes
        /// and give the impression that all is one connected component.
        /// </summary>
        /// <param name="pinsPerSide">The amount of pins per side.</param>
        /// <param name="omitSide">If one side's pin should be omitted.</param>
        /// <param name="omit3Pins">If three pins should be omitted (the omittedSide and the neighboring sides),</param>
        /// <param name="omittedSide">The side of the omitted pin/s.</param>
        /// <param name="isTex1">If this is texture 1 of the 2 possible texture positions in the final shader.</param>
        /// <param name="viewType">The type of the base hexagon texture. Circular and hexagonal forms have different pin positions.
        /// Accepted inputs: Hexagonal or HexagonalCirc, but not Circular!</param>
        /// <returns></returns>
        private static Texture2D GetPinBorderTexture(int pinsPerSide, bool omitSide, bool omit3Pins, int omittedSide, bool isTex1, ViewType viewType)
        {
            if(viewType == ViewType.Hexagonal)
            {
                if (isTex1 && omit3Pins && pinBorderTextures3Pins1.ContainsKey(pinsPerSide)) return pinBorderTextures3Pins1[pinsPerSide];
                if (isTex1 && !omit3Pins && pinBorderTextures5Pins1.ContainsKey(pinsPerSide)) return pinBorderTextures5Pins1[pinsPerSide];
                if (!isTex1 && omit3Pins && pinBorderTextures3Pins2.ContainsKey(pinsPerSide)) return pinBorderTextures3Pins2[pinsPerSide];
                if (!isTex1 && !omit3Pins && pinBorderTextures5Pins2.ContainsKey(pinsPerSide)) return pinBorderTextures5Pins2[pinsPerSide];
            }
            else if(viewType == ViewType.HexagonalCirc)
            {
                if (isTex1 && omit3Pins && pinBorderCircTextures3Pins1.ContainsKey(pinsPerSide)) return pinBorderCircTextures3Pins1[pinsPerSide];
                if (isTex1 && !omit3Pins && pinBorderCircTextures5Pins1.ContainsKey(pinsPerSide)) return pinBorderCircTextures5Pins1[pinsPerSide];
                if (!isTex1 && omit3Pins && pinBorderCircTextures3Pins2.ContainsKey(pinsPerSide)) return pinBorderCircTextures3Pins2[pinsPerSide];
                if (!isTex1 && !omit3Pins && pinBorderCircTextures5Pins2.ContainsKey(pinsPerSide)) return pinBorderCircTextures5Pins2[pinsPerSide];
            }

            // Create Texture
            Texture2D tex = new Texture2D(1024, 1024);
            tex.wrapMode = TextureWrapMode.Clamp;
            //tex.filterMode = ;

            // Metadata
            Vector2Int texCenterPixel = new Vector2Int(tex.width / 2, tex.height / 2);

            // Make Tex Transparent
            Color colorTransparent = new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(x, y, colorTransparent);
                }
            }

            // Add Pins to Texture
            //for (int i = 0; i < pinsPerSide; i++)
            //{
                // Calc center pos of this pin relative from texture center
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
                            Vector2Int absPosRotated = texCenterPixel + new Vector2Int((int)(0.5f * relPosRotated.x * tex.width), (int)(0.5f * relPosRotated.y * tex.height));
                            Vector2Int startPos = absPosRotated - new Vector2Int(pinTexture.width / 2, pinTexture.height / 2);
                            for (int x = 0; x < pinTexture.width; x++)
                            {
                                for (int y = 0; y < pinTexture.height; y++)
                                {
                                    Vector2Int texPos = new Vector2Int(startPos.x + x, startPos.y + y);
                                    if (texPos.x >= 0 && texPos.x < tex.width && texPos.y >= 0 && texPos.y < tex.height) tex.SetPixel(texPos.x, texPos.y, pinTexture.GetPixel(x, y));
                                }
                            }
                        }
                    }
                }
            //}

            // Apply
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

            // Metadata
            Vector2Int texCenterPixel = new Vector2Int(tex.width / 2, tex.height / 2);

            // Fill Tex with Hexagon/HexagonCirc Tex
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    if (viewType == ViewType.Hexagonal) tex.SetPixel(x, y, hexagonTexture.GetPixel(x, y));
                    else if (viewType == ViewType.HexagonalCirc) tex.SetPixel(x, y, hexagonCircTexture.GetPixel(x, y));
                }
            }

            // Add Pins to Texture
            // Calc center pos of right pin relative from texture center
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
                    Vector2Int absPosRotated = texCenterPixel + new Vector2Int((int)(0.5f * relPosRotated.x * tex.width), (int)(0.5f * relPosRotated.y * tex.height));
                    Vector2Int startPos = absPosRotated - new Vector2Int(pinTexture.width / 2, pinTexture.height / 2);
                    for (int x = 0; x < pinTexture.width; x++)
                    {
                        for (int y = 0; y < pinTexture.height; y++)
                        {
                            Vector2Int texPos = new Vector2Int(startPos.x + x, startPos.y + y);
                            if (texPos.x >= 0 && texPos.x < tex.width && texPos.y >= 0 && texPos.y < tex.height)
                            {
                                // Merge Textures (based on overlay alpha)
                                // Get colors to merge
                                Color colorBase = tex.GetPixel(texPos.x, texPos.y);
                                Color colorOver = pinTexture.GetPixel(x, y);
                                // Calculate final color (interpolate between colors)
                                Color colorNew = new Color(Mathf.Lerp(colorBase.r, colorOver.r, colorOver.a), Mathf.Lerp(colorBase.g, colorOver.g, colorOver.a), Mathf.Lerp(colorBase.b, colorOver.b, colorOver.a), colorBase.a + (1f - colorBase.a) * colorOver.a);
                                // Set color
                                tex.SetPixel(texPos.x, texPos.y, colorNew);
                            }
                        }
                    }
                }
            }

            // Apply
            tex.Apply();

            // Add Texture to Data
            if (viewType == ViewType.Hexagonal && hexagonTextures.ContainsKey(pinsPerSide) == false) hexagonTextures.Add(pinsPerSide, tex);
            if (viewType == ViewType.HexagonalCirc && hexagonCircTextures.ContainsKey(pinsPerSide) == false) hexagonCircTextures.Add(pinsPerSide, tex);

            // Return
            return tex;
        }

    }

}