using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// A polar coordinate that can be used to define positins relative to the center of a circle/quad.
    /// </summary>
    public struct Polar2DCoordinate
    {
        private bool isValid;
        /// <summary>
        /// The angle of the coordinate. For most cases 0 is straight upwards.
        /// </summary>
        public float angleDegrees;
        /// <summary>
        /// The radius of the coordinate. Max: 1.0f (outter border), min 0.0f.
        /// </summary>
        public float radiusPercentage;

        public Polar2DCoordinate(float angleDegrees, float radiusPercentage, bool isValid = true)
        {
            this.isValid = isValid;
            this.angleDegrees = angleDegrees;
            this.radiusPercentage = radiusPercentage;
        }

        /// <summary>
        /// Resets the values to invalid defaults.
        /// </summary>
        public void Reset()
        {
            angleDegrees = 0f;
            radiusPercentage = float.MinValue;
        }

        /// <summary>
        /// Invalidates the coordinate.
        /// </summary>
        public void Discard()
        {
            isValid = false;
        }
    }
}