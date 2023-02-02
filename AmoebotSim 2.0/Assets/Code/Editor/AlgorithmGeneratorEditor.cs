using System.CodeDom.Compiler;
using UnityEngine;
using UnityEditor;

namespace AS2
{

    /// <summary>
    /// Custom Unity Editor Inspector panel for the algorithm generation utility.
    /// </summary>
    [CustomEditor(typeof(AlgorithmGenerator))]
    public class AlgorithmGeneratorEditor : Editor
    {
        /// <summary>
        /// The base name of the algorithm to generate.
        /// </summary>
        SerializedProperty algoName;
        /// <summary>
        /// The name for the algorithm class.
        /// </summary>
        SerializedProperty className;
        /// <summary>
        /// The name under which the new algorithm should be displayed.
        /// </summary>
        SerializedProperty displayName;
        /// <summary>
        /// The number of pins per edge used by the algorithm.
        /// </summary>
        SerializedProperty numPins;

        private void OnEnable()
        {
            algoName = serializedObject.FindProperty("algoName");
            className = serializedObject.FindProperty("className");
            displayName = serializedObject.FindProperty("displayName");
            numPins = serializedObject.FindProperty("numPins");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Generate button
            if (GUILayout.Button("Generate..."))
            {
                string algoNameBase = algoName.stringValue;
                string classNameFinal = className.stringValue;
                string dispName = displayName.stringValue;

                // Default class name is base name + "Particle"
                if (classNameFinal.Length == 0)
                    classNameFinal = algoNameBase + "Particle";

                // Default display name is base name
                if (dispName.Length == 0)
                    dispName = algoNameBase;

                // Initialization method is called base name + "Initializer"
                string initializerName = algoNameBase + "Initializer";
                // Namespace is called AS2.Algos.<basename>
                string nameSpace = "AS2.Algos." + algoNameBase;

                // Number of pins must be at least 0
                int nPins = numPins.intValue;
                if (nPins < 0)
                {
                    Debug.LogWarning("Number of pins must be >= 0. Defaulting to 0.");
                    nPins = 0;
                }

                // Check if the given algorithm name is valid
                CodeDomProvider provider = CodeDomProvider.CreateProvider("C#");
                if (provider.IsValidIdentifier(algoNameBase))
                {
                    // Open save file dialog
                    string selectedFile = EditorUtility.SaveFilePanelInProject("Choose New Algorithm File", algoNameBase, "cs",
                        "Please save the algorithm in the Assets/Code/Algorithms folder", Application.dataPath + "/Code/Algorithms");

                    // Create the file and refresh the Editor if a file location was chosen
                    if (selectedFile.Length > 0)
                    {
                        AlgorithmGenerator.CreateAlgorithm(nameSpace, classNameFinal, initializerName, dispName, nPins, selectedFile);
                        AssetDatabase.Refresh();
                    }
                }
                else
                {
                    Debug.LogError("Invalid algorithm name: '" + algoNameBase + "'");
                }
            }
        }
    }

} // namespace AS2
