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
        public Button button_expansionContraction;
        public VerticalLayoutGroup movement_layoutGroup;
        public float movement_layoutGroupLeftPaddingContracted;
        public float movement_layoutGroupLeftPaddingExpanded;
        public RectTransform movement_panelTransform;
        public float movement_panelTransformWidthContracted;
        public float movement_panelTransformWidthExpanded;
        // Data
        bool movement_isExpanding = true;
        bool movement_useLerp = true;
        float movement_expansionPercentage = 1f;
        float movement_percentagePerSec = 1f;
        // UI
        public Sprite sprite_expanded;
        public Sprite sprite_contracted;

        private void OnEnable()
        {
            ContractRandomisationExtension(false);
        }

        public void ExpandRandomisationExtension(bool useLerp)
        {
            movement_isExpanding = true;
            movement_useLerp = useLerp;
        }

        public void ContractRandomisationExtension(bool useLerp)
        {
            movement_isExpanding = false;
            movement_useLerp = useLerp;
        }

        public void Update()
        {
            if (particlePanel.IsOpen())
            {
                bool updated = false;
                if(movement_isExpanding && movement_expansionPercentage != 1f)
                {
                    // Expanding
                    if (movement_useLerp == false) movement_expansionPercentage = 1f;
                    else movement_expansionPercentage = Mathf.Min(1f, movement_expansionPercentage + Time.deltaTime * movement_percentagePerSec);
                    updated = true;
                }
                else if(movement_isExpanding == false && movement_expansionPercentage != 0f)
                {
                    // Contracting
                    if (movement_useLerp == false) movement_expansionPercentage = 0f;
                    else movement_expansionPercentage = Mathf.Max(0f, movement_expansionPercentage - Time.deltaTime * movement_percentagePerSec);
                    updated = true;
                }
                if(updated)
                {
                    // Apply updates
                    float realPercentage = Engine.Library.InterpolationConstants.SmoothLerp(movement_expansionPercentage);
                    movement_layoutGroup.padding.left = (int)Mathf.Lerp(movement_layoutGroupLeftPaddingContracted, movement_layoutGroupLeftPaddingExpanded, realPercentage);
                    Vector2 rtSize = movement_panelTransform.sizeDelta;
                    rtSize.x = Mathf.Lerp(movement_panelTransformWidthContracted, movement_panelTransformWidthExpanded, realPercentage);
                    movement_panelTransform.sizeDelta = rtSize;
                    Image image = button_expansionContraction.gameObject.transform.GetChild(0).gameObject.GetComponent<Image>();
                    if (movement_isExpanding) image.sprite = sprite_expanded;
                    else image.sprite = sprite_contracted;
                }
            }
        }

        public void ToggleExpansionOrContraction()
        {
            if (movement_isExpanding) ContractRandomisationExtension(true);
            else ExpandRandomisationExtension(true);
        }

    }

}