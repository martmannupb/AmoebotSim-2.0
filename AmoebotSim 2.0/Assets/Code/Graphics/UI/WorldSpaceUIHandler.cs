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
    /// Controls the world space UI overlay (which shows the attributes of the particles).
    /// </summary>
    public class WorldSpaceUIHandler : MonoBehaviour
    {

        // Singleton
        public static WorldSpaceUIHandler instance;

        // World Space UI
        public GameObject go_worldSpaceUI;
        // Buttons
        public Button button_hideOverlay;
        // Fonts
        public TMP_FontAsset font_basic;
        public TMP_FontAsset font_circles; // must include the special ASCII circle arrow chars
        public TMP_FontAsset font_arrows; // must include the special ASCII arrows chars
        // Data
        private float cameraRotation = 0f;
        // Setting
        public bool showCompassDirArrows = true;
        // State
        private bool isVisible = false;

        private Dictionary<Direction, string> asciiDirectionArrows = new Dictionary<Direction, string>() {
            { Direction.NONE, "\u005F" },
            { Direction.N, "\uD83E\uDC71" },
            { Direction.NNE, "\uD83E\uDC75"},
            { Direction.ENE, "\uD83E\uDC75"},
            { Direction.E, "\uD83E\uDC72"},
            { Direction.ESE, "\uD83E\uDC76"},
            { Direction.SSE, "\uD83E\uDC76"},
            { Direction.S, "\uD83E\uDC73"},
            { Direction.SSW, "\uD83E\uDC77"},
            { Direction.WSW, "\uD83E\uDC77"},
            { Direction.W, "\uD83E\uDC70"},
            { Direction.WNW, "\uD83E\uDC74"},
            { Direction.NNW, "\uD83E\uDC74"} };

        public struct ParticleTextUIData
        {
            public GameObject go;
            public Vector2 pos;
            public bool isVisible;

            public ParticleTextUIData(GameObject go, Vector2 pos, bool isVisible)
            {
                this.go = go;
                this.pos = pos;
                this.isVisible = isVisible;
            }
        }


        // Data
        public Dictionary<IParticleState, ParticleTextUIData> particleTextUIData = new Dictionary<IParticleState, ParticleTextUIData>();
        // State
        private bool display_isVisible = true;
        private TextType display_type;
        private string display_identifier;

        // Temporary Data
        private Stack<IParticleState> tempParticleStack = new Stack<IParticleState>();

        // Defaults
        private Color color_particleTextBackgroundDefault = new Color(1f, 1f, 1f, 172f / 255f);
        private Color color_particleTextBackgroundTrue = new Color(90f / 255f, 255f / 255f, 99f / 255f, 172f / 255f);
        private Color color_particleTextBackgroundFalse = new Color(255f / 255f, 101f / 255f, 90f / 255f, 172f / 255f);
        private Color color_particleTextBackgroundCounterClockwise = new Color(252f / 255f, 255f / 255f, 90f / 255f, 172f / 255f);
        private Color color_particleTextBackgroundClockwise = new Color(90f / 255f, 251f / 255f, 255f / 255f, 172f / 255f);

        // Pooling
        private Stack<GameObject> pool_particleTextUI = new Stack<GameObject>();

        /// <summary>
        /// The type of the currently set overlay.
        /// </summary>
        public enum TextType
        {
            Attribute, Chirality, CompassDir, Text
        }

        public WorldSpaceUIHandler()
        {
            // Singleton
            instance = this;
        }

        private void Start()
        {
            // Test
            //DisplayText(TextType.Text, "Contract");

            // Hide
            HideAll();
        }

        /// <summary>
        /// Displays the overlay over every particle that has the given attribute/value/text.
        /// </summary>
        /// <param name="type">The type of data to display.</param>
        /// <param name="identifier">Identifier of the field to be displayed, e.g.,
        /// an attribute name.</param>
        /// <param name="showOverlay">Determines whether the overlay should be shown.</param>
        public void DisplayText(TextType type, string identifier, bool showOverlay = true)
        {
            // Save what we display
            this.display_type = type;
            this.display_identifier = identifier;
            // Update Texts
            foreach (var particle in particleTextUIData.Keys)
            {
                tempParticleStack.Push(particle);
            }
            while (tempParticleStack.Count > 0)
            {
                IParticleState particle = tempParticleStack.Pop();
                DisplayTextForParticle(particle);
            }
            tempParticleStack.Clear();
            // Print Message
            //switch (type)
            //{
            //    case TextType.Attribute:
            //        Log.Entry("Showing: Overlay for particle attribute " + identifier + ".");
            //        break;
            //    case TextType.Chirality:
            //        Log.Entry("Showing: Overlay for particle chiralities.");
            //        break;
            //    case TextType.CompassDir:
            //        Log.Entry("Showing: Overlay for compass direction.");
            //        break;
            //    case TextType.Text:
            //        //Log.Entry("This is text..");
            //        break;
            //    default:
            //        break;
            //}
            // Show
            if (showOverlay) ShowVisible();
        }

        /// <summary>
        /// Refreshes the overlay if it is already shown. Call this when something has changed in the shown attributes.
        /// </summary>
        public void Refresh()
        {
            if (display_isVisible)
            {
                DisplayText(display_type, display_identifier);
            }
        }

        /// <summary>
        /// Displays the currently set overlay for a given particle.
        /// </summary>
        /// <param name="particle">The particle for which the overlay should be displayed.</param>
        private void DisplayTextForParticle(IParticleState particle)
        {
            ParticleTextUIData data = particleTextUIData[particle];

            switch (this.display_type)
            {
                case TextType.Attribute:
                    IParticleAttribute attribute = particle.TryGetAttributeByName(display_identifier);
                    if (attribute != null)
                    {
                        // Show attribute
                        if (attribute is ParticleAttribute_Bool) UpdateParticleText(particle, ((ParticleAttribute_Bool)attribute).GetValue());
                        else UpdateParticleText(particle, attribute.ToString_AttributeValue());
                        data.isVisible = true;
                    }
                    else
                    {
                        // Attribute not available
                        data.isVisible = false;
                    }
                    break;
                case TextType.Chirality:
                    // Show Chirality (always available)
                    UpdateParticleText_Chirality(particle, particle.Chirality());
                    data.isVisible = true;
                    break;
                case TextType.CompassDir:
                    UpdateParticleText_CompassDir(particle, particle.CompassDir());
                    data.isVisible = true;
                    break;
                case TextType.Text:
                    UpdateParticleText(particle, display_identifier);
                    data.isVisible = true;
                    break;
                default:
                    break;
            }

            particleTextUIData[particle] = data;
        }

        /// <summary>
        /// Called by every particle after their regular movement updates.
        /// </summary>
        /// <param name="particle">The particle that should be updated.</param>
        /// <param name="curPos">The current world position of the particle.</param>
        public void ParticleUpdate(IParticleState particle, Vector3 curPos)
        {
            ParticleTextUIData data = particleTextUIData[particle];
            if (data.pos.x != curPos.x || data.pos.y != curPos.y)
            {
                UpdateParticleTextPosition(particle, curPos);
            }
        }

        /// <summary>
        /// Adds a particle to the overlay system.
        /// </summary>
        /// <param name="particle">The particle to add.</param>
        /// <param name="particlePosition">The initial position of the particle.</param>
        /// <param name="isVisible">If the particle overlay is initially visible.</param>
        /// <returns>The GameObject holding the overlay text.</returns>
        public GameObject AddParticleTextUI(IParticleState particle, Vector2 particlePosition, bool isVisible = true)
        {
            GameObject go = PoolCreate_particleTextUI(particle, isVisible);
            UpdateParticleTextPosition(particle, particlePosition);
            DisplayTextForParticle(particle);
            return go;
        }

        /// <summary>
        /// Removes a particle from the overlay system.
        /// </summary>
        /// <param name="particle">The particle to remove.</param>
        /// <returns><c>true</c> if and only if the given particle was
        /// successfully removed.</returns>
        public bool RemoveParticleTextUI(IParticleState particle)
        {
            if (particleTextUIData.ContainsKey(particle) == false)
            {
                Log.Error("WorldSpaceUIHandler: RemoveParticleTextUI: Particle text UI cannot be found in list!");
                return false;
            }
            // Pool Release
            PoolRealease_particleTextUI(particle);
            return true;
        }

        /// <summary>
        /// Updates the particle overlay for a single particle with a text.
        /// </summary>
        /// <param name="particle">The particle for which the overlay should be updated.</param>
        /// <param name="text">The text to display.</param>
        private void UpdateParticleText(IParticleState particle, string text)
        {
            ParticleTextUIData data = particleTextUIData[particle];
            // Set Color
            Color color = color_particleTextBackgroundDefault;
            data.go.GetComponent<Image>().color = color;
            // Set Text
            TextMeshProUGUI tmp = data.go.GetComponentInChildren<TextMeshProUGUI>();
            tmp.font = font_basic;
            tmp.text = text;
        }

        /// <summary>
        /// Updates the particle overlay for a single particle with a "True" or "False" text.
        /// Also sets a color based on the true/false value.
        /// </summary>
        /// <param name="particle">The particle for which the overlay should be updated.</param>
        /// <param name="isTrue">The truth value to display.</param>
        private void UpdateParticleText(IParticleState particle, bool isTrue)
        {
            ParticleTextUIData data = particleTextUIData[particle];
            // Set Color
            Color color;
            if (isTrue) color = color_particleTextBackgroundTrue;
            else color = color_particleTextBackgroundFalse;
            data.go.GetComponent<Image>().color = color;
            // Set Text
            TextMeshProUGUI tmp = data.go.GetComponentInChildren<TextMeshProUGUI>();
            tmp.font = font_basic;
            tmp.text = isTrue ? "True" : "False";
        }

        /// <summary>
        /// Updates the particle overlay for a single particle with a text showing its chirality.
        /// The chirality is displayed using an ASCII character that shows a circular arrow.
        /// </summary>
        /// <param name="particle">The particle for which the overlay should be updated.</param>
        /// <param name="counterClockwise">The chirality to set.</param>
        private void UpdateParticleText_Chirality(IParticleState particle, bool counterClockwise)
        {
            ParticleTextUIData data = particleTextUIData[particle];
            // Set Color
            Color color;
            if (counterClockwise) color = color_particleTextBackgroundCounterClockwise;
            else color = color_particleTextBackgroundClockwise;
            data.go.GetComponent<Image>().color = color;
            // Set Text
            TextMeshProUGUI tmp = data.go.GetComponentInChildren<TextMeshProUGUI>();
            tmp.font = font_circles;
            tmp.text = counterClockwise ? "\u2B6F" : "\u2B6E";
        }

        /// <summary>
        /// Updates the particle overlay for a single particle with a text showing its
        /// compass direction. The direction is displayed using an ASCII character
        /// that shows an arrow, if this is enabled.
        /// </summary>
        /// <param name="particle">The particle for which the overlay should be updated.</param>
        /// <param name="compassDir">The compass dir to display.</param>
        private void UpdateParticleText_CompassDir(IParticleState particle, Direction compassDir)
        {
            ParticleTextUIData data = particleTextUIData[particle];
            // Set Color
            Color color = color_particleTextBackgroundDefault;
            data.go.GetComponent<Image>().color = color;
            // Set Text
            TextMeshProUGUI tmp = data.go.GetComponentInChildren<TextMeshProUGUI>();
            if(showCompassDirArrows)
            {
                // Arrows
                tmp.font = font_arrows;
                int deg30rotAmount = (int)(((360f - cameraRotation) / 30f) % 360);
                Direction displayDir = DirectionHelpers.Rotate30(compassDir, deg30rotAmount);
                tmp.text = asciiDirectionArrows[displayDir];
            }
            else
            {
                // Text
                tmp.font = font_basic;
                tmp.text = compassDir.ToString();
            }

        }

        /// <summary>
        /// Updates the position of the particle overlay.
        /// </summary>
        /// <param name="particle">The particle for which the position of the overlay should be updated.</param>
        /// <param name="particlePosition">The new overlay position.</param>
        private void UpdateParticleTextPosition(IParticleState particle, Vector2 particlePosition)
        {
            ParticleTextUIData data = particleTextUIData[particle];
            data.go.transform.position = new Vector3(particlePosition.x, particlePosition.y, 0f); //RenderSystem.zLayer_worldSpaceUI); // this somehow prevents automatic batching
            data.pos = new Vector2(particlePosition.x, particlePosition.y);
        }

        /// <summary>
        /// Sets the rotation of the particle overlay. Use this in combination with the global rotation of the main camera.
        /// </summary>
        /// <param name="cameraRotationDegrees">The main camera's current rotation in degrees.</param>
        public void SetCameraRotation(float cameraRotationDegrees)
        {
            this.cameraRotation = cameraRotationDegrees;
            foreach (var item in particleTextUIData.Values)
            {
                GameObject go = item.go;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.rotation = Quaternion.Euler(0f, 0f, cameraRotationDegrees);
            }
            if (display_type == TextType.CompassDir) Refresh();
        }

        /// <summary>
        /// Shows the world space UI (all elements that are flagged as visible). This is the default value.
        /// Call <see cref="HideAll"/> to hide it.
        /// </summary>
        public void ShowVisible()
        {
            foreach (var item in particleTextUIData.Values)
            {
                if (item.isVisible) item.go.SetActive(true);
            }
            display_isVisible = true;
            button_hideOverlay.interactable = true;
            StartCoroutine(PaintButtonActiveInactive(button_hideOverlay, true));
            isVisible = true;
        }

        /// <summary>
        /// Hides the world space UI.
        /// Call <see cref="ShowVisible"/> to show it again.
        /// </summary>
        public void HideAll()
        {
            foreach (var item in particleTextUIData.Values)
            {
                item.go.SetActive(false);
            }
            display_isVisible = false;
            button_hideOverlay.interactable = false;
            StartCoroutine(PaintButtonActiveInactive(button_hideOverlay, false));
            isVisible = false;
        }

        /// <summary>
        /// Updates the color of the button.
        /// Called as coroutine to wait until the active and inactive button colors
        /// have been set by the main UI handler.
        /// </summary>
        /// <param name="button">The button to paint.</param>
        /// <param name="active">If the button is active.</param>
        /// <returns></returns>
        private IEnumerator PaintButtonActiveInactive(Button button, bool active)
        {
            while (!(AmoebotSimulator.instance != null && AmoebotSimulator.instance.uiHandler != null
                && AmoebotSimulator.instance.uiHandler.GetButtonColor_Active() != default(Color) && AmoebotSimulator.instance.uiHandler.GetButtonColor_Inactive() != default(Color)))
            {
                yield return new WaitForEndOfFrame();
            }
            if(active) button.gameObject.GetComponent<Image>().color = AmoebotSimulator.instance.uiHandler.GetButtonColor_Active();
            else button.gameObject.GetComponent<Image>().color = AmoebotSimulator.instance.uiHandler.GetButtonColor_Inactive();
        }

        /// <summary>
        /// Checks if the UI is active.
        /// </summary>
        /// <returns><c>true</c> if and only if the UI overlay is currently active.</returns>
        public bool IsActive()
        {
            return isVisible;
        }






        // Pooling ===================
        /// <summary>
        /// Makes a new text overlay GameObject available using the pooling mechanism.
        /// </summary>
        /// <param name="particle">The particle to which the text overlay should belong.</param>
        /// <param name="isVisible">Determines whether the overlay element should be visible.</param>
        /// <returns>The new text overlay GameObject.</returns>
        public GameObject PoolCreate_particleTextUI(IParticleState particle, bool isVisible)
        {
            GameObject go;
            if (pool_particleTextUI.Count > 0) go = pool_particleTextUI.Pop();
            else go = Instantiate<GameObject>(UIDatabase.prefab_worldSpace_particleTextUI, new Vector3(0f, 0f, 0f), Quaternion.identity, go_worldSpaceUI.transform);
            go.SetActive(display_isVisible && isVisible);
            particleTextUIData.Add(particle, new ParticleTextUIData(go, Vector2.zero, isVisible));
            return go;
        }

        /// <summary>
        /// Releases the text overlay element belonging to the given particle
        /// and returns it to the pool.
        /// </summary>
        /// <param name="particle">The particle whose overlay should be released.</param>
        public void PoolRealease_particleTextUI(IParticleState particle)
        {
            // Hide
            ParticleTextUIData data = particleTextUIData[particle];
            data.isVisible = false;
            particleTextUIData[particle] = data;
            data.go.SetActive(false);
            // Remove Link
            particleTextUIData.Remove(particle);
            // Pool
            pool_particleTextUI.Push(data.go);
        }

    }

}