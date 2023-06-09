using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using AS2.Visuals;
using UnityEngine;

namespace AS2.Sim
{

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
    /// Particle part = new Particle(particleSystem, startPosition[, compassDir][, chirality][, initialHeadDir]);
    /// part.isActive = true;
    /// AlgorithmManager.Instance.Instantiate(algorithmId, part);
    /// part.isActive = false;
    /// p.InitWithAlgorithm();
    /// </code>
    /// It is recommended to use the <see cref="ParticleFactory"/> class
    /// instead of doing this manually.
    /// </para>
    /// </summary>
    public class Particle : IParticleState, IReplayHistory
    {
        // References

        /// <summary>
        /// The system containing this particle.
        /// </summary>
        public ParticleSystem system;

        /// <summary>
        /// The algorithm controlling this particle's behavior.
        /// </summary>
        public ParticleAlgorithm algorithm;

        // Graphics

        /// <summary>
        /// An object that serves as the graphical representation
        /// of this particle for the rendering system.
        /// </summary>
        public IParticleGraphicsAdapter graphics;


        /*
         * Data
         */

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

        /// <summary>
        /// Special flag that is set while a particle is being reinitialized on load.
        /// Prevents state changes caused by an algorithm constructor.
        /// </summary>
        private bool inReinitialize = false;

        /*
         * State
         */

        // Position
        // Store tail position in history but always keep head position up to date

        /// <summary>
        /// The history of all global tail positions.
        /// </summary>
        private ValueHistory<Vector2Int> tailPosHistory;
        /// <summary>
        /// The current global head position.
        /// </summary>
        private Vector2Int pos_head;
        /// <summary>
        /// The current global tail position.
        /// </summary>
        private Vector2Int pos_tail;

        // Expansion
        // The expansion direction is the local direction pointing from
        // the particle's tail towards its head

        /// <summary>
        /// The history of all expansion directions.
        /// </summary>
        private ValueHistory<Direction> expansionDirHistory;
        /// <summary>
        /// Indicates whether the particle is currently expanded.
        /// </summary>
        private bool exp_isExpanded;
        /// <summary>
        /// The local direction pointing from the particle's tail towards its head.
        /// </summary>
        private Direction exp_expansionDir;

        // Attributes
        /// <summary>
        /// A list containing the particle's attributes.
        /// </summary>
        private List<IParticleAttribute> attributes = new List<IParticleAttribute>();

        // Bonds
        
        /// <summary>
        /// Flags indicating which bonds should be active. Indices are
        /// local labels. Only the 10 lowest bits are used.
        /// </summary>
        private BitVector32 activeBonds;
        /// <summary>
        /// The history of all active bond settings.
        /// </summary>
        private ValueHistory<int> activeBondHistory;

        /// <summary>
        /// Flags indicating which bonds have been marked for special
        /// behavior, i.e., moving with a particle's head during an
        /// expansion movement.
        /// </summary>
        private BitVector32 markedBonds;
        /// <summary>
        /// The history of all marked bond settings.
        /// </summary>
        private ValueHistory<int> markedBondHistory;

        /// <summary>
        /// Flags indicating which bonds should be visible.
        /// </summary>
        private BitVector32 visibleBonds;
        /// <summary>
        /// The history of all bond visibility settings.
        /// </summary>
        private ValueHistory<int> visibleBondHistory;

        // Pin Configuration

        /// <summary>
        /// The history of all pin configurations.
        /// </summary>
        private ValueHistoryPinConfiguration pinConfigurationHistory;

        /// <summary>
        /// The current pin configuration.
        /// </summary>
        private SysPinConfiguration pinConfiguration;
        /// <summary>
        /// The current pin configuration.
        /// </summary>
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
        /// <summary>
        /// The histories of received beeps for all partition sets.
        /// </summary>
        private ValueHistory<bool>[] receivedBeepsHistory;

        /// <summary>
        /// Array of the currently received messages. Indices are
        /// (local) partition set IDs.
        /// </summary>
        public Message[] receivedMessages;
        /// <summary>
        /// The histories of received messages for all partition sets.
        /// </summary>
        private ValueHistoryMessage[] receivedMessagesHistory;

        /// <summary>
        /// Flags indicating which partition sets of the planned pin
        /// configuration have scheduled sending a beep. Indices
        /// equal (local) partition set IDs.
        /// </summary>
        private BitArray plannedBeeps;
        /// <summary>
        /// The histories of sent beeps for all partition sets.
        /// </summary>
        private ValueHistory<bool>[] plannedBeepsHistory;
        /// <summary>
        /// Indicates whether the particle has planned sending any
        /// beeps in this round.
        /// </summary>
        private bool hasPlannedBeeps = false;

