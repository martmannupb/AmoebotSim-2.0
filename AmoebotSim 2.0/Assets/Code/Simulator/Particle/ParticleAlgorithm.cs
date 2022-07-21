using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple container for neighbor search results.
/// <para>
/// Contains a reference to a neighbor particle, the local direction
/// in which it was found, and a flag indicating whether the
/// direction is relative to the querying particle's head or tail.
/// </para>
/// </summary>
/// <typeparam name="T">The type of the neighbor particle.</typeparam>
public struct Neighbor<T> where T : ParticleAlgorithm
{
    public T neighbor;
    public int localDir;
    public bool atHead;

    public Neighbor(T neighbor, int localDir, bool atHead)
    {
        this.neighbor = neighbor;
        this.localDir = localDir;
        this.atHead = atHead;
    }
}

/// <summary>
/// The abstract base class for particle algorithms in the Amoebot model.
/// <para>
/// Every algorithm that should run in the simulation must be implemented
/// as a subclass of the <see cref="ParticleAlgorithm"/> class through its <see cref="Activate"/>
/// method.
/// </para>
/// <para>
/// The subclass constructor must call the base class constructor as follows:
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
/// </para>
/// </summary>
public abstract class ParticleAlgorithm
{
    // Reference to the particle's system representation
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
    public abstract int PinsPerEdge { get;}

    /// <summary>
    /// This is the main activation method of the particle.
    /// It is called exactly once in each round and should contain
    /// the particle algorithm code.
    /// <para>
    /// Only one movement operation may be performed during an
    /// execution of this method. If a second movement is performed,
    /// the previous movement will be nullified.
    /// </para>
    /// </summary>
    public abstract void Activate();


    /**
     * Attribute creation methods.
     */

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute{T}"/> representing an integer value.
    /// </summary>
    /// <param name="name">The name of the attribute to be displayed in the UI.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
    public ParticleAttribute<int> CreateAttributeInt(string name, int initialValue = 0)
    {
        return ParticleAttributeFactory.CreateParticleAttributeInt(particle, name, initialValue);
    }

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute{T}"/> representing a boolean value.
    /// </summary>
    /// <param name="name">The name of the attribute to be displayed in the UI.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
    public ParticleAttribute<bool> CreateAttributeBool(string name, bool initialValue = false)
    {
        return ParticleAttributeFactory.CreateParticleAttributeBool(particle, name, initialValue);
    }

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute{T}"/> representing a direction.
    /// <para>
    /// Note that only values in the set <c>{-1,0,1,2,3,4,5}</c> are valid directions,
    /// with <c>-1</c> representing no direction.
    /// </para>
    /// </summary>
    /// <param name="name">The name of the attribute to be displayed in the UI.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
    public ParticleAttribute<int> CreateAttributeDirection(string name, int initialValue = 0)
    {
        return ParticleAttributeFactory.CreateParticleAttributeDirection(particle, name, initialValue);
    }

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute{T}"/> representing an enum value.
    /// </summary>
    /// <typeparam name="EnumT">The enum specifying the possible values of this attribute.</typeparam>
    /// <param name="name">The name of the attribute to be displayed in the UI.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>The <see cref="ParticleAttribute{T}"/> initialized to <paramref name="initialValue"/>.</returns>
    public ParticleAttribute<EnumT> CreateAttributeEnum<EnumT>(string name, EnumT initialValue) where EnumT : System.Enum
    {
        return ParticleAttributeFactory.CreateParticleAttributeEnum<EnumT>(particle, name, initialValue);
    }


    /** ====================================================================================================
     * Particle actions defining the API.
     * These methods should be called from the Activate() method.
     * ====================================================================================================
     */

    /**
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
    /// this execution of the <see cref="Activate"/> method. Use
    /// <see cref="IsExpanded_After"/> to get the predicted value for after the
    /// round.
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
    /// this execution of the <see cref="Activate"/> method. Use
    /// <see cref="IsContracted_After"/> to get the predicted value for after the
    /// round.
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
    /// this execution of the <see cref="Activate"/> method. Use
    /// <see cref="HeadDirection_After"/> to get the predicted value for after the
    /// round.
    /// </para>
    /// </summary>
    /// <returns>The local direction pointing from the particle's tail towards its head,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int HeadDirection()
    {
        return particle.HeadDirection();
    }

    /// <summary>
    /// Returns the local direction pointing from the particle's head towards its tail.
    /// <para>
    /// Note: This method returns information from the snapshot taken at the
    /// beginning of the current round. Its return value will not change during
    /// this execution of the <see cref="Activate"/> method. Use
    /// <see cref="TailDirection_After"/> to get the predicted value for after the
    /// round.
    /// </para>
    /// </summary>
    /// <returns>The local direction pointing from the particle's head towards its tail,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int TailDirection()
    {
        return particle.TailDirection();
    }

    /**
     * Messages
     */
    // TODO: Implement these

