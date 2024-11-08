using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Script for the configuration editor GameObject.
    /// The custom Inspector content is implemented by the
    /// <see cref="AS2.ConfigurationEditorBehavior"/>.
    /// </summary>
    [ExecuteInEditMode]
    public class ConfigurationEditor : MonoBehaviour
    {
        /// <summary>
        /// The configuration data to be editable in the Inspector
        /// </summary>
        public ConfigData configData;
    }

} // namespace AS2
