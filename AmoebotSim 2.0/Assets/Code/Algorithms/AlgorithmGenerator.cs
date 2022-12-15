using UnityEngine;

namespace AS2
{

    [ExecuteInEditMode]
    public class AlgorithmGenerator : MonoBehaviour
    {
        private static readonly string template =
    "using AS2.Sim;\n" +
    "using UnityEngine;\n\n" +
    "namespace {0}\n{{\n\n" +
    "    public class {1} : ParticleAlgorithm\n    {{\n" +

    "        // This is the display name of the algorithm (must be unique)\n" +
    "        public static new string Name => \"{3}\";\n\n" +

    "        // Specify the number of pins (may be 0)\n" +
    "        public override int PinsPerEdge => {4};\n\n" +

    "        // If the algorithm has a special generation method, specify its full name here\n" +
    "        //public static new string GenerationMethod => typeof({2}).FullName;\n\n" +

    "        // Declare attributes here\n" +
    "        // ...\n\n" +

    "        public {1}(Particle p) : base(p)\n        {{\n" +
    "            // Initialize the attributes here\n" +
    "            // Also, set the default initial color\n" +
    "            //SetMainColor(ColorData.Particle_Black);\n" +
    "        }}\n\n" +

    "        // Implement this if the particles require special initialization\n" +
    "        // The parameters will be converted to particle attributes for initialization\n" +
    "        //public void Init(/* Custom parameters with default values */)\n        //{{\n" +
    "        //    // This code is executed directly after the constructor\n" +
    "        //}}\n\n" +

    "        // Implement this method if the algorithm terminates at some point\n" +
    "        //public override bool IsFinished()\n        //{{\n" +
    "        //    // Return true when this particle has terminated\n" +
    "        //    return false;\n" +
    "        //}}\n\n" +

    "        // The movement activation method\n" +
    "        public override void ActivateMove()\n        {{\n" +
    "            // Implement the movement code here\n" +
    "        }}\n\n" +

    "        // The beep activation method\n" +
    "        public override void ActivateBeep()\n        {{\n" +
    "            // Implement the communication code here\n" +
    "        }}\n" +
    "    }}\n\n" +

    "    // Use this to implement a generation method for this algorithm\n" +
    "    // Its class name must be specified as the algorithm's GenerationMethod\n" +
    "    //public class {2} : InitializationMethod\n    //{{\n" +
    "    //    public {2}(AS2.Sim.ParticleSystem system) : base(system) {{ }}\n\n" +

    "    //    // This method implements the system generation\n" +
    "    //    // Its parameters will be shown in the UI and they must have default values\n" +
    "    //    public void Generate(/* Parameters with default values */)\n    //    {{\n" +
    "    //        // The parameters of the Init() method can be set as particle attributes here\n" +
    "    //    }}\n" +
    "    //}}\n\n" +
    "}} // namespace {0}\n";

        [Header("Algorithm Generation Utility")]

        [Tooltip("The identifier of the new algorithm. It will be used to name the algorithm's namespace and the algorithm file, so it must be a valid identifier and unique among the algorithm namespaces.")]
        public string algoName = "MyNewAlgo";

        [Tooltip("The class name of the new algorithm. Must be a valid identifier. Default is algoName + 'Particle'.")]
        public string className = "";

        [Tooltip("The name of the algorithm displayed in the UI. Must be unique. Default value is the algorithm name.")]
        public string displayName = "";

        [Tooltip("The number of pins on each port of a particle. Must be >= 0.")]
        public int numPins = 1;

        public static void CreateAlgorithm(string nameSpace, string className, string initializerName, string dispName, int nPins, string filename)
        {
            try
            {
                System.IO.File.WriteAllText(filename, string.Format(template, nameSpace, className, initializerName, dispName, nPins));
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

} // namespace AS2
