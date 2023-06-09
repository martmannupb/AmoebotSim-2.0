using AS2.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Base class for all the rendering of the particles. Stores most render settings and initiates the render process.
    /// The render system is a tree-like structure with multiple sub-parts which all have their individual tasks.
    /// RendererBackground: Renders the background. RendererParticles: Renders particles, circuits and connections. RendererUI: Renders the overlay over particles.
    /// </summary>
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
        /// <summary>
        /// Width of graph view background lines as fraction of row height.
        /// </summary>
        public const float const_circularViewBGLineWidth = 0.06f;
        /// <summary>
        /// Length of graph view background lines in world units.
        /// </summary>
        public const int const_circularViewBGLineLength = 1000000;
        /// <summary>
        /// Number of lines in each direction of the background mesh.
        /// </summary>
        public const int const_amountOfLinesPerMesh = 100;
        /// <summary>
        /// Universal scale for hexagons.
        /// </summary>
        public const float const_hexagonalScale = 1f;
        /// <summary>
        /// The thickness of hexagon borders at the corners.
        /// Does not scale with <see cref="const_hexagonalScale"/>
        /// (increasing the scale leads to relatively thinner
        /// borders).
        /// </summary>
        public const float const_hexagonalBorderWidth = 0.12f;
        /// <summary>
        /// Number of hexagons in one mesh for the hexagonal background.
        /// </summary>
        public const int const_hexagonalBGHexLineAmount = 200;
        /// <summary>
        /// Width of circuit lines in the particles.
        /// </summary>
        public const float const_circuitLineWidth = 0.02f;
        /// <summary>
        /// Width of circuit lines outside of the particles and beeping
        /// circuit lines.
        /// </summary>
        public const float const_circuitConnectorLineWidth = 0.06f;
        /// <summary>
        /// Default scaling factor of internal pins representing
        /// partition sets.
        /// </summary>
        public const float const_circuitPinSize = 0.1f;
        /// <summary>
        /// Default scaling factor of internal pins representing
        /// singleton partition sets.
        /// </summary>
        public const float const_circuitSingletonPinSize = 0.085f;
        /// <summary>
        /// Default size of "pins" used to fill the gaps at the
        /// vertices of circuit connector lines. Should usually
        /// be equal to <see cref="const_circuitLineWidth"/>.
        /// </summary>
        public const float const_circuitPinConnectorSize = 0.02f;
        /// <summary>
        /// Fraction of the partition set pin that should be colored
        /// when the partition set is a beep or message origin.
        /// </summary>
        public const float const_circuitPinBeepSizePercentage = 0.5f;
        /// <summary>
        /// Width of bond lines in hexagonal view modes.
        /// </summary>
        public const float const_bondsLineWidthHex = 0.4f;
        /// <summary>
        /// Width of bond lines in graph view mode.
        /// </summary>
        public const float const_bondsLineWidthCirc = 0.15f;
        // Layers
        // The z layers of the objects determine how they are ordered
        // Smaller z layers are in front of larger ones
        // The camera is at z layer -10, everything below that will not be visible
        public const float zLayer_background = 10f;
        public const float ZLayer_bonds = 9f;
        public const float zLayer_particles = 8f;
        public const float zLayer_circuits = 7f;
        public const float zLayer_pins = 6f;
        public const float zLayer_ui = -5f;

        // Render queue priorities
        // These specify in which order objects are rendered (lower values are
        // rendered first)
        // This fixes layering issues that cannot be resolved by Z layers alone
        // All values are set in the material assets already. Only the hexagon
        // pin material with the invisible hexagon and the pin beep origin
        // highlights are updated via code to have the same / a lower render
        // queue priority as pins
        public static readonly int renderQueue_background = 2800;
        public static readonly int renderQueue_bonds = 2820;
        public static readonly int renderQueue_particles = 2840;
        public static readonly int renderQueue_circuits = 2860;
        public static readonly int renderQueue_circuitBeeps = 2870;
        public static readonly int renderQueue_pins = 2880;
        public static readonly int renderQueue_pinBeeps = 2890;
        public static readonly int renderQueue_overlays = 2900;

        // Global Data
        /// <summary>
        /// The global scaling factor for particles.
        /// </summary>
        public static float global_particleScale = MaterialDatabase.material_hexagonal_particleCombined.GetFloat("_Scale");

        // Dynamic Params _____
        /// <summary>
        /// Flag that should be set in the first frame after the
        /// particle visuals have been updated by a round simulation.
        /// Will be reset immediately after rendering the frame.
        /// </summary>
        public static bool flag_particleRoundOver = true;
        /// <summary>
        /// Determines whether or not pins and circuits should
        /// be rendered. Can be modified through the UI.
        /// </summary>
        public static bool flag_showCircuitView = true;
        /// <summary>
        /// Determines the partition set placement mode. Can be
        /// modified in the UI.
        /// </summary>
        public static PartitionSetViewType flag_partitionSetViewType = PartitionSetViewType.CodeOverride;
        /// <summary>
        /// Determines whether or not bonds should be rendered.
        /// Can be modified through the UI.
        /// </summary>
        public static bool flag_showBonds = true;
        /// <summary>
        /// Determines whether the outer ring should be drawn
        /// around particles in the graph view mode. Can be
        /// set in the Settings Panel.
        /// </summary>
        public static bool flag_showCircuitViewOuterRing = true;
        /// <summary>
        /// Determines whether circuit lines between particles
        /// should have a border. Can be set in the Settings Panel.
        /// </summary>
        public static bool flag_circuitBorderActive = false;

        // Dynamic Data _____
        /// <summary>
        /// The predicted time at which the current movement animation
        /// will be finished.
        /// </summary>
        public static float data_particleMovementFinishedTimestamp;
        // Animation + Beep Timing
        /// <summary>
        /// The time between two round simulations.
        /// </summary>
        public static float data_roundTime;
        /// <summary>
        /// Duration of each movement animation in seconds.
        /// </summary>
        public static float data_hexagonalAnimationDuration = 0.5f;
        /// <summary>
        /// The maximal duration of the movement animation in seconds.
        /// </summary>
        public const float const_maxHexagonalAnimationDuration = 2f;
        /// <summary>
        /// Circuit connection fade in time after movement.
        /// (Decided to disable this feature by setting the
        /// duration to 0.)
        /// </summary>
        public static float data_circuitAnimationDuration = 0.0f;
        /// <summary>
        /// Duration of a beep animation in seconds.
        /// </summary>
        public static float data_circuitBeepDuration = 0.5f;
        /// <summary>
        /// The maximal duration of the beep animation in seconds.
        /// </summary>
        public const float const_maxCircuitBeepDuration = 0.75f;
        /// <summary>
        /// The fraction of the round duration that should be used
        /// for the movement animation.
        /// </summary>
        public const float const_animationTimePercentage = 0.6f;
        /// <summary>
        /// The fraction of the round duration that should be used
        /// for the beep animation.
        /// </summary>
        public const float const_beepTimePercentage = 0.2f;
        /// <summary>
        /// DEPRECATED.
        /// <para>The delay between beep repetitions when the
        /// simulation is paused.</para>
        /// </summary>
        public static float data_circuitBeepRepeatDelay = 4f;
        /// <summary>
        /// DEPRECATED.
        /// <para>Determines whether the beep animation should
        /// be played repeatedly while the simulation is paused.
        /// Can be set in the Settings Panel.</para>
        /// </summary>
        public static bool data_circuitBeepRepeatOn = false;
        /// <summary>
        /// Determines whether the movement animations should
        /// be played. Can be set in the Settings Panel.
        /// </summary>
        public static bool animationsOn = true;

        /// <summary>
        /// The time at which the current movement animation
        /// was triggered.
        /// </summary>
        public static float animation_animationTriggerTimestamp;
        /// <summary>
        /// The fraction of the current movement animation time
        /// that has already passed.
        /// </summary>
        public static float animation_curAnimationPercentage;

        // Mesh Bounding Boxes
        /// <summary>
        /// Determines whether the bounding boxes of meshes should
        /// be enlarged to avoid objects being culled while they
        /// are still visible. This is especially helpful for
        /// animations that are implemented using shaders that
        /// apply vertex offsets.
        /// </summary>
        public static bool const_mesh_useManualBoundingBoxRadius = true;
        /// <summary>
        /// The radius used for calculating manual mesh bounding
        /// boxes.
        /// </summary>
        public static float const_mesh_boundingBoxRadius = float.MaxValue / 4;


        // Renderers _____
        /// <summary>
        /// The background renderer.
        /// </summary>
        public RendererBackground rendererBG;
        /// <summary>
        /// The particle and circuit renderer.
        /// </summary>
        public RendererParticles rendererP;
        /// <summary>
        /// The UI overlay renderer.
        /// </summary>
        public RendererUI rendererUI;


        public RenderSystem(AmoebotSimulator sim, InputController inputController)
        {
            rendererBG = new RendererBackground();
            rendererP = new RendererParticles();
            rendererUI = new RendererUI(sim, inputController);
        }

        /// <summary>
        /// The render loop of the render system. This is called once per frame.
        /// </summary>
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
        /// Signalizes the Renderer that the last round of particle movements
        /// has been successfully calculated, triggering the movement animation.
        /// </summary>
        public void ParticleMovementOver()
        {
            // Apply Particle Updates
            flag_particleRoundOver = true;
            animation_animationTriggerTimestamp = Time.timeSinceLevelLoad;
        }

        /// <summary>
        /// Signalizes the Renderer that the last round of circuit updates has been
        /// successfully calculated (all circuits have been updated).
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
        /// <param name="roundTime">The new duration of a round.
        /// Must not be 0.</param>
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

        /// <summary>
        /// Toggles through the available view types:
        /// Hexagonal -> Circular -> Graph.
        /// </summary>
        public void ToggleViewType()
        {
            switch (setting_viewType)
            {
                case ViewType.Hexagonal:
                    setting_viewType = ViewType.HexagonalCirc;
                    rendererP.circuitAndBondRenderer.GetCurrentInstance().Refresh(flag_partitionSetViewType);
                    break;
                case ViewType.HexagonalCirc:
                    setting_viewType = ViewType.Circular;
                    break;
                case ViewType.Circular:
                    setting_viewType = ViewType.Hexagonal;
                    rendererP.circuitAndBondRenderer.GetCurrentInstance().Refresh(flag_partitionSetViewType);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Returns the current view type.
        /// </summary>
        /// <returns>The <see cref="ViewType"/> currently used to
        /// render the particle system.</returns>
        public ViewType GetCurrentViewType()
        {
            return setting_viewType;
        }

        /// <summary>
        /// Returns the partition set view type.
        /// </summary>
        /// <returns>The <see cref="PartitionSetViewType"/> currently
        /// used to place partition sets.</returns>
        public PartitionSetViewType GetPSetViewType()
        {
            return flag_partitionSetViewType;
        }

        /// <summary>
        /// Toggles the circuit view on and off.
        /// </summary>
        public void ToggleCircuits()
        {
            flag_showCircuitView = !flag_showCircuitView;
            // Update particle materials to show or hide pins
            rendererP.SetPinsVisible(flag_showCircuitView);
        }

        /// <summary>
        /// Toggles the partition set positioning.
        /// Code override -> Auto disk -> Auto circle -> Line.
        /// </summary>
        public void TogglePSetPositioning()
        {
            switch (flag_partitionSetViewType)
            {
                case PartitionSetViewType.Line:
                    flag_partitionSetViewType = PartitionSetViewType.CodeOverride;
                    break;
                case PartitionSetViewType.Auto:
                    flag_partitionSetViewType = PartitionSetViewType.Line;
                    break;
                case PartitionSetViewType.Auto_2DCircle:
                    flag_partitionSetViewType = PartitionSetViewType.Auto;
                    break;
                case PartitionSetViewType.CodeOverride:
                    flag_partitionSetViewType = PartitionSetViewType.Auto_2DCircle;
                    break;
                default:
                    break;
            }
            rendererP.circuitAndBondRenderer.GetCurrentInstance().Refresh(flag_partitionSetViewType);
        }

        /// <summary>
        /// Toggles the bonds on and off.
        /// </summary>
        public void ToggleBonds()
        {
            flag_showBonds = !flag_showBonds;
        }

        /// <summary>
        /// Checks if the circuits are currently visible.
        /// </summary>
        /// <returns><c>true</c> if and only if the circuits
        /// are visible.</returns>
        public bool IsCircuitViewActive()
        {
            return flag_showCircuitView;
        }

        /// <summary>
        /// Checks if the bonds are currently shown.
        /// </summary>
        /// <returns><c>true</c> if and only if the bonds
        /// are visible.</returns>
        public bool AreBondsActive()
        {
            return flag_showBonds;
        }

        /// <summary>
        /// Updates the anti-aliasing setting of the graphical interface.
        /// </summary>
        /// <param name="value">0 = off, 2,4,8 = Anti-Aliasing samples.</param>
        public void SetAntiAliasing(int value)
        {
            if (value == 0 || value == 2 || value == 4 || value == 8) setting_antiAliasing = value;
            QualitySettings.antiAliasing = setting_antiAliasing;
        }

        /// <summary>
        /// Toggles through the anti-aliasing in the following order: 0->2->4->8->0 ...
        /// </summary>
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

        /// <summary>
        /// Returns the current anti-aliasing setting.
        /// </summary>
        /// <returns>The current number of anti-aliasing samples.</returns>
        public int GetAntiAliasing()
        {
            return setting_antiAliasing;
        }

        /// <summary>
        /// Increments the anti-aliasing samples.
        /// </summary>
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

        /// <summary>
        /// Decrements the anti-aliasing samples.
        /// </summary>
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
}
