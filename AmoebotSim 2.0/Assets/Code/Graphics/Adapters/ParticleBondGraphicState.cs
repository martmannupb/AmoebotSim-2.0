using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ParticleBondGraphicState
{

    // Variables (positions before and after the movement)
    public Vector2Int curBondPos1;
    public Vector2Int curBondPos2;
    public Vector2Int prevBondPos1;
    public Vector2Int prevBondPos2;
    
    // Movement Data
    /// <summary>
    /// True if the bond has been added in this round, false if the bond has been added in a previous round. Variable is not important if there is no animation or we just jumped into this round.
    /// </summary>
    public bool addedThisRound;

    public ParticleBondGraphicState(Vector2Int curBondPos1, Vector2Int curBondPos2, Vector2Int prevBondPos1, Vector2Int prevBondPos2, bool addedThisRound)
    {
        this.curBondPos1 = curBondPos1;
        this.curBondPos2 = curBondPos2;
        this.prevBondPos1 = prevBondPos1;
        this.prevBondPos2 = prevBondPos2;
        this.addedThisRound = addedThisRound;
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


