using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ParticleBondGraphicState
{

    /// <summary>
    ///  The IParticleGraphicsAdapter this method is passed with.
    /// </summary>
    public IParticleGraphicsAdapter originParticle;
    public bool originParticle_isConnectedToHead;
    /// <summary>
    /// The connected IParticleGraphicsAdapter.
    /// </summary>
    public IParticleGraphicsAdapter connectedParticle;
    public bool connectedParticle_isConnectedToHead;

    // Movement Data
    /// <summary>
    /// True if the bond has been added in this round, false if the bond has been added in a previous round. Variable is not important if there is no animation or we just jumped into this round.
    /// </summary>
    public bool addedThisRound;

    // (more data about the movement is going here: if not addedThisRound, we need to know how the bond did behave before the last movement, so we can add animations) ..

    public ParticleBondGraphicState(IParticleGraphicsAdapter originParticle, bool originParticle_isConnectedToHead, IParticleGraphicsAdapter connectedParticle, bool connectedParticle_isConnectedToHead, bool addedThisRound)
    {
        this.originParticle = originParticle;
        this.originParticle_isConnectedToHead = originParticle_isConnectedToHead;
        this.connectedParticle = connectedParticle;
        this.connectedParticle_isConnectedToHead = connectedParticle_isConnectedToHead;
        this.addedThisRound = addedThisRound;
    }

}


