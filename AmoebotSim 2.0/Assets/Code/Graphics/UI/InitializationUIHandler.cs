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
    public Button button_particle_load;
    public Button button_particle_save;
    public Button button_particle_generate;
    // Additional Parameter UI
    public GameObject addPar_go_chirality;
    public GameObject addPar_go_compassDir;
    // Algorithm Generation Menu UI
    public GameObject algGen_go_algo;
    public Button button_algorithm_start;
    public Button button_algorithm_abort;

    // Data
    // Particle Generation Menu UI
    private UISetting_Dropdown genAlg_setting_genAlg;
    private UISetting_Text genAlg_setting_amoebotAmount;
    private List<UISetting> genAlg_settings;
    // Additional Parameter UI
    private UISetting addPar_setting_chirality;
    private UISetting addPar_setting_compassDir;
    // Algorithm Generation Menu UI
    public UISetting_Dropdown algGen_setting_algo;

    private void InitAll()
    {
        // Algorithm Generation
        Type[] algorithmClasses = typeof(ParticleAlgorithm).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ParticleAlgorithm))).ToArray();
        List<string> algoStrings = new List<string>();
        for (int i = 0; i < algorithmClasses.Length; i++)
        {
            algoStrings.Add(algorithmClasses[i].ToString());
        }
        genAlg_setting_genAlg = new UISetting_Dropdown(genAlg_go_genAlg, null, "Generation Algorithm", algoStrings.ToArray(), algoStrings.Count > 0 ? algoStrings[0] : "");
        //throw new System.NotImplementedException(); // here we should init the params for the first algorithm
        genAlg_setting_amoebotAmount = new UISetting_Text(genAlg_go_amoebotAmount, null, "Amount Amoebots", "50", UISetting_Text.InputType.Int);
    }

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
        ResetUI();
    }

    public void ResetUI()
    {
        // Particle Generation
        //genAlg_field_amount.text = "50";
        //dropdown_particle_chirality.ClearOptions();
        //dropdown_particle_chirality.AddOptions(new List<string>(System.Enum.GetNames(typeof(SettingChirality))));
        //List<string> directionList = new List<string>(System.Enum.GetNames(typeof(Direction)));
        //directionList.Insert(0, "Random");
        //dropdown_particle_compassDir.value = 0;
        //dropdown_particle_compassDir.ClearOptions();
        //dropdown_particle_compassDir.AddOptions(directionList);
        
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
        //int amountParticles;
        //if(int.TryParse(genAlg_field_amount.text, out amountParticles) == false)
        //{
        //    Log.Error("Initialization: Generate: Could not parse particle amount!");
        //    return;
        //}
        //SettingChirality chirality;
        //if(SettingChirality.TryParse(dropdown_particle_chirality.options[dropdown_particle_chirality.value].text, out chirality) == false)
        //{
        //    Log.Error("Initialization: Generate: Could not parse chirality!");
        //    return;
        //}
        //bool randomCompassDir = dropdown_particle_compassDir.value == 0;
        //Direction compassDir = Direction.N;
        //if(randomCompassDir == false)
        //{
        //    object output;
        //    if (Direction.TryParse(typeof(Direction), dropdown_particle_compassDir.options[dropdown_particle_compassDir.value].text, out output) == false)
        //    {
        //        Log.Error("Initialization: Generate: Could not parse direction!");
        //        return;
        //    }
        //    compassDir = (Direction)output;
        //}
        

        //// Call Generation Method
        //uiHandler.sim.system.Reset();
        //uiHandler.sim.system.GenerateParticles(amountParticles, chirality, randomCompassDir, compassDir);
    }

    public void ButtonPressed_StartAlgorithm()
    {
        
    }

    public void ButtonPressed_Abort()
    {
        Close(true);
    }

}