    public void SendMessage(Message msg, int locDir, bool head = true)
    {
        particle.system.SendParticleMessage(particle, msg, locDir, head);
    }

    public bool HasMsgOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public T GetMsgOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public List<T> GetMsgsOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public T PopMsgOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public List<T> PopMsgsOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }


    /**
     * Predicted state information retrieval
     * These methods return the predicted values for after the
     * current round based on the particle's actions during this
     * call of <see cref="Activate"/>.
     */

    /// <summary>
    /// Like <see cref="IsExpanded"/>, but returns the predicted
    /// value for after the round
    /// </summary>
    /// <returns><c>true</c> if and only if the particle will be
    /// expanded at the end of the round if its planned movements succeed.</returns>
    public bool IsExpanded_After()
    {
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
        return particle.IsContracted_After();
    }

    /// <summary>
    /// Like <see cref="HeadDirection"/>, but returns the predicted value
    /// for after the round.
    /// </summary>
    /// <returns>The local direction pointing from the particle's tail towards its head,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int HeadDirection_After()
    {
        return particle.HeadDirection_After();
    }

    /// <summary>
    /// Like <see cref="TailDirection"/>, but returns the predicted value
    /// for after the round.
    /// </summary>
    /// <returns>The local direction pointing from the particle's head towards its tail,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int TailDirection_After()
    {
        return particle.TailDirection_After();
    }


    /**
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
    /// does not perform a movement.
    /// </para>
    /// <para>
    /// See also <seealso cref="GetContractedPinConfiguration"/>,
    /// <seealso cref="GetExpandedPinConfiguration(int)"/>,
    /// <seealso cref="SetPlannedPinConfiguration(PinConfiguration)"/>,
    /// <seealso cref="GetPlannedPinConfiguration"/>.
    /// </para>
    /// </summary>
    /// <returns>A copy of the pin configuration at the
    /// beginning of the current round.</returns>
    public PinConfiguration GetCurrentPinConfiguration()
    {
        return particle.GetCurrentPinConfiguration();
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
    /// <seealso cref="GetExpandedPinConfiguration(int)"/>,
    /// <seealso cref="SetPlannedPinConfiguration(PinConfiguration)"/>,
    /// <seealso cref="GetPlannedPinConfiguration"/>.
    /// </para>
    /// </summary>
    /// <returns>A new singleton pin configuration for the
    /// contracted state that can be modified arbitrarily.</returns>
    public PinConfiguration GetContractedPinConfiguration()
    {
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
    public PinConfiguration GetExpandedPinConfiguration(int headDirection)
    {
        return new SysPinConfiguration(particle, PinsPerEdge, headDirection);
    }

    /// <summary>
    /// Sets the pin configuration that should be applied to the particle
    /// at the end of this round.
    /// <para>
    /// The expansion state of the pin configuration must match the planned
    /// expansion state of the particle, i.e., if the particle plans to be
    /// contracted at the end of the round, the pin configuration must be made
    /// for the contracted state, and if the particle plans to be expanded at
    /// the end of the round, the pin configuration must match the
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
        particle.SetPlannedPinConfiguration((SysPinConfiguration)pinConfiguration);
    }

    /// <summary>
    /// Returns the pin configuration set using
    /// <see cref="SetPlannedPinConfiguration(PinConfiguration)"/> in
    /// this round.
    /// </summary>
    /// <returns>The pin configuration planned to be applied at the end of
    /// the current round. <c>null</c> if no configuration was planned yet.</returns>
    public PinConfiguration GetPlannedPinConfiguration()
    {
        return particle.GetPlannedPinConfiguration();
    }


    /**
     * System information retrieval
     * Mainly for finding neighbor particles
     */

    // TODO: Decide on uniform interface for specifying ports (through labels or direction + head/tail (or both))
    // TODO: Add many more helper functions (like firstNbrWithProperty etc.)

    /// <summary>
    /// Checks if this particle has a neighboring particle in the given local direction.
    /// For expanded particles, there are two different nodes in the same local direction,
    /// one seen from the particle's head and one seen from its tail.
    /// <para>See also <see cref="GetNeighborAt(int, bool)"/>.</para>
    /// <para>
    /// Note: This method returns information from the snapshot taken at the
    /// beginning of the current round. Its return value will not change during
    /// this execution of the <see cref="Activate"/> method.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction in which to search for a neighbor particle.</param>
    /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
    /// the particle's tail (only relevant if this particle is expanded).</param>
    /// <returns><c>true</c> if and only if there is a different particle in the specified position.</returns>
    public bool HasNeighborAt(int locDir, bool fromHead = true)
    {
        return particle.system.HasNeighborAt(particle, locDir, fromHead);
    }

    // TODO: What to do if there is no neighbor? Check beforehand, throw exception?
    /// <summary>
    /// Gets this particle's neighbor in the given local direction. The position to
    /// check is determined in the same way as in <see cref="HasNeighborAt(int, bool)"/>.
    /// <para>
    /// Note: This method returns information from the snapshot taken at the
    /// beginning of the current round. Its return value will not change during
    /// this execution of the <see cref="Activate"/> method.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction from which to get the neighbor particle.</param>
    /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
    /// the particle's tail (only relevant if this particle is expanded).</param>
    /// <returns>The neighboring particle in the specified position.</returns>
    public ParticleAlgorithm GetNeighborAt(int locDir, bool fromHead = true)
    {
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
    /// <see cref="HasNeighborAt(int, bool)"/>.
    /// <para>
    /// Note: This method returns information from the snapshot taken at the
    /// beginning of the current round. Its return value will not change during
    /// this execution of the <see cref="Activate"/> method.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction from which to get the neighbor particle.</param>
    /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise
    /// look from the particle's tail (only relevant if this particle is expanded.)</param>
    /// <returns><c>true</c> if and only if the grid node in the specified position is
    /// occupied by the head of a neighboring particle (for contracted particles, head and
    /// tail occupy the same node.)</returns>
    public bool IsHeadAt(int locDir, bool fromHead = true)
    {
        return particle.system.IsHeadAt(particle, locDir, fromHead);
    }

    /// <summary>
    /// Checks if the part of the neighboring particle in the given local direction is
    /// the neighbor's tail. The position to check is determined in the same way as in
    /// <see cref="HasNeighborAt(int, bool)"/>.
    /// <para>
    /// Note: This method returns information from the snapshot taken at the
    /// beginning of the current round. Its return value will not change during
    /// this execution of the <see cref="Activate"/> method.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction from which to get the neighbor particle.</param>
    /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise
    /// look from the particle's tail (only relevant if this particle is expanded.)</param>
    /// <returns><c>true</c> if and only if the grid node in the specified position is
    /// occupied by the tail of a neighboring particle (for contracted particles, head and
    /// tail occupy the same node.)</returns>
    public bool IsTailAt(int locDir, bool fromHead = true)
    {
        return particle.system.IsTailAt(particle, locDir, fromHead);
    }

    // TODO: Documentation

    public bool FindFirstNeighbor<T>(out Neighbor<T> neighbor, int startDir = 0, bool startAtHead = true, bool withChirality = true, int maxNumber = -1) where T : ParticleAlgorithm
    {
        return particle.system.FindFirstNeighbor<T>(particle, out neighbor, startDir, startAtHead, withChirality, maxNumber);
    }

    public bool FindFirstNeighborWithProperty<T>(System.Func<T, bool> prop, out Neighbor<T> neighbor, int startDir = 0, bool startAtHead = true, bool withChirality = true, int maxNumber = -1) where T : ParticleAlgorithm
    {
        return particle.system.FindFirstNeighborWithProperty<T>(particle, prop, out neighbor, startDir, startAtHead, withChirality, maxNumber);
    }


    /**
     * Movement actions
     */

    /// <summary>
    /// Expands this particle in the specified local direction.
    /// After the expansion, the particle's head will occupy the grid node
    /// in that direction, and its tail will remain at its current position.
    /// <para>Only allowed if there is no contracted particle in the
    /// specified direction.</para>
    /// <para>Note that movements are only applied at the end of a round,
    /// i.e., after the activation is over. This means that calling this
    /// method will have no immediate effect.</para>
    /// <para>
    /// See also <seealso cref="ContractHead"/>, <seealso cref="ContractTail"/>,
    /// <seealso cref="PushHandover(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction in which to expand.</param>
    public void Expand(int locDir)
    {
        particle.system.ExpandParticle(particle, locDir);
    }

    /// <summary>
    /// Contracts this particle into the grid node that is currently
    /// occupied by the particle's head.
    /// After the contraction, the head and tail will both occupy this
    /// node.
    /// <para>Note that movements are only applied at the end of a round,
    /// i.e., after the activation is over. This means that calling this
    /// method will have no immediate effect.</para>
    /// <para>
    /// See also <seealso cref="Expand(int)"/>,
    /// <seealso cref="ContractTail"/>,
    /// <seealso cref="PullHandoverHead(int)"/>.
    /// </para>
    /// </summary>
    public void ContractHead()
    {
        particle.system.ContractParticleHead(particle);
    }

    /// <summary>
    /// Contracts this particle into the grid node that is currently
    /// occupied by the particle's tail.
    /// After the contraction, the head and tail will both occupy this
    /// node.
    /// <para>Note that movements are only applied at the end of a round,
    /// i.e., after the activation is over. This means that calling this
    /// method will have no immediate effect.</para>
    /// <para>
    /// See also <seealso cref="Expand(int)"/>,
    /// <seealso cref="ContractHead"/>,
    /// <seealso cref="PullHandoverTail(int)"/>.
    /// </para>
    /// </summary>
    public void ContractTail()
    {
        particle.system.ContractParticleTail(particle);
    }

    /// <summary>
    /// Expands this particle in the specified local direction and tries to
    /// force the expanded neighbor particle to contract.
    /// After the expansion, the particle's head will occupy the grid node
    /// in that direction, and its tail will remain at its current position.
    /// The neighbor will have contracted away from that node.
    /// <para>
    /// Only allowed if there is an expanded particle in the
    /// specified direction.
    /// The handover will lead to a conflict if this neighboring particle
    /// performs a movement that is not consistent with the handover in
    /// the same round, such as contracting into the neighboring node or
    /// attempting a pull handover with a third particle.
    /// </para>
    /// <para>Note that movements are only applied at the end of a round,
    /// i.e., after the activation is over. This means that calling this
    /// method will have no immediate effect.</para>
    /// <para>
    /// See also <seealso cref="Expand(int)"/>,
    /// <seealso cref="PullHandoverHead(int)"/>,
    /// <seealso cref="PullHandoverTail(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction into which the particle should expand.</param>
    public void PushHandover(int locDir)
    {
        particle.system.PerformPushHandover(particle, locDir);
    }

    /// <summary>
    /// Contracts this particle into the grid node that is currently
    /// occupied by the particle's head and tries to force the contracted
    /// neighbor particle in the specified direction to expand onto the
    /// current tail node.
    /// After the contraction, the head and tail of this particle will both
    /// occupy the current head node and the current tail node will be
    /// occupied by the neighbor.
    /// <para>
    /// Only allowed if there is a contracted particle in the specified
    /// direction relative to this particle's tail.
    /// The handover will lead to a conflict if that neighboring particle
    /// performs a movement that is not consistent with the handover in
    /// the same round, such as expanding onto a different node.
    /// </para>
    /// <para>Note that movements are only applied at the end of a round,
    /// i.e., after the activation is over. This means that calling this
    /// method will have no immediate effect.</para>
    /// <para>
    /// See also <seealso cref="PushHandover(int)"/>,
    /// <seealso cref="ContractHead"/>,
    /// <seealso cref="PullHandoverTail(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction relative to this particle's
    /// tail from which the contracted neighbor particle should be pulled.</param>
    public void PullHandoverHead(int locDir)
    {
        particle.system.PerformPullHandoverHead(particle, locDir);
    }

    /// <summary>
    /// Contracts this particle into the grid node that is currently
    /// occupied by the particle's tail and tries to force the contracted
    /// neighbor particle in the specified direction to expand onto the
    /// current head node.
    /// After the contraction, the head and tail of this particle will both
    /// occupy the current tail node and the current head node will be
    /// occupied by the neighbor.
    /// <para>
    /// Only allowed if there is a contracted particle in the specified
    /// direction relative to this particle's head.
    /// The handover will lead to a conflict if that neighboring particle
    /// performs a movement that is not consistent with the handover in
    /// the same round, such as expanding onto a different node.
    /// </para>
    /// <para>Note that movements are only applied at the end of a round,
    /// i.e., after the activation is over. This means that calling this
    /// method will have no immediate effect.</para>
    /// <para>
    /// See also <seealso cref="PushHandover(int)"/>,
    /// <seealso cref="ContractTail"/>,
    /// <seealso cref="PullHandoverHead(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="locDir">The local direction relative to this particle's
    /// head from which the contracted neighbor particle should be pulled.</param>
    public void PullHandoverTail(int locDir)
    {
        particle.system.PerformPullHandoverTail(particle, locDir);
    }


    /**
     * Visualization
     * These methods should only be called on the particle itself.
     * Calling them on other particles is not defined and should
     * never be done.
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
        return particle.GetParticleColor();
    }

    /// <summary>
    /// Sets the main color of this particle.
    /// </summary>
    /// <param name="c">The color to be applied to the particle.</param>
    public void SetMainColor(Color c)
    {
        particle.SetParticleColor(c);
    }

    /// <summary>
    /// Resets the particle's main color to its default value.
    /// </summary>
    public void ResetMainColor()
    {
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
        return particle.IsParticleColorSet();
    }
}
