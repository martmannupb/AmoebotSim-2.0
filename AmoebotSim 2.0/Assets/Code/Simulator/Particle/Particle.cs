using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public readonly Direction comDir;
    /// <summary>
    /// If <c>true</c>, the positive rotation direction of the particle is counter-clockwise,
    /// else it is clockwise.
    /// </summary>
    public readonly bool chirality;

    // Special flag that is set while a particle is being reinitialized on load
    // Prevents state changes caused by an algorithm constructor
    private bool inReinitialize = false;

    // State _____
    // Position: Store tail position in history but always keep head position up to date
    private ValueHistory<Vector2Int> tailPosHistory;
    private Vector2Int pos_head;
    private Vector2Int pos_tail;

    // Expansion
    // Store expansion direction in history
    private ValueHistory<Direction> expansionDirHistory;
    private bool exp_isExpanded;
    /// <summary>
    /// The local direction pointing from the particle's tail towards its head.
    /// </summary>
    private Direction exp_expansionDir;
    
    // Attributes
    private List<IParticleAttribute> attributes = new List<IParticleAttribute>();

    // Bonds
    /// <summary>
    /// Flags indicating which bonds should be active. Indices are
    /// local labels. Only the 10 lowest bits are used.
    /// </summary>
    private BitVector32 activeBonds;
    private ValueHistory<int> activeBondHistory;

    /// <summary>
    /// Flags indicating which bonds have been marked for special
    /// behavior. This includes being pulled with an expansion and
    /// being transferred during a handover. Indices are local
    /// labels and only the 10 lowest bits are used.
    /// </summary>
    private BitVector32 markedBonds;
    private ValueHistory<int> markedBondHistory;

    // Pin Configuration
    private ValueHistoryPinConfiguration pinConfigurationHistory;
    private SysPinConfiguration pinConfiguration;
    public SysPinConfiguration PinConfiguration
    {
        get { return pinConfiguration; }
        private set { pinConfiguration = value; }
    }

    // Beeps and messages
    /// <summary>
    /// Flags indicating which partition sets have received a beep.
    /// Indices equal (local) partition set IDs.
    /// </summary>
    private BitArray receivedBeeps;
    private ValueHistoryBitArray receivedBeepsHistory;

    public Message[] receivedMessages;
    private ValueHistoryMessage[] receivedMessagesHistory;

    /// <summary>
    /// Flags indicating which partition sets of the planned pin
    /// configuration have scheduled sending a beep. Indices
    /// equal (local) partition set IDs.
    /// </summary>
    private BitArray plannedBeeps;
    private ValueHistory<bool>[] plannedBeepsHistory;
    private bool hasPlannedBeeps = false;

    private Message[] plannedMessages;
    private ValueHistoryMessage[] plannedMessageHistory;
    private bool hasPlannedMessages = false;


    // Visualization

    // Particle fill color
    private ValueHistory<Color> mainColorHistory;
    private ValueHistory<bool> mainColorSetHistory;
    private Color mainColor = new Color();
    private bool mainColorSet = false;

    // Partition set colors
    private ValueHistory<Color>[] partitionSetColorHistory;
    private ValueHistory<bool>[] partitionSetColorOverrideHistory;
    private Color[] partitionSetColors;
    private bool[] partitionSetColorsOverride;
    public Color[] PartitionSetColors
    {
        get { return partitionSetColors; }
    }
    public bool[] PartitionSetColorsOverride
    {
        get { return partitionSetColorsOverride; }
    }


    /**
     * Data used by simulator to coordinate particle actions
     * 
     * This section is used by the particle and the simulator to
     * store information on planned actions such that they can be
     * coordinated and applied easily, and to provide a way for
     * the developer to access predicted information.
     * 
     * Some information is maintained internally by the Particle to
     * cache predicted information.
     */

    /// <summary>
    /// Stores the movement action the particle scheduled during the current round.
    /// Used to find out if a particle intends to perform a movement to check if
    /// movements are consistent or will lead to conflicts.
    /// <para>Reset this to <c>null</c> after applying the movement action.</para>
    /// </summary>
    public ParticleAction ScheduledMovement
    {
        get { return scheduledMovement; }
        set { if (value == null) ResetScheduledMovement(); else SetScheduledMovement(value); }
    }
    private ParticleAction scheduledMovement = null;
    private bool predictIsExpanded = false;
    private Direction predictTailDir = Direction.NONE;
    private Direction predictHeadDir = Direction.NONE;

    /// <summary>
    /// The absolute offset from the particle's initial location,
    /// accumulated by joint movements.
    /// </summary>
    public Vector2Int jmOffset;

    /// <summary>
    /// The global offset by which the particle would move a
    /// bonded neighbor relative to its own origin if the bond
    /// moves relative to the origin. This is simply the
    /// direction of movement written as a vector.
    /// </summary>
    public Vector2Int movementOffset;

    /// <summary>
    /// Indicates whether the particle's head is its local origin,
    /// i.e., the part that would stay at its absolute position if
    /// the particle was the anchor of the system. The head is the
    /// anchor if the particle is and stays contracted or if it is
    /// expanded and contracts into its head.
    /// </summary>
    public bool isHeadOrigin;

    /// <summary>
    /// Flag indicating whether the particle has already been
    /// processed for joint movements. Becomes <c>true</c> as
    /// soon as the particle's movement has been validated
    /// against all of its neighbors' movements and its new
    /// location has been determined.
    /// </summary>
    public bool processedJointMovement = false;

    /// <summary>
    /// Flag indicating whether the particle has been added to the BFS
    /// queue for joint movement simulation. Used to ensure that each
    /// particle is only added to the queue once.
    /// </summary>
    public bool queuedForJMProcessing = false;

    /// <summary>
    /// Active bonds indexed by global labels.
    /// </summary>
    public bool[] activeBondsGlobal = new bool[10];
    /// <summary>
    /// Marked bonds indexed by global labels.
    /// </summary>
    public bool[] markedBondsGlobal = new bool[10];

    /// <summary>
    /// Flag indicating whether the particle has moved during the current round.
    /// Used to determine whether a particle can be pushed or pulled by a
    /// handover.
    /// <para>Reset to <c>false</c> after simulating a round.</para>
    /// </summary>
    public bool hasMoved = false;

    /// <summary>
    /// Flag indicating whether the pin configuration of the particle has
    /// already been processed for finding circuits. Becomes <c>true</c>
    /// as soon as all non-empty partition sets have been assigned to a
    /// circuit, and is reset to <c>false</c> after finishing the round.
    /// </summary>
    public bool processedPinConfig = false;

    /// <summary>
    /// Flag indicating whether the particle has been added to the BFS
    /// queue for circuit discovery. Used to ensure that each particle
    /// is only added to the queue once.
    /// </summary>
    public bool queuedForPinConfigProcessing = false;

    /// <summary>
    /// Stores the pin configuration that is planned to be applied at the end
    /// of the current round.
    /// </summary>
    public SysPinConfiguration PlannedPinConfiguration
    {
        get { return plannedPinConfiguration; }
        set { plannedPinConfiguration = value; }
    }
    private SysPinConfiguration plannedPinConfiguration;

    // Flag used to indicate that the particle is currently being activated
    // (Causes ParticleAttributes to behave differently for this particle than
    // for others)
    public bool isActive = false;

    // Graphical information
    // TODO: Cache neighbor information in particles instead of this
    public ParticlePinGraphicState gCircuit;

    public Particle(ParticleSystem system, Vector2Int pos, Direction compassDir = Direction.NONE, bool chirality = true)
    {
        this.system = system;
        int currentRound = system.CurrentRound;

        // Start contracted
        tailPosHistory = new ValueHistory<Vector2Int>(pos, currentRound);
        if (compassDir == Direction.NONE)
            compassDir = DirectionHelpers.Cardinal(0);
        expansionDirHistory = new ValueHistory<Direction>(Direction.NONE, currentRound);
        pos_head = pos;
        pos_tail = pos;
        exp_isExpanded = false;
        exp_expansionDir = Direction.NONE;

        comDir = compassDir;
        this.chirality = chirality;

        activeBonds = new BitVector32(1023);    // All flags set to true
        activeBondHistory = new ValueHistory<int>(activeBonds.Data, currentRound);
        markedBonds = new BitVector32(0);       // All flags are set to false
        markedBondHistory = new ValueHistory<int>(markedBonds.Data, currentRound);

        // Graphics
        // Initialize color
        mainColorHistory = new ValueHistory<Color>(mainColor, currentRound);
        mainColorSetHistory = new ValueHistory<bool>(mainColorSet, currentRound);

        // Add particle to the system and update the visuals of the particle
        graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
    }

    /// <summary>
    /// Initialization to be called after the <see cref="ParticleAlgorithm"/>
    /// has been bound to this particle.
    /// </summary>
    public void InitWithAlgorithm()
    {
        int maxNumPins = algorithm.PinsPerEdge * 10;
        int currentRound = system.CurrentRound;
        pinConfiguration = new SysPinConfiguration(this, algorithm.PinsPerEdge);
        pinConfigurationHistory = new ValueHistoryPinConfiguration(pinConfiguration, currentRound);
        receivedBeeps = new BitArray(maxNumPins);
        receivedMessages = new Message[maxNumPins];
        plannedBeeps = new BitArray(maxNumPins);
        plannedMessages = new Message[maxNumPins];
        receivedBeepsHistory = new ValueHistoryBitArray((BitArray)receivedBeeps.Clone(), currentRound);
        receivedMessagesHistory = new ValueHistoryMessage[maxNumPins];
        plannedBeepsHistory = new ValueHistory<bool>[maxNumPins];
        plannedMessageHistory = new ValueHistoryMessage[maxNumPins];
        partitionSetColorHistory = new ValueHistory<Color>[maxNumPins];
        partitionSetColorOverrideHistory = new ValueHistory<bool>[maxNumPins];
        partitionSetColors = new Color[maxNumPins];
        partitionSetColorsOverride = new bool[maxNumPins];
        for (int i = 0; i < maxNumPins; i++)
        {
            receivedMessagesHistory[i] = new ValueHistoryMessage(null, currentRound);
            plannedBeepsHistory[i] = new ValueHistory<bool>(false, currentRound);
            plannedMessageHistory[i] = new ValueHistoryMessage(null, currentRound);
            partitionSetColorHistory[i] = new ValueHistory<Color>(new Color(), currentRound);
            partitionSetColorOverrideHistory[i] = new ValueHistory<bool>(false, currentRound);
            partitionSetColors[i] = new Color();
            partitionSetColorsOverride[i] = false;
        }
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

    public void ActivateMove()
    {
        isActive = true;
        algorithm.ActivateMove();
        isActive = false;
    }

    public void ActivateBeep()
    {
        isActive = true;
        algorithm.ActivateBeep();
        isActive = false;
    }


    /**
     * State information retrieval
     * 
     * These methods return the latest known state of the Particle, not
     * including the planned actions stored by the simulator.
     * During an activation, these will return the Particle's state at
     * the beginning of the round.
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
    /// if it is expanded, otherwise <see cref="Direction.NONE"/>.</returns>
    public Direction HeadDirection()
    {
        return exp_expansionDir;
    }

    /// <summary>
    /// Returns the local direction pointing from the particle's head towards its tail.
    /// </summary>
    /// <returns>The local direction pointing from the particle's head towards its tail,
    /// if it is expanded, otherwise <see cref="Direction.NONE"/>.</returns>
    public Direction TailDirection()
    {
        return exp_isExpanded ? exp_expansionDir.Opposite() : Direction.NONE;
    }

    /// <summary>
    /// Converts the particle's local direction that points from its tail towards its
    /// head into a global direction.
    /// </summary>
    /// <returns>The global direction pointing from this particle's tail towards its head,
    /// if it is expanded, otherwise <see cref="Direction.NONE"/>.</returns>
    public Direction GlobalHeadDirection()
    {
        return exp_isExpanded ? ParticleSystem_Utils.LocalToGlobalDir(exp_expansionDir, comDir, chirality) : Direction.NONE;
    }

    // For IParticleState interface with rendering system
    public int GlobalHeadDirectionInt()
    {
        return GlobalHeadDirection().ToInt();
    }

    /// <summary>
    /// Converts the particle's local direction that points from its head towards its
    /// tail into a global direction.
    /// </summary>
    /// <returns>The global direction pointing from this particle's head towards its tail,
    /// if it is expanded, otherwise <see cref="Direction.NONE"/>.</returns>
    public Direction GlobalTailDirection()
    {
        return exp_isExpanded ? ParticleSystem_Utils.LocalToGlobalDir(exp_expansionDir.Opposite(), comDir, chirality) : Direction.NONE;
    }


    /**
     * Bonds
     */

    /// <summary>
    /// Marks the bond with the given label to be released before the scheduled movement.
    /// <para>See <see cref="BondActive(int)"/>.</para>
    /// </summary>
    /// <param name="label">The local label of the bond to be released.</param>
    public void ReleaseBond(int label)
    {
        activeBonds[1 << label] = false;
    }

    /// <summary>
    /// Checks if the bond at the given label is still marked as active or
    /// will be released.
    /// <para>See <see cref="ReleaseBond(int)"/>.</para>
    /// </summary>
    /// <param name="label">The local label of the bond to be checked.</param>
    /// <returns><c>true</c> if and only if the bond at the given label is
    /// still marked as active.</returns>
    public bool BondActive(int label)
    {
        return activeBonds[1 << label];
    }

    /// <summary>
    /// Marks the bond with the given label to have special behavior.
    /// This means that it will be pulled with an expansion movement
    /// or transferred during a handover contraction.
    /// <para>See <see cref="BondMarked(int)"/>.</para>
    /// </summary>
    /// <param name="label">The local label of the bond to be marked.</param>
    public void MarkBond(int label)
    {
        markedBonds[1 << label] = true;
    }

    /// <summary>
    /// Checks if the bond at the given label is marked for special
    /// behavior.
    /// <para>See <see cref="MarkBond(int)"/>.</para>
    /// </summary>
    /// <param name="label">The local label of the bond to be checked.</param>
    /// <returns><c>true</c> if and only if the bond at the given label has
    /// been marked.</returns>
    public bool BondMarked(int label)
    {
        return markedBonds[1 << label];
    }


    /**
     * Pin configuration
     */

    /// <summary>
    /// Returns a copy of the current pin configuration.
    /// <para>
    /// This copy can be used to check which partition sets
    /// have received beeps or messages in the last round.
    /// </para>
    /// </summary>
    /// <returns>A copy of the pin configuration at the
    /// start of the current round.</returns>
    public SysPinConfiguration GetCurrentPinConfiguration()
    {
        SysPinConfiguration current = pinConfiguration.Copy();
        current.isCurrent = true;
        return current;
    }

    /// <summary>
    /// Sets the pin configuration to be applied at the end
    /// of the current round.
    /// <para>
    /// This configuration must match the expansion state of
    /// the particle at the end of the round.
    /// </para>
    /// <para>
    /// After setting it to be the planned configuration, it
    /// can be used to send beeps and messages. If it is
    /// altered, its ability to send data expires. Setting
    /// the planned pin configuration to a different value
    /// after sending beeps or messages nullifies the
    /// previously sent data. Partition set colors have the
    /// same behavior.
    /// </para>
    /// </summary>
    /// <param name="pc">The new pin configuration.</param>
    public void SetPlannedPinConfiguration(SysPinConfiguration pc)
    {
        if (!pc.isPlanned)
        {
            if (hasPlannedBeeps)
            {
                Debug.LogWarning("Setting planned pin configuration after sending data erases the sent data.");
                ResetPlannedBeepsAndMessages();
            }
            if (!pc.isCurrent)
                ResetAllPartitionSetColors();
        }
        plannedPinConfiguration = pc.Copy();
        pc.isPlanned = true;
    }

    /// <summary>
    /// Returns the currently planned pin configuration for
    /// the end of the round. Equivalent to
    /// <see cref="PlannedPinConfiguration"/>, but part of the
    /// algorithm API.
    /// </summary>
    /// <returns>The pin configuration planned to be applied at
    /// the end of the current round.</returns>
    public SysPinConfiguration GetPlannedPinConfiguration()
    {
        if (!(plannedPinConfiguration is null))
        {
            SysPinConfiguration planned = plannedPinConfiguration.Copy();
            planned.isPlanned = true;
            return planned;
        }
        else
        {
            return null;
        }
        
    }


    /**
     * Predicted state information retrieval.
     * 
     * These methods provide a way for the developer to access the
     * updated particle state during the Activate() method, based
     * on the actions that are currently planned.
     */

    /// <summary>
    /// Like <see cref="IsExpanded"/>, but returns the predicted value
    /// for after the round.
    /// </summary>
    /// <returns><c>true</c> if and only if the particle will be
    /// expanded after the current round if its planned movements succeed.</returns>
    public bool IsExpanded_After()
    {
        if (scheduledMovement != null)
        {
            return predictIsExpanded;
        }
        else
        {
            return IsExpanded();
        }
    }

    /// <summary>
    /// Like <see cref="IsContracted"/>, but returns the predicted value
    /// for after the round.
    /// </summary>
    /// <returns><c>true</c> if and only if the particle will be
    /// contracted after the current round if its planned movements succeed.</returns>
    public bool IsContracted_After()
    {
        if (scheduledMovement != null)
        {
            return !predictIsExpanded;
        }
        else
        {
            return IsContracted();
        }
    }

    /// <summary>
    /// Like <see cref="HeadDirection"/>, but returns the predicted value
    /// for after the round.
    /// </summary>
    /// <returns>The local direction pointing from the particle's tail towards its head,
    /// if it is expanded, otherwise <see cref="Direction.NONE"/>.</returns>
    public Direction HeadDirection_After()
    {
        if (scheduledMovement != null)
        {
            return predictHeadDir;
        }
        else
        {
            return HeadDirection();
        }
    }

    /// <summary>
    /// Like <see cref="TailDirection"/>, but returns the predicted value
    /// for after the round.
    /// </summary>
    /// <returns>The local direction pointing from the particle's head towards its tail,
    /// if it is expanded, otherwise <see cref="Direction.NONE"/>.</returns>
    public Direction TailDirection_After()
    {
        if (scheduledMovement != null)
        {
            return predictTailDir;
        }
        else
        {
            return TailDirection();
        }
    }

    /**
     * Visualization
     */

    public int GetCircuitPinsPerSide()
    {
        return algorithm.PinsPerEdge;
    }

    /// <summary>
    /// Returns the main color of this particle.
    /// <para>
    /// Call <see cref="IsParticleColorSet"/> to check if this
    /// color has been set or not. If not, the particle's color
    /// should default to some other value.
    /// </para>
    /// </summary>
    /// <returns>The current main color of this particle.</returns>
    public Color GetParticleColor()
    {
        return mainColor;
    }

    /// <summary>
    /// Sets this particle's main color.
    /// <para>
    /// Should only be called during a simulation, i.e., while
    /// the system is in the tracking state.
    /// </para>
    /// </summary>
    /// <param name="c">The new main color of the particle.</param>
    public void SetParticleColor(Color c)
    {
        if (inReinitialize)
            return;
        mainColor = c;
        mainColorSet = true;
        mainColorHistory.RecordValueInRound(c, system.CurrentRound);
        mainColorSetHistory.RecordValueInRound(true, system.CurrentRound);
        graphics.SetParticleColor(c);
    }

    /// <summary>
    /// Resets the particle's main color to its default value.
    /// <para>
    /// Should only be called during a simulation, i.e., while
    /// the system is in the tracking state.
    /// </para>
    /// </summary>
    public void ResetParticleColor()
    {
        if (inReinitialize)
            return;
        mainColorSet = false;
        mainColorSetHistory.RecordValueInRound(false, system.CurrentRound);
        graphics.ClearParticleColor();
    }

    /// <summary>
    /// Checks whether the particle's main color has been overwritten.
    /// </summary>
    /// <returns><c>true</c> if and only if the main color has been
    /// overwritten by the particle algorithm.</returns>
    public bool IsParticleColorSet()
    {
        if (inReinitialize)
            return false;
        return mainColorSet;
    }

    /// <summary>
    /// Overrides the specified partition set's color with the
    /// given color.
    /// <para>
    /// The circuit to which the partition set belongs will be
    /// displayed with this color unless its color has been
    /// overridden by another partition set already.
    /// This method must only be called when the system is in
    /// a tracking state.
    /// </para>
    /// <para>
    /// See also <seealso cref="ResetPartitionSetColor(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="idx">The index of the partition set.</param>
    /// <param name="color">The color to give to the partition set.</param>
    public void SetPartitionSetColor(int idx, Color color)
    {
        partitionSetColors[idx] = color;
        partitionSetColorsOverride[idx] = true;
        partitionSetColorHistory[idx].RecordValueInRound(color, system.CurrentRound);
        partitionSetColorOverrideHistory[idx].RecordValueInRound(true, system.CurrentRound);
    }

    /// <summary>
    /// Resets the color override of the specified partition set.
    /// <para>
    /// This method must only be called when the system is in a
    /// tracking state.
    /// </para>
    /// <para>
    /// See also <seealso cref="SetPartitionSetColor(int, Color)"/>,
    /// <seealso cref="ResetAllPartitionSetColors"/>.
    /// </para>
    /// </summary>
    /// <param name="idx">The index of the partition set.</param>
    public void ResetPartitionSetColor(int idx)
    {
        if (partitionSetColorsOverride[idx])
        {
            partitionSetColors[idx] = Color.black;
            partitionSetColorsOverride[idx] = false;
            partitionSetColorHistory[idx].RecordValueInRound(Color.black, system.CurrentRound);
            partitionSetColorOverrideHistory[idx].RecordValueInRound(false, system.CurrentRound);
        }
    }

    /// <summary>
    /// Resets the colors of all partition sets.
    /// <para>
    /// This method must only be called when the system is in
    /// a tracking state.
    /// </para>
    /// <para>
    /// See also <seealso cref="ResetPartitionSetColor(int)"/>.
    /// </para>
    /// </summary>
    public void ResetAllPartitionSetColors()
    {
        for (int i = 0; i < partitionSetColors.Length; i++)
            ResetPartitionSetColor(i);
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
    /// Expands this particle in the specified local direction and moves
    /// it by the specified global offset.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    /// <param name="locDir">The local direction into which this particle should expand.</param>
    /// <param name="offset">The global offset by which this particle moves in the system.</param>
    public void Apply_Expand(Direction locDir, Vector2Int offset)
    {
        exp_isExpanded = true;
        exp_expansionDir = locDir;
        pos_head = ParticleSystem_Utils.GetNbrInDir(pos_head, ParticleSystem_Utils.LocalToGlobalDir(locDir, comDir, chirality)) + offset;
        pos_tail += offset;
        expansionDirHistory.RecordValueInRound(exp_expansionDir, system.CurrentRound);
        tailPosHistory.RecordValueInRound(pos_tail, system.CurrentRound);
    }

    /// <summary>
    /// Expands this particle in the specified local direction.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    /// <param name="locDir">The local direction into which this particle should expand.</param>
    public void Apply_Expand(Direction locDir)
    {
        Apply_Expand(locDir, Vector2Int.zero);
    }

    /// <summary>
    /// Contracts this particle into the node occupied by its head and
    /// moves the particle by the specified global offset.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    /// <param name="offset">The global offset by which this particle
    /// moves in the system.</param>
    public void Apply_ContractHead(Vector2Int offset)
    {
        exp_isExpanded = false;
        exp_expansionDir = Direction.NONE;
        pos_head += offset;
        pos_tail = pos_head;
        tailPosHistory.RecordValueInRound(pos_tail, system.CurrentRound);
        expansionDirHistory.RecordValueInRound(Direction.NONE, system.CurrentRound);
    }

    /// <summary>
    /// Contracts this particle into the node occupied by its head.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    public void Apply_ContractHead()
    {
        Apply_ContractHead(Vector2Int.zero);
    }

    /// <summary>
    /// Contracts this particle into the node occupied by its tail and
    /// moves the particle by the specified global offset.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    /// <param name="offset">The global offset by which this particle
    /// moves in the system.</param>
    public void Apply_ContractTail(Vector2Int offset)
    {
        exp_isExpanded = false;
        exp_expansionDir = Direction.NONE;
        pos_tail += offset;
        pos_head = pos_tail;
        expansionDirHistory.RecordValueInRound(Direction.NONE, system.CurrentRound);
    }

    /// <summary>
    /// Contracts this particle into the node occupied by its tail.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    public void Apply_ContractTail()
    {
        Apply_ContractTail(Vector2Int.zero);
    }

    // TODO: Check if we need to do anything else in these 3 methods

    /// <summary>
    /// Same as <see cref="Apply_Expand(Direction)"/>.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid.
    /// </remarks>
    /// <param name="locDir">The local direction into which to expand.</param>
    public void Apply_PushHandover(Direction locDir)
    {
        Apply_Expand(locDir);
    }

    // TODO: Remove unused parameters?

    /// <summary>
    /// Same as <see cref="Apply_ContractHead"/>.
    /// </summary>
    /// <remarks>
    /// The method will not check if this operation is valid and the
    /// <paramref name="locDir"/> parameter is not used.
    /// </remarks>
    /// <param name="locDir">The local direction from where to pull the
    /// neighbor particle, relative to this particle's tail.</param>
    public void Apply_PullHandoverHead(Direction locDir)
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
    public void Apply_PullHandoverTail(Direction locDir)
    {
        Apply_ContractTail();
    }

    /// <summary>
    /// Updates the current pin configuration to the one that was
    /// planned for this round. If no configuration was planned and
    /// the particle has moved, the pins are reset to a singleton
    /// pattern. Also resets the planned pin configuration to
    /// <c>null</c> and resets the received beeps and messages.
    /// </summary>
    public void ApplyPlannedPinConfiguration()
    {
        SysPinConfiguration newPC = null;
        if (!(plannedPinConfiguration is null))
        {
            // Have a planned pin configuration, try to apply it
            if (plannedPinConfiguration.HeadDirection != exp_expansionDir)
            {
                throw new System.InvalidOperationException("Particle's planned pin configuration has head direction " + plannedPinConfiguration.HeadDirection + ", but particle's head direction is " + exp_expansionDir);
            }
            newPC = plannedPinConfiguration;
        }
        else
        {
            // Have no planned configuration, generate default one if necessary
            if (pinConfiguration.HeadDirection != exp_expansionDir)
            {
                // Have moved
                newPC = new SysPinConfiguration(this, algorithm.PinsPerEdge, exp_expansionDir);
                ResetAllPartitionSetColors();
            }
        }
        if (!(newPC is null))
        {
            pinConfigurationHistory.RecordValueInRound(newPC, system.CurrentRound);
            pinConfiguration = newPC;
        }
        pinConfiguration.isCurrent = true;
        pinConfiguration.isPlanned = false;
        plannedPinConfiguration = null;

        ResetReceivedBeepsAndMessages();
    }


    /**
     * Methods used by the system to read, set and reset planned actions.
     */

    private void SetScheduledMovement(ParticleAction a)
    {
        scheduledMovement = a;
        // Update predicted information
        switch (a.type)
        {
            case ActionType.EXPAND:
            case ActionType.PUSH:
                predictIsExpanded = true;
                predictHeadDir = a.localDir;
                predictTailDir = predictHeadDir.Opposite();
                break;
            case ActionType.CONTRACT_HEAD:
            case ActionType.CONTRACT_TAIL:
            case ActionType.PULL_HEAD:
            case ActionType.PULL_TAIL:
                predictIsExpanded = false;
                predictHeadDir = Direction.NONE;
                predictTailDir = Direction.NONE;
                break;
        }
    }

    private void ResetScheduledMovement()
    {
        scheduledMovement = null;
    }

    /// <summary>
    /// Stores the active and marked bond info in the particle's history
    /// and resets their values for the next round. All bonds are active
    /// again and no bonds are marked after this method is called.
    /// </summary>
    public void StoreAndResetMovementInfo()
    {
        activeBondHistory.RecordValueInRound(activeBonds.Data, system.CurrentRound);
        markedBondHistory.RecordValueInRound(markedBonds.Data, system.CurrentRound);
        activeBonds = new BitVector32(1023);    // All flags set to true
        markedBonds = new BitVector32(0);       // All flags set to false
    }

    /// <summary>
    /// Checks if the specified partition set has received
    /// a beep.
    /// </summary>
    /// <param name="idx">ID of the partition set to check.</param>
    /// <returns><c>true</c> if and only if the partition set with
    /// ID <paramref name="idx"/> has received a beep.</returns>
    public bool HasReceivedBeep(int idx)
    {
        return receivedBeeps[idx];
    }

    /// <summary>
    /// Checks if the specified partition set has received
    /// a message.
    /// </summary>
    /// <param name="idx">ID of the partition set to check.</param>
    /// <returns><c>true</c> if and only if the partition set with
    /// ID <paramref name="idx"/> has received a message.</returns>
    public bool HasReceivedMessage(int idx)
    {
        return receivedMessages[idx] != null;
    }

    /// <summary>
    /// Returns the message received by the specified
    /// partition set.
    /// </summary>
    /// <param name="idx">The ID of the partition set.</param>
    /// <returns>A <see cref="Message"/> instance if the specified
    /// partition set has received one, otherwise <c>null</c>.</returns>
    public Message GetReceivedMessage(int idx)
    {
        Message rec = receivedMessages[idx];
        if (rec == null)
        {
            return null;
        }
        else
        {
            return rec.Copy();
        }
    }

    /// <summary>
    /// Checks if the specified partition set has planned a beep.
    /// </summary>
    /// <param name="idx">ID of the partition set to check.</param>
    /// <returns><c>true</c> if and only if the partition set of the
    /// planned pin configuration with ID <paramref name="idx"/>
    /// has planned a beep.</returns>
    public bool HasPlannedBeep(int idx)
    {
        return plannedBeeps[idx];
    }

    /// <summary>
    /// Checks if the specified partition set has planned a message.
    /// </summary>
    /// <param name="idx">ID of the partition set to check.</param>
    /// <returns><c>true</c> if and only if the partition set of the
    /// planned pin configuration with ID <paramref name="idx"/>
    /// has planned a message.</returns>
    public bool HasPlannedMessage(int idx)
    {
        return plannedMessages[idx] != null;
    }

    /// <summary>
    /// Returns the message planned for the specified partition set,
    /// if it exists.
    /// </summary>
    /// <param name="idx">ID of the partition set from which
    /// to get the message.</param>
    /// <returns>The message planned for the specified partition set,
    /// if one has been planned, otherwise <c>null</c>.</returns>
    public Message GetPlannedMessage(int idx)
    {
        Message msg = plannedMessages[idx];
        return msg != null ? msg.Copy() : null;
    }

    /// <summary>
    /// Sets the flag that the partition set with the given ID
    /// has received a beep.
    /// </summary>
    /// <param name="idx">The ID of the partition set to
    /// receive the beep.</param>
    public void ReceiveBeep(int idx)
    {
        receivedBeeps[idx] = true;
    }

    /// <summary>
    /// Stores the given message to be recceived by the partition
    /// set with the given ID.
    /// </summary>
    /// <param name="idx">The ID of the partition set to
    /// receive the message.</param>
    /// <param name="msg">The message to be received.</param>
    public void ReceiveMessage(int idx, Message msg)
    {
        receivedMessages[idx] = msg;
    }

    /// <summary>
    /// Sets the flag that the partition set of the planned
    /// pin configuration with the given ID has received a beep.
    /// </summary>
    /// <param name="idx">The ID of the planned partition set to
    /// send the beep.</param>
    public void PlanBeep(int idx)
    {
        plannedBeeps[idx] = true;
        hasPlannedBeeps = true;
    }

    /// <summary>
    /// Stores the given message to be sent on the partition
    /// set with the given ID.
    /// </summary>
    /// <param name="idx">The ID of the planned partition set to
    /// send the message.</param>
    /// <param name="msg">The message to be sent.</param>
    public void PlanMessage(int idx, Message msg)
    {
        plannedMessages[idx] = msg;
        hasPlannedMessages = true;
    }

    public void ResetPlannedBeepsAndMessages()
    {
        plannedBeeps = new BitArray(algorithm.PinsPerEdge * 10);
        hasPlannedBeeps = false;
        if (hasPlannedMessages)
        {
            for (int i = 0; i < plannedMessages.Length; i++)
            {
                plannedMessages[i] = null;
            }
            hasPlannedMessages = false;
        }
    }

    public void ResetReceivedBeepsAndMessages()
    {
        receivedBeeps = new BitArray(algorithm.PinsPerEdge * 10);
        for (int i = 0; i < receivedMessages.Length; i++)
        {
            receivedMessages[i] = null;
        }
    }

    /// <summary>
    /// Triggers the insertion of the planned and received beeps and
    /// messages into the history. Should be called after each activation,
    /// after the beeps and messages have been distributed.
    /// </summary>
    public void StoreBeepsAndMessages()
    {
        receivedBeepsHistory.RecordValueInRound((BitArray)receivedBeeps.Clone(), system.CurrentRound);
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessagesHistory[i].RecordValueInRound(receivedMessages[i], system.CurrentRound);
            plannedBeepsHistory[i].RecordValueInRound(plannedBeeps[i], system.CurrentRound);
            plannedMessageHistory[i].RecordValueInRound(plannedMessages[i], system.CurrentRound);
        }
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
        Debug.Log("PinConfiguration history:");
        pinConfigurationHistory.Print();
        Debug.Log("Main color history:");
        mainColorHistory.Print();
        mainColorSetHistory.Print();
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

    public bool IsTracking()
    {
        return tailPosHistory.IsTracking();
    }

    public void SetMarkerToRound(int round)
    {
        // Reset position and expansion state
        tailPosHistory.SetMarkerToRound(round);
        expansionDirHistory.SetMarkerToRound(round);

        // Reset pin configuration
        pinConfigurationHistory.SetMarkerToRound(round);
        receivedBeepsHistory.SetMarkerToRound(round);
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessagesHistory[i].SetMarkerToRound(round);
            plannedBeepsHistory[i].SetMarkerToRound(round);
            plannedMessageHistory[i].SetMarkerToRound(round);
            partitionSetColorHistory[i].SetMarkerToRound(round);
            partitionSetColorOverrideHistory[i].SetMarkerToRound(round);
        }

        // Reset visuals
        mainColorHistory.SetMarkerToRound(round);
        mainColorSetHistory.SetMarkerToRound(round);

        // Need to update private fields accordingly
        UpdateInternalState();

        // Reset all ParticleAttributes
        foreach (IParticleAttribute attr in attributes)
        {
            attr.SetMarkerToRound(round);
        }
    }

    // Note for StepBack and StepForward:
    // The individual histories are not synchronized automatically
    // The ParticleSystem is responsible for synchronizing all particles
    // before calling one of these two methods.
    public void StepBack()
    {
        tailPosHistory.StepBack();
        expansionDirHistory.StepBack();

        pinConfigurationHistory.StepBack();
        receivedBeepsHistory.StepBack();
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessagesHistory[i].StepBack();
            plannedBeepsHistory[i].StepBack();
            plannedMessageHistory[i].StepBack();
            partitionSetColorHistory[i].StepBack();
            partitionSetColorOverrideHistory[i].StepBack();
        }

        mainColorHistory.StepBack();
        mainColorSetHistory.StepBack();

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

        pinConfigurationHistory.StepForward();
        receivedBeepsHistory.StepForward();
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessagesHistory[i].StepForward();
            plannedBeepsHistory[i].StepForward();
            plannedMessageHistory[i].StepForward();
            partitionSetColorHistory[i].StepForward();
            partitionSetColorOverrideHistory[i].StepForward();
        }

        mainColorHistory.StepForward();
        mainColorSetHistory.StepForward();

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

        pinConfigurationHistory.ContinueTracking();
        receivedBeepsHistory.ContinueTracking();
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessagesHistory[i].ContinueTracking();
            plannedBeepsHistory[i].ContinueTracking();
            plannedMessageHistory[i].ContinueTracking();
            partitionSetColorHistory[i].ContinueTracking();
            partitionSetColorOverrideHistory[i].ContinueTracking();
        }

        mainColorHistory.ContinueTracking();
        mainColorSetHistory.ContinueTracking();

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

        pinConfigurationHistory.CutOffAtMarker();
        receivedBeepsHistory.CutOffAtMarker();
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessagesHistory[i].CutOffAtMarker();
            plannedBeepsHistory[i].CutOffAtMarker();
            plannedMessageHistory[i].CutOffAtMarker();
            partitionSetColorHistory[i].CutOffAtMarker();
            partitionSetColorOverrideHistory[i].CutOffAtMarker();
        }

        mainColorHistory.CutOffAtMarker();
        mainColorSetHistory.CutOffAtMarker();

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

        pinConfigurationHistory.ShiftTimescale(amount);
        receivedBeepsHistory.ShiftTimescale(amount);
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessagesHistory[i].ShiftTimescale(amount);
            plannedBeepsHistory[i].ShiftTimescale(amount);
            plannedMessageHistory[i].ShiftTimescale(amount);
            partitionSetColorHistory[i].ShiftTimescale(amount);
            partitionSetColorOverrideHistory[i].ShiftTimescale(amount);
        }

        mainColorHistory.ShiftTimescale(amount);
        mainColorSetHistory.ShiftTimescale(amount);

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
    /// <para>
    /// Also loads planned beeps and messages from the history,
    /// which must be reset before starting the next round.
    /// </para>
    /// </summary>
    private void UpdateInternalState()
    {
        pos_tail = tailPosHistory.GetMarkedValue();
        exp_expansionDir = expansionDirHistory.GetMarkedValue();
        exp_isExpanded = exp_expansionDir != Direction.NONE;
        if (exp_isExpanded)
        {
            pos_head = ParticleSystem_Utils.GetNbrInDir(pos_tail, exp_expansionDir);
        }
        else
        {
            pos_head = pos_tail;
        }

        pinConfiguration = pinConfigurationHistory.GetMarkedValue(this);
        receivedBeeps = (BitArray)receivedBeepsHistory.GetMarkedValue().Clone();
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            receivedMessages[i] = receivedMessagesHistory[i].GetMarkedValue();
            plannedBeeps[i] = plannedBeepsHistory[i].GetMarkedValue();
            plannedMessages[i] = plannedMessageHistory[i].GetMarkedValue();
            partitionSetColors[i] = partitionSetColorHistory[i].GetMarkedValue();
            partitionSetColorsOverride[i] = partitionSetColorOverrideHistory[i].GetMarkedValue();
        }

        mainColor = mainColorHistory.GetMarkedValue();
        mainColorSet = mainColorSetHistory.GetMarkedValue();
    }


    /**
     * Saving and loading functionality.
     */

    public ParticleStateSaveData GenerateSaveData()
    {
        ParticleStateSaveData data = new ParticleStateSaveData();

        data.comDir = comDir;
        data.chirality = chirality;

        data.algorithmType = algorithm.GetType().FullName;

        data.tailPositionHistory = tailPosHistory.GenerateSaveData();
        data.expansionDirHistory = expansionDirHistory.GenerateSaveData();

        data.boolAttributes = new List<ParticleAttributeSaveData<bool>>();
        data.dirAttributes = new List<ParticleAttributeSaveData<Direction>>();
        data.intAttributes = new List<ParticleAttributeSaveData<int>>();
        data.enumAttributes = new List<ParticleAttributeEnumSaveData>();
        data.pcAttributes = new List<ParticleAttributePCSaveData>();
        // Fill in the particle attributes ordered by type
        // Must use reflection here
        for (int i = 0; i < attributes.Count; i++)
        {
            System.Type t = attributes[i].GetAttributeType();
            if (t == typeof(int))
            {
                ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                data.intAttributes.Add(aData as ParticleAttributeSaveData<int>);
            }
            else if (t == typeof(bool))
            {
                ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                data.boolAttributes.Add(aData as ParticleAttributeSaveData<bool>);
            }
            else if (t == typeof(Direction))
            {
                ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                data.dirAttributes.Add(aData as ParticleAttributeSaveData<Direction>);
            }
            else if (t == typeof(PinConfiguration))
            {
                ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                data.pcAttributes.Add(aData as ParticleAttributePCSaveData);
            }
            else if (attributes[i].GetType().IsGenericType && attributes[i].GetType().GetGenericTypeDefinition() == typeof(ParticleAttribute_Enum<>))
            {
                ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                data.enumAttributes.Add(aData as ParticleAttributeEnumSaveData);
            }
        }

        data.pinConfigurationHistory = pinConfigurationHistory.GeneratePCSaveData();
        data.receivedBeepsHistory = receivedBeepsHistory.GenerateSaveData();
        data.receivedMessagesHistory = new ValueHistorySaveData<MessageSaveData>[receivedMessagesHistory.Length];
        data.plannedBeepsHistory = new ValueHistorySaveData<bool>[plannedBeepsHistory.Length];
        data.plannedMessagesHistory = new ValueHistorySaveData<MessageSaveData>[plannedMessageHistory.Length];
        for (int i = 0; i < receivedMessagesHistory.Length; i++)
        {
            data.receivedMessagesHistory[i] = receivedMessagesHistory[i].GenerateMessageSaveData();
            data.plannedBeepsHistory[i] = plannedBeepsHistory[i].GenerateSaveData();
            data.plannedMessagesHistory[i] = plannedMessageHistory[i].GenerateMessageSaveData();
        }

        data.mainColorHistory = mainColorHistory.GenerateSaveData();
        data.mainColorSetHistory = mainColorSetHistory.GenerateSaveData();
        data.partitionSetColorHistory = new ValueHistorySaveData<Color>[partitionSetColorHistory.Length];
        data.partitionSetColorOverrideHistory = new ValueHistorySaveData<bool>[partitionSetColorOverrideHistory.Length];
        for (int i = 0; i < partitionSetColorHistory.Length; i++)
        {
            data.partitionSetColorHistory[i] = partitionSetColorHistory[i].GenerateSaveData();
            data.partitionSetColorOverrideHistory[i] = partitionSetColorOverrideHistory[i].GenerateSaveData();
        }

        return data;
    }

    public static Particle CreateFromSaveState(ParticleSystem system, ParticleStateSaveData data)
    {
        // First initialize particle's base data
        Particle p = new Particle(system, data);
        p.isActive = true;
        p.inReinitialize = true;
        // Then create algorithm
        Type algoType = Type.GetType(data.algorithmType);
        algoType.GetConstructor(new Type[] { typeof(Particle) }).Invoke(new object[] { p });
        p.inReinitialize = false;
        p.isActive = false;
        p.InitWithAlgorithm(data);

        // Finally set up the graphics info
        p.graphics.AddParticle();
        p.graphics.Update();
        return p;
    }

    private Particle(ParticleSystem system, ParticleStateSaveData data)
    {
        this.system = system;

        comDir = data.comDir;
        chirality = data.chirality;

        tailPosHistory = new ValueHistory<Vector2Int>(data.tailPositionHistory);
        expansionDirHistory = new ValueHistory<Direction>(data.expansionDirHistory);

        exp_expansionDir = expansionDirHistory.GetMarkedValue();
        exp_isExpanded = exp_expansionDir != Direction.NONE;
        pos_tail = tailPosHistory.GetMarkedValue();
        pos_head = exp_isExpanded ? ParticleSystem_Utils.GetNbrInDir(pos_tail, ParticleSystem_Utils.LocalToGlobalDir(exp_expansionDir, comDir, chirality)) : pos_tail;

        mainColorHistory = new ValueHistory<Color>(data.mainColorHistory);
        mainColorSetHistory = new ValueHistory<bool>(data.mainColorSetHistory);
        mainColor = mainColorHistory.GetMarkedValue();
        mainColorSet = mainColorSetHistory.GetMarkedValue();

        // Add particle to the system and update the visuals of the particle
        graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
    }

    private void InitWithAlgorithm(ParticleStateSaveData data)
    {
        int maxNumPins = algorithm.PinsPerEdge * 10;

        // TODO: Check if saved data even matches the algorithm in terms of array sizes

        pinConfigurationHistory = new ValueHistoryPinConfiguration(data.pinConfigurationHistory);
        pinConfiguration = pinConfigurationHistory.GetMarkedValue(this);

        receivedBeepsHistory = new ValueHistoryBitArray(data.receivedBeepsHistory);
        receivedBeeps = (BitArray)receivedBeepsHistory.GetMarkedValue().Clone();

        plannedBeeps = new BitArray(maxNumPins);
        plannedMessages = new Message[maxNumPins];

        receivedMessagesHistory = new ValueHistoryMessage[maxNumPins];
        plannedBeepsHistory = new ValueHistory<bool>[maxNumPins];
        plannedMessageHistory = new ValueHistoryMessage[maxNumPins];
        partitionSetColorHistory = new ValueHistory<Color>[maxNumPins];
        partitionSetColorOverrideHistory = new ValueHistory<bool>[maxNumPins];
        receivedMessages = new Message[maxNumPins];
        partitionSetColors = new Color[maxNumPins];
        partitionSetColorsOverride = new bool[maxNumPins];
        for (int i = 0; i < maxNumPins; i++)
        {
            receivedMessagesHistory[i] = new ValueHistoryMessage(data.receivedMessagesHistory[i]);
            plannedBeepsHistory[i] = new ValueHistory<bool>(data.plannedBeepsHistory[i]);
            plannedMessageHistory[i] = new ValueHistoryMessage(data.plannedMessagesHistory[i]);
            partitionSetColorHistory[i] = new ValueHistory<Color>(data.partitionSetColorHistory[i]);
            partitionSetColorOverrideHistory[i] = new ValueHistory<bool>(data.partitionSetColorOverrideHistory[i]);

            receivedMessages[i] = receivedMessagesHistory[i].GetMarkedValue();
            plannedBeeps[i] = plannedBeepsHistory[i].GetMarkedValue();
            plannedMessages[i] = plannedMessageHistory[i].GetMarkedValue();
            partitionSetColors[i] = partitionSetColorHistory[i].GetMarkedValue();
            partitionSetColorsOverride[i] = partitionSetColorOverrideHistory[i].GetMarkedValue();
        }

        // Finally, update the attributes
        // The attributes have already been set up by the algorithm, we now just need to fill them with different values
        // We store the attributes provided by the save state in a dictionary for easy access
        Dictionary<string, ParticleAttributeSaveDataBase> savedAttrs = new Dictionary<string, ParticleAttributeSaveDataBase>();
        foreach (var list in new IEnumerable[] { data.boolAttributes, data.dirAttributes, data.intAttributes, data.enumAttributes, data.pcAttributes })
        {
            foreach (ParticleAttributeSaveDataBase a in list)
            {
                savedAttrs.Add(a.name, a);
            }
        }

        foreach (IParticleAttribute myAttr in attributes)
        {
            string name = myAttr.ToString_AttributeName();
            if (!savedAttrs.ContainsKey(name))
            {
                Debug.LogError("Attribute " + name + " not stored in save data.");
                continue;
            }
            // Try filling in the values
            if (!myAttr.RestoreFromSaveData(savedAttrs[name]))
            {
                Debug.LogError("Unable to restore attribute " + name + " from saved data.");
            }
        }
    }
}
