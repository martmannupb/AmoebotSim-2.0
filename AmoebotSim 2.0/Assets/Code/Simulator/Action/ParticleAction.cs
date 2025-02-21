// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.



namespace AS2.Sim
{

    /// <summary>
    /// All possible movement actions of a particle.
    /// </summary>
    public enum ActionType { EXPAND, CONTRACT_HEAD, CONTRACT_TAIL, PUSH, PULL_HEAD, PULL_TAIL, NULL }

    /// <summary>
    /// Represents an action a particle can schedule when it is activated.
    /// <para>
    /// Some particle actions need to be scheduled because applying them
    /// immediately would violate the FSYNC execution model where all particles
    /// operate on the same snapshot of the system. Thus, these actions are
    /// scheduled and only applied after all particles have been activated.
    /// </para>
    /// </summary>
    public class ParticleAction
    {
        /// <summary>
        /// The particle scheduling the action.
        /// </summary>
        public Particle particle;
        /// <summary>
        /// The type of action. <see cref="ActionType.NULL"/> means no action.
        /// </summary>
        public ActionType type;
        /// <summary>
        /// The local direction into which the action is aimed, if applicable.
        /// </summary>
        public Direction localDir;

        public ParticleAction(Particle particle = null, ActionType type = ActionType.NULL, Direction localDir = Direction.NONE)
        {
            this.particle = particle;
            this.type = type;
            this.localDir = localDir;
        }

        /// <summary>
        /// Checks whether this action is a contraction of any kind.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a regular
        /// contraction or a handover contraction.</returns>
        public bool IsContraction()
        {
            return type == ActionType.CONTRACT_HEAD || type == ActionType.CONTRACT_TAIL || type == ActionType.PULL_HEAD || type == ActionType.PULL_TAIL;
        }

        /// <summary>
        /// Checks whether this action is a regular contraction.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a
        /// regular contraction, not a handover.</returns>
        public bool IsRegularContraction()
        {
            return type == ActionType.CONTRACT_HEAD || type == ActionType.CONTRACT_TAIL;
        }

        /// <summary>
        /// Checks whether this action is a handover contraction.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a handover
        /// contraction and not a regular contraction.</returns>
        public bool IsHandoverContraction()
        {
            return type == ActionType.PULL_HEAD || type == ActionType.PULL_TAIL;
        }

        /// <summary>
        /// Checks whether this action is a Head contraction.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a
        /// contraction or pull handover into the particle's Head.</returns>
        public bool IsHeadContraction()
        {
            return type == ActionType.CONTRACT_HEAD || type == ActionType.PULL_HEAD;
        }

        /// <summary>
        /// Checks whether this action is a Tail contraction.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a
        /// contraction or pull handover into the particle's Tail.</returns>
        public bool IsTailContraction()
        {
            return type == ActionType.CONTRACT_TAIL || type == ActionType.PULL_TAIL;
        }

        /// <summary>
        /// Checks whether this action is an expansion of any kind.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a
        /// regular expansion or a handover expansion.</returns>
        public bool IsExpansion()
        {
            return type == ActionType.EXPAND || type == ActionType.PUSH;
        }

        /// <summary>
        /// Checks whether this action is a regular expansion.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a
        /// regular expansion, not a handover.</returns>
        public bool IsRegularExpansion()
        {
            return type == ActionType.EXPAND;
        }

        /// <summary>
        /// Checks whether this action is a handover expansion.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a
        /// handover expansion, not a regular expansion.</returns>
        public bool IsHandoverExpansion()
        {
            return type == ActionType.PUSH;
        }

        /// <summary>
        /// Checks whether this action is a handover.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents
        /// a handover contraction or expansion and not a regular movement.</returns>
        public bool IsHandover()
        {
            return type == ActionType.PUSH || type == ActionType.PULL_HEAD || type == ActionType.PULL_TAIL;
        }

        /// <summary>
        /// Checks whether this action is a regular movement.
        /// </summary>
        /// <returns><c>true</c> if and only if the action represents a regular
        /// contraction or expansion and not a handover movement.</returns>
        public bool IsRegularMovement()
        {
            return type == ActionType.EXPAND || type == ActionType.CONTRACT_HEAD || type == ActionType.CONTRACT_TAIL;
        }
    }

} // namespace AS2.Sim
