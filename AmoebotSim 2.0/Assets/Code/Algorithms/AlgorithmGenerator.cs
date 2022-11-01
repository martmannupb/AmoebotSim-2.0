using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AlgorithmGenerator
{
    private static string template =
"using UnityEngine;\n\n" +
"public class {0} : ParticleAlgorithm\n{{\n" +

"    // Specify the number of pins (may be 0)\n" +
"    public override int PinsPerEdge => 1;\n\n" +

"    // This is the display name of the algorithm (must be unique)\n" +
"    public static new string Name => \"{0}\";\n\n" +

"    // If the algorithm has a special generation method, specify its full name here\n" +
"    //public static new string GenerationMethod => typeof(/* Generation method class */).FullName;\n\n" +

"    // Declare attributes here\n" +
"    // ...\n\n" +

"    public {0}(Particle p) : base(p)\n    {{\n" +
"        // Initialize the attributes here\n" +
"        // Also, set the default initial color\n" +
"        //SetMainColor(ColorData.Particle_Black);\n" +
"    }}\n\n" +

"    // Implement this if the particles require special initialization\n" +
"    // The parameters will be converted to particle attributes for initialization\n" +
"    //public void Init(/* Custom parameters with default values */)\n    //{{\n" +
"    //    // This code is executed directly after the constructor\n" +
"    //}}\n\n" +

"    // Implement this method if the algorithm terminates at some point\n" +
"    //public override bool IsFinished()\n    //{{\n" +
"    //    // Return true when this particle has terminated\n" +
"    //    return false;\n" +
"    //}}\n\n" +

"    // The movement activation method\n" +
"    public override void ActivateMove()\n    {{\n" +
"        // Implement the movement code here\n" +
"    }}\n\n" +

"    // The beep activation method\n" +
"    public override void ActivateBeep()\n    {{\n" +
"        // Implement the communication code here\n" +
"    }}\n" +
"}}\n\n" +
 
"// Use this to implement a generation method for this algorithm\n" +
"// Its class name must be specified as the algorithm's GenerationMethod\n" +
"//public class {0}Initializer : InitializationMethod\n//{{\n" +
"//    public {0}Initializer(ParticleSystem system) : base(system) {{ }}\n\n" +

"//    // This method implements the system generation\n" +
"//    // Its parameters will be shown in the UI and they must have default values\n" +
"//    public void Generate(/* Parameters with default values */)\n//    {{\n" +
"//        // The parameters of the Init() method can be set as particle attributes here\n" +
"//    }}\n" +
"//}}\n";

    public static void CreateAlgorithm(string algoName, string fileName)
    {
        // TODO: Check if algoName is valid
        // TODO: Handle cases that file exists or path does not exist
        System.IO.File.WriteAllText(fileName, string.Format(template, algoName));
    }
}
