using UnityEngine;
using System.Collections;

// Copyright ©
// Part of the personal code library of Tobias Maurer (tobias.maurer.it@web.de).
// Usage by any current or previous members the University of paderborn and projects associated with the University or programmable matter is permitted.

/// <summary>
/// Goal: Use the stream of MouseState information to build a logical InputState.
/// This should contain timestamps when a button has been pressed and where it has been pressed,
/// so we can determine when a correct mouse click or drag action has been processed.
/// Afterwards clicks, keyboard actions and are converted into InputActions and forwarded to the engine.
/// </summary>
public static class InputHandler
{

    // Constants =====
    private readonly static float ClickDragMinStraightWorldSpaceMovement = 0.4f; // the max movement during a click to be counted as click and not drag

    // Mouse Clicks =====
    // Left Mouse
    public static bool mouseLeft_clickAction;
    public static ClickAction.ClickType mouseLeft_clickActionType;
    public static Vector2 mouseLeft_clickAction_positionInitial;
    public static Vector2 mouseLeft_clickAction_positionLatest;
    public static float mouseLeft_clickAction_timestamp;
    public static float mouseLeft_clickAction_timePassed;

    // Right Mouse
    public static bool mouseRight_clickAction;
    public static ClickAction.ClickType mouseRight_clickActionType;
    public static Vector2 mouseRight_clickAction_positionInitial;
    public static Vector2 mouseRight_clickAction_positionLatest;
    public static float mouseRight_clickAction_timestamp;
    public static float mouseRight_clickAction_timePassed;

    // Middle Mouse
    public static bool mouseMiddle_clickAction;
    public static ClickAction.ClickType mouseMiddle_clickActionType;
    public static Vector2 mouseMiddle_clickAction_positionInitial;
    public static Vector2 mouseMiddle_clickAction_positionLatest;
    public static float mouseMiddle_clickAction_timestamp;
    public static float mouseMiddle_clickAction_timePassed;
    public static int mouseMiddle_scroll;

    /// <summary>
    /// Receives information from the InputController and converts all inputs from the mouse and keyboard into ClickAction objects that are sent to the InputManager.
    /// </summary>
    /// <param name="mouseState"></param>
    public static void InputTick(MouseState mouseState)
    {
        // Left Mouse Button
        if(mouseLeft_clickAction)
        {
            // Mouse is having a ClickAction
            // Update position data and check when mouse is released
            mouseLeft_clickAction_timePassed += Time.deltaTime;
            mouseLeft_clickAction_positionLatest = mouseState.mouse_positionWorld;
            if (Mathf.Abs(mouseLeft_clickAction_positionInitial.x - mouseState.mouse_positionWorld.x) >= ClickDragMinStraightWorldSpaceMovement || Mathf.Abs(mouseLeft_clickAction_positionInitial.y - mouseState.mouse_positionWorld.y) >= ClickDragMinStraightWorldSpaceMovement)
            {
                mouseLeft_clickActionType = ClickAction.ClickType.Drag;
            }
            if (mouseState.mouseLeft_released || mouseState.mouseLeft_hold == false)
            {
                ExecuteAction_LeftMouse(false);
                mouseLeft_clickAction = false;
            }
            else if(mouseState.mouseLeft_hold && mouseLeft_clickActionType == ClickAction.ClickType.Drag)
            {
                ExecuteAction_LeftMouse(true);
            }
        }
        else
        {
            // Mouse had no ClickAction
            // Check if mouse has been pressed
            if(mouseState.mouseLeft_clicked) // todo: check if over UI, problem: mouseState.mouse_overUI is always true
            {
                mouseLeft_clickAction = true;
                mouseLeft_clickActionType = ClickAction.ClickType.Click;
                mouseLeft_clickAction_positionInitial = mouseState.mouse_positionWorld;
                mouseLeft_clickAction_timestamp = Time.time;
                mouseLeft_clickAction_timePassed = Time.deltaTime;
            }
        }

        // Right Mouse Button
        if (mouseRight_clickAction)
        {
            // Mouse is having a ClickAction
            // Update position data and check when mouse is released
            mouseRight_clickAction_timePassed += Time.deltaTime;
            mouseRight_clickAction_positionLatest = mouseState.mouse_positionWorld;
            if (Mathf.Abs(mouseRight_clickAction_positionInitial.x - mouseState.mouse_positionWorld.x) >= ClickDragMinStraightWorldSpaceMovement || Mathf.Abs(mouseRight_clickAction_positionInitial.y - mouseState.mouse_positionWorld.y) >= ClickDragMinStraightWorldSpaceMovement)
            {
                mouseRight_clickActionType = ClickAction.ClickType.Drag;
            }
            if (mouseState.mouseRight_released || mouseState.mouseRight_hold == false)
            {
                ExecuteAction_RightMouse(false);
                mouseRight_clickAction = false;
            }
            else if(mouseState.mouseRight_hold && mouseRight_clickActionType == ClickAction.ClickType.Drag)
            {
                ExecuteAction_RightMouse(true);
            }
        }
        else
        {
            // Mouse had no ClickAction
            // Check if mouse has been pressed
            if (mouseState.mouseRight_clicked) // todo: check if over UI, problem: mouseState.mouse_overUI is always true
            {
                mouseRight_clickAction = true;
                mouseRight_clickActionType = ClickAction.ClickType.Click;
                mouseRight_clickAction_positionInitial = mouseState.mouse_positionWorld;
                mouseRight_clickAction_timestamp = Time.time;
                mouseRight_clickAction_timePassed = Time.deltaTime;
            }
        }

        // todo: add middle mouse actions
    }