        /// <summary>
        /// Array of the currently planned messages. Indices are
        /// (local) partition set IDs.
        /// </summary>
        private Message[] plannedMessages;
        /// <summary>
        /// The histories of sent messages for all partition sets.
        /// </summary>
        private ValueHistoryMessage[] plannedMessageHistory;
        /// <summary>
        /// Indicates whether the particle has planned sending any
        /// messages in this round.
        /// </summary>
        private bool hasPlannedMessages = false;

        // Neighbor cache
        // Indices are global labels.
        // The two caches are swapped each round such that we always have
        // the cache of the previous round available.
        // The cache is used to determine whether pin connections to the
        // neighbors should be displayed or not.
        /// <summary>
        /// The current neighbors of the particle. Indices are
        /// global labels.
        /// </summary>
        public Particle[] neighborsNew = new Particle[10];
        /// <summary>
        /// The neighbors of the particle in the last round. Indices
        /// are global labels.
        /// </summary>
        public Particle[] neighborsOld = new Particle[10];


        /*
         * Visualization
         */

        // Particle fill color
        /// <summary>
        /// History of particle colors.
        /// </summary>
        private ValueHistory<Color> mainColorHistory;
        /// <summary>
        /// History of flags showing whether the particle color
        /// is set.
        /// </summary>
        private ValueHistory<bool> mainColorSetHistory;
        /// <summary>
        /// The current particle color.
        /// </summary>
        private Color mainColor = new Color(0, 0, 0, 1);
        /// <summary>
        /// Indicates whether the particle color has been set.
        /// </summary>
        private bool mainColorSet = false;

        // Bond and movement info
        /// <summary>
        /// History of joint movement information records.
        /// </summary>
        private ValueHistoryJointMovement jointMovementHistory;
        /// <summary>
        /// History of bond information records.
        /// </summary>
        private ValueHistoryBondInfo bondMovementHistory;


        /*
         * Data used by simulator to coordinate particle actions
         * 
         * This section is used by the particle and the simulator to
         * store information on planned actions such that they can be
         * coordinated and applied easily, and to provide a way for
         * the developer to access predicted information.
         * 
         * Some information is maintained internally by the particle to
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
        /// <summary>
        /// The movement action scheduled during the current simulation round.
        /// </summary>
        private ParticleAction scheduledMovement = null;
        /// <summary>
        /// Indicates whether the particle will be expanded at the end of this
        /// round according to the currently scheduled movement.
        /// </summary>
        private bool predictIsExpanded = false;
        /// <summary>
        /// Predicted tail direction if the particle will be expanded at the
        /// end of this round according to the scheduled movement.
        /// </summary>
        private Direction predictTailDir = Direction.NONE;
        /// <summary>
        /// Predicted head direction if the particle will be expanded at the
        /// end of this round according to the scheduled movement.
        /// </summary>
        private Direction predictHeadDir = Direction.NONE;

        /// <summary>
        /// Flag indicating that the particle switched to automatic
        /// bonds mode, which will cause the bonds to create movements
        /// like in the original movement model, with no joint
        /// movements.
        /// </summary>
        public bool markedForAutomaticBonds;

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
        /// origin if the particle is and stays contracted or if it is
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
        /// Visible bonds indexed by global labels.
        /// </summary>
        public bool[] visibleBondsGlobal = new bool[10];

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
        /// <summary>
        /// The currently planned pin configuration to be applied at the end
        /// of the round.
        /// </summary>
        private SysPinConfiguration plannedPinConfiguration;

        /// <summary>
        /// Flag used to indicate that the particle is currently being activated.
        /// Causes the particle's attributes to behave differently for this particle
        /// than for others to enable the "snapshot" semantics.
        /// </summary>
        public bool isActive = false;


        /*
         * Graphical information
         */

        /// <summary>
        /// An object that represents this particle's pin configuration
        /// and its pin connections (internal and external) so that it
        /// can be rendered correctly.
        /// </summary>
        public ParticlePinGraphicState gCircuit;

        /// <summary>
        /// Stores visualization info about some of the bonds incident to
        /// this particle so that they can be animated correctly.
        /// </summary>
        public List<ParticleBondGraphicState> bondGraphicInfo = new List<ParticleBondGraphicState>();


