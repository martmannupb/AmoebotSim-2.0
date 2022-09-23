using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UIDatabase
{
    // Settings / Particles
    public static GameObject prefab_setting_header = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Header");
    public static GameObject prefab_setting_spacing = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Spacing");
    public static GameObject prefab_setting_slider = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Slider");
    public static GameObject prefab_setting_text = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Value");
    public static GameObject prefab_setting_toggle = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Toggle");
    public static GameObject prefab_setting_dropdown = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Dropdown");
    public static GameObject prefab_setting_valueSlider = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/ValueSlider");

    // Log
    public static GameObject prefab_log_element = Resources.Load<GameObject>(FilePaths.path_ui + "Prefabs/Log Element");

}
