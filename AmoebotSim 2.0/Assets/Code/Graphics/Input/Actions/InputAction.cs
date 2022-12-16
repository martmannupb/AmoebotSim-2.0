using UnityEngine;
using System.Collections;

namespace AS2.UI
{

    // Copyright ©
    // Part of the personal code library of Tobias Maurer (tobias.maurer.it@web.de).
    // Usage by any current or previous members the University of paderborn and projects associated with the University or programmable matter is permitted.

    /// <summary>
    /// Superclass for all kinds of mouse and keyboard events.
    /// </summary>
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

}