        /// <summary>
        /// Creates a new particle without an algorithm controlling its behavior.
        /// </summary>
        /// <param name="system">The system containing the new particle.</param>
        /// <param name="pos">The initial grid position of the particle's tail.</param>
        /// <param name="compassDir">The compass direction of the particle.</param>
        /// <param name="chirality">The chirality of the particle. <c>true</c> means
        /// counter-clockwise, <c>false</c> means clockwise.</param>
        /// <param name="initialHeadDir">The initial local head direction if the
        /// particle should be expanded. <see cref="Direction.NONE"/> will make
        /// the particle contracted.</param>
        public Particle(ParticleSystem system, Vector2Int pos, Direction compassDir = Direction.NONE, bool chirality = true, Direction initialHeadDir = Direction.NONE)
        {
            this.system = system;
            int currentRound = system.CurrentRound;

            // Initialize position
            tailPosHistory = new ValueHistory<Vector2Int>(pos, currentRound);
            if (compassDir == Direction.NONE)
                compassDir = DirectionHelpers.Cardinal(0);

            expansionDirHistory = new ValueHistory<Direction>(initialHeadDir, currentRound);
            pos_tail = pos;
            exp_isExpanded = initialHeadDir != Direction.NONE;
            exp_expansionDir = initialHeadDir;
            pos_head = exp_isExpanded ? ParticleSystem_Utils.GetNbrInDir(pos, ParticleSystem_Utils.LocalToGlobalDir(initialHeadDir, compassDir, chirality)) : pos;

            comDir = compassDir;
            this.chirality = chirality;

            activeBonds = new BitVector32(1023);    // All flags set to true
            activeBondHistory = new ValueHistory<int>(activeBonds.Data, currentRound);
            markedBonds = new BitVector32(0);       // All flags set to false
            markedBondHistory = new ValueHistory<int>(markedBonds.Data, currentRound);
            visibleBonds = new BitVector32(1023);   // All flags set to true
            visibleBondHistory = new ValueHistory<int>(visibleBonds.Data, currentRound);

            // Graphics
            // Initialize color
            mainColorHistory = new ValueHistory<Color>(mainColor, currentRound);
            mainColorSetHistory = new ValueHistory<bool>(mainColorSet, currentRound);

            // Initial value is empty, can be replaced by actual bonds when the whole system is initialized
            jointMovementHistory = new ValueHistoryJointMovement(JointMovementInfo.Empty, currentRound);
            bondMovementHistory = new ValueHistoryBondInfo(BondMovementInfoList.Empty, currentRound);

            // Add particle to the system and update the visuals of the particle
            graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
        }

        /// <summary>
        /// Initialization to be called after the <see cref="ParticleAlgorithm"/>
        /// has been bound to this particle by instantiating it.
        /// </summary>
        public void InitWithAlgorithm()
        {
            // Initialize everything that depends on the number of pins
            int maxNumPins = algorithm.PinsPerEdge * 10;
            int currentRound = system.CurrentRound;
            pinConfiguration = new SysPinConfiguration(this, algorithm.PinsPerEdge, exp_expansionDir);
            pinConfigurationHistory = new ValueHistoryPinConfiguration(pinConfiguration, currentRound);
            receivedBeeps = new BitArray(maxNumPins);
            receivedMessages = new Message[maxNumPins];
            plannedBeeps = new BitArray(maxNumPins);
            plannedMessages = new Message[maxNumPins];
            receivedBeepsHistory = new ValueHistory<bool>[maxNumPins];
            receivedMessagesHistory = new ValueHistoryMessage[maxNumPins];
            plannedBeepsHistory = new ValueHistory<bool>[maxNumPins];
            plannedMessageHistory = new ValueHistoryMessage[maxNumPins];
            for (int i = 0; i < maxNumPins; i++)
            {
                receivedBeepsHistory[i] = new ValueHistory<bool>(false, currentRound);
                receivedMessagesHistory[i] = new ValueHistoryMessage(null, currentRound);
                plannedBeepsHistory[i] = new ValueHistory<bool>(false, currentRound);
                plannedMessageHistory[i] = new ValueHistoryMessage(null, currentRound);
            }
        }

        /// <summary>
        /// This is the movement activation method of the particle.
        /// It is implemented by the particle algorithm and should be
        /// called exactly once in each round, before the beep
        /// activation.
        /// </summary>
        public void ActivateMove()
        {
            isActive = true;
            algorithm.ActivateMove();
            isActive = false;
        }

        /// <summary>
        /// This is the beep activation method of the particle.
        /// It is implemented by the particle algorithm and should be
        /// called exactly once in each round, after the movement
        /// activation.
        /// </summary>
        public void ActivateBeep()
        {
            isActive = true;
            algorithm.ActivateBeep();
            isActive = false;
        }

        /// <summary>
        /// Checks whether the algorithm executed by this particle
        /// is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the particle has
        /// finished executing its algorithm.</returns>
        public bool IsFinished()
        {
            return algorithm.IsFinished();
        }


