using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureCreator
{

    public static Dictionary<int, Texture2D> pinBorderTextures = new Dictionary<int, Texture2D>();
    public static Dictionary<int, Material> pinBorderMaterials = new Dictionary<int, Material>();

    private static Texture2D pinTexture = Resources.Load<Texture2D>(FilePaths.path_textures+"PinTex");
    private static Texture2D transTexture = Resources.Load<Texture2D>(FilePaths.path_textures + "TransparentPixel");

    public static Material GetPinBorderMaterial(int pinsPerSide)
    {
        if (pinBorderMaterials.ContainsKey(pinsPerSide)) return pinBorderMaterials[pinsPerSide];

        // Create Material
        Material hexMat = MaterialDatabase.material_circular_particleComplete;
        Material mat = new Material(hexMat.shader);
        mat.CopyPropertiesFromMaterial(hexMat);
        Texture2D borderTex1 = GetPinBorderTexture(pinsPerSide);
        mat.SetTexture("_TextureHexagon", borderTex1);
        mat.SetTexture("_TextureHexagon2", borderTex1);
        mat.SetTexture("_TextureHexagonConnector", transTexture);

        // Add Material to Data
        pinBorderMaterials.Add(pinsPerSide, mat);
        
        // Return
        return mat;
    }

    private static Texture2D GetPinBorderTexture(int pinsPerSide)
    {
        if (pinBorderTextures.ContainsKey(pinsPerSide)) return pinBorderTextures[pinsPerSide];

        // Create Texture
        Texture2D tex = new Texture2D(1024, 1024);
        tex.wrapMode = TextureWrapMode.Clamp;
        //tex.filterMode = ;

        // Metadata
        Vector2Int texCenterPixel = new Vector2Int(tex.width / 2, tex.height / 2);

        // Make Tex Transparent
        Color colorTransparent = new Color(0f, 0f, 0f, 1f);
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
                    Vector2 relPosRotated = Quaternion.Euler(new Vector3(0f, 0f, 60f * k)) * relPosPinRight;
                    Vector2Int absPosRotated = texCenterPixel + new Vector2Int((int)(0.5f * relPosRotated.x * tex.width), (int)(0.5f * relPosRotated.y * tex.height));
                    Vector2Int startPos = absPosRotated - new Vector2Int(pinTexture.width / 2, pinTexture.height / 2);
                    for (int x = 0; x < pinTexture.width; x++)
                    {
                        for (int y = 0; y < pinTexture.height; y++)
                        {
                            Vector2Int texPos = new Vector2Int(startPos.x + x, startPos.y + y);
                            if(texPos.x >= 0 && texPos.x < tex.width && texPos.y >= 0 && texPos.y < tex.height) tex.SetPixel(texPos.x, texPos.y, pinTexture.GetPixel(x, y));
                        }
                    }
                }
            }
        }

        // Apply
        tex.Apply();

        // Add Texture to Data
        pinBorderTextures.Add(pinsPerSide, tex);

        // Return
        return tex;
    }
    
}
