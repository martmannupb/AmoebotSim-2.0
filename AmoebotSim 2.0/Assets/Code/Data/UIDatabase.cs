using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UIDatabase
{
    // Settings / Particles
    public static GameObject prefab_setting_header = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Header");
    public static GameObject prefab_setting_spacing = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Spacing");
    public static GameObject prefab_setting_slider = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Slider");
    public static GameObject prefab_setting_text = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Value");
    public static GameObject prefab_setting_toggle = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Toggle");
    public static GameObject prefab_setting_dropdown = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/Dropdown");
    public static GameObject prefab_setting_valueSlider = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/ValueSlider");
    public static GameObject prefab_setting_minMax = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Settings/MinMax");

    // World Space
    public static GameObject prefab_worldSpace_particleTextUI = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/World Space/ParticleTextUI");
    public static GameObject prefab_worldSpace_backgroundTextUI = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/World Space/BackgroundTextUI");

    // Log
    public static GameObject prefab_log_element = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Log/Log Element");

}
