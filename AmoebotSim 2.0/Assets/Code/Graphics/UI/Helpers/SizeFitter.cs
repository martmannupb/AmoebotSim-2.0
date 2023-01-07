using UnityEngine;
using System;
using System.Collections;

namespace AS2.UI
{

    /// <summary>
    /// Simple script for resizing UI elements based on the childrens' sizes.
    /// Used for resizing the elements in the log (e.g. when there is a text block).
    /// </summary>
    public class SizeFitter : MonoBehaviour
    {

        // Settings
        public bool updateWidth = true;
        public bool updateHeight = true;

        // Flag
        private int resizeInFrameAmount = -1;
        private int resizeCentralizedInFrameAmount = -1;

        public void SetUpdatedValues(bool updateWidth, bool updateHeight)
        {
            this.updateWidth = updateWidth;
            this.updateHeight = updateHeight;
        }

        public void Update()
        {
            if (resizeInFrameAmount == 0) Resize();
            if (resizeInFrameAmount >= 0) resizeInFrameAmount--;

            if (resizeCentralizedInFrameAmount == 0) ResizeCentralized();
            if (resizeCentralizedInFrameAmount >= 0) resizeCentralizedInFrameAmount--;
        }

        /// <summary>
        /// Normal resizing (probably) assumes all coordinates are set in world space and true coordinates are set.
        /// Not 100% sure though, got most of this from Stackoverflow.
        /// </summary>
        /// <param name="frameAmount"></param>
        public void ResizeInFrameAmount(int frameAmount)
        {
            this.resizeInFrameAmount = frameAmount;
        }

        /// <summary>
        /// Centralized resizing assumes all children for a horizontal line (please disable updateWidth) or vertical line (please disable updateHeight) and are automatically oriented
        /// towards the center.
        /// </summary>
        /// <param name="frameAmount"></param>
        public void ResizeCentralizedInFrameAmount(int frameAmount)
        {
            this.resizeCentralizedInFrameAmount = frameAmount;
        }

        /// <summary>
        /// Internal normal resizing.
        /// </summary>
        private void Resize()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            RectTransform[] children = transform.GetComponentsInChildren<RectTransform>();
            RectTransform[] childrenWithoutFirst = new RectTransform[children.Length - 1];
            for (int i = 1; i < children.Length; i++)
            {
                childrenWithoutFirst[i - 1] = children[i];
            }

            float min_x, max_x, min_y, max_y;
            min_x = max_x = transform.localPosition.x;
            min_y = max_y = transform.localPosition.y;

            foreach (RectTransform child in childrenWithoutFirst)
            {
                Vector2 scale = child.sizeDelta;
                float temp_min_x, temp_max_x, temp_min_y, temp_max_y;

                temp_min_x = child.localPosition.x - (scale.x / 2);
                temp_max_x = child.localPosition.x + (scale.x / 2);
                temp_min_y = child.localPosition.y - (scale.y / 2);
                temp_max_y = child.localPosition.y + (scale.y / 2);

                if (temp_min_x < min_x)
                    min_x = temp_min_x;
                if (temp_max_x > max_x)
                    max_x = temp_max_x;

                if (temp_min_y < min_y)
                    min_y = temp_min_y;
                if (temp_max_y > max_y)
                    max_y = temp_max_y;
            }

            float width = rectTransform.sizeDelta.x;
            float height = rectTransform.sizeDelta.y;
            if (updateWidth) width = max_x - min_x;
            if (updateHeight) height = max_y - min_y;
            rectTransform.sizeDelta = new Vector2(width, height);

            //rectTransform.sizeDelta = new Vector2(max_x - min_x, max_y - min_y);
        }

        /// <summary>
        /// Internal centralized resizing.
        /// </summary>
        private void ResizeCentralized()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            RectTransform[] children = transform.GetComponentsInChildren<RectTransform>();
            RectTransform[] childrenWithoutFirst = new RectTransform[children.Length - 1];
            for (int i = 1; i < children.Length; i++)
            {
                childrenWithoutFirst[i - 1] = children[i];
            }

            float min_x, max_x, min_y, max_y;
            min_x = max_x = transform.localPosition.x;
            min_y = max_y = transform.localPosition.y;
            float max_width = 0f;
            float max_height = 0f;

            foreach (RectTransform child in childrenWithoutFirst)
            {
                Vector2 scale = child.sizeDelta;
                float temp_min_x, temp_max_x, temp_min_y, temp_max_y;

                temp_min_x = child.localPosition.x - (scale.x / 2);
                temp_max_x = child.localPosition.x + (scale.x / 2);
                temp_min_y = child.localPosition.y - (scale.y / 2);
                temp_max_y = child.localPosition.y + (scale.y / 2);

                if (max_width < temp_max_x - temp_min_x) max_width = temp_max_x - temp_min_x;
                if (max_height < temp_max_y - temp_min_y) max_height = temp_max_y - temp_min_y;
            }

            float width = rectTransform.sizeDelta.x;
            float height = rectTransform.sizeDelta.y;
            if (updateWidth) width = max_width;
            if (updateHeight) height = max_height;
            rectTransform.sizeDelta = new Vector2(width, height);

            //rectTransform.sizeDelta = new Vector2(max_x - min_x, max_y - min_y);
        }
    }

}