        /*
         * State information retrieval
         * 
         * These methods return the latest known state of the particle, not
         * including the planned actions stored by the simulator.
         * During an activation, these will return the particle's state at
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


        /*
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

        /// <summary>
        /// Sets the visibility of the bond at the given label.
        /// <para>See <see cref="BondVisible(int)"/>.</para>
        /// </summary>
        /// <param name="label">The local label of the bond.</param>
        /// <param name="visible">Whether the bond should be visible.</param>
        public void SetBondVisible(int label, bool visible)
        {
            visibleBonds[1 << label] = visible;
        }

        /// <summary>
        /// Checks if the bond at the given label is currently visible.
        /// <para>See <see cref="SetBondVisible(int, bool)"/>.</para>
        /// </summary>
        /// <param name="label">The local label of the bond to be checked.</param>
        /// <returns><c>true</c> if and only if the bond at the given label
        /// is currently visible.</returns>
        public bool BondVisible(int label)
        {
            return visibleBonds[1 << label];
        }


        /*
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
        /// previously sent data. Partition set colors and positions
        /// can still be changed as long as the pin configuration
        /// itself does not change.
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


        /*
         * Predicted state information retrieval.
         * 
         * These methods provide a way for the developer to access the
         * updated particle state during the ActivateXYZ() methods, based
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


        /*
         * Visualization
         */

        /// <summary>
        /// Returns the number of pins the particle's algorithm
        /// defines for each edge.
        /// </summary>
        /// <returns>The number of pins on each edge.</returns>
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


        /*
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
        /// <param name="offset">The global offset by which this particle moves in the system,
        /// relative to its tail.</param>
        public void Apply_Expand(Direction locDir, Vector2Int offset)
        {
            exp_isExpanded = true;
            exp_expansionDir = locDir;
            pos_tail += offset;
            pos_head = ParticleSystem_Utils.GetNbrInDir(pos_tail, ParticleSystem_Utils.LocalToGlobalDir(locDir, comDir, chirality));
            expansionDirHistory.RecordValueInRound(exp_expansionDir, system.CurrentRound);
            tailPosHistory.RecordValueInRound(pos_tail, system.CurrentRound);

            // Set new pin configuration to be expanded
            ResetPinConfigurationAfterMovement();
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
        /// moves in the system, relative to its head.</param>
        public void Apply_ContractHead(Vector2Int offset)
        {
            exp_isExpanded = false;
            exp_expansionDir = Direction.NONE;
            pos_head += offset;
            pos_tail = pos_head;
            tailPosHistory.RecordValueInRound(pos_tail, system.CurrentRound);
            expansionDirHistory.RecordValueInRound(Direction.NONE, system.CurrentRound);

            // Set new pin configuration to be contracted
            ResetPinConfigurationAfterMovement();
        }

        /// <summary>
        /// Contracts this particle into the node occupied by its tail and
        /// moves the particle by the specified global offset.
        /// </summary>
        /// <remarks>
        /// The method will not check if this operation is valid.
        /// </remarks>
        /// <param name="offset">The global offset by which this particle
        /// moves in the system, relative to its tail.</param>
        public void Apply_ContractTail(Vector2Int offset)
        {
            exp_isExpanded = false;
            exp_expansionDir = Direction.NONE;
            pos_tail += offset;
            pos_head = pos_tail;
            tailPosHistory.RecordValueInRound(pos_tail, system.CurrentRound);
            expansionDirHistory.RecordValueInRound(Direction.NONE, system.CurrentRound);

            // Set new pin configuration to be contracted
            ResetPinConfigurationAfterMovement();
        }

        /// <summary>
        /// Sets the current pin configuration to match the expansion state after
        /// a movement, deleting all received beeps and messages.
        /// </summary>
        private void ResetPinConfigurationAfterMovement()
        {
            plannedPinConfiguration = null;
            ApplyPlannedPinConfiguration();
        }

        /// <summary>
        /// Moves the particle by the given global offset in the grid,
        /// without changing its expansion state.
        /// </summary>
        /// <param name="offset">The global offset by which the
        /// particle should be moved.</param>
        public void Apply_Offset(Vector2Int offset)
        {
            pos_tail += offset;
            pos_head += offset;
            tailPosHistory.RecordValueInRound(pos_tail, system.CurrentRound);
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

        /*
         * Methods used by the system to read, set and reset planned actions.
         */

        /// <summary>
        /// Sets the scheduled movement of this particle to the given action.
        /// </summary>
        /// <param name="a">The new movement action.</param>
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

