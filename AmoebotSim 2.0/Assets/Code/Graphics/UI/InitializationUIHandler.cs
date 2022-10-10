using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System.Linq;
using System;
using SFB;

public class InitializationUIHandler : MonoBehaviour
{

    // References
    private UIHandler uiHandler;

    // UI References
    // UI
    public GameObject initModePanel;
    // Particle Generation Menu UI
    public GameObject genAlg_go_genAlg;
    public GameObject genAlg_go_amoebotAmount;
    public GameObject genAlg_go_paramParent;
    public Button button_particle_load;
    public Button button_particle_save;
    public Button button_particle_generate;
    // Additional Parameter UI
    public GameObject addPar_go_chirality;
    public GameObject addPar_go_compassDir;
    // Algorithm Generation Menu UI
    public GameObject alg_go_algo;
    public GameObject alg_go_paramAmount;
    public GameObject alg_go_genericParamParent;
    public Button button_algorithm_start;
    public Button button_algorithm_abort;

    // Data
    // Particle Generation Menu UI
    private UISetting_Dropdown genAlg_setting_genAlg;
    private UISetting_Text genAlg_setting_amoebotAmount;
    private List<UISetting> genAlg_settings = new List<UISetting>();
    // Additional Parameter UI
    private UISetting_Dropdown addPar_setting_chirality;
    private UISetting_Dropdown addPar_setting_compassDir;
    // Algorithm Generation Menu UI
    private UISetting_Dropdown alg_setting_algo;
    private UISetting_ValueSlider alg_setting_paramAmount;

    // Updated
    private List<UISetting> updatedSettings = new List<UISetting>();

    // Data
    System.Reflection.ParameterInfo[] genAlg_paramInfo;

    // Camera Colors
    private Color camColorBG;
    public Color camColorInitModeBG;

    public enum SettingChirality
    {
        Random,
        Clockwise,
        Counterclockwise
    }

    private void Start()
    {
        // Set References
        uiHandler = FindObjectOfType<UIHandler>();
        if (uiHandler == null) Log.Error("Could not find UIHandler.");

        // Hide Panel
        initModePanel.SetActive(false);
        // Collect Data
        camColorBG = Camera.main.backgroundColor;
        // Init
        InitUI();
        ResetUI();
    }

    private void Update()
    {
        // Update Settings
        foreach (var setting in updatedSettings)
        {
            setting.InteractiveBarUpdate();
        }
    }

    private void InitUI()
    {
        // Destroy generic param dummies
        for (int i = alg_go_genericParamParent.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(alg_go_genericParamParent.transform.GetChild(i).gameObject);
        }
    }

    private void ResetUI() // todo: reset would not work correctly
    {
        // Null check
        if (InitializationMethodManager.Instance == null)
        {
            Log.Error("InitializationUIHandler: InitializationMethodManager.Instance is null!");
            throw new System.NullReferenceException();
        }

        // Particle Generation
        List<string> genAlgStrings = InitializationMethodManager.Instance.GetAlgorithmNames();
        genAlg_setting_genAlg = new UISetting_Dropdown(genAlg_go_genAlg, null, "Gen. Alg.", genAlgStrings.ToArray(), genAlgStrings.Count > 0 ? genAlgStrings[0] : "");
        SetUpAlgorithmUI(genAlgStrings[0]);
        genAlg_setting_amoebotAmount = new UISetting_Text(genAlg_go_amoebotAmount, null, "Amount", "50", UISetting_Text.InputType.Int);
        // Additional Parameters
        List<string> chiralityList = new List<string>(System.Enum.GetNames(typeof(Initialization.Chirality)));
        chiralityList.Remove(Initialization.Chirality.Random.ToString());
        chiralityList.Insert(0, "Random");
        addPar_setting_chirality = new UISetting_Dropdown(addPar_go_chirality, null, "Chirality", chiralityList.ToArray(), chiralityList[0]);
        addPar_setting_chirality.backgroundButton_onButtonPressedLongEvent += SettingBarPressedLong;
        updatedSettings.Add(addPar_setting_chirality);
        List<string> compassDirList = new List<string>(System.Enum.GetNames(typeof(Initialization.Compass)));
        chiralityList.Remove(Initialization.Compass.Random.ToString());
        compassDirList.Insert(0, "Random");
        addPar_setting_compassDir = new UISetting_Dropdown(addPar_go_compassDir, null, "Compass Dir", compassDirList.ToArray(), compassDirList[0]);
        addPar_setting_compassDir.backgroundButton_onButtonPressedLongEvent += SettingBarPressedLong;
        updatedSettings.Add(addPar_setting_compassDir);
        // Algorithm Generation
        List<string> algStrings = AlgorithmManager.Instance.GetAlgorithmNames();
        alg_setting_algo = new UISetting_Dropdown(alg_go_algo, null, "Algorithm", algStrings.ToArray(), algStrings[0]);
        alg_setting_paramAmount = new UISetting_ValueSlider(alg_go_paramAmount, null, "Param Amount", 0, 10, 0, true);
        SetUpDynamicParams();
    }

