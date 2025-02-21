// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Contains the visual information for a single bond for a single round.
    /// </summary>
    public struct ParticleBondGraphicState
    {

        // Variables (positions before and after the movement)
        /// <summary>
        /// First endpoint of the bond after the animation.
        /// </summary>
        public Vector2Int curBondPos1;
        /// <summary>
        /// Second endpoint of the bond after the animation.
        /// </summary>
        public Vector2Int curBondPos2;
        /// <summary>
        /// First endpoint of the bond before the animation.
        /// </summary>
        public Vector2Int prevBondPos1;
        /// <summary>
        /// Second endpoint of the bond before the animation.
        /// </summary>
        public Vector2Int prevBondPos2;
        /// <summary>
        /// Flag indicating whether the bond should be hidden.
        /// </summary>
        public bool hidden;

        public ParticleBondGraphicState(Vector2Int curBondPos1, Vector2Int curBondPos2, Vector2Int prevBondPos1, Vector2Int prevBondPos2, bool hidden = false)
        {
            this.curBondPos1 = curBondPos1;
            this.curBondPos2 = curBondPos2;
            this.prevBondPos1 = prevBondPos1;
            this.prevBondPos2 = prevBondPos2;
            this.hidden = hidden;
        }

        /// <summary>
        /// The bond is animated if previous and current positions differ.
        /// </summary>
        /// <returns><c>true</c> if and only if the the previous and
        /// current bond positions are not the same.</returns>
        public bool IsAnimated()
        {
            return prevBondPos1 != curBondPos1 || prevBondPos2 != curBondPos2;
        }
    }

}