        /// <summary>
        /// Resets the scheduled movement to <c>null</c>.
        /// Predicted state information is not reset.
        /// </summary>
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
            visibleBondHistory.RecordValueInRound(visibleBonds.Data, system.CurrentRound);
            activeBonds = new BitVector32(1023);    // All flags set to true
            markedBonds = new BitVector32(0);       // All flags set to false
            visibleBonds = new BitVector32(1023);   // All flags set to true

            // Also record finished joint movement visualization info
            BondMovementInfo[] bondMovements = new BondMovementInfo[bondGraphicInfo.Count];
            // Offset the vectors to avoid unnecessary memory consumption
            Vector2Int beforeOffset = Tail() - jmOffset;
            Vector2Int afterOffset = Tail();
            for (int i = 0; i < bondGraphicInfo.Count; i++)
            {
                ParticleBondGraphicState s = bondGraphicInfo[i];
                bondMovements[i] = new BondMovementInfo(s.prevBondPos1 - beforeOffset, s.prevBondPos2 - beforeOffset, s.curBondPos1 - afterOffset, s.curBondPos2 - afterOffset, s.hidden);
            }
            JointMovementInfo movementInfo = new JointMovementInfo(jmOffset, movementOffset, scheduledMovement != null ? scheduledMovement.type : ActionType.NULL);
            BondMovementInfoList bondInfoList = new BondMovementInfoList(bondMovements);
            jointMovementHistory.RecordValueInRound(movementInfo, system.CurrentRound);
            bondMovementHistory.RecordValueInRound(bondInfoList, system.CurrentRound);
        }

        /// <summary>
        /// Returns the currently loaded graphics information for
        /// joint movements.
        /// </summary>
        /// <returns>A <see cref="JointMovementInfo"/> that holds the
        /// joint movement information for the currently
        /// loaded state.</returns>
        public JointMovementInfo GetCurrentMovementGraphicsInfo()
        {
            return jointMovementHistory.GetMarkedValue();
        }

