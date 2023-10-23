using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    [ExecuteInEditMode]
    public class ConfigurationEditor : MonoBehaviour
    {
        public ConfigData configData;

        private static ConfigurationEditor instance;
        public static ConfigurationEditor Instance
        {
            get { return instance; }
        }

        private void OnValidate()
        {
            if (instance is null)
                instance = this;
        }
    }

} // namespace AS2
