using UnityEngine;
using System.Collections;

namespace AS2.UI
{

    /// <summary>
    /// Sets the cursor types.
    /// </summary>
    public class CursorHandler
    {

        private CursorType currentCursorType = CursorType.Default;

        private Texture2D texture_mouseGameDefault = Resources.Load<Texture2D>(FilePaths.path_ui + "Cursor_Default");

        /// <summary>
        /// Changes the cursor to the given type.
        /// </summary>
        /// <param name="type"></param>
        public void SetCursorType(CursorType type)
        {
            if (type == currentCursorType)
            {
                return;
            }

            switch (type)
            {
                case CursorType.Default:
                    throw new System.NotImplementedException();
                case CursorType.AmoebotSimDefault:
                    Cursor.SetCursor(texture_mouseGameDefault, new Vector2(145f / 500f, 52f / 500f), CursorMode.Auto);
                    currentCursorType = CursorType.AmoebotSimDefault;
                    Log.Debug("Cursor Updated!");
                    break;
                default:
                    break;
            }
        }

        public enum CursorType
        {
            Default, AmoebotSimDefault
        }

    }

}