    private static void ExecuteAction_LeftMouse(bool ongoing)
    {
        // Create ClickAction
        ClickAction clickAction = new ClickAction(ClickAction.ClickButton.LeftMouse, mouseLeft_clickActionType, mouseLeft_clickAction_positionInitial, mouseLeft_clickAction_positionLatest, ongoing, mouseLeft_clickAction_timestamp);
        // Process ClickAction
        if (InputManager.GetCurrentInstance() != null) InputManager.GetCurrentInstance().ProcessInput(clickAction);

        /*if(mouseLeft_clickActionType == ClickAction.ClickType.Click)
        {
            Debug.Log("We have a left mouse " + mouseLeft_clickActionType.ToString() + " on world position " + mouseLeft_clickAction_positionLatest + "!");
        }
        else
        {
            Debug.Log("We have a left mouse " + mouseLeft_clickActionType.ToString() + " from world position " + mouseLeft_clickAction_positionInitial + " to world position " + mouseLeft_clickAction_positionLatest + "!");
        }*/
    }

    private static void ExecuteAction_RightMouse(bool ongoing)
    {
        // Create ClickAction
        ClickAction clickAction = new ClickAction(ClickAction.ClickButton.RightMouse, mouseRight_clickActionType, mouseRight_clickAction_positionInitial, mouseRight_clickAction_positionLatest, ongoing, mouseRight_clickAction_timestamp);
        // Process ClickAction
        if (InputManager.GetCurrentInstance() != null) InputManager.GetCurrentInstance().ProcessInput(clickAction);
    }

    private static void ExecuteAction_MiddleMouse()
    {

    }








    /// <summary>
    /// Each frame one of these is sent to the InputHandler. It processes the data and converts it to a valid
    /// InputState.
    /// </summary>
    public struct MouseState
    {
        // Camera Data
        public Vector2Int cameraScreenSize;

        // Mouse Position =====
        public bool mouse_overUI;
        public Vector2 mouse_positionWorld;
        public Vector2Int GetMouseTilePosition()
        {
            return new Vector2Int(Mathf.RoundToInt(mouse_positionWorld.x), Mathf.RoundToInt(mouse_positionWorld.y));
        }

        // Mouse Clicks =====
        // Left Mouse
        public bool mouseLeft_clicked;
        public bool mouseLeft_hold;
        public bool mouseLeft_released;
        // Right Mouse
        public bool mouseRight_clicked;
        public bool mouseRight_hold;
        public bool mouseRight_released;
        // Middle Mouse
        public bool mouseMiddle_clicked;
        public bool mouseMiddle_hold;
        public bool mouseMiddle_released;
        public float mouseMiddle_value;
    }

}