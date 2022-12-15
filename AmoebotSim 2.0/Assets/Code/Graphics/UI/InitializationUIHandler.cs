using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using AS2.Sim;
using UnityEngine;
using System.Linq;
using System;
using SFB;

namespace AS2.UI
{

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
        // Additional Parameter UI
        public GameObject addPar_go_chirality;
        public GameObject addPar_go_compassDir;
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

        private void InitUI()
        {
            // Null check
            if (InitializationMethodManager.Instance == null)
            {
                Log.Error("InitializationUIHandler: InitializationMethodManager.Instance is null!");
                throw new System.NullReferenceException();
            }

            // Init UI
            // Algorithm Generation
            List<string> algStrings = AlgorithmManager.Instance.GetAlgorithmNames();
            alg_setting_algo = new UISetting_Dropdown(alg_go_algo, null, "Algorithm", algStrings.ToArray(), algStrings[0]);
            alg_setting_algo.onValueChangedEvent += ValueChanged_Text;
            // Particle Generation
            List<string> genAlgStrings = InitializationMethodManager.Instance.GetAlgorithmNames();
            genAlg_setting_genAlg = new UISetting_Dropdown(genAlg_go_genAlg, null, "Gen. Alg.", genAlgStrings.ToArray(), genAlgStrings[0]);
            genAlg_setting_genAlg.onValueChangedEvent += ValueChanged_Text;
            genAlg_setting_genAlg.SetInteractable(false);
            // Additional Parameters
            List<string> chiralityList = new List<string>(System.Enum.GetNames(typeof(Initialization.Chirality)));
            chiralityList.Remove(Initialization.Chirality.Random.ToString());
            chiralityList.Insert(0, "Random");
            addPar_setting_chirality = new UISetting_Dropdown(addPar_go_chirality, null, "Chirality", chiralityList.ToArray(), chiralityList[0]);
            addPar_setting_chirality.backgroundButton_onButtonPressedLongEvent += SettingBarPressedLong;
            updatedSettings.Add(addPar_setting_chirality);
            List<string> compassDirList = new List<string>(System.Enum.GetNames(typeof(Initialization.Compass)));
            compassDirList.Remove(Initialization.Compass.Random.ToString());
            compassDirList.Insert(0, "Random");
            addPar_setting_compassDir = new UISetting_Dropdown(addPar_go_compassDir, null, "Compass Dir", compassDirList.ToArray(), compassDirList[0]);
            addPar_setting_compassDir.backgroundButton_onButtonPressedLongEvent += SettingBarPressedLong;
            updatedSettings.Add(addPar_setting_compassDir);
            // Reset
            ResetUI();
        }

        public bool IsInitialized()
        {
            return initialized;
        }

        private void ResetUI()
        {
            SetUpGenAlgUI(genAlg_setting_genAlg.GetValueString()); // reinit this first so that the default for the algo can be set afterwards (if available)
            SetUpAlgUI(alg_setting_algo.GetValueString());
        }

        private void SetUpAlgUI(string algorithm)
        {
            // Null Check (should never trigger)
            if (AlgorithmManager.Instance.IsAlgorithmKnown(algorithm) == false)
            {
                Log.Error("Could not find algorithm " + algorithm + "!");
                throw new System.NullReferenceException();
            }

            // Show default generation algorithm
            string defaultGenAlg = AlgorithmManager.Instance.GetAlgorithmGenerationMethod(algorithm);
            if (defaultGenAlg != null)
            {
                SetUpGenAlgUI(defaultGenAlg);
            }
            // Show algorithm parameters
            AmoebotSimulator.instance.system.SetSelectedAlgorithm(algorithm);
        }

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

        public Initialization.Chirality GetDropdownValue_Chirality()
        {
            Initialization.Chirality chirality = (Initialization.Chirality)Enum.Parse(typeof(Initialization.Chirality), addPar_setting_chirality.GetValueString());
            return chirality;
        }

        public Initialization.Compass GetDropdownValue_Compass()
        {
            Initialization.Compass compass = (Initialization.Compass)Enum.Parse(typeof(Initialization.Compass), addPar_setting_compassDir.GetValueString());
            return compass;

        }

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

        public void ValueChanged_Float(string name, float number)
        {
            switch (name)
            {
                default:
                    break;
            }
        }

        public void Open()
        {
            // Pause Sim
            AmoebotSimulator.instance.PauseSim();
            // Update UI
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
            // Event
            if (EventDatabase.event_initializationUI_initModeOpenClose != null) EventDatabase.event_initializationUI_initModeOpenClose(true);
        }

        public void Close(bool aborted)
        {
            // Update UI
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
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Load Particle Setup", "", "aminit", false);
            if (paths.Length != 0)
            {
                // Init particle system
                InitModeSaveData initModeSaveData = AmoebotSimulator.instance.system.LoadInitSaveState(SaveStateUtility.LoadInit(paths[0]));
                // Update init mode
                alg_setting_algo.SetValue(initModeSaveData.algString);
                genAlg_setting_genAlg.SetValue(initModeSaveData.genAlgString);
                for (int i = 0; i < genAlg_settings.Count; i++)
                {
                    UISetting setting = genAlg_settings[i];
                    setting.SetValueString(initModeSaveData.genAlg_parameters[i]);
                }
                // Log
                Log.Entry("Loaded initialization state from path: " + paths[0] + ".");
            }
            //else Log.Debug("No file chosen.");
        }

        /// <summary>
        /// Saves the current configuration in a file.
        /// </summary>
        public void ButtonPressed_Save()
        {
            string path = StandaloneFileBrowser.SaveFilePanel("Save Particle Setup", "", "initState", "aminit");
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

            // Call Generation Method
            AmoebotSimulator.instance.system.Reset();
            //uiHandler.sim.system.SetSelectedAlgorithm(algorithm);
            AmoebotSimulator.instance.system.GenerateParticles(genAlgorithm, parameterObjects);

            // Center Camera
            AmoebotSimulator.instance.uiHandler.Button_CameraCenterPressed();
        }

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

        public void ButtonPressed_StartAlgorithm()
        {
            AmoebotSimulator.instance.system.InitializationModeFinished(alg_setting_algo.GetValueString());
            Close(false);
        }

        public void ButtonPressed_Abort()
        {
            Close(true);
        }

    }

} // namespace AS2.UI
