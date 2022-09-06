using UnityEngine;
using System.Collections;

// Copyright ©
// Part of the personal code library of Tobias Maurer (tobias.maurer.it@web.de).
// Usage by any current or previous members the University of paderborn and projects associated with the University or programmable matter is permitted.

public class InputAction
{

    public InputType inputType;

    public InputAction(InputType inputType)
    {
        this.inputType = inputType;
    }


    public enum InputType
    {
        Mouse, Keyboard
    }

}