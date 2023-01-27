using System;

namespace AS2
{

    /// <summary>
    /// Serializable representation of UI data in
    /// Initialization Mode.
    /// </summary>
    [Serializable]
    public class InitModeSaveData
    {
        /// <summary>
        /// Name of the selected algorithm.
        /// </summary>
        public string algString;
        /// <summary>
        /// Name of the generation method associated with the
        /// selected algorithm.
        /// </summary>
        public string genAlgString;
        /// <summary>
        /// Parameter values of the generation method.
        /// </summary>
        public string[] genAlg_parameters;
    }

} // namespace AS2