    private void SetUpAlgorithmUI(string algorithm)
    {
        // Clear old UI
        foreach (var setting in genAlg_settings)
        {
            setting.Clear();
            Destroy(setting.GetGameObject());
        }
        genAlg_settings.Clear();
        // Instantiate new UI
        genAlg_paramInfo = InitializationMethodManager.Instance.GetAlgorithmParameters(algorithm);
        for (int i = 0; i < genAlg_paramInfo.Length; i++)
        {
            System.Reflection.ParameterInfo param = genAlg_paramInfo[i];
            //Log.Debug(param.ParameterType.ToString());
            Type type = param.ParameterType;
            if (type == typeof(bool))
            {
                bool defValue = false;
                if (param.HasDefaultValue) defValue = (bool)param.DefaultValue;
                UISetting_Toggle setting = new UISetting_Toggle(null, genAlg_go_paramParent.transform, param.Name, defValue);
                genAlg_settings.Add((UISetting)setting);
            }
            else if(type == typeof(int))
            {
                string defValue = "0";
                if (param.HasDefaultValue) defValue = "" + (int)param.DefaultValue;
                UISetting_Text setting = new UISetting_Text(null, genAlg_go_paramParent.transform, param.Name, defValue, UISetting_Text.InputType.Int);
                genAlg_settings.Add((UISetting)setting);
            }
            else if (type == typeof(float))
            {
                string defValue = "0";
                if (param.HasDefaultValue) defValue = "" + (float)param.DefaultValue;
                UISetting_Text setting = new UISetting_Text(null, genAlg_go_paramParent.transform, param.Name, defValue, UISetting_Text.InputType.Float);
                genAlg_settings.Add((UISetting)setting);
            }
            else if (type == typeof(string))
            {
                string defValue = "";
                if (param.HasDefaultValue) defValue = (string)param.DefaultValue;
                UISetting_Text setting = new UISetting_Text(null, genAlg_go_paramParent.transform, param.Name, defValue, UISetting_Text.InputType.Text);
                genAlg_settings.Add((UISetting)setting);
            }
            else if (type.IsSubclassOf(typeof(Enum)))
            {
                string defValue = Enum.GetNames(type)[0].ToString();
                if (param.HasDefaultValue) defValue = ((Enum)param.DefaultValue).ToString();
                UISetting_Dropdown setting = new UISetting_Dropdown(null, genAlg_go_paramParent.transform, param.Name, Enum.GetNames(type), defValue);
                genAlg_settings.Add((UISetting)setting);
            }
        }
        Log.Debug("Continue to code here.");

        // ..

    }

    private void SetUpDynamicParams()
    {
        // (implement dynamic params here ...)
        Log.Debug("Dynamic Params not implemented yet.");
        
    }

