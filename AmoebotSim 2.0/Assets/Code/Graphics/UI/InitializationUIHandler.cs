using AS2.Sim;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AS2.UI
{

    /// <summary>
    /// Controls the init mode panel.
    /// </summary>
    public class InitializationUIHandler : MonoBehaviour
    {
        // Init
        private bool initialized = false;

        // UI References
        // UI
        public GameObject initModePanel;
        // Particle Generation Menu UI
        public GameObject genAlg_go_genAlg;
        public GameObject genAlg_go_paramParent;
        public Button button_particle_load;
        public Button button_particle_save;
        public Button button_particle_generate;
        // Algorithm Generation Menu UI
        public GameObject alg_go_algo;
        public GameObject alg_go_paramAmount;
        public Button button_algorithm_start;
        public Button button_algorithm_abort;

        // Data
        // Particle Generation Menu UI
        private UISetting_Dropdown genAlg_setting_genAlg;
        private List<UISetting> genAlg_settings = new List<UISetting>();
        // Additional Parameter UI
        private UISetting_Dropdown addPar_setting_chirality;
        private UISetting_Dropdown addPar_setting_compassDir;
        // Algorithm Generation Menu UI
        private UISetting_Dropdown alg_setting_algo;

        // Updated
        private List<UISetting> updatedSettings = new List<UISetting>();

        // Data
        System.Reflection.ParameterInfo[] genAlg_paramInfo;

        // Camera Colors
        private Color camColorBG;
        public Color camColorInitModeBG;

        private void Start()
        {
            // Hide Panel
            initModePanel.SetActive(false);
            // Collect Data
            camColorBG = Camera.main.backgroundColor;
            // Init
            InitUI();
            ResetUI();
            initialized = true;
        }

        private void Update()
        {
            // Update Settings
            foreach (var setting in updatedSettings)
            {
                setting.InteractiveBarUpdate();
            }
        }

        /// <summary>
        /// Initializes the initialization UI, so that everything is set up.
        /// </summary>
        private void InitUI()
        {
            // Null check
            if (InitializationMethodManager.Instance == null)
            {
                Log.Error("InitializationUIHandler: InitializationMethodManager.Instance is null!");
                throw new System.NullReferenceException();
            }

            // Init UI
            // Algorithm selection
            List<string> algStrings = AlgorithmManager.Instance.GetAlgorithmNames();
            // Sort names alphabetically
            algStrings.Sort();
            alg_setting_algo = new UISetting_Dropdown(alg_go_algo, null, "Algorithm", algStrings.ToArray(), algStrings[0]);
            alg_setting_algo.onValueChangedEvent += ValueChanged_Text;
            // System generation
            List<string> genAlgStrings = InitializationMethodManager.Instance.GetAlgorithmNames();
            genAlg_setting_genAlg = new UISetting_Dropdown(genAlg_go_genAlg, null, "Gen. Alg.", genAlgStrings.ToArray(), genAlgStrings[0]);
            genAlg_setting_genAlg.onValueChangedEvent += ValueChanged_Text;
            genAlg_setting_genAlg.SetInteractable(false);
            // Reset
            ResetUI();
        }

        /// <summary>
        /// Checks if the class has been initialized already.
        /// </summary>
        /// <returns><c>true</c> if and only if the class has been initialized.</returns>
        public bool IsInitialized()
        {
            return initialized;
        }

        /// <summary>
        /// Regenerates the generation algorithm and the algorithm settings.
        /// </summary>
        private void ResetUI()
        {
            SetUpGenAlgUI(genAlg_setting_genAlg.GetValueString()); // reinit this first so that the default for the algo can be set afterwards (if available)
            SetUpAlgUI(alg_setting_algo.GetValueString());
        }

        /// <summary>
        /// Sets up the UI for the given algorithm.
        /// Checks if the given algorithm exists and then loads a default generation
        /// algorithm UI, if one has been defined.
        /// At the end, the system is generated once.
        /// </summary>
        /// <param name="algorithm">The unique name of the new selected algorithm.</param>
        private void SetUpAlgUI(string algorithm)
        {
            // Null Check (should never trigger)
            if (AlgorithmManager.Instance.IsAlgorithmKnown(algorithm) == false)
            {
                Log.Error("Could not find algorithm " + algorithm + "!");
                throw new System.NullReferenceException();
            }

            // Close particle panel
            AmoebotSimulator.instance.uiHandler.particleUI.Close();
            // Show default generation algorithm
            string defaultGenAlg = AlgorithmManager.Instance.GetAlgorithmGenerationMethod(algorithm);
            if (defaultGenAlg != null)
            {
                SetUpGenAlgUI(defaultGenAlg);
            }
            // Show algorithm parameters
            AmoebotSimulator.instance.system.SetSelectedAlgorithm(algorithm);
            AmoebotSimulator.instance.uiHandler.Button_FrameSystemPressed();
        }

        /// <summary>
        /// Sets up the UI for the given generation algorithm.
        /// Here the parameters of the generation algorithm are loaded via reflection,
        /// packed into a list of UI settings and shown to the user. Default values
        /// defined in the code are also set.
        /// </summary>
        /// <param name="algorithm">The unique name of the new selected
        /// generation method.</param>
        private void SetUpGenAlgUI(string algorithm)
        {
            // Set Value
            genAlg_setting_genAlg.SetValue(algorithm);
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
                else if (type == typeof(int))
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
                else if (type == typeof(MinMax))
                {
                    MinMax defValue = new MinMax(0, 0, true);
                    if (param.HasDefaultValue) defValue = (MinMax)param.DefaultValue;
                    UISetting_MinMax setting = new UISetting_MinMax(null, genAlg_go_paramParent.transform, param.Name, defValue);
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
        }

        /// <summary>
        /// Called when the user changes some values in certain dropdowns.
        /// </summary>
        /// <param name="name">The name of the changed setting.</param>
        /// <param name="text">The new string value of the setting.</param>
        public void ValueChanged_Text(string name, string text)
        {
            switch (name)
            {
                case "Algorithm":
                    SetUpAlgUI(text);
                    break;
                case "Gen. Alg.":
                    SetUpGenAlgUI(text);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Callback for changed float settings. Does not do anything yet.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="number"></param>
        public void ValueChanged_Float(string name, float number)
        {
            switch (name)
            {
                default:
                    break;
            }
        }

        /// <summary>
        /// Opens the initialization panel.
        /// </summary>
        public void Open()
        {
            // Pause Sim
            AmoebotSimulator.instance.PauseSim();
            // Update UI
            AmoebotSimulator.instance.uiHandler.Button_ToolStandardPressed();
            AmoebotSimulator.instance.uiHandler.HideTopRightButtons();
            AmoebotSimulator.instance.uiHandler.settingsUI.Close();
            AmoebotSimulator.instance.uiHandler.particleUI.Close();
            initModePanel.SetActive(true);
            // Update Cam Color
            Camera.main.backgroundColor = camColorInitModeBG;
            // Notify System
            AmoebotSimulator.instance.system.InitializationModeStarted(alg_setting_algo.GetValueString());
            // Generate (can be skipped)
            //ButtonPressed_Generate();
            // Center camera
            AmoebotSimulator.instance.uiHandler.Button_FrameSystemPressed();
            // Event
            EventDatabase.event_initializationUI_initModeOpenClose?.Invoke(true);
        }

        /// <summary>
        /// Closes the initialization panel.
        /// </summary>
        /// <param name="aborted">True if the init mode has been aborted.
        /// False if execution is successful.</param>
        public void Close(bool aborted)
        {
            // Update UI
            AmoebotSimulator.instance.uiHandler.Button_ToolStandardPressed();
            AmoebotSimulator.instance.uiHandler.ShowTopRightButtons();
            AmoebotSimulator.instance.uiHandler.particleUI.Close();
            initModePanel.SetActive(false);
            // Update Cam Color
            Camera.main.backgroundColor = camColorBG;
            // Notify System
            if (aborted) AmoebotSimulator.instance.system.InitializationModeAborted();
            AmoebotSimulator.instance.uiHandler.Update();
            AmoebotSimulator.instance.uiHandler.UpdateUI(false, true);
            // Event
            EventDatabase.event_initializationUI_initModeOpenClose?.Invoke(false);
        }

        /// <summary>
        /// True if init mode is open.
        /// </summary>
        /// <returns><c>true</c> if and only if the Initialization Panel is currently active.</returns>
        public bool IsOpen()
        {
            return initModePanel.activeSelf;
        }

        /// <summary>
        /// Opens a file chooser that loads an algorithm.
        /// </summary>
        public void ButtonPressed_Load()
        {
            string path = FileBrowser.LoadInitFile();
            if (!path.Equals(""))
            {
                // Init particle system
                InitModeSaveData initModeSaveData = AmoebotSimulator.instance.system.LoadInitSaveState(SaveStateUtility.LoadInit(path));
                // Update init mode
                alg_setting_algo.SetValue(initModeSaveData.algString);
                genAlg_setting_genAlg.SetValue(initModeSaveData.genAlgString);
                for (int i = 0; i < genAlg_settings.Count; i++)
                {
                    UISetting setting = genAlg_settings[i];
                    setting.SetValueString(initModeSaveData.genAlg_parameters[i]);
                }
                // Log
                Log.Entry("Loaded initialization state from path: " + path + ".");
            }
            //else Log.Debug("No file chosen.");
        }

        /// <summary>
        /// Saves the current configuration in a file.
        /// </summary>
        public void ButtonPressed_Save()
        {
            string path = FileBrowser.SaveInitFile();
            if (path.Equals("") == false)
            {
                // Generate general save data
                InitializationStateSaveData saveData = AmoebotSimulator.instance.system.GenerateInitSaveData();
                // Generate init mode save data
                InitModeSaveData initModeSaveData = new InitModeSaveData();
                initModeSaveData.algString = alg_setting_algo.GetValueString();
                initModeSaveData.genAlgString = genAlg_setting_genAlg.GetValueString();
                initModeSaveData.genAlg_parameters = new string[genAlg_settings.Count];
                for (int i = 0; i < genAlg_settings.Count; i++)
                {
                    UISetting setting = genAlg_settings[i];
                    initModeSaveData.genAlg_parameters[i] = setting.GetValueString();
                }
                // Combine and give to save utility
                saveData.initModeSaveData = initModeSaveData;
                SaveStateUtility.SaveInit(saveData, path);
                // Log
                Log.Entry("Saved initialization state at path: " + path + ".");
            }
            //else Log.Entry("No path chosen.");
        }

        /// <summary>
        /// Takes the currently chosen arguments to generate a particle environment.
        /// </summary>
        public void ButtonPressed_Generate()
        {
            // Collect Input Data
            string algorithm = alg_setting_algo.GetValueString();
            string genAlgorithm = genAlg_setting_genAlg.GetValueString();
            string[] parameters = new string[genAlg_settings.Count];
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

            // Reset selection
            AmoebotSimulator.instance.renderSystem.rendererUI.ResetSelection();

            // Call Generation Method
            //AmoebotSimulator.instance.system.Reset();
            //uiHandler.sim.system.SetSelectedAlgorithm(algorithm);
            AmoebotSimulator.instance.system.GenerateParticles(genAlgorithm, parameterObjects);

            // Bring whole system into view
            AmoebotSimulator.instance.uiHandler.Button_FrameSystemPressed();
        }

        /// <summary>
        /// Handler for the event that a setting has been pressed for
        /// an extended time.
        /// Applies the chirality/compass dir if the corresponding button has been clicked long enough.
        /// </summary>
        /// <param name="name">The name of the setting that was pressed.</param>
        /// <param name="duration">The duration for which the button has been pressed.</param>
        public void SettingBarPressedLong(string name, float duration)
        {
            switch (name)
            {
                case "Chirality":
                    // Apply chirality setting to all particles
                    AmoebotSimulator.instance.system.SetSystemChirality((Initialization.Chirality)Enum.Parse(typeof(Initialization.Chirality), addPar_setting_chirality.GetValueString()));
                    if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                    Log.Entry("Chirality" + addPar_setting_chirality.GetValueString() + "applied to all particles.");
                    break;
                case "Compass Dir":
                    // Apply compass dir setting to all particles
                    AmoebotSimulator.instance.system.SetSystemCompassDir((Initialization.Compass)Enum.Parse(typeof(Initialization.Compass), addPar_setting_compassDir.GetValueString()));
                    if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                    Log.Entry("Compass dir" + addPar_setting_compassDir.GetValueString() + " applied to all particles.");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// The simulation is started. Loads everything and closes the init mode.
        /// </summary>
        public void ButtonPressed_StartAlgorithm()
        {
            AmoebotSimulator.instance.system.InitializationModeFinished(alg_setting_algo.GetValueString());
            // Close tooltips
            TooltipHandler.Instance.Close();
            Close(false);
        }

        /// <summary>
        /// The init mode is aborted. Closes the init mode, the previous state is loaded afterwards.
        /// </summary>
        public void ButtonPressed_Abort()
        {
            // Close tooltips
            TooltipHandler.Instance.Close();
            Close(true);
        }

    }

}