        /// <summary>
        /// Returns the currently loaded graphics information for bonds.
        /// </summary>
        /// <returns>A <see cref="BondMovementInfoList"/> that holds the
        /// bond movement information for the currently loaded state.</returns>
        public BondMovementInfoList GetCurrentBondGraphicsInfo()
        {
            return bondMovementHistory.GetMarkedValue();
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
        /// <param name="pc">The pin configuration on which the send
        /// call was made. Must equal the currently planned pin configuration.</param>
        public void PlanBeep(int idx, SysPinConfiguration pc)
        {
            if (pc is null || !pc.isPlanned || pc != plannedPinConfiguration)
            {
                throw new AlgorithmException(this, "Cannot send beeps in non-planned pin configuration.");
            }
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
        /// <param name="pc">The pin configuration on which the send
        /// call was made. Must equal the currently planned pin configuration.</param>
        public void PlanMessage(int idx, Message msg, SysPinConfiguration pc)
        {
            if (pc is null || !pc.isPlanned || pc != plannedPinConfiguration)
            {
                throw new AlgorithmException(this, "Cannot send messages in non-planned pin configuration.");
            }
            plannedMessages[idx] = msg;
            hasPlannedMessages = true;
        }

        /// <summary>
        /// Deletes all planned beeps and messages to prepare
        /// for planning new ones in the next round.
        /// </summary>
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

        /// <summary>
        /// Deletes all received beeps and messages to prepare for
        /// receiving new ones in the next round.
        /// </summary>
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
            for (int i = 0; i < receivedMessagesHistory.Length; i++)
            {
                receivedBeepsHistory[i].RecordValueInRound(receivedBeeps[i], system.CurrentRound);
                receivedMessagesHistory[i].RecordValueInRound(receivedMessages[i], system.CurrentRound);
                plannedBeepsHistory[i].RecordValueInRound(plannedBeeps[i], system.CurrentRound);
                plannedMessageHistory[i].RecordValueInRound(plannedMessages[i], system.CurrentRound);
            }
        }

        /*
         * Other helper methods
         */

        /// <summary>
        /// Swaps the old and the new neighbor cache so that the
        /// new cache can be written and the current new cache is
        /// turned into the old cache.
        /// </summary>
        public void SwapNeighborCaches()
        {
            Particle[] tmp = neighborsNew;
            neighborsNew = neighborsOld;
            neighborsOld = tmp;
        }

        /*
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

        /// <summary>
        /// Resets the intermediate values of all attributes so that they can be
        /// reused in the next round.
        /// </summary>
        public void ResetAttributeIntermediateValues()
        {
            foreach (IParticleAttribute attr in attributes)
                attr.ResetIntermediateValue();
        }

        // From IParticleState interface

        public IParticleAttribute TryGetAttributeByName(string attrName)
        {
            foreach (IParticleAttribute attr in attributes)
            {
                if (attr.ToString_AttributeName().Equals(attrName))
                    return attr;
            }
            return null;
        }

        public bool Chirality()
        {
            return chirality;
        }

        public Direction CompassDir()
        {
            return comDir;
        }

        public void SetChirality(bool chirality)
        {
            // Do nothing
            Log.Warning("Chirality of particles cannot be changed during simulation.");
        }

        public void SetCompassDir(Direction compassDir)
        {
            // Do nothing
            Log.Warning("Compass direction of particles cannot be changed during simulation.");
        }


        /*
         * Additional IParticleState methods.
         */

        public bool IsAnchor()
        {
            return system.IsAnchor(this);
        }

        public bool MakeAnchor()
        {
            return system.SetAnchor(this);
        }


        /*
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

            // Reset bonds
            activeBondHistory.SetMarkerToRound(round);
            markedBondHistory.SetMarkerToRound(round);
            visibleBondHistory.SetMarkerToRound(round);

            // Reset pin configuration
            pinConfigurationHistory.SetMarkerToRound(round);
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                receivedBeepsHistory[i].SetMarkerToRound(round);
                receivedMessagesHistory[i].SetMarkerToRound(round);
                plannedBeepsHistory[i].SetMarkerToRound(round);
                plannedMessageHistory[i].SetMarkerToRound(round);
            }

            // Reset visuals
            mainColorHistory.SetMarkerToRound(round);
            mainColorSetHistory.SetMarkerToRound(round);

            jointMovementHistory.SetMarkerToRound(round);
            bondMovementHistory.SetMarkerToRound(round);

            // Need to update private fields accordingly
            UpdateInternalState();

            // Reset all ParticleAttributes
            foreach (IParticleAttribute attr in attributes)
            {
                attr.SetMarkerToRound(round);
            }
        }

        // Note for StepBack and StepForward:
        // The individual histories are not synchronized automatically.
        // The ParticleSystem is responsible for synchronizing all particles
        // before calling one of these two methods.
        public void StepBack()
        {
            tailPosHistory.StepBack();
            expansionDirHistory.StepBack();

            activeBondHistory.StepBack();
            markedBondHistory.StepBack();
            visibleBondHistory.StepBack();

            pinConfigurationHistory.StepBack();
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                receivedBeepsHistory[i].StepBack();
                receivedMessagesHistory[i].StepBack();
                plannedBeepsHistory[i].StepBack();
                plannedMessageHistory[i].StepBack();
            }

            mainColorHistory.StepBack();
            mainColorSetHistory.StepBack();

            jointMovementHistory.StepBack();
            bondMovementHistory.StepBack();

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

            activeBondHistory.StepForward();
            markedBondHistory.StepForward();
            visibleBondHistory.StepForward();

            pinConfigurationHistory.StepForward();
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                receivedBeepsHistory[i].StepForward();
                receivedMessagesHistory[i].StepForward();
                plannedBeepsHistory[i].StepForward();
                plannedMessageHistory[i].StepForward();
            }

            mainColorHistory.StepForward();
            mainColorSetHistory.StepForward();

            jointMovementHistory.StepForward();
            bondMovementHistory.StepForward();

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

            activeBondHistory.ContinueTracking();
            markedBondHistory.ContinueTracking();
            visibleBondHistory.ContinueTracking();

            pinConfigurationHistory.ContinueTracking();
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                receivedBeepsHistory[i].ContinueTracking();
                receivedMessagesHistory[i].ContinueTracking();
                plannedBeepsHistory[i].ContinueTracking();
                plannedMessageHistory[i].ContinueTracking();
            }

            mainColorHistory.ContinueTracking();
            mainColorSetHistory.ContinueTracking();

            jointMovementHistory.ContinueTracking();
            bondMovementHistory.ContinueTracking();

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

            activeBondHistory.CutOffAtMarker();
            markedBondHistory.CutOffAtMarker();
            visibleBondHistory.CutOffAtMarker();

            pinConfigurationHistory.CutOffAtMarker();
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                receivedBeepsHistory[i].CutOffAtMarker();
                receivedMessagesHistory[i].CutOffAtMarker();
                plannedBeepsHistory[i].CutOffAtMarker();
                plannedMessageHistory[i].CutOffAtMarker();
            }

            mainColorHistory.CutOffAtMarker();
            mainColorSetHistory.CutOffAtMarker();

            jointMovementHistory.CutOffAtMarker();
            bondMovementHistory.CutOffAtMarker();

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

            activeBondHistory.ShiftTimescale(amount);
            markedBondHistory.ShiftTimescale(amount);
            visibleBondHistory.ShiftTimescale(amount);

            pinConfigurationHistory.ShiftTimescale(amount);
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                receivedBeepsHistory[i].ShiftTimescale(amount);
                receivedMessagesHistory[i].ShiftTimescale(amount);
                plannedBeepsHistory[i].ShiftTimescale(amount);
                plannedMessageHistory[i].ShiftTimescale(amount);
            }

            mainColorHistory.ShiftTimescale(amount);
            mainColorSetHistory.ShiftTimescale(amount);

            jointMovementHistory.ShiftTimescale(amount);
            bondMovementHistory.ShiftTimescale(amount);

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
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                receivedBeeps[i] = receivedBeepsHistory[i].GetMarkedValue();
                receivedMessages[i] = receivedMessagesHistory[i].GetMarkedValue();
                plannedBeeps[i] = plannedBeepsHistory[i].GetMarkedValue();
                plannedMessages[i] = plannedMessageHistory[i].GetMarkedValue();
                if (plannedMessages[i] != null)
                    hasPlannedMessages = true;
            }

            mainColor = mainColorHistory.GetMarkedValue();
            mainColorSet = mainColorSetHistory.GetMarkedValue();
        }


        /*
         * Saving and loading functionality.
         */

        /// <summary>
        /// Generates a serializable object that contains the current
        /// state and history of this particle.
        /// </summary>
        /// <returns>A serializable object containing the entire
        /// state history of the particle.</returns>
        public ParticleStateSaveData GenerateSaveData()
        {
            ParticleStateSaveData data = new ParticleStateSaveData();

            data.comDir = comDir;
            data.chirality = chirality;

            data.algorithmType = algorithm.GetAlgorithmName();

            data.tailPositionHistory = tailPosHistory.GenerateSaveData();
            data.expansionDirHistory = expansionDirHistory.GenerateSaveData();

            data.boolAttributes = new List<ParticleAttributeSaveData<bool>>();
            data.dirAttributes = new List<ParticleAttributeSaveData<Direction>>();
            data.floatAttributes = new List<ParticleAttributeSaveData<float>>();
            data.intAttributes = new List<ParticleAttributeSaveData<int>>();
            data.enumAttributes = new List<ParticleAttributeEnumSaveData>();
            data.pcAttributes = new List<ParticleAttributePCSaveData>();
            data.stringAttributes = new List<ParticleAttributeSaveData<string>>();

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
                else if (t == typeof(float))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.floatAttributes.Add(aData as ParticleAttributeSaveData<float>);
                }
                else if (t == typeof(string))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.stringAttributes.Add(aData as ParticleAttributeSaveData<string>);
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

            data.activeBondHistory = activeBondHistory.GenerateSaveData();
            data.markedBondHistory = markedBondHistory.GenerateSaveData();
            data.visibleBondHistory = visibleBondHistory.GenerateSaveData();

            data.pinConfigurationHistory = pinConfigurationHistory.GeneratePCSaveData();
            data.receivedBeepsHistory = new ValueHistorySaveData<bool>[receivedBeepsHistory.Length];
            data.receivedMessagesHistory = new ValueHistorySaveData<MessageSaveData>[receivedMessagesHistory.Length];
            data.plannedBeepsHistory = new ValueHistorySaveData<bool>[plannedBeepsHistory.Length];
            data.plannedMessagesHistory = new ValueHistorySaveData<MessageSaveData>[plannedMessageHistory.Length];
            for (int i = 0; i < receivedBeepsHistory.Length; i++)
            {
                data.receivedBeepsHistory[i] = receivedBeepsHistory[i].GenerateSaveData();
                data.receivedMessagesHistory[i] = receivedMessagesHistory[i].GenerateMessageSaveData();
                data.plannedBeepsHistory[i] = plannedBeepsHistory[i].GenerateSaveData();
                data.plannedMessagesHistory[i] = plannedMessageHistory[i].GenerateMessageSaveData();
            }

            data.mainColorHistory = mainColorHistory.GenerateSaveData();
            data.mainColorSetHistory = mainColorSetHistory.GenerateSaveData();

            data.jointMovementHistory = jointMovementHistory.GenerateSaveData();
            data.bondMovementHistory = bondMovementHistory.GenerateSaveData();

            return data;
        }

