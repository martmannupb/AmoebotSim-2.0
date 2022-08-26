using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderSystem
{

    // References
    private ParticleSystem map;

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
    public const float const_hexagonalBorderWidth = 0.12f;
    public const int const_hexagonalBGHexLineAmount = 200;
    public const float const_circuitLineWidth = 0.02f;
    public const float const_circuitConnectorLineWidth = 0.06f;
    public const float const_circuitPinSize = 0.1f;
    public const float const_circuitSingletonPinSize = 0.1f;
    public const float const_circuitPinConnectorSize = 0.02f;
    // Layers
    public const float zLayer_background = 1f;
    public const float ZLayer_particlesBG = 0.1f;
    public const float zLayer_particles = 0f;
    public const float zLayer_circuits = -0.5f;
    public const float zLayer_pins = -1f;
    public const float zLayer_ui = -5f;
    // Global Data
    public static float global_particleScale = MaterialDatabase.material_hexagonal_particleCombined.GetFloat("_Scale");

    // Dynamic Params _____
    public static bool flag_particleRoundOver = true;
    public static bool flag_showCircuitView = true;

    // Dynamic Data _____
    public static float data_particleMovementFinishedTimestamp;
    // Animation + Beep Timing
    public static float data_hexagonalAnimationDuration = 0.5f;     // particle animation duration
    public const float const_maxHexagonalAnimationDuration = 1f;
    public static float data_circuitAnimationDuration = 0.0f;       // pin/circuit fade in time after movement
    public const float const_maxCircuitAnimationDuration = 0.0f;
    public static float data_circuitBeepDuration = 0.5f;            // circuit beep duration
    public const float const_maxCircuitBeepDuration = 0.5f;
    public const float const_animationTimePercentage = 0.7f;
    public const float const_beepTimePercentage = 0.2f;


    // Renderers _____
    public RendererBackground rendererBG;
    public RendererParticles rendererP;
    public RendererUI rendererUI;





    public RenderSystem()
    {
        rendererBG = new RendererBackground();
        rendererP = new RendererParticles();
        rendererUI = new RendererUI();
    }

    public void Render()
    {
        // Render
        rendererBG.Render(setting_viewType);
        rendererP.Render(setting_viewType);
        rendererUI.Render(setting_viewType);

        // Reset Round Flag
        flag_particleRoundOver = false;
    }

    /// <summary>
    /// Signalizes the Renderer that the last round of particle movements has been successfully calculated.
    /// </summary>
    public void ParticleMovementOver()
    {
        // Apply Particle Updates
        flag_particleRoundOver = true;
        // (so far we only use one array and apply updates directly)
        // (later we could use two arrays here)
    }

    /// <summary>
    /// Signalizes the Renderer that the last round of circuit updates has been successfully calculated (all circuits have been updated).
    /// Updates that have not yet been shown will be displayed now.
    /// </summary>
    public void CircuitCalculationOver()
    {
        // Switch Circuit Instances
        rendererP.circuitRenderer.SwitchInstances();
    }

    public void AddReferenceToParticleSystem(ParticleSystem map)
    {
        this.map = map;
        rendererUI.AddReferenceToMap(map);
    }

    //public static float data_hexagonalAnimationDuration = 0.5f;
    //public const float data_maxHexagonalAnimationDuration = 1f;
    //public static float data_circuitAnimationDuration = 0.2f;
    //public const float data_maxCircuitAnimationDuration = 0.2f;
    //public static float data_circuitBeepDuration = 0.5f;
    //public const float data_maxCircuitBeepDuration = 0.5f;
    //public const float data_animationTimePercentage = 0.7f;
    //public const float data_beepTimePercentage = 0.2f;
    public void SetRoundTime(float roundTime)
    {
        if (roundTime == 0f) throw new System.NotImplementedException();

        // Particle Animation Duration
        data_hexagonalAnimationDuration = roundTime * const_animationTimePercentage;
        data_hexagonalAnimationDuration = Mathf.Clamp(data_hexagonalAnimationDuration, 0f, const_maxHexagonalAnimationDuration);
        // Beep Duration
        data_circuitBeepDuration = roundTime * const_beepTimePercentage;
        data_circuitBeepDuration = Mathf.Clamp(data_circuitBeepDuration, 0f, const_maxCircuitBeepDuration);

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

    public void ToggleCircuits()
    {
        flag_showCircuitView = !flag_showCircuitView;
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
