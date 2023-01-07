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
        public Vector2Int curBondPos1;
        public Vector2Int curBondPos2;
        public Vector2Int prevBondPos1;
        public Vector2Int prevBondPos2;

        public ParticleBondGraphicState(Vector2Int curBondPos1, Vector2Int curBondPos2, Vector2Int prevBondPos1, Vector2Int prevBondPos2)
        {
            this.curBondPos1 = curBondPos1;
            this.curBondPos2 = curBondPos2;
            this.prevBondPos1 = prevBondPos1;
            this.prevBondPos2 = prevBondPos2;
        }

        /// <summary>
        /// The bond is animated if previous and current positions differ.
        /// </summary>
        /// <returns></returns>
        public bool IsAnimated()
        {
            return prevBondPos1 != curBondPos1 || prevBondPos2 != curBondPos2;
        }
    }

}