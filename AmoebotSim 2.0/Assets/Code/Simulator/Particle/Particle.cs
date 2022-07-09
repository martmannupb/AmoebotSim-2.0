using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The system-side representation of a particle.
/// <para>
/// This class stores all internal particle information that should be
/// hidden from the developers of a particle algorithm. It also
/// provides methods for changing the particle's state to be called by
/// the <see cref="ParticleSystem"/>.
/// </para>
/// <para>
/// There is a 1:1 correspondence between <see cref="Particle"/> and
/// <see cref="ParticleAlgorithm"/> instances. The conventional way to
/// instantiate a pair of these objects is the following:
/// <code>
/// Particle part = new Particle(particleSystem, startPosition[, compassDir][, chirality]);
/// new ParticleAlgorithmSubclass(part);
/// </code>
/// </para>
/// </summary>
public class Particle : IParticleState, IReplayHistory
{
    // References _____
    public ParticleSystem system;
    public ParticleAlgorithm algorithm;

    // Graphics _____
    public IParticleGraphicsAdapter graphics;

    // Data _____
    // General
    /// <summary>
    /// The compass orientation of the particle, represented as a global direction.
    /// </summary>
    public readonly int comDir;
    /// <summary>
    /// If <c>true</c>, the positive rotation direction of the particle is counter-clockwise,
    /// else it is clockwise.
    /// </summary>
    public readonly bool chirality;

    // State _____
    // Position: Store tail position in history but always keep head position up to date
    private ValueHistory<Vector2Int> tailPosHistory;
    private Vector2Int pos_head;
    private Vector2Int pos_tail;

    // Expansion
    // Store expansion direction in history
    private ValueHistory<int> expansionDirHistory;
    private bool exp_isExpanded;
    /// <summary>
    /// The local direction pointing from the particle's tail towards its head.
    /// </summary>
    private int exp_expansionDir;
    
    // Attributes
    private List<IParticleAttribute> attributes = new List<IParticleAttribute>();
    // Flag used to indicate that the particle is currently being activated
    // (Causes ParticleAttributes to behave differently for this particle than
    // for others)
    public bool isActive = false;
    
    // Messages
    private Queue<Message> messageQueue = new Queue<Message>();

    // Data used by system to coordinate movements
    /// <summary>
    /// Stores the movement action the particle scheduled during the current round.
    /// Used to find out if a particle intends to perform a movement to check if
    /// movements are consistent or will lead to conflicts.
    /// <para>Reset to <c>null</c> after applying the movement action.</para>
    /// </summary>
    public ParticleAction scheduledAction = null;
    /// <summary>
    /// Flag indicating whether the particle has moved during the current round.
    /// Used to determine whether a particle can be pushed or pulled by a
    /// handover.
    /// <para>Reset to <c>false</c> after simulating a round.</para>
    /// </summary>
    public bool hasMoved = false;

    public Particle(ParticleSystem system, Vector2Int pos, int compassDir = 0, bool chirality = true)
    {
        this.system = system;

        // Start contracted
        tailPosHistory = new ValueHistory<Vector2Int>(pos, system.CurrentRound);
        expansionDirHistory = new ValueHistory<int>(-1, system.CurrentRound);
        pos_head = pos;
        pos_tail = pos;
        exp_isExpanded = false;
        exp_expansionDir = -1;

        comDir = compassDir;
        this.chirality = chirality;

        // Graphics
        // Add particle to the system and update the visuals of the particle
        graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
        graphics.AddParticle();
        graphics.Update();
    }

    /// <summary>
    /// This is the main activation method of the particle.
    /// It is implemented by the particle algorithm and should
    /// be called exactly once in each round.
    /// </summary>
    public void Activate()
    {
        isActive = true;
        algorithm.Activate();
        isActive = false;
    }


    /**
     * State information retrieval
     */

    /// <summary>
    /// Returns the grid node on which the particle's head is currently positioned.
    /// When a particle expands, its part on the node it expands to becomes the
    /// particle's head. For contracted particles, the head and tail are on the same node.
    /// <para>See <see cref="Tail"/></para>
    /// </summary>
    /// <returns>The node on which the particle's head is currently positioned.</returns>
    public Vector2Int Head()
    {
        return pos_head;
    }

    /// <summary>
    /// Returns the grid node on which the particle's tail is currently positioned.
    /// When a particle expands, its part on the node it originally occupies becomes
    /// the particle's tail. For contracted particles, the head and tail are on the same node.
    /// <para>See <see cref="Head"/></para>
    /// </summary>
    /// <returns>The node on which the particle's tail is currently positioned.</returns>
    public Vector2Int Tail()
    {
        return pos_tail;
    }

    /// <summary>
    /// Checks if the particle is currently expanded, i.e., occupies 2 neighboring
    /// grid nodes simultaneously.
    /// <para>See <see cref="IsContracted"/></para>
    /// </summary>
    /// <returns><c>true</c> if and only if the particle is expanded.</returns>
    public bool IsExpanded()
    {
        return exp_isExpanded;
    }

