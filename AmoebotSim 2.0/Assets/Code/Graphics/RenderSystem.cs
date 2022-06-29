using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderSystem
{

    // Dynamic Settings _____
    // View
    public ViewType setting_viewType = ViewType.Hexagonal;
    // Performance
    public int setting_antiAliasing = 8; // Valid values are 0 (no MSAA), 2, 4, and 8


    // Static Params _____
    // General
    public const float const_circularViewBGLineWidth = 0.06f;
    public const int const_circularViewBGLineLength = 1000000;
    public const int const_amountOfLinesPerMesh = 100;
    public const float const_hexagonalScale = 1f;
    public const float const_hexagonalBorderWidth = 0.1f;
    public const int const_hexagonalBGHexLineAmount = 200;
    // Layers
    public const float zLayer_background = 1f;
    public const float zLayer_particles = 0f;
    public const float zLayer_pins = -1f;


    // Renderers _____
    public RendererBackground rendererBG;
    public RendererParticles rendererP;

    


    public RenderSystem()
    {
        rendererBG = new RendererBackground();
        rendererP = new RendererParticles();
    }

    public void Render()
    {
        rendererBG.Render(setting_viewType);
        rendererP.Render(setting_viewType);
    }


    public void ToggleViewType()
    {
        switch (setting_viewType)
        {
            case ViewType.Hexagonal:
                setting_viewType = ViewType.Circular;
                break;
            case ViewType.Circular:
                setting_viewType = ViewType.Hexagonal;
                break;
            default:
                break;
        }
    }

    public void ToggleAntiAliasing()
    {
        switch (setting_antiAliasing)
        {
            case 0:
                setting_antiAliasing = 2;
                break;
            case 2:
                setting_antiAliasing = 4;
                break;
            case 4:
                setting_antiAliasing = 8;
                break;
            case 8:
                setting_antiAliasing = 0;
                break;
        }
        QualitySettings.antiAliasing = setting_antiAliasing;
    }

    public void AntiAliasing_Incr()
    {
        switch (setting_antiAliasing)
        {
            case 0:
                setting_antiAliasing = 2;
                break;
            case 2:
                setting_antiAliasing = 4;
                break;
            case 4:
                setting_antiAliasing = 8;
                break;
            case 8:
                setting_antiAliasing = 8;
                break;
        }
        QualitySettings.antiAliasing = setting_antiAliasing;
    }

    public void AntiAliasing_Decr()
    {
        switch (setting_antiAliasing)
        {
            case 0:
                setting_antiAliasing = 0;
                break;
            case 2:
                setting_antiAliasing = 0;
                break;
            case 4:
                setting_antiAliasing = 2;
                break;
            case 8:
                setting_antiAliasing = 4;
                break;
        }
        QualitySettings.antiAliasing = setting_antiAliasing;
    }
}
