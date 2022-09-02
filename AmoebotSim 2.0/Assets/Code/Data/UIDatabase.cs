using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UIDatabase
{
    public static GameObject template_setting_slider = Resources.Load<GameObject>(FilePaths.path_ui + "Setting Slider");
    public static GameObject template_setting_text = Resources.Load<GameObject>(FilePaths.path_ui + "Setting Value");

}