    /// <summary>
    /// Checks if the particle is currently contracted, i.e., occupies exactly one
    /// node of the grid.
    /// <para>See <see cref="IsExpanded"/></para>
    /// </summary>
    /// <returns><c>true</c> if and only if the particle is contracted.</returns>
    public bool IsContracted()
    {
        return !exp_isExpanded;
    }

    /// <summary>
    /// Returns the local direction pointing from the particle's tail towards its head.
    /// </summary>
    /// <returns>The local direction pointing from the particle's tail towards its head,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int HeadDirection()
    {
        return exp_expansionDir;
    }

    /// <summary>
    /// Returns the local direction pointing from the particle's head towards its tail.
    /// </summary>
    /// <returns>The local direction pointing from the particle's head towards its tail,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int TailDirection()
    {
        return exp_isExpanded ? (exp_expansionDir + 3) % 6 : -1;
    }

    /// <summary>
    /// Converts the particle's local direction that points from its tail towards its
    /// head into a global direction.
    /// </summary>
    /// <returns>The global direction pointing from this particle's tail towards its head,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int GlobalHeadDirection()
    {
        return exp_isExpanded ? ParticleSystem_Utils.LocalToGlobalDir(exp_expansionDir, comDir, chirality) : -1;
    }

    /// <summary>
    /// Converts the particle's local direction that points from its head towards its
    /// tail into a global direction.
    /// </summary>
    /// <returns>The global direction pointing from this particle's head towards its tail,
    /// if it is expanded, otherwise <c>-1</c>.</returns>
    public int GlobalTailDirection()
    {
        return exp_isExpanded ? ParticleSystem_Utils.LocalToGlobalDir((exp_expansionDir + 3) % 6, comDir, chirality) : -1;
    }


    /**
     * Messages
     */
    // TODO: Check if these are even needed (could directly call ParticleSystem methods like for the movements)
    // TODO: Implement these