        /// <summary>
        /// Initializes a new particle from the data stored in the given
        /// serializable object.
        /// </summary>
        /// <param name="system">The system in which the new particle should
        /// be created.</param>
        /// <param name="data">A serializable object created using
        /// <see cref="GenerateSaveData"/> that stores the particle's
        /// state history.</param>
        /// <returns>A new particle recovered from the serializable object.</returns>
        public static Particle CreateFromSaveState(ParticleSystem system, ParticleStateSaveData data)
        {
            // First initialize particle's base data
            Particle p = new Particle(system, data);
            p.isActive = true;
            p.inReinitialize = true;
            // Then create algorithm
            string algoName = data.algorithmType;
            AlgorithmManager man = AlgorithmManager.Instance;
            if (man.IsAlgorithmKnown(algoName))
            {
                man.Instantiate(algoName, p);
            }
            else
            {
                Log.Error("Error: Algorithm '" + algoName + "' is not known, cannot instantiate algorithm");
            }

            p.inReinitialize = false;
            p.isActive = false;
            p.InitWithAlgorithm(data);

            // Finally set up the graphics info
            p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
            p.graphics.UpdateReset();
            return p;
        }

        /// <summary>
        /// Creates a new particle from the given serializable object.
        /// </summary>
        /// <param name="system">The system in which the particle should
        /// be created.</param>
        /// <param name="data">The serializable object from which the
        /// particle's state history will be recovered.</param>
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

