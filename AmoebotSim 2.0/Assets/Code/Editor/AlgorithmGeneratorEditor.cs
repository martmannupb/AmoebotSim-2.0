using System.CodeDom.Compiler;
using UnityEngine;
using UnityEditor;

namespace AS2
{

    [CustomEditor(typeof(AlgorithmGenerator))]
    public class AlgorithmGeneratorEditor : Editor
    {
        SerializedProperty algoName;
        SerializedProperty className;
        SerializedProperty displayName;
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

            if (GUILayout.Button("Generate..."))
            {
                string algoNameBase = algoName.stringValue;
                string classNameFinal = className.stringValue;
                string dispName = displayName.stringValue;
                if (classNameFinal.Length == 0)
                    classNameFinal = algoNameBase + "Particle";
                if (dispName.Length == 0)
                    dispName = algoNameBase;
                string initializerName = algoNameBase + "Initializer";
                string nameSpace = "AS2.Algos." + algoNameBase;
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