    public void SendMessage(Message msg, int locDir, bool head = true)
    {
        system.SendParticleMessage(this, msg, locDir, head);
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
     * Particle action methods that are used by the system to change the
     * particle's state at the appropriate time. They are NOT part of the
     * ParticleAlgorithm's interface used by the algorithm developer.
     * 
     * Note that these must not be called while the particle is in a
     * previous state. The ParticleSystem managing this particle is
     * responsible for ensuring that all particles are in a tracking
     * state when a round is simulated.
     */

    /// <summary>
    /// Expands this particle in the specified local direction.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    /// <param name="locDir">The local direction into which this particle should expand.</param>
    public void Apply_Expand(int locDir)
    {
        exp_isExpanded = true;
        exp_expansionDir = locDir;
        pos_head = ParticleSystem_Utils.GetNbrInDir(pos_head, ParticleSystem_Utils.LocalToGlobalDir(locDir, comDir, chirality));
        expansionDirHistory.RecordValueInRound(exp_expansionDir, system.CurrentRound);
    }

    /// <summary>
    /// Contracts this particle into the node occupied by its head.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    public void Apply_ContractHead()
    {
        exp_isExpanded = false;
        exp_expansionDir = -1;
        pos_tail = pos_head;
        tailPosHistory.RecordValueInRound(pos_tail, system.CurrentRound);
        expansionDirHistory.RecordValueInRound(-1, system.CurrentRound);
    }

    /// <summary>
    /// Contracts this particle into the node occupied by its tail.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    public void Apply_ContractTail()
    {
        exp_isExpanded = false;
        exp_expansionDir = -1;
        pos_head = pos_tail;
        expansionDirHistory.RecordValueInRound(-1, system.CurrentRound);
    }

    // TODO: Check if we need to do anything else in these 3 methods

    /// <summary>
    /// Same as <see cref="Apply_Expand(int)"/>.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    /// <param name="locDir">The local direction into which to expand.</param>
    public void Apply_PushHandover(int locDir)
    {
        Apply_Expand(locDir);
    }

    /// <summary>
    /// Same as <see cref="Apply_ContractHead"/>.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid and the
    /// <paramref name="locDir"/> parameter is not used.
    /// </remarks>
    /// <param name="locDir">The local direction from where to pull the
    /// neighbor particle, relative to this particle's tail.</param>
    public void Apply_PullHandoverHead(int locDir)
    {
        Apply_ContractHead();
    }

    /// <summary>
    /// Same as <see cref="Apply_ContractHead"/>.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid and the
    /// <paramref name="locDir"/> parameter is not used.
    /// </remarks>
    /// <param name="locDir">The local direction from where to pull the
    /// neighbor particle, relative to this particle's head.</param>
    public void Apply_PullHandoverTail(int locDir)
    {
        Apply_ContractTail();
    }

    public void Apply_SendMessage(Message msg, int locDir, bool head = true)
    {
        throw new System.NotImplementedException();
    }


    /**
     * Attribute handling
     */

    /// <summary>
    /// Adds the given <see cref="IParticleAttribute"/> to the particle's list of
    /// attributes. All attributes on this list will be displayed and editable in
    /// the simulation UI. This function is called by the
    /// <see cref="ParticleAttributeFactory"/> when it is called by this particle's
    /// <see cref="ParticleAlgorithm"/> to instantiate the attribute.
    /// </summary>
    /// <param name="attr">The attribute to add to this particle's attribute list.</param>
    public void AddAttribute(IParticleAttribute attr)
    {
        attributes.Add(attr);
    }

    /// <summary>
    /// Gets this particle's list of <see cref="IParticleAttribute"/>s. These
    /// attributes are supposed to be shown and edited in the simulation UI.
    /// </summary>
    /// <returns>The list of <see cref="IParticleAttribute"/>s belonging to this particle.</returns>
    public List<IParticleAttribute> GetAttributes()
    {
        return attributes;
    }

    public void Print()
    {
        Debug.Log("Position history:");
        tailPosHistory.Print();
        Debug.Log("Expansion dir history:");
        expansionDirHistory.Print();
        foreach (IParticleAttribute attr in attributes)
        {
            attr.Print();
        }
    }


    /**
     * Methods implementing the IReplayHistory interface.
     * These allow the particle to be reset to any
     * previous round.
     * 
     * Note that the histories stored in this particle may
     * not all have the same marker position while they
     * are tracking. The first marker action should
     * always be <see cref="SetMarkerToRound(int)"/> to
     * synchronize all markers. This is the responsibility
     * of the ParticleSystem.
     */

    /// <summary>
    /// Implementation of <see cref="IReplayHistory.GetFirstRecordedRound"/>.
    /// <para>
    /// Note that a particle stores multiple value histories and their first
    /// recorded rounds may differ.
    /// </para>
    /// </summary>
    /// <returns>The first recorded round of one of the value histories that
    /// were initialized in the constructor.</returns>
    public int GetFirstRecordedRound()
    {
        // Position and expansion direction histories are initialized in the constructor
        return tailPosHistory.GetFirstRecordedRound();
    }

    public void SetMarkerToRound(int round)
    {
        // Reset position and expansion state
        tailPosHistory.SetMarkerToRound(round);
        expansionDirHistory.SetMarkerToRound(round);

        // Need to update private fields accordingly
        UpdateInternalState();

        // Reset all ParticleAttributes
        foreach (IParticleAttribute attr in attributes)
        {
            attr.SetMarkerToRound(round);
        }
    }

    public void StepBack()
    {
        tailPosHistory.StepBack();
        expansionDirHistory.StepBack();

        UpdateInternalState();

        foreach (IParticleAttribute attr in attributes)
        {
            attr.StepBack();
        }
    }

    public void StepForward()
    {
        tailPosHistory.StepForward();
        expansionDirHistory.StepForward();

        UpdateInternalState();

        foreach (IParticleAttribute attr in attributes)
        {
            attr.StepForward();
        }
    }

    /// <summary>
    /// Implementation of <see cref="IReplayHistory.GetMarkedRound"/>.
    /// <para>
    /// Note that a particle stores multiple value histories and their
    /// currently marked rounds may differ unless they were set to track
    /// the same round previously.
    /// </para>
    /// </summary>
    /// <returns>The currently marked round of one of the value histories that
    /// was initialized in the constructor.</returns>
    public int GetMarkedRound()
    {
        return tailPosHistory.GetMarkedRound();
    }

    public void ContinueTracking()
    {
        tailPosHistory.ContinueTracking();
        expansionDirHistory.ContinueTracking();

        UpdateInternalState();

        foreach (IParticleAttribute attr in attributes)
        {
            attr.ContinueTracking();
        }
    }

    /// <summary>
    /// Implementation of <see cref="IReplayHistory.CutOffAtMarker"/>.
    /// <para>
    /// Note that a particle stores multiple value histories and their
    /// current marker positions may differ unless they were set to track
    /// the same round previously
    /// </para>
    /// </summary>
    public void CutOffAtMarker()
    {
        tailPosHistory.CutOffAtMarker();
        expansionDirHistory.CutOffAtMarker();

        // No need to update internal state because this does not change
        // the current value of a history

        foreach (IParticleAttribute attr in attributes)
        {
            attr.CutOffAtMarker();
        }
    }

    public void ShiftTimescale(int amount)
    {
        tailPosHistory.ShiftTimescale(amount);
        expansionDirHistory.ShiftTimescale(amount);

        // No need to update internal state because this does not change
        // the current value of a history

        foreach (IParticleAttribute attr in attributes)
        {
            attr.ShiftTimescale(amount);
        }
    }

    /// <summary>
    /// Helper that updates the private state variables after
    /// the histories were changed in any way.
    /// </summary>
    private void UpdateInternalState()
    {
        pos_tail = tailPosHistory.GetMarkedValue();
        exp_expansionDir = expansionDirHistory.GetMarkedValue();
        exp_isExpanded = exp_expansionDir != -1;
        if (exp_isExpanded)
        {
            pos_head = ParticleSystem_Utils.GetNbrInDir(pos_tail, exp_expansionDir);
        }
        else
        {
            pos_head = pos_tail;
        }
    }
}