    public void Open()
    {
        // Update UI
        uiHandler.HideTopRightButtons();
        uiHandler.settingsUI.Close();
        uiHandler.particleUI.Close();
        initModePanel.SetActive(true);
        // Update Cam Color
        Camera.main.backgroundColor = camColorInitModeBG;
        // Notify System
        uiHandler.sim.PauseSim();
        uiHandler.sim.system.InitializationModeStarted();
        // Event
        if(EventDatabase.event_initializationUI_initModeOpenClose != null) EventDatabase.event_initializationUI_initModeOpenClose(true);
    }

    public void Close(bool aborted)
    {
        // Update UI
        uiHandler.ShowTopRightButtons();
        uiHandler.particleUI.Close();
        initModePanel.SetActive(false);
        // Update Cam Color
        Camera.main.backgroundColor = camColorBG;
        // Notify System
        if (aborted) uiHandler.sim.system.InitializationModeAborted();
        // Event
        if (EventDatabase.event_initializationUI_initModeOpenClose != null) EventDatabase.event_initializationUI_initModeOpenClose(false);
    }

    public bool IsOpen()
    {
        return initModePanel.activeSelf;
    }

    /// <summary>
    /// Opens a file chooser that loads an algorithm.
    /// </summary>
    public void ButtonPressed_Load()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Load Particle Setup", "", "json", false);
        if (paths.Length != 0) Log.Debug("Here we should load the file " + paths[0] + ".");
        else Log.Debug("No file chosen.");
    }

    /// <summary>
    /// Saves the current configuration in a file.
    /// </summary>
    public void ButtonPressed_Save()
    {
        string path = StandaloneFileBrowser.SaveFilePanel("Save Particle Setup", "", "state", "json");
        if (path.Equals("") == false) Log.Debug("Here we should save the file " + path + ".");
        else Log.Debug("No file chosen.");
    }

    /// <summary>
    /// Takes the currently chosen arguments to generate a particle environment.
    /// </summary>
    public void ButtonPressed_Generate()
    {
        // Collect Input Data
        string algorithm = genAlg_setting_genAlg.GetValueString();
        int amountParticles;
        string[] parameters = new string[genAlg_settings.Count];
        if (int.TryParse(genAlg_setting_amoebotAmount.GetValueString(), out amountParticles) == false)
        {
            Log.Error("Initialization: Generate: Could not parse particle amount!");
            return;
        }
        if(amountParticles <= 0)
        {
            Log.Error("Initialization: Generate: Please initialize the system with a positive number of particles!");
            return;
        }
        for (int i = 0; i < genAlg_settings.Count; i++)
        {
            UISetting setting = genAlg_settings[i];
            parameters[i] = setting.GetValueString();
        }
        object[] parameterObjects = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            parameterObjects[i] = TypeConverter.ConvertStringToObjectOfType(genAlg_paramInfo[i].ParameterType, parameters[i]);
        }

        // Call Generation Method
        uiHandler.sim.system.Reset();
        uiHandler.sim.system.GenerateParticles(algorithm, parameterObjects);

        // Center Camera
        uiHandler.Button_CameraCenterPressed();
    }

    public void SettingBarPressedLong(string name, float duration)
    {
        switch (name)
        {
            case "Chirality":
                // Apply chirality setting to all particles
                uiHandler.sim.system.SetSystemChirality((Initialization.Chirality)Enum.Parse(typeof(Initialization.Chirality), addPar_setting_chirality.GetValueString()));
                Log.Entry("Chirality" + addPar_setting_chirality.GetValueString() + "applied to all particles.");
                break;
            case "Compass Dir":
                // Apply compass dir setting to all particles
                uiHandler.sim.system.SetSystemCompassDir((Initialization.Compass)Enum.Parse(typeof(Initialization.Compass), addPar_setting_compassDir.GetValueString()));
                Log.Entry("Compass dir" + addPar_setting_compassDir.GetValueString() + " applied to all particles.");
                break;
            default:
                break;
        }
    }

    public void ButtonPressed_StartAlgorithm()
    {
        uiHandler.sim.system.InitializationModeFinished(alg_setting_algo.GetValueString());
        Close(false);
    }

    public void ButtonPressed_Abort()
    {
        Close(true);
    }

}
