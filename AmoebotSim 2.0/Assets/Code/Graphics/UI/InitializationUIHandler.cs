using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class InitializationUIHandler : MonoBehaviour
{

    // References
    private UIHandler uiHandler;

    // Particle Generation Menu UI
    public TextMeshProUGUI field_particle_amountParticles;
    public TMP_Dropdown dropdown_particle_chirality;
    public TMP_Dropdown dropdown_particle_compassDir;
    public Button button_particle_load;
    public Button button_particle_save;
    public Button button_particle_generate;

    // Algorithm Generation Menu UI
    public TMP_Dropdown dropdown_algorithm_algo;
    public Button dropdown_algorithm_start;

    public InitializationUIHandler()
    {
        // Set References
        uiHandler = FindObjectOfType<UIHandler>();
        if (uiHandler == null) Log.Error("Could not find UIHandler.");
    }

    public enum SettingChirality
    {
        Random,
        Clockwise,
        Counterclockwise
    }

    public void InitUI()
    {
        // Particle Generation
        dropdown_particle_chirality.AddOptions(new List<string>(System.Enum.GetNames(typeof(SettingChirality))));
        List<string> compassDirList = new List<string>(System.Enum.GetNames(typeof(Direction)));
        dropdown_particle_compassDir.AddOptions(compassDirList);

        // Algorithm Generation
        System.Type[] algorithmClasses = new System.Type[0]; //dummy //= typeof(ParticleAlgorithm).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ParticleAlgorithm)));
        List<string> algoStrings = new List<string>();
        for (int i = 0; i < algorithmClasses.Length; i++)
        {
            algoStrings.Add(algorithmClasses[i].ToString());
        }
        dropdown_algorithm_algo.AddOptions(algoStrings);

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

        // Call Generation Method
        uiHandler.sim.system.Reset();
        uiHandler.sim.system.GenerateParticles(amountParticles, chirality);
    }

    public void ButtonPressed_StartAlgorithm()
    {
        
    }

}