            activeBondHistory = new ValueHistory<int>(data.activeBondHistory);
            markedBondHistory = new ValueHistory<int>(data.markedBondHistory);
            visibleBondHistory = new ValueHistory<int>(data.visibleBondHistory);

            activeBonds = new BitVector32(1023);    // All flags set to true
            markedBonds = new BitVector32(0);       // All flags set to false
            visibleBonds = new BitVector32(1023);   // All flags set to true

            mainColorHistory = new ValueHistory<Color>(data.mainColorHistory);
            mainColorSetHistory = new ValueHistory<bool>(data.mainColorSetHistory);
            mainColor = mainColorHistory.GetMarkedValue();
            mainColorSet = mainColorSetHistory.GetMarkedValue();

            jointMovementHistory = new ValueHistoryJointMovement(data.jointMovementHistory);
            bondMovementHistory = new ValueHistoryBondInfo(data.bondMovementHistory);

            // Add particle to the system and update the visuals of the particle
            graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
        }

        /// <summary>
        /// Equivalent of <see cref="InitWithAlgorithm"/> using data
        /// stored in a serializable object.
        /// </summary>
        /// <param name="data">The serializable object from which the
        /// particle's algorithm information should be recovered.</param>
        private void InitWithAlgorithm(ParticleStateSaveData data)
        {
            int maxNumPins = algorithm.PinsPerEdge * 10;

            // TODO: Check if saved data even matches the algorithm in terms of array sizes

            pinConfigurationHistory = new ValueHistoryPinConfiguration(data.pinConfigurationHistory);
            pinConfiguration = pinConfigurationHistory.GetMarkedValue(this);

            receivedBeeps = new BitArray(maxNumPins);
            receivedMessages = new Message[maxNumPins];
            plannedBeeps = new BitArray(maxNumPins);
            plannedMessages = new Message[maxNumPins];

            receivedBeepsHistory = new ValueHistory<bool>[maxNumPins];
            receivedMessagesHistory = new ValueHistoryMessage[maxNumPins];
            plannedBeepsHistory = new ValueHistory<bool>[maxNumPins];
            plannedMessageHistory = new ValueHistoryMessage[maxNumPins];

            for (int i = 0; i < maxNumPins; i++)
            {
                receivedBeepsHistory[i] = new ValueHistory<bool>(data.receivedBeepsHistory[i]);
                receivedMessagesHistory[i] = new ValueHistoryMessage(data.receivedMessagesHistory[i]);
                plannedBeepsHistory[i] = new ValueHistory<bool>(data.plannedBeepsHistory[i]);
                plannedMessageHistory[i] = new ValueHistoryMessage(data.plannedMessagesHistory[i]);

                receivedBeeps[i] = receivedBeepsHistory[i].GetMarkedValue();
                receivedMessages[i] = receivedMessagesHistory[i].GetMarkedValue();
                plannedBeeps[i] = plannedBeepsHistory[i].GetMarkedValue();
                plannedMessages[i] = plannedMessageHistory[i].GetMarkedValue();
            }

            // Finally, update the attributes
            // The attributes have already been set up by the algorithm, we now just need to fill them with different values
            // We store the attributes provided by the save state in a dictionary for easy access
            Dictionary<string, ParticleAttributeSaveDataBase> savedAttrs = new Dictionary<string, ParticleAttributeSaveDataBase>();
            foreach (var list in new IEnumerable[] { data.boolAttributes, data.dirAttributes, data.floatAttributes, data.intAttributes,
            data.enumAttributes, data.pcAttributes, data.stringAttributes })
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





        // Added by Tobias (put it where you like) ____________________________
        
        public IParticleGraphicsAdapter GetGraphicsAdapter()
        {
            return graphics;
        }

    }

} // namespace AS2.Sim
