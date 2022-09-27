using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ParticleBondGraphicState
{

    // Variables (positions before and after the movement)
    public Vector2Int bondPos1_prev;
    public Vector2Int bondPos1_after;
    public Vector2Int bondPos2_prev;
    public Vector2Int bondPos2_after;
    
    // Movement Data
    /// <summary>
    /// True if the bond has been added in this round, false if the bond has been added in a previous round. Variable is not important if there is no animation or we just jumped into this round.
    /// </summary>
    public bool addedThisRound;

    public ParticleBondGraphicState(Vector2Int bondPos1_prev, Vector2Int bondPos1_after, Vector2Int bondPos2_prev, Vector2Int bondPos2_after, bool addedThisRound)
    {
        this.bondPos1_prev = bondPos1_prev;
        this.bondPos1_after = bondPos1_after;
        this.bondPos2_prev = bondPos2_prev;
        this.bondPos2_after = bondPos2_after;
        this.addedThisRound = addedThisRound;
    }
}


