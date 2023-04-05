using AS2.Sim;
using AS2.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AS2.UI
{
    /// <summary>
    /// Controls the particle panel extension lerping movement.
    /// </summary>
    public class ParticleUIExtensionSmoothLerp : MonoBehaviour
    {
        // References
        public ParticleUIHandler particlePanel;
        public Button button_expansionContraction;                      // The button that triggers the expansion and contraction
        public VerticalLayoutGroup movement_layoutGroup;                // The layout group that is animated (randomization panel)
        public float movement_layoutGroupLeftPaddingContracted;
        public float movement_layoutGroupLeftPaddingExpanded;
        public RectTransform movement_panelTransform;                   // The rect transform of the containing element (particle panel)
        public float movement_panelTransformWidthContracted;
        public float movement_panelTransformWidthExpanded;
        // Data
        bool movement_isExpanding = true;
        bool movement_enableLerp = false;
        bool movement_useLerp = false;
        float movement_expansionPercentage = float.MinValue; // update once after init
        float movement_percentagePerSec = 1f;
        // UI
        public Sprite sprite_expanded;
        public Sprite sprite_contracted;

        private void Start()
        {
            EventDatabase.event_particleUI_particlePanelOpenClose += OnOpenClose;
            OnOpenClose(false);
        }

        /// <summary>
        /// Callback that should be triggered when the particle panel
        /// is opened or closed.
        /// <para>
        /// If the particle panel is opened in Init Mode, the randomization
        /// panel is automatically opened without animation.
        /// </para>
        /// </summary>
        /// <param name="opened">Indicates whether the particle panel was opened.</param>
        private void OnOpenClose(bool opened)
        {
            if(opened && AmoebotSimulator.instance != null
                && AmoebotSimulator.instance.uiHandler != null && AmoebotSimulator.instance.uiHandler.initializationUI != null
                && AmoebotSimulator.instance.uiHandler.initializationUI.IsOpen())
            {
                // Init Mode
                ExpandRandomisationExtension(false);
            }
            else
            {
                // Reset
                SetToDefault();
            }
        }

        /// <summary>
        /// Closes the randomization panel without animation.
        /// </summary>
        private void SetToDefault()
        {
            ContractRandomisationExtension(false);
        }

        /// <summary>
        /// Starts the expansion of the randomization panel.
        /// </summary>
        /// <param name="useLerp">Indicates whether the expansion
        /// should be animated.</param>
        public void ExpandRandomisationExtension(bool useLerp)
        {
            movement_isExpanding = true;
            movement_useLerp = useLerp && movement_enableLerp;
        }

        /// <summary>
        /// Starts the contraction of the randomization panel.
        /// </summary>
        /// <param name="useLerp">Indicates whether the contraction
        /// should be animated.</param>
        public void ContractRandomisationExtension(bool useLerp)
        {
            movement_isExpanding = false;
            movement_useLerp = useLerp && movement_enableLerp;
        }

        public void Update()
        {
            if(movement_isExpanding && movement_expansionPercentage != 1f)
            {
                // Expanding
                if (movement_useLerp == false) movement_expansionPercentage = 1f;
                else movement_expansionPercentage = Mathf.Min(1f, movement_expansionPercentage + Time.deltaTime * movement_percentagePerSec);
                UpdateUI();
            }
            else if(movement_isExpanding == false && movement_expansionPercentage != 0f)
            {
                // Contracting
                if (movement_useLerp == false) movement_expansionPercentage = 0f;
                else movement_expansionPercentage = Mathf.Max(0f, movement_expansionPercentage - Time.deltaTime * movement_percentagePerSec);
                UpdateUI();
            }
        }

        /// <summary>
        /// Translates the linear movement percentage into a smooth
        /// movement and converts the percentage into real size values
        /// for the vertical layout group and the rect transform.
        /// Also applies the correct image to the control button.
        /// </summary>
        private void UpdateUI()
        {
            // Apply updates
            float realPercentage = Visuals.Library.InterpolationConstants.SmoothLerp(movement_expansionPercentage);
            movement_layoutGroup.padding.left = (int)Mathf.Lerp(movement_layoutGroupLeftPaddingContracted, movement_layoutGroupLeftPaddingExpanded, realPercentage);
            Vector2 rtSize = movement_panelTransform.sizeDelta;
            rtSize.x = Mathf.Lerp(movement_panelTransformWidthContracted, movement_panelTransformWidthExpanded, realPercentage);
            movement_panelTransform.sizeDelta = rtSize;
            Image image = button_expansionContraction.gameObject.transform.GetChild(0).gameObject.GetComponent<Image>();
            if (movement_isExpanding) image.sprite = sprite_expanded;
            else image.sprite = sprite_contracted;
        }

        /// <summary>
        /// Switches the current expansion state or movement to
        /// the respective other one with animation, if animation
        /// is enabled.
        /// </summary>
        public void ToggleExpansionOrContraction()
        {
            if (movement_isExpanding) ContractRandomisationExtension(true);
            else ExpandRandomisationExtension(true);
        }

        /// <summary>
        /// Enables or disables the smooth lerping animation.
        /// </summary>
        /// <param name="lerpEnabled">The new enabled status.</param>
        public void SetLerpEnabled(bool lerpEnabled)
        {
            this.movement_enableLerp = lerpEnabled;
        }

        /// <summary>
        /// Checks whether or not the smooth lerping animation is enabled.
        /// </summary>
        /// <returns><c>true</c> if and only if the animation is enabled.</returns>
        public bool GetLerpEnabled()
        {
            return this.movement_enableLerp;
        }

    }

}