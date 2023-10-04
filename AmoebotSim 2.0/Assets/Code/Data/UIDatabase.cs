using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AS2
{

    /// <summary>
    /// Database of UI element prefabs used to
    /// assemble UI content dynamically.
    /// </summary>
    public static class UIDatabase
    {
        public static GameObject prefab_ui_button = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Button");

        // Settings / Particles
        public static GameObject prefab_setting_header = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Header");
        public static GameObject prefab_setting_spacing = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Spacing");
        public static GameObject prefab_setting_slider = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Slider");
        public static GameObject prefab_setting_text = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Value");
        public static GameObject prefab_setting_toggle = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Toggle");
        public static GameObject prefab_setting_dropdown = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Dropdown");
        public static GameObject prefab_setting_valueSlider = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/ValueSlider");
        public static GameObject prefab_setting_minMax = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/MinMax");
        public static GameObject prefab_setting_color = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Color");

        // Randomization
        public static GameObject prefab_randomization_dices = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Randomization/Randomization Dices");
        public static GameObject prefab_randomization_placeholder = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Randomization/Randomization Placeholder");

        // World Space
        public static GameObject prefab_worldSpace_particleTextUI = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/World Space/ParticleTextUI");
        public static GameObject prefab_worldSpace_backgroundTextUI = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/World Space/BackgroundTextUI");
        public static GameObject prefab_worldSpace_backgroundTextUIAdvanced = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/World Space/BackgroundTextUI (Combined, Y offset 0.16)");

        // Log
        public static GameObject prefab_log_element = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Log/Log Element");
        public static GameObject prefab_log_elementLarge = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Log/Log Element Large");

    }

} // namespace AS2
