// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Utility for opening file dialogs used to save and load files
    /// of several types. Uses the <c>StandaloneFileBrowser</c> plugin.
    /// <para>
    /// This class saves the last used directory separately for each
    /// file type.
    /// </para>
    /// </summary>
    public static class FileBrowser
    {
        /// <summary>
        /// Helper class encapsulating all data and functionality
        /// needed to handle one file type.
        /// </summary>
        protected class FileTypeHandler
        {
            /// <summary>
            /// The file extension(s) used for the file type.
            /// </summary>
            public string fileExt;
            /// <summary>
            /// The title of the load dialog if no other title is specified.
            /// </summary>
            public string defaultTitleLoad = "";
            /// <summary>
            /// The title of the save dialog if no other title is specified.
            /// </summary>
            public string defaultTitleSave = "";
            /// <summary>
            /// The initial of the saved file if no other name is specified.
            /// </summary>
            public string defaultName = "";
            /// <summary>
            /// The path of the last directory in which a file was saved or loaded.
            /// </summary>
            public string lastDir = "";

            public FileTypeHandler(string fileExt, string defaultTitleLoad = "", string defaultTitleSave = "", string defaultName = "")
            {
                this.fileExt = fileExt;
                this.defaultTitleLoad = defaultTitleLoad;
                this.defaultTitleSave = defaultTitleSave;
                this.defaultName = defaultName;
            }

            /// <summary>
            /// Opens a save file dialog and returns the selected file.
            /// </summary>
            /// <param name="title">The title of the dialog window.</param>
            /// <param name="defaultName">The initial name of the save file.</param>
            /// <param name="directory">The initial directory in which the dialog starts.</param>
            /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
            public string SaveFile(string title = "", string defaultName = "", string directory = "")
            {
                if (title.Equals(""))
                    title = defaultTitleSave;
                if (defaultName.Equals(""))
                    defaultName = this.defaultName;
                if (directory.Equals("") && !lastDir.Equals(""))
                    directory = lastDir;
#if (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
                string path = StandaloneFileBrowser.SaveFilePanel(title, directory, defaultName + "." + fileExt, "");
#else
                string path = StandaloneFileBrowser.SaveFilePanel(title, directory, defaultName, fileExt);
#endif
                UpdateLastDir(path);
                return path;
            }

            /// <summary>
            /// Opens a load file dialog and returns the selected file.
            /// </summary>
            /// <param name="title">The title of the dialog window.</param>
            /// <param name="directory">The initial directory in which the dialog starts.</param>
            /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
            public string LoadFile(string title = "", string directory = "")
            {
                if (title.Equals(""))
                    title = defaultTitleLoad;
                if (directory.Equals("") && !lastDir.Equals(""))
                    directory = lastDir;
#if (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
                string[] paths = StandaloneFileBrowser.OpenFilePanel(title, directory, "", false);
#else
                string[] paths = StandaloneFileBrowser.OpenFilePanel(title, directory, fileExt, false);
#endif
                if (paths.Length > 0)
                {
                    UpdateLastDir(paths[0]);
                    return paths[0];
                }
                else
                    return "";
            }

            /// <summary>
            /// Updates the last used directory using the path returned by the
            /// standalone file browser.
            /// </summary>
            /// <param name="path">The path returned by the last opened file browser.</param>
            private void UpdateLastDir(string path)
            {
                if (!path.Equals(""))
                {
                    try
                    {
                        lastDir = Directory.GetParent(path).FullName + Path.DirectorySeparatorChar;
                    }
                    catch (Exception e)
                    {
                        Log.Warning("Unable to determine parent directory of path " + path);
                        Debug.LogException(e);
                    }
                }
            }
        }


        private static FileTypeHandler initHandler = new FileTypeHandler("aminit", "Load Initialization State", "Save Initialization State", "initState");
        private static FileTypeHandler simHandler = new FileTypeHandler("amalgo", "Load Simulation State", "Save Simulation State", "algorithm");
        private static FileTypeHandler pngHandler = new FileTypeHandler("png", "Load Image", "Save Image", "screenshot");
        private static FileTypeHandler textHandler = new FileTypeHandler("txt", "Load Text", "Save Text", "text");

        /// <summary>
        /// Opens a save file dialog for text files.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="defaultName">The initial name of the save file.</param>
        /// <param name="directory">The initial directory.</param>
        /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
        public static string SaveTextFile(string title = "", string defaultName = "", string directory = "")
        {
            return textHandler.SaveFile(title, defaultName, directory);
        }

        /// <summary>
        /// Opens a save file dialog for PNG files.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="defaultName">The initial name of the save file.</param>
        /// <param name="directory">The initial directory.</param>
        /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
        public static string SavePNGFile(string title = "", string defaultName = "", string directory = "")
        {
            return pngHandler.SaveFile(title, defaultName, directory);
        }

        /// <summary>
        /// Opens a save file dialog for simulation state files.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="defaultName">The initial name of the save file.</param>
        /// <param name="directory">The initial directory.</param>
        /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
        public static string SaveSimFile(string title = "Save Simulation State", string defaultName = "algorithm", string directory = "")
        {
            return simHandler.SaveFile(title, defaultName, directory);
        }

        /// <summary>
        /// Opens a load file dialog for simulation state files.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="directory">The initial directory.</param>
        /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
        public static string LoadSimFile(string title = "Load Simulation State", string directory = "")
        {
            return simHandler.LoadFile(title, directory);
        }

        /// <summary>
        /// Opens a save file dialog for initialization state files.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="defaultName">The initial name of the save file.</param>
        /// <param name="directory">The initial directory.</param>
        /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
        public static string SaveInitFile(string title = "Save Initialization State", string defaultName = "initState", string directory = "")
        {
            return initHandler.SaveFile(title, defaultName, directory);
        }

        /// <summary>
        /// Opens a save file dialog for initialization state files.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="directory">The initial directory.</param>
        /// <returns>The path to the selected file. Will be empty if no file was selected.</returns>
        public static string LoadInitFile(string title = "Load Initialization State", string directory = "")
        {
            return initHandler.LoadFile(title, directory);
        }
    }

} // namespace AS2
