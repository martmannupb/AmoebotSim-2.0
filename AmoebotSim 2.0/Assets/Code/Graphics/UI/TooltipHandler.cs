using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace AS2.UI
{

    /// <summary>
    /// The main behavior script for the tooltip system.
    /// This class manages the visibility of the tooltip text box
    /// and provides the interface for requesting a tooltip and
    /// closing the box again.
    /// </summary>
    public class TooltipHandler : MonoBehaviour
    {
        [Tooltip("How long the cursor has to remain on a button for the tooltip to appear.")]
        public float tooltipDelay = 1.0f;

        public TextMeshProUGUI textObj;
        public RectTransform childTransform;

        private static TooltipHandler instance;

        // For tooltip delay
        private readonly float mouseDeltaThreshold = 0.1f;
        private bool waitForDisplay = false;
        private float lastTimestamp;
        private Vector2 lastMousePosition;

        // For global enabling/disabling
        private bool isEnabled = true;
        /// <summary>
        /// Whether the tooltip system should be enabled.
        /// </summary>
        public bool Enabled
        {
            get { return isEnabled; }
            set { SetEnabled(value); }
        }

        /// <summary>
        /// The singleton tooltip instance.
        /// </summary>
        public static TooltipHandler Instance
        {
            get { return instance; }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        void Start()
        {
            childTransform.gameObject.SetActive(false);
            isEnabled = Config.ConfigData.settingsMenu.showTooltips;
        }

        void Update()
        {
            if (!isEnabled)
                return;

            // Update window position
            Vector3 pos = Input.mousePosition;
            // If the box is too far right or left: Move it back
            Rect boxRect = childTransform.rect;
            // Horizontal
            if (pos.x + boxRect.width > Screen.width)
            {
                pos.x -= (pos.x + boxRect.width - Screen.width);
            }
            if (pos.y - boxRect.height < 0)
            {
                pos.y += (boxRect.height - pos.y);
            }
            gameObject.transform.position = pos;

            // Update visibility
            if (waitForDisplay)
            {
                // If cursor has moved: Reset
                Vector2 mousePos = Input.mousePosition;
                if (Vector2.Distance(mousePos, lastMousePosition) > mouseDeltaThreshold)
                {
                    lastMousePosition = mousePos;
                    lastTimestamp = Time.realtimeSinceStartup;
                }
                // If timer has expired: Show tooltip
                else if (Time.realtimeSinceStartup - lastTimestamp >= tooltipDelay)
                {
                    waitForDisplay = false;
                    childTransform.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Schedules the tooltip to be displayed with the given message.
        /// Should be called when the cursor enters the UI element.
        /// </summary>
        /// <param name="message">The new tooltip message.</param>
        public void Open(string message)
        {
            if (!isEnabled)
                return;

            waitForDisplay = true;
            lastMousePosition = Input.mousePosition;
            lastTimestamp = Time.realtimeSinceStartup;
            textObj.text = message;
        }

        /// <summary>
        /// Hides the tooltip until the next call to
        /// <see cref="Open(string)"/>.
        /// </summary>
        public void Close()
        {
            waitForDisplay = false;
            childTransform.gameObject.SetActive(false);
            textObj.text = string.Empty;
        }

        /// <summary>
        /// Changes the tooltip message while it is
        /// displayed.
        /// </summary>
        /// <param name="message">The new tooltip message.</param>
        public void ChangeMessage(string message)
        {
            if (childTransform.gameObject.activeSelf || waitForDisplay)
                textObj.text = message;
        }

        private void SetEnabled(bool newEnabled)
        {
            if (newEnabled == this.isEnabled)
                return;

            if (newEnabled)
            {
                isEnabled = true;
                Close();
                this.gameObject.SetActive(true);
            }
            else
            {
                isEnabled = false;
                Close();
                this.gameObject.SetActive(false);
            }
        }
    }

} // namespace AS2.UI
