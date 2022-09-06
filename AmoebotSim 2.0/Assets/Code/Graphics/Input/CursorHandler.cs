using UnityEngine;
using System.Collections;

// Copyright ©
// Part of the personal code library of Tobias Maurer (tobias.maurer.it@web.de).
// Usage by any current or previous members the University of paderborn and projects associated with the University or programmable matter is permitted.

public class CursorHandler
{

    private CursorType currentCursorType = CursorType.Default;

    private Texture2D texture_mouseGameDefault = Resources.Load<Texture2D>(FilePaths.path_ui + "Cursor_Default");

    public void SetCursorType(CursorType type)
    {
        if(type == currentCursorType)
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