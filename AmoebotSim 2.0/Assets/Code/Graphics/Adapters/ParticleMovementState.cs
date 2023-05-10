using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Contains the position and movement information for
    /// a single particle in a single round.
    /// </summary>
    public struct ParticleMovementState
    {

        // Normal Movmement (the final coordinates)
        /// <summary>
        /// Current head position of the particle.
        /// </summary>
        public Vector2Int posHead;
        /// <summary>
        /// Current tail position of the particle.
        /// For contracted particles just use the same position as the head.
        /// </summary>
        public Vector2Int posTail;
        /// <summary>
        /// Current expansion state of the particle.
        /// </summary>
        public bool isExpanded;
        /// <summary>
        /// The expansion direction of the particle. For contractions
        /// just use the expansion direction of the particle before
        /// the contraction.
        /// </summary>
        public int expansionOrContractionDir;


        /// <summary>
        /// Data for the joint movements.
        /// Set <see cref="ParticleJointMovementState.None"/>
        /// if there is no joint movement.
        /// </summary>
        public ParticleJointMovementState jointMovement;

        public ParticleMovementState(Vector2Int posHead, Vector2Int posTail, bool isExpanded, int expansionOrContractionDir, ParticleJointMovementState jointMovement)
        {
            this.posHead = posHead;
            this.posTail = posTail;
            this.isExpanded = isExpanded;
            this.expansionOrContractionDir = expansionOrContractionDir;
            this.jointMovement = jointMovement;
        }
    }

}
