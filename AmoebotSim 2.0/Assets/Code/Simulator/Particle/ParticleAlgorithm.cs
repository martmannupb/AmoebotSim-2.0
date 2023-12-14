using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// A simple container for neighbor search results.
    /// <para>
    /// Contains a reference to a neighbor particle, the local direction
    /// in which it was found, and a flag indicating whether the
    /// direction is relative to the querying particle's head or tail.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the neighbor particle.</typeparam>
    public struct Neighbor<T> where T : class
    {
        public T neighbor;
        public Direction localDir;
        public bool atHead;

        public Neighbor(T neighbor, Direction localDir, bool atHead)
        {
            this.neighbor = neighbor;
            this.localDir = localDir;
            this.atHead = atHead;
        }

        public static Neighbor<T> Null = new Neighbor<T>(null, Direction.NONE, false);

        public static bool operator ==(Neighbor<T> nbr1, Neighbor<T> nbr2)
        {
            return nbr1.neighbor == nbr2.neighbor && nbr1.localDir == nbr2.localDir && nbr1.atHead == nbr2.atHead;
        }

        public static bool operator !=(Neighbor<T> nbr1, Neighbor<T> nbr2)
        {
            return !(nbr1 == nbr2);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj.GetType() == GetType() && this == (Neighbor<T>)obj;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(neighbor, localDir, atHead);
        }
    }

    /// <summary>
    /// The abstract base class for particle algorithms in the Amoebot model.
    /// <para>
    /// Every algorithm that should run in the simulation must be implemented
    /// as a subclass of the <see cref="ParticleAlgorithm"/> class through its
    /// <see cref="ActivateMove"/> and <see cref="ActivateBeep"/> methods.
    /// </para>
    /// <para>
    /// The subclass constructor must have the following signature and call the
    /// base class constructor:
    /// <code>
    /// public MyClass(Particle p) : base(p) { ... }
    /// </code>
    /// </para>
    /// <para>
    /// The number of pins used by the algorithm must be specified by overriding
    /// the <see cref="PinsPerEdge"/> property.
    /// </para>
    /// <para>
    /// Particle attributes that represent a part of the particle's state must be implemented
    /// using the <see cref="ParticleAttribute{T}"/> class.
    /// <para/>
    /// <example>
    /// Example for attribute initialization in subclass:
    /// <code>
    /// public class MyParticle : ParticleAlgorithm {
    ///     ParticleAttribute<![CDATA[<int>]]> myIntAttr;
    ///     public MyParticle(Particle p) : base(p) {
    ///         myIntAttr = CreateAttributeInt("Fancy display name for myIntAttr", 42);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// Note that the display names of particle attributes must be unique because
    /// they are used to identify attributes when loading a saved simulation state.
    /// </para>
    /// </summary>
    public abstract class ParticleAlgorithm
    {
        /// <summary>
        /// Reference to the particle's system representation.
        /// </summary>
        private Particle particle;

        public ParticleAlgorithm(Particle particle)
        {
            this.particle = particle;
            particle.algorithm = this;
        }

        // TODO: Optimization if this number is 0?
        /// <summary>
        /// The number of pins on each edge.
        /// <para>
        /// This number must be the same constant for all
        /// particles.
        /// </para>
        /// </summary>
        public abstract int PinsPerEdge { get; }

        /// <summary>
        /// The display name of the algorithm. Must be unique among
        /// all algorithms.
        /// </summary>
        public static string Name { get { return ""; } }

        /// <summary>
        /// The full type name of the algorithm's generation method.
        /// This method must be a subclass of <see cref="AlgorithmGenerator"/>.
        /// </summary>
        public static string GenerationMethod { get { return typeof(InitRandomWithHoles).FullName; } }

        /// <summary>
        /// Finds the official name of this algorithm. This is the name used by
        /// the algorithm manager to identify the algorithms. It is defined by
        /// the static <c>Name</c> property.
        /// </summary>
        /// <returns>The official name of this algorithm, if it is defined,
        /// otherwise the full name of the class.</returns>
        public string GetAlgorithmName()
        {
            System.Reflection.PropertyInfo nameProp = this.GetType().GetProperty("Name");
            if (nameProp == null)
                return this.GetType().FullName;
            else
                return (string)nameProp.GetValue(null);
        }

        /// <summary>
        /// This is one part of the main activation logic of the particle.
        /// It is called exactly once in each round and should contain the
        /// algorithm code that implements the look-compute-move cycle.
        /// After the movements are executed, <see cref="ActivateBeep"/>
        /// is called within the same round.
        /// <para>
        /// Inside of this method, particles are allowed to release bonds,
        /// define which bonds should be marked, and schedule movements.
        /// Only the last movement operation scheduled in this method will
        /// be applied.
        /// </para>
        /// </summary>
        public abstract void ActivateMove();

        /// <summary>
        /// This is the second part of the main activation logic of the
        /// particle. It is called exactly once in each round, after the
        /// movements scheduled in <see cref="ActivateMove"/> have been
        /// executed, and should contain the algorithm code that
        /// implements the look-compute-beep cycle.
        /// <para>
        /// Inside of this method, particles are allowed to change their
        /// pin configuration and send beeps and messages on the updated
        /// configuration.
        /// </para>
        /// <para>
        /// Note that beeps and messages sent in the current round will
        /// be readable in both the <see cref="ActivateMove"/> and
        /// <see cref="ActivateBeep"/> calls in the next round.
        /// </para>
        /// </summary>
        public abstract void ActivateBeep();

        /// <summary>
        /// Checks whether this particle has finished its algorithm.
        /// <para>
        /// Override this method to return <c>true</c> when a particle
        /// is done executing the algorithm. Once all particles in the
        /// system are finished, the simulation will stop automatically.
        /// When a particle's state results in this method returning
        /// <c>true</c>, its activation methods should not change its
        /// state any more.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if and only if this particle has
        /// finished its algorithm.</returns>
        public virtual bool IsFinished()
        {
            return false;
        }

        /// <summary>
        /// Checks if the particle is currently active and throws
        /// an exception if it is not.
        /// <para>
        /// Used to guard operations from being called on particles
        /// that are not currently active.
        /// </para>
        /// </summary>
        /// <param name="errorMessage">The error message to be
        /// used for the exception.</param>
        /// <exception cref="InvalidActionException">
        /// Thrown if this particle is currently not active.
        /// </exception>
        private void CheckActive(string errorMessage)
        {
            if (!particle.isActive)
            {
                throw new InvalidActionException(particle, errorMessage);
            }
        }

        /// <summary>
        /// Checks if we are currently in the move phase and logs
        /// a warning message if we are not.
        /// </summary>
        /// <param name="warningMessage">The warning message to be logged
        /// if we are not in the move phase. No warning will be logged if
        /// this is <c>null</c>.</param>
        /// <param name="exception">If <c>true</c>, throw an exception
        /// with the given message.</param>
        /// <returns><c>true</c> if and only if the system is currently
        /// simulating the move phase.</returns>
        private bool CheckMove(string warningMessage, bool exception = false)
        {
            if (!particle.system.InMovePhase)
            {
                if (exception)
                    throw new InvalidActionException(particle, warningMessage);
                else if (warningMessage != null)
                    Debug.LogWarning(warningMessage);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if we are currently in the beep phase and logs
        /// a warning message if we are not.
        /// </summary>
        /// <param name="warningMessage">The warning message to be logged
        /// if we are not in the beep phase. No warning will be logged if
        /// this is <c>null</c>.</param>
        /// <param name="exception">If <c>true</c>, throw an exception
        /// with the given message.</param>
        /// <returns><c>true</c> if and only if the system is currently
        /// simulating the beep phase.</returns>
        private bool CheckBeep(string warningMessage, bool exception = false)
        {
            if (!particle.system.InBeepPhase)
            {
                if (exception)
                    throw new InvalidActionException(particle, warningMessage);
                else if (warningMessage != null)
                    Debug.LogWarning(warningMessage);
                return false;
            }
            return true;
        }

        /*
         * Attribute creation methods.
         * 
         * To be called in the particle constructor.
         * (There is currently no way to check if they are called
         * anywhere else, but this is strongly discouraged)
         */

        #region AttributeCreation

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute{T}"/> representing an integer value.
        /// </summary>
        /// <param name="name">The name of the attribute to be displayed in the UI.
        /// Must be unique for saving and loading of simulation states to work correctly.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
        public ParticleAttribute<int> CreateAttributeInt(string name, int initialValue = 0)
        {
            CheckActive("Particles can only create attributes for themselves, not for other particles.");
            return ParticleAttributeFactory.CreateParticleAttributeInt(particle, name, initialValue);
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute{T}"/> representing a float value.
        /// </summary>
        /// <param name="name">The name of the attribute to be displayed in the UI.
        /// Must be unique for saving and loading of simulation states to work correctly.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
        public ParticleAttribute<float> CreateAttributeFloat(string name, float initialValue = 0f)
        {
            CheckActive("Particles can only create attributes for themselves, not for other particles.");
            return ParticleAttributeFactory.CreateParticleAttributeFloat(particle, name, initialValue);
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute{T}"/> representing a string value.
        /// </summary>
        /// <param name="name">The name of the attribute to be displayed in the UI.
        /// Must be unique for saving and loading of simulation states to work correctly.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
        public ParticleAttribute<string> CreateAttributeString(string name, string initialValue = "")
        {
            CheckActive("Particles can only create attributes for themselves, not for other particles.");
            return ParticleAttributeFactory.CreateParticleAttributeString(particle, name, initialValue);
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute{T}"/> representing a boolean value.
        /// </summary>
        /// <param name="name">The name of the attribute to be displayed in the UI.
        /// Must be unique for saving and loading of simulation states to work correctly.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
        public ParticleAttribute<bool> CreateAttributeBool(string name, bool initialValue = false)
        {
            CheckActive("Particles can only create attributes for themselves, not for other particles.");
            return ParticleAttributeFactory.CreateParticleAttributeBool(particle, name, initialValue);
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute{T}"/> representing a direction.
        /// <para>
        /// The <see cref="Direction"/> enum specifies which values can be stored in the attribute.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the attribute to be displayed in the UI.
        /// Must be unique for saving and loading of simulation states to work correctly.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
        public ParticleAttribute<Direction> CreateAttributeDirection(string name, Direction initialValue = Direction.NONE)
        {
            CheckActive("Particles can only create attributes for themselves, not for other particles.");
            return ParticleAttributeFactory.CreateParticleAttributeDirection(particle, name, initialValue);
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute{T}"/> representing an enum value.
        /// </summary>
        /// <typeparam name="EnumT">The enum specifying the possible values of this attribute.</typeparam>
        /// <param name="name">The name of the attribute to be displayed in the UI.
        /// Must be unique for saving and loading of simulation states to work correctly.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
        public ParticleAttribute<EnumT> CreateAttributeEnum<EnumT>(string name, EnumT initialValue) where EnumT : System.Enum
        {
            CheckActive("Particles can only create attributes for themselves, not for other particles.");
            return ParticleAttributeFactory.CreateParticleAttributeEnum<EnumT>(particle, name, initialValue);
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute{T}"/> representing a pin configuration.
        /// <para>
        /// Note that <see cref="PinConfiguration"/> is a reference type but particle attributes
        /// have value semantics. This means that changes to the value of an attribute of this
        /// type must be "committed" by calling the <see cref="ParticleAttribute{T}.SetValue(T)"/>
        /// method. It is not sufficient to call methods on the pin configuration instance.
        /// </para>
        /// <para>
        /// Additionally, <see cref="PinConfiguration"/>s stored in attributes do not retain any
        /// of their status flags, i.e., saving the current pin configuration in an attribute and
        /// then reading it will return a pin configuration that is identical to the current one
        /// but that is not marked as the current configuration and can thus not be used to read
        /// received beeps and messages. The same holds for storing the planned pin configuration.
        /// </para>
        /// <para>
        /// Note that reading stored pin configurations of other particles is not supported.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the attribute to be displayed in the UI.
        /// Must be unique for saving and loading of simulation states to work correctly.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
        public ParticleAttribute<PinConfiguration> CreateAttributePinConfiguration(string name, PinConfiguration initialValue)
        {
            CheckActive("Particles can only create attributes for themselves, not for other particles.");
            return ParticleAttributeFactory.CreateParticleAttributePinConfiguration(particle, name, initialValue);
        }

        #endregion // AttributeCreation


        /* ====================================================================================================
         * Particle actions defining the API.
         * These methods should be called from the ActivateX() methods.
         * ====================================================================================================
         */

        /*
         * State information retrieval
         * The default methods return the state information recorded in the
         * snapshot at the beginning of the current round. There are specialized
         * methods that return the predicted state at the end of the round.
         */

        /// <summary>
        /// Checks if the particle is currently expanded, i.e., occupies 2 neighboring
        /// grid nodes simultaneously.
        /// <para>See <see cref="IsContracted"/>.</para>
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation. Use <see cref="IsExpanded_After"/> to get the predicted
        /// value for after the round.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if and only if the particle is expanded.</returns>
        public bool IsExpanded()
        {
            return particle.IsExpanded();
        }

        /// <summary>
        /// Checks if the particle is currently contracted, i.e., occupies exactly one
        /// node of the grid.
        /// <para>See <see cref="IsExpanded"/>.</para>
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation. Use <see cref="IsContracted_After"/> to get the predicted
        /// value for after the round.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if and only if the particle is contracted.</returns>
        public bool IsContracted()
        {
            return particle.IsContracted();
        }

        /// <summary>
        /// Returns the local direction pointing from the particle's tail towards its head.
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation. Use <see cref="HeadDirection_After"/> to get the predicted
        /// value for after the round.
        /// </para>
        /// </summary>
        /// <returns>The local direction pointing from the particle's tail towards its head,
        /// if it is expanded, otherwise <c>-1</c>.</returns>
        public Direction HeadDirection()
        {
            return particle.HeadDirection();
        }

        /// <summary>
        /// Returns the local direction pointing from the particle's head towards its tail.
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation. Use <see cref="TailDirection_After"/> to get the predicted
        /// value for after the round.
        /// </para>
        /// </summary>
        /// <returns>The local direction pointing from the particle's head towards its tail,
        /// if it is expanded, otherwise <c>-1</c>.</returns>
        public Direction TailDirection()
        {
            return particle.TailDirection();
        }


        /*
         * Predicted state information retrieval
         * These methods return the predicted values for after the
         * current round based on the particle's actions during this
         * call of <see cref="ActivateMove"/>.
         */

        /// <summary>
        /// Like <see cref="IsExpanded"/>, but returns the predicted
        /// value for after the round
        /// </summary>
        /// <returns><c>true</c> if and only if the particle will be
        /// expanded at the end of the round if its planned movements succeed.</returns>
        public bool IsExpanded_After()
        {
            CheckActive("Predicted state information is not available for other particles.");
            return particle.IsExpanded_After();
        }

        /// <summary>
        /// Like <see cref="IsContracted"/>, but returns the predicted
        /// value for after the round
        /// </summary>
        /// <returns><c>true</c> if and only if the particle will be
        /// contracted at the end of the round if its planned movements succeed.</returns>
        public bool IsContracted_After()
        {
            CheckActive("Predicted state information is not available for other particles.");
            return particle.IsContracted_After();
        }

        /// <summary>
        /// Like <see cref="HeadDirection"/>, but returns the predicted value
        /// for after the round.
        /// </summary>
        /// <returns>The local direction pointing from the particle's tail towards its head,
        /// if it is expanded, otherwise <c>-1</c>.</returns>
        public Direction HeadDirection_After()
        {
            CheckActive("Predicted state information is not available for other particles.");
            return particle.HeadDirection_After();
        }

        /// <summary>
        /// Like <see cref="TailDirection"/>, but returns the predicted value
        /// for after the round.
        /// </summary>
        /// <returns>The local direction pointing from the particle's head towards its tail,
        /// if it is expanded, otherwise <c>-1</c>.</returns>
        public Direction TailDirection_After()
        {
            CheckActive("Predicted state information is not available for other particles.");
            return particle.TailDirection_After();
        }


        /*
         * Pin configuration management
         * 
         * API is defined by <see cref="PinConfiguration"/>,
         * <see cref="PartitionSet"/> and <see cref="Pin"/>
         * interfaces.
         */

        /// <summary>
        /// Returns a copy of the pin configuration at the
        /// beginning of the round.
        /// <para>
        /// This object can be used as a basis for defining the
        /// new pin configuration after the round if the particle
        /// does not perform a movement. It also provides the
        /// interface for reading beeps and messages that were
        /// received.
        /// </para>
        /// <para>
        /// See also <seealso cref="GetContractedPinConfiguration"/>,
        /// <seealso cref="GetExpandedPinConfiguration(Direction)"/>,
        /// <seealso cref="SetPlannedPinConfiguration(PinConfiguration)"/>,
        /// <seealso cref="GetPlannedPinConfiguration"/>.
        /// </para>
        /// </summary>
        /// <returns>A copy of the pin configuration at the
        /// beginning of the current round.</returns>
        public PinConfiguration GetCurrentPinConfiguration()
        {
            CheckActive("Pin configurations cannot be obtained from other particles.");
            return particle.GetCurrentPinConfiguration();
        }

        /// <summary>
        /// Same as <see cref="GetPlannedPinConfiguration"/> but also
        /// sets the current pin configuration to be the planned one
        /// such that it can immediately be used to send beeps and messages.
        /// </summary>
        /// <returns>The current pin configuration, already planned
        /// for the next round.</returns>
        public PinConfiguration GetCurrentPCAsPlanned()
        {
            CheckActive("Pin configurations cannot be obtained from other particles.");
            SysPinConfiguration pc = particle.GetCurrentPinConfiguration();
            SetPlannedPinConfiguration(pc);
            return pc;
        }

        /// <summary>
        /// Creates a pin configuration for the contracted state
        /// with the default singleton pattern.
        /// <para>
        /// The returned object can be used to define the next
        /// pin configuration if the particle plans to perform
        /// a contraction movement in this round.
        /// </para>
        /// <para>
        /// See also <seealso cref="GetCurrentPinConfiguration"/>,
        /// <seealso cref="GetExpandedPinConfiguration(Direction)"/>,
        /// <seealso cref="SetPlannedPinConfiguration(PinConfiguration)"/>,
        /// <seealso cref="GetPlannedPinConfiguration"/>.
        /// </para>
        /// </summary>
        /// <returns>A new singleton pin configuration for the
        /// contracted state that can be modified arbitrarily.</returns>
        public PinConfiguration GetContractedPinConfiguration()
        {
            CheckActive("Pin configurations cannot be obtained from other particles.");
            return new SysPinConfiguration(particle, PinsPerEdge);
        }

        /// <summary>
        /// Creates a pin configuration for an expanded state
        /// with the default singleton pattern.
        /// <para>
        /// The returned object can be used to define the next
        /// pin configuration if the particle plans to perform
        /// an expansion movement such that its head direction
        /// after the movement is <paramref name="headDirection"/>.
        /// </para>
        /// <para>
        /// See also <seealso cref="GetCurrentPinConfiguration"/>,
        /// <seealso cref="GetContractedPinConfiguration"/>,
        /// <seealso cref="SetPlannedPinConfiguration(PinConfiguration)"/>,
        /// <seealso cref="GetPlannedPinConfiguration"/>.
        /// </para>
        /// </summary>
        /// <param name="headDirection">The head direction defining the
        /// expansion state for which the pin configuration should be created.</param>
        /// <returns>A new singleton pin configuration for the specified
        /// expansion state that can be modified arbitrarily.</returns>
        public PinConfiguration GetExpandedPinConfiguration(Direction headDirection)
        {
            CheckActive("Pin configurations cannot be obtained from other particles.");
            return new SysPinConfiguration(particle, PinsPerEdge, headDirection);
        }

        /// <summary>
        /// Sets the pin configuration that should be applied to the particle
        /// at the end of this round. Only works in <see cref="ActivateBeep"/>.
        /// <para>
        /// The expansion state of the pin configuration must match the
        /// expansion state of the particle, i.e., if the particle is contracted,
        /// the pin configuration must be made for the contracted state, and if
        /// the particle is expanded, the pin configuration must match the
        /// corresponding head direction.
        /// </para>
        /// <para>
        /// If no pin configuration is planned in the same round in which a
        /// movement is performed, the pin configuration defaults to the
        /// singleton pattern. If no movement and no new pin configuration
        /// are planned, the configuration is left unchanged.
        /// </para>
        /// </summary>
        /// <param name="pinConfiguration">The pin configuration to be applied
        /// at the end of the current round.</param>
        public void SetPlannedPinConfiguration(PinConfiguration pinConfiguration)
        {
            CheckActive("Cannot set pin configuration of other particles.");
            if (!CheckBeep("Cannot set pin configuration during move phase.", true))
            {
                return;
            }
            particle.SetPlannedPinConfiguration((SysPinConfiguration)pinConfiguration);
        }

        /// <summary>
        /// Returns the pin configuration set using
        /// <see cref="SetPlannedPinConfiguration(PinConfiguration)"/> in
        /// this round. Always returns <c>null</c> when called in
        /// <see cref="ActivateMove"/>.
        /// </summary>
        /// <returns>The pin configuration planned to be applied at the end of
        /// the current round. <c>null</c> if no configuration was planned yet.</returns>
        public PinConfiguration GetPlannedPinConfiguration()
        {
            CheckActive("Predicted state information is not available for other particles.");
            return particle.GetPlannedPinConfiguration();
        }


        /*
         * System information retrieval
         * Mainly for finding neighbor particles
         */

        /// <summary>
        /// Checks if this particle has a neighboring particle in the given local direction.
        /// For expanded particles, there are two different nodes in the same local direction,
        /// one seen from the particle's head and one seen from its tail.
        /// <para>See also <see cref="GetNeighborAt(Direction, bool)"/>.</para>
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction in which to search for a neighbor particle.</param>
        /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
        /// the particle's tail (only relevant if this particle is expanded).</param>
        /// <returns><c>true</c> if and only if there is a different particle in the specified position.</returns>
        public bool HasNeighborAt(Direction locDir, bool fromHead = true)
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.HasNeighborAt(particle, locDir, fromHead);
        }

        /// <summary>
        /// Gets this particle's neighbor in the given local direction. The position to
        /// check is determined in the same way as in <see cref="HasNeighborAt(Direction, bool)"/>.
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction from which to get the neighbor particle.</param>
        /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
        /// the particle's tail (only relevant if this particle is expanded).</param>
        /// <returns>The neighboring particle in the specified position.</returns>
        public ParticleAlgorithm GetNeighborAt(Direction locDir, bool fromHead = true)
        {
            CheckActive("Neighbor information is not available for other particles.");
            Particle p = particle.system.GetNeighborAt(particle, locDir, fromHead);
            if (p == null)
            {
                return null;
            }
            else
            {
                return p.algorithm;
            }
        }

        /// <summary>
        /// Checks if the part of the neighboring particle in the given local direction is
        /// the neighbor's head. The position to check is determined in the same way as in
        /// <see cref="HasNeighborAt(Direction, bool)"/>.
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction from which to get the neighbor particle.</param>
        /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise
        /// look from the particle's tail (only relevant if this particle is expanded.)</param>
        /// <returns><c>true</c> if and only if the grid node in the specified position is
        /// occupied by the head of a neighboring particle (for contracted particles, head and
        /// tail occupy the same node.)</returns>
        public bool IsHeadAt(Direction locDir, bool fromHead = true)
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.IsHeadAt(particle, locDir, fromHead);
        }

        /// <summary>
        /// Checks if the part of the neighboring particle in the given local direction is
        /// the neighbor's tail. The position to check is determined in the same way as in
        /// <see cref="HasNeighborAt(Direction, bool)"/>.
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction from which to get the neighbor particle.</param>
        /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise
        /// look from the particle's tail (only relevant if this particle is expanded.)</param>
        /// <returns><c>true</c> if and only if the grid node in the specified position is
        /// occupied by the tail of a neighboring particle (for contracted particles, head and
        /// tail occupy the same node.)</returns>
        public bool IsTailAt(Direction locDir, bool fromHead = true)
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.IsTailAt(particle, locDir, fromHead);
        }

        /// <summary>
        /// Searches for the first neighboring particle in the specified range.
        /// </summary>
        /// <typeparam name="T">The algorithm type to search for. Should be the
        /// same type as the algorithm that calls this method.</typeparam>
        /// <param name="neighbor">The first neighbor particle that is encountered,
        /// or <see cref="Neighbor{T}.Null"/> if no neighbor is found.</param>
        /// <param name="startDir">The local direction of the first port to search.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxNumber">The maximum number of ports to check.
        /// The maximum value is <c>6</c> for contracted particles and <c>10</c>
        /// for expanded particles. Negative values automatically select the
        /// maximum number.</param>
        /// <returns><c>true</c> if and only if a neighbor was found.</returns>
        public bool FindFirstNeighbor<T>(out Neighbor<T> neighbor, Direction startDir = Direction.E, bool startAtHead = true, bool withChirality = true, int maxNumber = -1) where T : ParticleAlgorithm
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindFirstNeighbor<T>(particle, out neighbor, startDir, startAtHead, withChirality, maxNumber);
        }

        /// <summary>
        /// Searches for the first neighboring object in the specified range.
        /// </summary>
        /// <param name="neighbor">The first neighbor object that is encountered,
        /// or <see cref="Neighbor{T}.Null"/> if no neighbor is found.</param>
        /// <param name="startDir">The local direction of the first port to search.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxNumber">The maximum number of ports to check.
        /// The maximum value is <c>6</c> for contracted particles and <c>10</c>
        /// for expanded particles. Negative values automatically select the
        /// maximum number.</param>
        /// <returns><c>true</c> if and only if a neighbor object was found.</returns>
        public bool FindFirstObjectNeighbor(out Neighbor<IParticleObject> neighbor, Direction startDir = Direction.E, bool startAtHead = true, bool withChirality = true, int maxNumber = -1)
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindFirstObjectNeighbor(particle, out neighbor, startDir, startAtHead, withChirality, maxNumber);
        }

        /// <summary>
        /// Searches for the first neighboring particle in the specified range
        /// that satisfies the given property.
        /// </summary>
        /// <typeparam name="T">The algorithm type to search for. Should be the
        /// same type as the algorithm that calls this method.</typeparam>
        /// <param name="prop">The property the neighbor has to satisfy.</param>
        /// <param name="neighbor">The first neighbor particle that is encountered,
        /// or <see cref="Neighbor{T}.Null"/> if no neighbor is found.</param>
        /// <param name="startDir">The local direction of the first port to search
        /// at.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxNumber">The maximum number of ports to check.
        /// The maximum value is <c>6</c> for contracted particles and <c>10</c>
        /// for expanded particles. Negative values automatically select the
        /// maximum number.</param>
        /// <returns><c>true</c> if and only if a neighbor was found.</returns>
        public bool FindFirstNeighborWithProperty<T>(System.Func<T, bool> prop, out Neighbor<T> neighbor, Direction startDir = Direction.E, bool startAtHead = true, bool withChirality = true, int maxNumber = -1) where T : ParticleAlgorithm
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindFirstNeighborWithProperty<T>(particle, prop, out neighbor, startDir, startAtHead, withChirality, maxNumber);
        }

        /// <summary>
        /// Searches for the first neighboring object in the specified range
        /// that satisfies the given property.
        /// </summary>
        /// <param name="prop">The property the neighbor object has to satisfy.</param>
        /// <param name="neighbor">The first neighbor object that is encountered,
        /// or <see cref="Neighbor{T}.Null"/> if no neighbor is found.</param>
        /// <param name="startDir">The local direction of the first port to search
        /// at.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxNumber">The maximum number of ports to check.
        /// The maximum value is <c>6</c> for contracted particles and <c>10</c>
        /// for expanded particles. Negative values automatically select the
        /// maximum number.</param>
        /// <returns><c>true</c> if and only if a neighbor object was found.</returns>
        public bool FindFirstNeighborObjectWithProperty(System.Func<IParticleObject, bool> prop, out Neighbor<IParticleObject> neighbor, Direction startDir = Direction.E, bool startAtHead = true, bool withChirality = true, int maxNumber = -1)
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindFirstNeighborObjectWithProperty(particle, prop, out neighbor, startDir, startAtHead, withChirality, maxNumber);
        }

        /// <summary>
        /// Searches for neighboring particles in the specified range.
        /// </summary>
        /// <typeparam name="T">The algorithm type to search for. Should be the
        /// same type as the algorithm that calls this method.</typeparam>
        /// <param name="startDir">The local direction of the first port to search.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxSearch">The maximum number of ports to search. Will always
        /// be limited to the total number of ports. Negative values mean that all
        /// ports will be searched.</param>
        /// <param name="maxReturn">The maximum number of neighbors to return. The
        /// same restrictions as for <paramref name="maxSearch"/> apply.</param>
        /// <returns>A list containing all discovered neighbors.</returns>
        public List<Neighbor<T>> FindNeighbors<T>(Direction startDir = Direction.E, bool startAtHead = true,
            bool withChirality = true, int maxSearch = -1, int maxReturn = -1) where T : ParticleAlgorithm
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindNeighbors<T>(particle, startDir, startAtHead, withChirality, maxSearch, maxReturn);
        }

        /// <summary>
        /// Searches for neighboring objects in the specified range.
        /// </summary>
        /// <param name="startDir">The local direction of the first port to search.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxSearch">The maximum number of ports to search. Will always
        /// be limited to the total number of ports. Negative values mean that all
        /// ports will be searched.</param>
        /// <param name="maxReturn">The maximum number of neighbors to return. The
        /// same restrictions as for <paramref name="maxSearch"/> apply.</param>
        /// <returns>A list containing all discovered neighbor objects.</returns>
        public List<Neighbor<IParticleObject>> FindNeighborObjects(Direction startDir = Direction.E, bool startAtHead = true,
            bool withChirality = true, int maxSearch = -1, int maxReturn = -1)
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindNeighborObjects(particle, startDir, startAtHead, withChirality, maxSearch, maxReturn);
        }

        /// <summary>
        /// Searches for neighboring particles in the specified range that satisfy
        /// the given property.
        /// </summary>
        /// <typeparam name="T">The algorithm type to search for. Should be the
        /// same type as the algorithm that calls this method.</typeparam>
        /// <param name="prop">The property the neighbors have to satisfy.</param>
        /// <param name="startDir">The local direction of the first port to search.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxSearch">The maximum number of ports to search. Will always
        /// be limited to the total number of ports. Negative values mean that all
        /// ports will be searched.</param>
        /// <param name="maxReturn">The maximum number of neighbors to return. The
        /// same restrictions as for <paramref name="maxSearch"/> apply.</param>
        /// <returns>A list containing all discovered neighbors.</returns>
        public List<Neighbor<T>> FindNeighborsWithProperty<T>(System.Func<T, bool> prop, Direction startDir = Direction.E, bool startAtHead = true,
            bool withChirality = true, int maxSearch = -1, int maxReturn = -1) where T : ParticleAlgorithm
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindNeighborsWithProperty<T>(particle, prop, startDir, startAtHead, withChirality, maxSearch, maxReturn);
        }

        /// <summary>
        /// Searches for neighboring objects in the specified range that satisfy
        /// the given property.
        /// </summary>
        /// <param name="prop">The property the neighbor objects have to satisfy.</param>
        /// <param name="startDir">The local direction of the first port to search.</param>
        /// <param name="startAtHead">Indicates whether <paramref name="startDir"/>
        /// is relative to the particle's head. Has no effect for contracted
        /// particles.</param>
        /// <param name="withChirality">If <c>true</c>, the search progresses in
        /// the particle's local counter-clockwise direction, otherwise in the
        /// local clockwise direction.</param>
        /// <param name="maxSearch">The maximum number of ports to search. Will always
        /// be limited to the total number of ports. Negative values mean that all
        /// ports will be searched.</param>
        /// <param name="maxReturn">The maximum number of neighbors to return. The
        /// same restrictions as for <paramref name="maxSearch"/> apply.</param>
        /// <returns>A list containing all discovered neighbor objects.</returns>
        public List<Neighbor<IParticleObject>> FindNeighborObjectsWithProperty(System.Func<IParticleObject, bool> prop, Direction startDir = Direction.E, bool startAtHead = true,
            bool withChirality = true, int maxSearch = -1, int maxReturn = -1)
        {
            CheckActive("Neighbor information is not available for other particles.");
            return particle.system.FindNeighborObjectsWithProperty(particle, prop, startDir, startAtHead, withChirality, maxSearch, maxReturn);
        }

        /// <summary>
        /// Checks if this particle has a neighboring object in the given local direction.
        /// For expanded particles, there are two different nodes in the same local direction,
        /// one seen from the particle's head and one seen from its tail.
        /// <para>See also <see cref="HasNeighborAt(Direction, bool)"/>.</para>
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction in which to search for a neighboring object.</param>
        /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
        /// the particle's tail (only relevant if this particle is expanded).</param>
        /// <returns><c>true</c> if and only if there is a grid node occupied by an object in the
        /// specified position.</returns>
        public bool HasObjectAt(Direction locDir, bool fromHead = true)
        {
            CheckActive("Neighbor object information is not available for other particles.");
            return particle.system.HasObjectAt(particle, locDir, fromHead);
        }

        /// <summary>
        /// Gets this particle's neighbor object in the given local direction. The position to
        /// check is determined in the same way as in <see cref="HasObjectAt(Direction, bool)"/>.
        /// <para>
        /// Note: This method returns information from the snapshot taken at the
        /// beginning of the current round. Its return value will not change during
        /// this activation.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction from which to get the neighbor object.</param>
        /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
        /// the particle's tail (only relevant if this particle is expanded).</param>
        /// <returns>The neighboring object in the specified position.</returns>
        public IParticleObject GetObjectAt(Direction locDir, bool fromHead = true)
        {
            CheckActive("Neighbor object information is not available for other particles.");
            return particle.system.GetObjectAt(particle, locDir, fromHead);
        }

        /*
         * Bond management
         */

        /// <summary>
        /// Marks the specified bond to be released before the
        /// scheduled movement. Only works in <see cref="ActivateMove"/>.
        /// </summary>
        /// <param name="locDir">The local direction into which the bond
        /// is pointing.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// indicates whether the bond is located at the head or the tail.</param>
        public void ReleaseBond(Direction locDir, bool head = true)
        {
            CheckActive("Particles cannot change bonds of neighbors.");
            if (!CheckMove("Cannot release bonds during beep phase.", true))
            {
                return;
            }
            int label = ParticleSystem_Utils.GetLabelInDir(locDir, particle.HeadDirection(), head);
            particle.ReleaseBond(label);
        }

        /// <summary>
        /// Checks if the specified bond is still active or marked to
        /// be released. Always returns <c>true</c> when called in
        /// <see cref="ActivateBeep"/>.
        /// </summary>
        /// <param name="locDir">The local direction into which the bond
        /// is pointing.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// indicates whether the bond is located at the head or the tail.</param>
        /// <returns><c>true</c> if and only if the specified bond is
        /// still marked as active.</returns>
        public bool BondActive(Direction locDir, bool head = true)
        {
            CheckActive("Particles cannot check bonds of neighbors.");
            if (!CheckMove(null))
                return true;
            int label = ParticleSystem_Utils.GetLabelInDir(locDir, particle.HeadDirection(), head);
            return particle.BondActive(label);
        }

        /// <summary>
        /// Marks the specified bond to have special behavior.
        /// This means that it will be pulled with an expansion
        /// movement if it does not point in the opposite direction
        /// as the movement. The bond pointing in the same direction
        /// as the movement will always be marked automatically.
        /// Only works in <see cref="ActivateMove"/>.
        /// </summary>
        /// <param name="locDir">The local direction into which the bond
        /// is pointing.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// indicates whether the bond is located at the head or the tail.</param>
        public void MarkBond(Direction locDir, bool head = true)
        {
            CheckActive("Particles cannot change bonds of neighbors.");
            if (!CheckMove("Cannot mark bonds during beep phase.", true))
            {
                return;
            }
            int label = ParticleSystem_Utils.GetLabelInDir(locDir, particle.HeadDirection(), head);
            particle.MarkBond(label);
        }

        /// <summary>
        /// Checks if the bond at the given label is marked for special
        /// behavior. Always returns <c>false</c> when called in
        /// <see cref="ActivateBeep"/>.
        /// <para>See <see cref="MarkBond(Direction, bool)"/>.</para>
        /// </summary>
        /// <param name="locDir">The local direction into which the bond
        /// is pointing.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// indicates whether the bond is located at the head or the tail.</param>
        /// <returns><c>true</c> if and only if the bond at the given position has
        /// been marked.</returns>
        public bool BondMarked(Direction locDir, bool head = true)
        {
            CheckActive("Particles cannot check bonds of neighbors.");
            if (!CheckMove(null))
                return false;
            int label = ParticleSystem_Utils.GetLabelInDir(locDir, particle.HeadDirection(), head);
            return particle.BondMarked(label);
        }

        /// <summary>
        /// Hides the bond at the given label without releasing it.
        /// <para>See <see cref="ShowBond(Direction, bool)"/>,
        /// <see cref="IsBondVisible(Direction, bool)"/>.</para>
        /// </summary>
        /// <param name="locDir">The local direction into which the bond
        /// is pointing.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// indicates whether the bond is located at the head or the tail</param>
        public void HideBond(Direction locDir, bool head = true)
        {
            CheckActive("Particles cannot hide bonds of neighbors.");
            if (!CheckMove(null))
                return;
            int label = ParticleSystem_Utils.GetLabelInDir(locDir, particle.HeadDirection(), head);
            particle.SetBondVisible(label, false);
        }

        /// <summary>
        /// Shows the bond at the given label if it has previously
        /// been hidden.
        /// <para>See <see cref="HideBond(Direction, bool)"/>,
        /// <see cref="IsBondVisible(Direction, bool)"/>.</para>
        /// </summary>
        /// <param name="locDir">The local direction into which the bond
        /// is pointing.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// indicates whether the bond is located at the head or the tail</param>
        public void ShowBond(Direction locDir, bool head = true)
        {
            CheckActive("Particles cannot show bonds of neighbors.");
            if (!CheckMove(null))
                return;
            int label = ParticleSystem_Utils.GetLabelInDir(locDir, particle.HeadDirection(), head);
            particle.SetBondVisible(label, true);
        }

        /// <summary>
        /// Checks if the bond at the given label is visible.
        /// Always returns <c>false</c> when called in
        /// <see cref="ActivateBeep"/>.
        /// <para>See <see cref="HideBond(Direction, bool)"/>,
        /// <see cref="ShowBond(Direction, bool)"/>.</para>
        /// </summary>
        /// <param name="locDir">The local direction into which the bond
        /// is pointing.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// indicates whether the bond is located at the head or the tail</param>
        /// <returns><c>true</c> if and only if the bond at the given position
        /// is currently visible.</returns>
        public bool IsBondVisible(Direction locDir, bool head = true)
        {
            CheckActive("Particles cannot check bond visibility of neighbors.");
            if (!CheckMove(null))
                return false;
            int label = ParticleSystem_Utils.GetLabelInDir(locDir, particle.HeadDirection(), head);
            return particle.BondVisible(label);
        }

        /// <summary>
        /// Switches to automatic bond mode, causing the movements of the
        /// particle to behave like in the original model without joint
        /// movements.
        /// <para>
        /// Any changes made to the bond setup will be overwritten by the
        /// automatic bonds. Setting this mode will also avoid warning
        /// messages caused by disagreeing bonds.
        /// Only works in <see cref="ActivateMove"/>.
        /// </para>
        /// <para>
        /// Note that joint movements are not disabled completely by this
        /// method. For example, expanding into another particle will
        /// result in pushing the neighbor away if it causes no conflict.
        /// </para>
        /// </summary>
        public void UseAutomaticBonds()
        {
            CheckActive("Particles cannot change bonds of neighbors.");
            if (!CheckMove("Cannot change bond settings during beep phase", true)) return;
            particle.markedForAutomaticBonds = true;
        }


        /*
         * Movement actions
         */

        /// <summary>
        /// Expands this particle in the specified local direction.
        /// After the expansion, the particle's head will occupy the grid node
        /// in that direction, and its tail will remain at its current position.
        /// Only works in <see cref="ActivateMove"/>.
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="ContractHead"/>, <seealso cref="ContractTail"/>,
        /// <seealso cref="PushHandover(Direction)"/>.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction in which to expand.</param>
        public void Expand(Direction locDir)
        {
            CheckActive("Movement actions cannot be triggered for other particles.");
            if (!CheckMove("Cannot schedule expansion movement during beep phase.", true))
                return;
            if (!locDir.IsCardinal())
                throw new AlgorithmException(particle, "Invalid expansion direction: " + locDir);
            particle.system.ExpandParticle(particle, locDir);
        }

        /// <summary>
        /// Contracts this particle into the grid node that is currently
        /// occupied by the particle's head.
        /// After the contraction, the head and tail will both occupy this
        /// node. Only works in <see cref="ActivateMove"/>.
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="Expand(Direction)"/>,
        /// <seealso cref="ContractTail"/>,
        /// <seealso cref="Contract(bool)"/>,
        /// <seealso cref="PullHandoverHead(Direction)"/>.
        /// </para>
        /// </summary>
        public void ContractHead()
        {
            CheckActive("Movement actions cannot be triggered for other particles.");
            if (!CheckMove("Cannot schedule contraction movement during beep phase.", true))
                return;
            particle.system.ContractParticleHead(particle);
        }

        /// <summary>
        /// Contracts this particle into the grid node that is currently
        /// occupied by the particle's tail.
        /// After the contraction, the head and tail will both occupy this
        /// node. Only works in <see cref="ActivateMove"/>.
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="Expand(Direction)"/>,
        /// <seealso cref="ContractHead"/>,
        /// <seealso cref="Contract(bool)"/>,
        /// <seealso cref="PullHandoverTail(Direction)"/>.
        /// </para>
        /// </summary>
        public void ContractTail()
        {
            CheckActive("Movement actions cannot be triggered for other particles.");
            if (!CheckMove("Cannot schedule contraction movement during beep phase.", true))
                return;
            particle.system.ContractParticleTail(particle);
        }

        /// <summary>
        /// Contracts this particle into the grid node that is currently
        /// occupied by the particle's head or tail, depending on
        /// <paramref name="head"/>.
        /// After the contraction, the head and tail will both occupy this
        /// node. Only works in <see cref="ActivateMove"/>.
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="ContractHead"/>, <seealso cref="ContractTail"/>.
        /// </para>
        /// </summary>
        /// <param name="head">If <c>true</c>, contract into the particle's head,
        /// otherwise contract into the tail.</param>
        public void Contract(bool head)
        {
            if (head)
                ContractHead();
            else
                ContractTail();
        }

        /// <summary>
        /// Expands this particle in the specified local direction while the
        /// expanded neighbor particle in that direction contracts.
        /// After the expansion, the particle's head will occupy the grid node
        /// in that direction, and its tail will remain at its current position.
        /// The neighbor will have contracted away from that node.
        /// Only works in <see cref="ActivateMove"/>.
        /// <para>
        /// Only allowed if there is an expanded particle in the specified
        /// direction that simultaneously performs a pull handover aimed at
        /// this particle.
        /// </para>
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="Expand(Direction)"/>,
        /// <seealso cref="PullHandoverHead(Direction)"/>,
        /// <seealso cref="PullHandoverTail(Direction)"/>.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction into which the particle should expand.</param>
        public void PushHandover(Direction locDir)
        {
            CheckActive("Movement actions cannot be triggered for other particles.");
            if (!CheckMove("Cannot schedule handover movement during beep phase.", true))
                return;
            particle.system.PerformPushHandover(particle, locDir);
        }

        /// <summary>
        /// Contracts this particle into the grid node that is currently
        /// occupied by the particle's head while the contracted neighbor
        /// particle in the specified direction expands onto the current
        /// tail node.
        /// After the contraction, the head and tail of this particle will both
        /// occupy the current head node and the current tail node will be
        /// occupied by the neighbor.
        /// Only works in <see cref="ActivateMove"/>.
        /// <para>
        /// Only allowed if there is a contracted particle in the specified
        /// direction relative to this particle's tail that simultaneously
        /// performs a push handover.
        /// </para>
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="PushHandover(Direction)"/>,
        /// <seealso cref="ContractHead"/>,
        /// <seealso cref="PullHandoverTail(Direction)"/>.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction relative to this particle's
        /// tail from which the contracted neighbor particle should be pulled.</param>
        public void PullHandoverHead(Direction locDir)
        {
            CheckActive("Movement actions cannot be triggered for other particles.");
            if (!CheckMove("Cannot schedule handover movement during beep phase.", true))
                return;
            particle.system.PerformPullHandoverHead(particle, locDir);
        }

        /// <summary>
        /// Contracts this particle into the grid node that is currently
        /// occupied by the particle's tail while the contracted neighbor
        /// particle in the specified direction expands onto the current
        /// head node.
        /// After the contraction, the head and tail of this particle will both
        /// occupy the current tail node and the current head node will be
        /// occupied by the neighbor.
        /// Only works in <see cref="ActivateMove"/>.
        /// <para>
        /// Only allowed if there is a contracted particle in the specified
        /// direction relative to this particle's head that simultaneously
        /// performs a push handover.
        /// </para>
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="PushHandover(Direction)"/>,
        /// <seealso cref="ContractTail"/>,
        /// <seealso cref="PullHandoverHead(Direction)"/>.
        /// </para>
        /// </summary>
        /// <param name="locDir">The local direction relative to this particle's
        /// head from which the contracted neighbor particle should be pulled.</param>
        public void PullHandoverTail(Direction locDir)
        {
            CheckActive("Movement actions cannot be triggered for other particles.");
            if (!CheckMove("Cannot schedule handover movement during beep phase.", true))
                return;
            particle.system.PerformPullHandoverTail(particle, locDir);
        }

        /// <summary>
        /// Wrapper for <see cref="PullHandoverHead(Direction)"/> and
        /// <see cref="PullHandoverTail(Direction)"/> where the contraction
        /// target is specified as parameter <paramref name="head"/>.
        /// <para>Note that movements are only applied at the end of a round,
        /// i.e., after the activation is over. This means that calling this
        /// method will have no immediate effect.</para>
        /// <para>
        /// See also <seealso cref="PushHandover(Direction)"/>,
        /// <seealso cref="PullHandoverHead(Direction)"/>,
        /// <seealso cref="PullHandoverTail(Direction)"/>.
        /// </para>
        /// </summary>
        /// <param name="head">If <c>true</c>, perform a head contraction,
        /// otherwise perform a tail contraction.</param>
        /// <param name="locDir">The local direction relative to this particle's
        /// head/tail from which the contracted neighbor particle should be pulled.</param>
        public void PullHandover(bool head, Direction locDir)
        {
            if (head)
                PullHandoverHead(locDir);
            else
                PullHandoverTail(locDir);
        }


        /*
         * Visualization
         * These methods should only be called on the particle itself.
         * Calling them on other particles will lead to errors.
         */

        /// <summary>
        /// Returns the main color of this particle.
        /// <para>
        /// Call <see cref="IsMainColorSet"/> first to check if
        /// the color has been set or not. If not, the return
        /// value of this method has no meaning.
        /// </para>
        /// </summary>
        /// <returns>The currently set main color of the particle,
        /// if it has been set previously.</returns>
        public Color GetMainColor()
        {
            CheckActive("Visualization info is not available for other particles.");
            return particle.GetParticleColor();
        }

        /// <summary>
        /// Sets the main color of this particle. The color will be displayed
        /// as soon as the round computation is finished.
        /// </summary>
        /// <param name="c">The color to be applied to the particle.</param>
        public void SetMainColor(Color c)
        {
            CheckActive("Visualization info is not available for other particles.");
            particle.SetParticleColor(c);
        }

        /// <summary>
        /// Resets the particle's main color to its default value. The color will
        /// be shown as soon as the round computation is finished.
        /// </summary>
        public void ResetMainColor()
        {
            CheckActive("Visualization info is not available for other particles.");
            particle.ResetParticleColor();
        }

        /// <summary>
        /// Checks whether this particle's main color has been overwritten.
        /// <para>
        /// <see cref="GetMainColor"/> only returns a meaningful value if this
        /// method returns <c>true</c>.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if and only if the particle's main color
        /// has been overwritten and has not been reset since.</returns>
        public bool IsMainColorSet()
        {
            CheckActive("Visualization info is not available for other particles.");
            return particle.IsParticleColorSet();
        }

        /// <summary>
        /// Turns this particle into the anchor of the system. The anchor
        /// particle defines the movement of the whole system during joint
        /// movements by keeping its global position.
        /// <para>
        /// If multiple particles call this method or
        /// <see cref="MakeObjectAnchor(Direction, bool)"/> in the same
        /// activation, the one that is activated last will become the anchor.
        /// </para>
        /// </summary>
        public void MakeAnchor()
        {
            CheckActive("Cannot turn other particles into anchor.");
            particle.MakeAnchor();
        }

        /// <summary>
        /// Turns the neighboring object in the indicated direction into
        /// the anchor of the system.
        /// <para>
        /// If multiple particle call this method or <see cref="MakeAnchor"/>
        /// in the same activation, the particle that is activated last will
        /// determine the anchor.
        /// </para>
        /// </summary>
        /// <param name="d">The local direction in which the object neighbor lies.</param>
        /// <param name="fromHead">Whether the neighboring object is at this
        /// particle's head (only relevant for expanded particles).</param>
        public void MakeObjectAnchor(Direction d, bool fromHead = true)
        {
            CheckActive("Cannot turn other particles' neighbor objects into anchor.");
            particle.system.MakeObjectAnchor(particle, d, fromHead);
        }

        /// <summary>
        /// Makes the neighboring object in the indicated direction release
        /// all of its bonds to other objects in the current round. Can only
        /// be called in the movement phase.
        /// </summary>
        /// <param name="d">The local direction in which the object neighbor lies.</param>
        /// <param name="fromHead">Whether the neighboring object is at this
        /// particle's head (only relevant for expanded particles).</param>
        public void TriggerObjectBondRelease(Direction d, bool fromHead = true)
        {
            CheckActive("Bond releases cannot be triggered for other particles.");
            if (!CheckMove("Cannot release object bonds during beep phase.", true))
                return;
            particle.system.TriggerObjectBondRelease(particle, d, fromHead);
        }
    }

} // namespace AS2.Sim
