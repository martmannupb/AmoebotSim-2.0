using System.Collections;
using System.Collections.Generic;
using AS2.UI;
using UnityEngine;

namespace AS2.Graphics
{

    public class RenderSystem
    {

        // References
        private ParticleSystem map;

        // Dynamic Settings _____
        // View
        public static ViewType setting_viewType = ViewType.Hexagonal;
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
        public const float const_circuitSingletonPinSize = 0.085f;
        public const float const_circuitPinConnectorSize = 0.02f;
        public const float const_circuitPinBeepSizePercentage = 0.5f;
        public const float const_bondsLineWidthHex = 0.4f;
        public const float const_bondsLineWidthCirc = 0.15f;
        // Layers
        public const float zLayer_background = 10f;
        public const float ZLayer_bonds = 9f;
        public const float ZLayer_particlesBG = 5.1f;
        public const float zLayer_particles = 5f;
        public const float zLayer_circuits = 4f;
        public const float zLayer_pins = 3f;
        public const float zLayer_ui = -5f;
        // Global Data
        public static float global_particleScale = MaterialDatabase.material_hexagonal_particleCombined.GetFloat("_Scale");

        // Dynamic Params _____
        public static bool flag_particleRoundOver = true;
        public static bool flag_showCircuitView = true;
        public static bool flag_showBonds = true;
        public static bool flag_showCircuitViewOutterRing = true;

        // Dynamic Data _____
        public static float data_particleMovementFinishedTimestamp;
        // Animation + Beep Timing
        public static float data_roundTime;
        public static float data_hexagonalAnimationDuration = 0.5f;     // particle animation duration
        public const float const_maxHexagonalAnimationDuration = 1f;
        public static float data_circuitAnimationDuration = 0.0f;       // pin/circuit fade in time after movement
        public const float const_maxCircuitAnimationDuration = 0.0f;
        public static float data_circuitBeepDuration = 0.5f;            // circuit beep duration
        public const float const_maxCircuitBeepDuration = 0.5f;
        public const float const_animationTimePercentage = 0.6f;        // percentages: animation/beeps (sequentially)
        public const float const_beepTimePercentage = 0.2f;             // percentages: animation/beeps (sequentially)
        public static float data_circuitBeepRepeatDelay = 4f;
        public static bool data_circuitBeepRepeatOn = true;
        // Animation Toggle
        public static bool animationsOn = true;
        // Trigger Times
        public static float animation_animationTriggerTimestamp;
        public static float animation_curAnimationPercentage;


        // Renderers _____
        public RendererBackground rendererBG;
        public RendererParticles rendererP;
        public RendererUI rendererUI;





        public RenderSystem(AmoebotSimulator sim, InputController inputController)
        {
            rendererBG = new RendererBackground();
            rendererP = new RendererParticles();
            rendererUI = new RendererUI(sim, inputController);
        }

        public void Render()
        {
            // Calculate Progress
            animation_curAnimationPercentage = Mathf.Clamp(Time.timeSinceLevelLoad - animation_animationTriggerTimestamp, 0f, data_hexagonalAnimationDuration) / data_hexagonalAnimationDuration;

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
            animation_animationTriggerTimestamp = Time.timeSinceLevelLoad;
        }

        /// <summary>
        /// Signalizes the Renderer that the last round of circuit updates has been successfully calculated (all circuits have been updated).
        /// Updates that have not yet been shown will be displayed now.
        /// </summary>
        public void CircuitCalculationOver()
        {
            // Switch Circuit Instances
            rendererP.circuitAndBondRenderer.SwitchInstances();
        }

        /// <summary>
        /// Updates the timing of the animations.
        /// </summary>
        /// <param name="roundTime"></param>
        public void SetRoundTime(float roundTime)
        {
            data_roundTime = roundTime;
            if (roundTime == 0f) throw new System.NotImplementedException();

            // Particle Animation Duration
            data_hexagonalAnimationDuration = roundTime * const_animationTimePercentage;
            data_hexagonalAnimationDuration = Mathf.Clamp(data_hexagonalAnimationDuration, 0f, const_maxHexagonalAnimationDuration);
            // Beep Duration
            data_circuitBeepDuration = roundTime * const_beepTimePercentage;
            data_circuitBeepDuration = Mathf.Clamp(data_circuitBeepDuration, 0f, const_maxCircuitBeepDuration);

            //if(animationsOn == false) data_hexagonalAnimationDuration = 0f;
        }


        public void ToggleViewType()
        {
            switch (setting_viewType)
            {
                case ViewType.Hexagonal:
                    setting_viewType = ViewType.HexagonalCirc;
                    break;
                case ViewType.HexagonalCirc:
                    setting_viewType = ViewType.Circular;
                    break;
                case ViewType.Circular:
                    setting_viewType = ViewType.Hexagonal;
                    break;
                default:
                    break;
            }
        }

        public ViewType GetCurrentViewType()
        {
            return setting_viewType;
        }

        public void ToggleCircuits()
        {
            flag_showCircuitView = !flag_showCircuitView;
        }

        public void ToggleBonds()
        {
            flag_showBonds = !flag_showBonds;
        }

        public bool IsCircuitViewActive()
        {
            return flag_showCircuitView;
        }

        public bool AreBondsActive()
        {
            return flag_showBonds;
        }

        public void SetAntiAliasing(int value)
        {
            if (value == 0 || value == 2 || value == 4 || value == 8) setting_antiAliasing = value;
            QualitySettings.antiAliasing = setting_antiAliasing;
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

        public int GetAntiAliasing()
        {
            return setting_antiAliasing;
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

} // namespace AS2.Graphics
