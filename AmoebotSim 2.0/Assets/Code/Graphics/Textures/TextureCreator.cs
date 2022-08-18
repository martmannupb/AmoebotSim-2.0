using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureCreator
{

    public static Texture2D pinBorderTextureEmpty;
    public static Dictionary<int, Texture2D> pinBorderTextures3Pins1 = new Dictionary<int, Texture2D>();
    public static Dictionary<int, Texture2D> pinBorderTextures3Pins2 = new Dictionary<int, Texture2D>();
    public static Dictionary<int, Texture2D> pinBorderTextures5Pins1 = new Dictionary<int, Texture2D>();
    public static Dictionary<int, Texture2D> pinBorderTextures5Pins2 = new Dictionary<int, Texture2D>();

    public static Dictionary<int, Material> pinBorderMaterials = new Dictionary<int, Material>();

    private static Texture2D pinTexture = Resources.Load<Texture2D>(FilePaths.path_textures+"PinTex");
    private static Texture2D transTexture = Resources.Load<Texture2D>(FilePaths.path_textures + "TransparentPixel");

    public static Material GetPinBorderMaterial(int pinsPerSide)
    {
        if (pinBorderMaterials.ContainsKey(pinsPerSide)) return pinBorderMaterials[pinsPerSide];

        // Create Material
        Material hexMat = MaterialDatabase.material_hexagonal_particleCombined;
        Material mat = new Material(hexMat.shader);
        mat.CopyPropertiesFromMaterial(hexMat);
        Texture2D borderTex1 = GetPinBorderTexture(pinsPerSide, true, true, 0, false);
        Texture2D borderTex2 = GetPinBorderTexture(pinsPerSide, true, true, 3, true);
        //Texture2D borderTex1 = GetPinBorderTextureEmpty();
        //Texture2D borderTex2 = GetPinBorderTextureEmpty();
        Texture2D borderTex100P = GetPinBorderTexture(pinsPerSide, true, false, 0, false);
        Texture2D borderTex100P2 = GetPinBorderTexture(pinsPerSide, true, false, 3, true);
        mat.SetTexture("_TextureHexagon", borderTex1);
        mat.SetTexture("_TextureHexagon2", borderTex2);
        mat.SetTexture("_TextureHexagon100P", borderTex100P);
        mat.SetTexture("_TextureHexagon100P2", borderTex100P2);
        mat.SetTexture("_TextureHexagonConnector", transTexture);

        // Add Material to Data
        pinBorderMaterials.Add(pinsPerSide, mat);
        
        // Return
        return mat;
    }

    private static Texture2D GetPinBorderTexture(int pinsPerSide, bool omitSide, bool omit3Pins, int omittedSide, bool isTex1)
    {
        if (isTex1 && omit3Pins && pinBorderTextures3Pins1.ContainsKey(pinsPerSide)) return pinBorderTextures3Pins1[pinsPerSide];
        if (isTex1 && !omit3Pins && pinBorderTextures5Pins1.ContainsKey(pinsPerSide)) return pinBorderTextures5Pins1[pinsPerSide];
        if (!isTex1 && omit3Pins && pinBorderTextures3Pins2.ContainsKey(pinsPerSide)) return pinBorderTextures3Pins2[pinsPerSide];
        if (!isTex1 && !omit3Pins && pinBorderTextures5Pins2.ContainsKey(pinsPerSide)) return pinBorderTextures5Pins2[pinsPerSide];

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
        for (int i = 0; i < pinsPerSide; i++)
        {
            // Calc center pos of this pin relative from texture center
            Vector2 relPosTopRight = new Vector2(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides());
            Vector2 relPosBottomRight = new Vector2(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides());
            for (int j = 0; j < pinsPerSide; j++)
            {
                Vector2 relPosPinRight;
                Vector2 relDistBottomToTop = relPosTopRight - relPosBottomRight;
                if (pinsPerSide == 1) relPosPinRight = relPosBottomRight + 0.5f * relDistBottomToTop;
                else
                {
                    Vector2 relStep = relDistBottomToTop / (pinsPerSide + 1);
                    relPosPinRight = relPosBottomRight + (j + 1) * relStep;
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
        }

        // Apply
        tex.Apply();

        // Add Texture to Data
        if (isTex1 && omit3Pins) pinBorderTextures3Pins1.Add(pinsPerSide, tex);
        if (isTex1 && !omit3Pins) pinBorderTextures5Pins1.Add(pinsPerSide, tex);
        if (!isTex1 && omit3Pins) pinBorderTextures3Pins2.Add(pinsPerSide, tex);
        if (!isTex1 && !omit3Pins) pinBorderTextures5Pins2.Add(pinsPerSide, tex);

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

}
