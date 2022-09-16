using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System.Linq;
using System;

public class InitializationUIHandler : MonoBehaviour
{

    // References
    private UIHandler uiHandler;

    // UI
    public GameObject panel;
    // Particle Generation Menu UI
    public TMP_InputField field_particle_amountParticles;
    public TMP_Dropdown dropdown_particle_chirality;
    public TMP_Dropdown dropdown_particle_compassDir;
    public Button button_particle_load;
    public Button button_particle_save;
    public Button button_particle_generate;
    // Algorithm Generation Menu UI
    public TMP_Dropdown dropdown_algorithm_algo;
    public Button button_algorithm_start;
    public Button button_algorithm_abort;

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

        // Init
        InitUI();
    }

    public void InitUI()
    {
        // Hide Panel
        panel.SetActive(false);
        // Collect Data
        camColorBG = Camera.main.backgroundColor;
        // Particle Generation
        dropdown_particle_chirality.AddOptions(new List<string>(System.Enum.GetNames(typeof(SettingChirality))));
        List<string> directionList = new List<string>(System.Enum.GetNames(typeof(Direction)));
        directionList.Insert(0, "Random");
        dropdown_particle_compassDir.AddOptions(directionList);
        // Algorithm Generation
        Type[] algorithmClasses = typeof(ParticleAlgorithm).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ParticleAlgorithm))).ToArray();
        List<string> algoStrings = new List<string>();
        for (int i = 0; i < algorithmClasses.Length; i++)
        {
            algoStrings.Add(algorithmClasses[i].ToString());
        }
        dropdown_algorithm_algo.AddOptions(algoStrings);
    }

    public void Open()
    {
        // Update UI
        uiHandler.HideTopRightButtons();
        uiHandler.settingsUI.Close();
        uiHandler.particleUI.Close();
        panel.SetActive(true);
        // Update Cam Color
        Camera.main.backgroundColor = camColorInitModeBG;
        // Notify System
        uiHandler.sim.PauseSim();
        uiHandler.sim.system.InitializationModeStarted();
    }

    public void Close(bool aborted)
    {
        // Update UI
        uiHandler.ShowTopRightButtons();
        panel.SetActive(false);
        // Update Cam Color
        Camera.main.backgroundColor = camColorBG;
        // Notify System
        if (aborted) uiHandler.sim.system.InitializationModeAborted();
    }

    /// <summary>
    /// Opens a file chooser that loads an algorithm.
    /// </summary>
    public void ButtonPressed_Load()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Saves the current configuration in a file.
    /// </summary>
    public void ButtonPressed_Save()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Takes the currently chosen arguments to generate a particle environment.
    /// </summary>
    public void ButtonPressed_Generate()
    {
        // Collect Input Data
        int amountParticles;
        if(int.TryParse(field_particle_amountParticles.text, out amountParticles) == false)
        {
            Log.Error("Initialization: Generate: Could not parse particle amount!");
            return;
        }
        SettingChirality chirality;
        if(SettingChirality.TryParse(dropdown_particle_chirality.options[dropdown_particle_chirality.value].text, out chirality) == false)
        {
            Log.Error("Initialization: Generate: Could not parse chirality!");
            return;
        }
        bool randomCompassDir = dropdown_particle_compassDir.value == 0;
        Direction compassDir = Direction.N;
        if(randomCompassDir == false)
        {
            object output;
            if (Direction.TryParse(typeof(Direction), dropdown_particle_compassDir.options[dropdown_particle_compassDir.value].text, out output) == false)
            {
                Log.Error("Initialization: Generate: Could not parse direction!");
                return;
            }
            compassDir = (Direction)output;
        }
        

        // Call Generation Method
        uiHandler.sim.system.Reset();
        uiHandler.sim.system.GenerateParticles(amountParticles, chirality, randomCompassDir, compassDir);
    }

    public void ButtonPressed_StartAlgorithm()
    {
        
    }

    public void ButtonPressed_Abort()
    {
        Close(true);
    }

}
