using UnityEngine;
using System.Collections;

// Copyright ©
// Part of the personal code library of Tobias Maurer (tobias.maurer.it@web.de).
// Usage by any current or previous members the University of paderborn and projects associated with the University or programmable matter is permitted.

/// <summary>
/// This class contains the information of a click, drag that is finished or ongoing.
/// </summary>
public class ClickAction : InputAction
{

    // Data
    public ClickButton clickButton;
    public ClickType clickType;
    public Vector2 positionStart;
    public Vector2 positionTarget;
    public bool ongoing;
    public float timestamp;

    public ClickAction(ClickButton clickButton, ClickType clickType, Vector2 positionStart, Vector2 positionTarget, bool ongoing, float timestamp) : base(InputType.Mouse)
    {
        this.clickButton = clickButton;
        this.clickType = clickType;
        this.positionStart = positionStart;
        this.positionTarget = positionTarget;
        this.ongoing = ongoing;
        this.timestamp = timestamp;
    }



    public enum ClickButton
    {
        LeftMouse, MiddleMouse, RightMouse
    }

    public enum ClickType
    {
        Click, Drag
    }

}