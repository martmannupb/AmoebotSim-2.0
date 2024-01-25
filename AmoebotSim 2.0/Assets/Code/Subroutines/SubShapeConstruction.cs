using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.ShapeContainment;
using AS2.Subroutines.PASC;

namespace AS2.Subroutines.ShapeConstruction
{
    /// <summary>
    /// Procedure for constructing a given shape after a placement, rotation
    /// and scale have been determined. Works in any amoebot system and for
    /// any shape.
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge.
    /// </para>
    /// <para>
    /// <b>Usage</b>:
    /// <list type="bullet">
    /// <item>
    ///     Determine a placement representative (placement of the shape's origin),
    ///     let all amoebots know the rotation amount <c>m</c> and make the bits of
    ///     the scale factor available somewhere, e.g., in a binary counter.
    /// </item>
    /// <item>
    ///     Initialize using the <see cref="Init(bool, int)"/> method.
    ///     You must pass the representative flag and the rotation amount. The bits
    ///     of the scale are supplied as a stream during the procedure.
    /// </item>
    /// <item>
    ///     Create a pin configuration and call <see cref="SetupPinConfig(PinConfiguration)"/>, then
    ///     call <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/> to commit the
    ///     pin configuration changes.
    /// </item>
    /// <item>
    ///     Call <see cref="ActivateSend(bool, bool)"/> in the same round to start the procedure.
    ///     The first call does not require any parameters.
    /// </item>
    /// <item>
    ///     After this, call <see cref="ActivateReceive"/>, <see cref="SetupPinConfig(PinConfiguration)"/>,
    ///     <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/> and
    ///     <see cref="ActivateSend(bool, bool)"/> in this order in every round.
    /// </item>
    /// <item>
    ///     After calling <see cref="ActivateReceive"/>, call <see cref="ResetScaleCounter"/> to
    ///     check whether the scale counter has to be reset. If <c>true</c>, the next bit required
    ///     by the procedure is <b>the first bit of the scale</b> again. This is repeated for each
    ///     edge of the target shape.
    /// </item>
    /// <item>
    ///     Before calling <see cref="ActivateSend(bool, bool)"/>, call <see cref="NeedScaleBit"/>
    ///     to check whether or not scale information has to be passed. If it returns <c>true</c>,
    ///     some amoebot must pass the current scale bit as the first parameter so that it can be
    ///     used by the procedure. Additionally, the <b>scale index must be increased</b> and the
    ///     second parameter must be set to <c>true</c> if this is the last bit of the scale.
    ///     It is allowed to set the second parameter to <c>true</c> in some later iteration
    ///     instead as long as the scale bit remains <c>0</c>.
    /// </item>
    /// <item>
    ///     The procedure can be paused after each <see cref="ActivateReceive"/> call and resumed by
    ///     continuing with <see cref="SetupPinConfig(PinConfiguration)"/> in some future round.
    /// </item>
    /// <item>
    ///     Call <see cref="IsFinished"/> after <see cref="ActivateReceive"/> to check whether the
    ///     procedure is finished. If it returns <c>true</c>, call <see cref="IsSuccessful"/> to
    ///     check whether the procedure was successful. If this is the case, you can access the
    ///     element type and index via <see cref="ElementType"/> and <see cref="ElementIndex"/>.
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage example:</b>
    /// <code>
    /// // Round n:
    /// sub.Init(isRepresentative, rotation);
    /// PinConfiguration pc = GetContractedPinConfiguration();
    /// sub.SetupPinConfig(pc);
    /// SetPlannedPinConfiguration(pc);
    /// sub.ActivateSend();
    /// // Go to round n+1
    /// 
    /// // Round n+1:
    /// sub.ActivateReceive();
    /// if (sub.IsFinished()) {
    ///     // Check for success etc.
    /// }
    /// if (sub.ResetScaleCounter()) {
    ///     // Reset the scale counter to the first bit
    /// }
    /// PinConfiguration pc = GetContractedPinConfiguration();
    /// sub.SetupPinConfig(pc);
    /// SetPlannedPinConfiguration(pc);
    /// if (sub.NeedScaleBit()) {
    ///     sub.ActivateSend(scaleBit, isLastBit);
    ///     // Increment scaleBit
    /// } else {
    ///     sub.ActivateSend();
    /// }
    /// </code>
    /// </para>
    /// </summary>

    // Init:
    //  - Set representative
    //  - Set rotation
    //  - Set traversal index to 0
    //  - Reset shape element identifier
    //  - Representative registers itself as origin node

    // Round 0:
    //  Send:
    //  - Find current edge of traversal
    //      - Determine direction and start point
    //  - Establish axis circuits for the edge direction and split at start point
    //  - Start point sends beep in edge direction

    // Round 1:
    //  Receive:
    //  - Amoebots receiving the beep become participants of PASC and initialize their subroutine
    //  - Other amoebots do nothing
    //  Send:
    //  - Setup PASC circuit for two pins
    //  - Setup two global circuits using the other pins
    //  - Send first PASC beep
    //  - Send first bit of scale on first global circuit
    //  - Send beep on second global circuit if this is the scale's MSB

    // Round 2:
    //  Receive:
    //  - Receive PASC beeps
    //      - This creates the PASC bit
    //  - Receive bit of scale
    //  - Update comparison result
    //  - If there was a beep on the second global circuit: Store it
    //  Send:
    //  - If there was a beep on the second global circuit:
    //      - Setup PASC cutoff circuit and send cutoff beep
    //      - Go to round 3
    //  - Else:
    //      - Setup PASC and global circuits
    //      - Send next PASC and scale beeps
    //      - Stay in round 2

    // Round 3:
    //  Receive:
    //  - Receive PASC cutoff beep
    //  - Finalize comparison result (if cutoff beep was 1, distance is greater than scale)
    //  - Categorize to edge or end node
    //  Send:
    //  - Setup global circuit and let edge end point beep if it was created successfully

    // Round 4:
    //  Receive:
    //  - If no beep received: Terminate with failure
    //  - Otherwise: increase traversal counter
    //  - If traversal is not finished yet: Go back to round 0
    //  Send:
    //  - (Only get here if traversal is finished)
    //  - Setup face circuits
    //  - Boundary amoebots send beeps on face circuits

    // Round 5:
    //  Receive:
    //  - If any face receives a beep: Store this information
    //  Send:
    //  - Setup global circuit
    //  - Send beep if face circuit has received beep

    // Round 6:
    //  Receive:
    //  - If beep is received: Terminate with failure
    //  Send:
    //  - Setup face circuits again
    //  - Send beep for first face

    // Round 7:
    //  Receive:
    //  - Assign face identifier if beep was received
    //  - Terminate with success if this was the last face
    //  - Otherwise increment face counter
    //  Send:
    //  - Send beep on face circuit for current face

    public class SubShapeConstruction : Subroutine
    {
        public enum ShapeElement
        {
            NONE = 0,
            NODE = 1,
            EDGE = 2,
            FACE = 3
        }

        public enum ComparisonResult
        {
            NONE = 0,
            EQUAL = 1,
            LESS = 2,
            GREATER = 3
        }

        // This int stores most of the state variables
        // 32 bits encode the state as follows:
        // The lowest 3 bits represent the round counter (possible values 0-7)
        // Bits 3, 4, 5 store the rotation of the shape (possible values 0-5)
        // Bits 6, 7 store the type of shape element we represent (4 possible values)
        // Bits 8, 9 store the comparison result
        // Bit 10 is the PASC participant flag
        // Bit 11 is the flag remembering whether we received a PASC end beep
        // Bit 12 is the flag remembering whether we received a face hole beep
        // Bit 13 is the finished flag
        // Bit 14 is the success flag
        //                         14        13         12          11         10     98           76        543        210
        // xxxx xxxx xxxx xxxx x   x         x          x           x          x      xx           xx        xxx        xxx
        //                         Success   Finished   Face hole   PASC End   PASC   Comparison   Element   Rotation   Round
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_Rotation = 3;
        private const int bit_Element = 6;
        private const int bit_Comparison = 8;
        private const int bit_PASC = 10;
        private const int bit_Cutoff = 11;
        private const int bit_Hole = 12;
        private const int bit_Finished = 13;
        private const int bit_Success = 14;

        ParticleAttribute<int> index;           // Index for traversal edge or face
        ParticleAttribute<int> elementIndex;    // Index of the shape element we represent

        // INT Round                            + 3
        // INT Rotation                         + 3
        // ENUM Shape element type              + 2
        // BOOL PASC participant                + 1
        // BOOL Received termination beep       + 1
        // BOOL Face circuit hole beep          + 1
        // BOOL Finished                        + 1
        // BOOL Success                         + 1
        // INT Traversal/Face index
        // INT Shape element index
        // SUB PASC

        Shape shape;
        SubPASC2 pasc;

        public SubShapeConstruction(Particle p, Shape shape, SubPASC2 pasc = null) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Shape Constr.] State"), 0);
            index = algo.CreateAttributeInt(FindValidAttributeName("[Shape Constr.] Index"), 0);
            elementIndex = algo.CreateAttributeInt(FindValidAttributeName("[Shape Constr.] Element"), -1);
            if (pasc is null)
            {
                this.pasc = new SubPASC2(p);
            }
            else
            {
                this.pasc = pasc;
            }
            this.shape = shape;
        }

        /// <summary>
        /// Initializes the subroutine.
        /// </summary>
        /// <param name="representative"></param>
        /// <param name="rotation"></param>
        public void Init(bool representative, int rotation)
        {
            state.SetValue(0);
            index.SetValue(0);
            elementIndex.SetValue(-1);

            SetRotation(rotation);
            // Representative becomes origin node
            if (representative)
            {
                SetElementType(ShapeElement.NODE);
                elementIndex.SetValue(0);
            }
        }

        /// <summary>
        /// Activation during <see cref="ParticleAlgorithm.ActivateBeep"/> to receive the
        /// beeps sent in the last round. Should always be called before
        /// <see cref="SetupPinConfig(PinConfiguration)"/> and <see cref="ActivateSend"/>,
        /// except in the very first activation, where it should not be called.
        /// </summary>
        public void ActivateReceive()
        {
            if (IsFinished())
                return;

            int round = Round();

            PinConfiguration pc = algo.GetCurrentPinConfiguration();

            if (round == 1)
            {
                // Listen on partition set 0 for axis activation beep
                SetComparison(ComparisonResult.NONE);
                SetStateBit(bit_Cutoff, false);
                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    SetStateBit(bit_PASC, true);
                    SetComparison(ComparisonResult.EQUAL);
                    // Initialize PASC
                    int edge = GetTraversalEdge();
                    bool leader = ElementType() == ShapeElement.NODE && elementIndex.GetCurrentValue() == shape.edges[edge].u;
                    Direction d = GetEdgeDirection(edge);

                    List<Direction> predecessors = new List<Direction>();
                    List<Direction> successors = new List<Direction>() { d };
                    if (!leader)
                        predecessors.Add(d.Opposite());
                    pasc.Init(predecessors, successors, 0, 1, 0, 1, leader);
                }
                else
                {
                    SetStateBit(bit_PASC, false);
                }
            }
            else if (round == 2)
            {
                // PASC participants receive bits
                if (GetStateBit(bit_PASC))
                {
                    pasc.ActivateReceive();
                    bool bitPASC = pasc.GetReceivedBit() != 0;
                    bool bitScale = pc.ReceivedBeepOnPartitionSet(2);

                    // Update comparison result
                    if (bitPASC && !bitScale)
                    {
                        SetComparison(ComparisonResult.GREATER);
                    }
                    else if (!bitPASC && bitScale)
                    {
                        SetComparison(ComparisonResult.LESS);
                    }
                }

                // Check if there was a termination beep
                if (pc.ReceivedBeepOnPartitionSet(3))
                {
                    SetStateBit(bit_Cutoff, true);
                }
            }
            else if (round == 3)
            {
                // Receive PASC cutoff beep
                if (GetStateBit(bit_PASC))
                {
                    pasc.ReceiveCutoffBeep();
                    // Finalize comparison result
                    if (pasc.GetReceivedBit() != 0)
                    {
                        SetComparison(ComparisonResult.GREATER);
                    }

                    // Categorize ourselves
                    int edge = GetTraversalEdge();
                    if (Comparison() == ComparisonResult.LESS && ElementType() == ShapeElement.NONE)
                    {
                        SetElementType(ShapeElement.EDGE);
                        elementIndex.SetValue(edge);
                    }
                    else if (Comparison() == ComparisonResult.EQUAL)
                    {
                        SetElementType(ShapeElement.NODE);
                        elementIndex.SetValue(shape.edges[edge].v);
                    }
                }
            }
            else if (round == 4)
            {
                // If we received no beep on the global circuit: Terminate with failure
                if (!pc.ReceivedBeepOnPartitionSet(2))
                {
                    SetStateBit(bit_Finished, true);
                    SetStateBit(bit_Success, false);
                    return;
                }

                // Otherwise: Increase traversal counter if not finished yet
                if (index.GetCurrentValue() < shape.traversal.Count - 1)
                {
                    index.SetValue(index + 1);
                    SetRound(0);
                }
                // Traversal is finished: Stay in round 4
            }
            else if (round == 5)
            {
                // If any edge amoebot with a face received a beep: Store this info
                if (ElementType() == ShapeElement.EDGE)
                {
                    int edge = elementIndex.GetCurrentValue();
                    if (shape.GetEdgeFaceCorners(edge, out int left, out int right))
                    {
                        if (left != -1 && pc.ReceivedBeepOnPartitionSet(0) || right != -1 && pc.ReceivedBeepOnPartitionSet(1))
                        {
                            SetStateBit(bit_Hole, true);
                        }
                    }
                }
            }
            else if (round == 6)
            {
                // If we received a beep: Terminate with failure
                if (pc.ReceivedBeepOnPartitionSet(2))
                {
                    SetStateBit(bit_Finished, true);
                    SetStateBit(bit_Success, false);
                    return;
                }
            }
            else if (round == 7)
            {
                // Listen for beeps on global circuit 0 to decide if we are part of a face
                if (ElementType() == ShapeElement.NONE && pc.ReceivedBeepOnPartitionSet(0))
                {
                    SetElementType(ShapeElement.FACE);
                    elementIndex.SetValue(index.GetCurrentValue());
                }

                // Increment face counter unless we are done
                if (index.GetCurrentValue() < shape.faces.Count - 1)
                {
                    index.SetValue(index.GetCurrentValue() + 1);
                }
                else
                {
                    SetStateBit(bit_Finished, true);
                    SetStateBit(bit_Success, true);
                }
            }
        }

        /// <summary>
        /// Sets up the required circuits for the next step in the given
        /// pin configuration. This must be called after <see cref="ActivateReceive"/>
        /// and before <see cref="ActivateSend"/>. The given pin configuration
        /// will not be planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to set up. Partition set IDs will
        /// always equal one of the IDs of the contained pins.</param>
        public void SetupPinConfig(PinConfiguration pc)
        {
            if (IsFinished())
                return;

            int round = Round();

            if (round == 0)
            {
                // Establish axis circuit for the current edge
                // Only partition set 0 will be used
                int edge = GetTraversalEdge();
                Direction d = GetEdgeDirection(edge);
                SetupAxisCircuit(pc, d, ElementType() == ShapeElement.NODE && elementIndex.GetCurrentValue() == shape.edges[edge].u);
            }
            else if (round == 1)
            {
                // Participants setup PASC circuit (partition sets 0 and 1)
                if (GetStateBit(bit_PASC))
                {
                    pasc.SetupPC(pc);
                }

                // Setup two global circuits on the other pins (partition sets 2, 3)
                Direction d = GetEdgeDirection(GetTraversalEdge());
                SetupGlobalCircuits(pc, d);
            }
            else if (round == 2)
            {
                if (GetStateBit(bit_PASC))
                {
                    // If there was a cutoff indicator beep: Setup PASC cutoff circuit
                    if (GetStateBit(bit_Cutoff))
                    {
                        pasc.SetupCutoffCircuit(pc);
                    }
                    else
                    {
                        pasc.SetupPC(pc);
                    }
                }

                // Setup two global circuits on the other pins (partition sets 2, 3)
                Direction d = GetEdgeDirection(GetTraversalEdge());
                SetupGlobalCircuits(pc, d);
            }
            else if (round == 3)
            {
                // Setup global circuit
                // (Just use the same pin configuration with 2 global circuits as before)
                Direction d = GetEdgeDirection(GetTraversalEdge());
                SetupGlobalCircuits(pc, d);
            }
            else if (round == 4)
            {
                // Setup face circuits for the first time
                SetupFaceCircuits(pc);
            }
            else if (round == 5)
            {
                // Setup simple global circuit again
                SetupGlobalCircuits(pc, Direction.E);
            }
            else if (round == 6 || round == 7)
            {
                // Setup face circuits again
                SetupFaceCircuits(pc);
            }
        }

        /// <summary>
        /// Activation during <see cref="ParticleAlgorithm.ActivateBeep"/> to send the
        /// beeps required for this step. Must be called after <see cref="ActivateReceive"/>
        /// and <see cref="SetupPinConfig(PinConfiguration)"/> and after the pin configuration
        /// has been planned. Call <see cref="NeedScaleBit"/> to check whether a scale bit
        /// has to be transmitted.
        /// </summary>
        /// <param name="scaleBit">The current bit of the scale factor. Only has to be
        /// set by the amoebot that knows this bit.</param>
        /// <param name="scaleEnd">Whether this bit is the last of the scale factor.
        /// Only has to be set by the amoebot that knows this fact.</param>
        public void ActivateSend(bool scaleBit = false, bool scaleEnd = false)
        {
            if (IsFinished())
                return;

            int round = Round();

            PinConfiguration pc = GetPlannedPC();

            if (round == 0)
            {
                // Start point sends beep on partition set 0
                if (ElementType() == ShapeElement.NODE && elementIndex.GetCurrentValue() == shape.edges[GetTraversalEdge()].u)
                {
                    pc.SendBeepOnPartitionSet(0);
                }

                SetRound(1);
            }
            else if (round == 1)
            {
                // PASC participants send PASC beep
                if (GetStateBit(bit_PASC))
                {
                    pasc.ActivateSend();
                }
                // Send scale bit on first global circuit (pSet 2)
                if (scaleBit)
                {
                    pc.SendBeepOnPartitionSet(2);
                }
                // Send scale end beep on second global circuit (pSet 3)
                if (scaleEnd)
                {
                    pc.SendBeepOnPartitionSet(3);
                }

                SetRound(2);
            }
            else if (round == 2)
            {
                // If we are performing a cutoff: Send PASC cutoff beep and go to round 3
                if (GetStateBit(bit_Cutoff))
                {
                    if (GetStateBit(bit_PASC))
                    {
                        pasc.SendCutoffBeep();
                    }
                    SetRound(3);
                }
                // Otherwise, proceed with PASC and scale
                else
                {
                    if (GetStateBit(bit_PASC))
                    {
                        pasc.ActivateSend();
                    }
                    // Send scale bit on first global circuit (pSet 2)
                    if (scaleBit)
                    {
                        pc.SendBeepOnPartitionSet(2);
                    }
                    // Send scale end beep on second global circuit (pSet 3)
                    if (scaleEnd)
                    {
                        pc.SendBeepOnPartitionSet(3);
                    }
                }
            }
            else if (round == 3)
            {
                // Let edge end point beep if it exists
                int edge = GetTraversalEdge();
                if (ElementType() == ShapeElement.NODE && elementIndex.GetCurrentValue() == shape.edges[edge].v)
                {
                    pc.SendBeepOnPartitionSet(2);
                    pc.SendBeepOnPartitionSet(3);
                }

                SetRound(4);
            }
            else if (round == 4)
            {
                // Boundary amoebots send beeps on the face circuits (edges must do this too!)
                if (ElementType() == ShapeElement.EDGE)
                {
                    int edge = elementIndex.GetCurrentValue();
                    if (shape.GetEdgeFaceCorners(edge, out int left, out int right))
                    {
                        // We have incident faces, check for unoccupied neighbors
                        Direction d = GetEdgeDirection(edge);
                        if (left != -1 && (!algo.HasNeighborAt(d.Rotate60(1)) || !algo.HasNeighborAt(d.Rotate60(2))))
                        {
                            pc.SendBeepOnPartitionSet(0);
                        }
                        if (right != -1 && (!algo.HasNeighborAt(d.Rotate60(-1)) || !algo.HasNeighborAt(d.Rotate60(-2))))
                        {
                            pc.SendBeepOnPartitionSet(1);
                        }
                    }
                }
                // Inner amoebots simply beep if one neighbor is missing
                else if (ElementType() == ShapeElement.NONE)
                {
                    foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
                    {
                        if (!algo.HasNeighborAt(d))
                        {
                            pc.SendBeepOnPartitionSet(0);
                            break;
                        }
                    }
                }

                SetRound(5);
            }
            else if (round == 5)
            {
                // Send beep if face circuit has received beep
                if (GetStateBit(bit_Hole))
                {
                    pc.SendBeepOnPartitionSet(2);
                    pc.SendBeepOnPartitionSet(3);
                }

                SetRound(6);
            }
            else if (round == 6)
            {
                // Reset index for face assignment
                index.SetValue(0);
                if (shape.faces.Count > 0)
                {
                    // Send beep on first face circuit
                    SendFaceBeeps(pc);
                }

                SetRound(7);
            }
            else if (round == 7)
            {
                // Send beep on face circuit for the current face
                SendFaceBeeps(pc);
            }
        }

        /// <summary>
        /// Checks whether a scale bit is required in this round.
        /// Every time a scale bit is sent, the index must be forwarded.
        /// </summary>
        /// <returns><c>true</c> if and only if a scale bit is required
        /// in this send activation.</returns>
        public bool NeedScaleBit()
        {
            return Round() < 3 && !GetStateBit(bit_Cutoff);
        }

        /// <summary>
        /// Checks whether the scale counter has to be reset in this round.
        /// This is necessary after each edge traversal and must be done
        /// before the next sending activation.
        /// </summary>
        /// <returns><c>true</c> if and only if the scale counter should
        /// be reset immediately.</returns>
        public bool ResetScaleCounter()
        {
            return Round() == 0;
        }

        /// <summary>
        /// Helper setting up a simple axis circuit on pin 0 for the given direction.
        /// The resulting partition set has ID 0.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        /// <param name="d">The direction of the axis.</param>
        /// <param name="split">Whether this amoebot should split the circuit.</param>
        private void SetupAxisCircuit(PinConfiguration pc, Direction d, bool split = false)
        {
            if (split)
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(d, 0).Id }, 0);
            }
            else
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(d, 0).Id, pc.GetPinAt(d.Opposite(), algo.PinsPerEdge - 1).Id }, 0);
            }
        }

        /// <summary>
        /// Helper setting up two global circuits such that they do not interfere
        /// with PASC. Their partition set IDs will be 2 and 3.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        /// <param name="d">The direction in which PASC is currently running.</param>
        private void SetupGlobalCircuits(PinConfiguration pc, Direction d)
        {
            bool[] invertedDirs = new bool[] { false, false, false, true, true, true };
            bool invertPins = d == Direction.E || d == Direction.NNE || d == Direction.NNW;
            pc.SetStarConfig(invertPins ? 3 : 0, invertedDirs, 2);
            pc.SetStarConfig(invertPins ? 2 : 1, invertedDirs, 3);
        }

        /// <summary>
        /// Helper for setting up face circuits.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        private void SetupFaceCircuits(PinConfiguration pc)
        {
            if (ElementType() == ShapeElement.EDGE)
            {
                // We belong to an edge
                // Setup one face circuit for each incident face
                // The "left" circuit will have ID 0, the "right" one will have ID 1
                int edge = elementIndex.GetCurrentValue();
                if (!shape.GetEdgeFaceCorners(edge, out int cLeft, out int cRight)) {
                    return;
                }
                Direction d = GetEdgeDirection(edge);
                if (cLeft != -1)
                {
                    Direction d1 = d.Rotate60(1);
                    Direction d2 = d.Rotate60(2);
                    pc.MakePartitionSet(new int[] {
                        pc.GetPinAt(d1, 0).Id,
                        pc.GetPinAt(d1, 1).Id,
                        pc.GetPinAt(d1, 2).Id,
                        pc.GetPinAt(d1, 3).Id,
                        pc.GetPinAt(d2, 0).Id,
                        pc.GetPinAt(d2, 1).Id,
                        pc.GetPinAt(d2, 2).Id,
                        pc.GetPinAt(d2, 3).Id,
                    }, 0);
                    pc.SetPartitionSetPosition(0, new Vector2((d1.ToInt() + 0.5f) * 60, 0.7f));
                }
                if (cRight != -1)
                {
                    Direction d1 = d.Rotate60(-1);
                    Direction d2 = d.Rotate60(-2);
                    pc.MakePartitionSet(new int[] {
                        pc.GetPinAt(d1, 0).Id,
                        pc.GetPinAt(d1, 1).Id,
                        pc.GetPinAt(d1, 2).Id,
                        pc.GetPinAt(d1, 3).Id,
                        pc.GetPinAt(d2, 0).Id,
                        pc.GetPinAt(d2, 1).Id,
                        pc.GetPinAt(d2, 2).Id,
                        pc.GetPinAt(d2, 3).Id,
                    }, 1);
                    pc.SetPartitionSetPosition(1, new Vector2((d1.ToInt() - 0.5f) * 60, 0.7f));
                }
            }
            else if (ElementType() == ShapeElement.NONE)
            {
                // We do not belong to the shape: Setup global circuit with ID 0
                pc.SetToGlobal(0);
            }
        }

        /// <summary>
        /// Helper for sending beeps on face circuits in the correct round.
        /// </summary>
        /// <param name="pc">The pin configuration on which to send the beep.</param>
        private void SendFaceBeeps(PinConfiguration pc)
        {
            if (ElementType() == ShapeElement.EDGE)
            {
                int edge = elementIndex.GetCurrentValue();
                if (shape.GetEdgeFaceCorners(edge, out int left, out int right))
                {
                    Shape.Face face = shape.faces[index.GetCurrentValue()];
                    HashSet<int> nodes = new HashSet<int>() { face.u, face.v, face.w };
                    if (nodes.Contains(shape.edges[edge].u) && nodes.Contains(shape.edges[edge].v))
                    {
                        if (nodes.Contains(left))
                        {
                            pc.SendBeepOnPartitionSet(0);
                        }
                        if (nodes.Contains(right))
                        {
                            pc.SendBeepOnPartitionSet(1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper for reading a single bit from the state integer.
        /// </summary>
        /// <param name="bit">The position of the bit.</param>
        /// <returns>The value of the state bit at position <paramref name="bit"/>.</returns>
        private bool GetStateBit(int bit)
        {
            return (state.GetCurrentValue() & (1 << bit)) != 0;
        }

        /// <summary>
        /// Helper for setting a single bit from the state integer.
        /// </summary>
        /// <param name="bit">The position of the bit.</param>
        /// <param name="value">The new value of the bit.</param>
        private void SetStateBit(int bit, bool value)
        {
            state.SetValue(value ? state.GetCurrentValue() | (1 << bit) : state.GetCurrentValue() & ~(1 << bit));
        }

        /// <summary>
        /// Helper for reading the round number from the state integer.
        /// </summary>
        /// <returns>The current round number.</returns>
        private int Round()
        {
            return state.GetCurrentValue() & 7;
        }

        /// <summary>
        /// Helper for reading the rotation from the state integer.
        /// </summary>
        /// <returns>The number of clockwise 60 degree rotations of the shape.</returns>
        private int Rotation()
        {
            return (state.GetCurrentValue() >> bit_Rotation) & 7;
        }

        /// <summary>
        /// Returns the result of comparing the PASC bit stream
        /// to the scale bit stream.
        /// </summary>
        /// <returns>The comparison result during PASC.</returns>
        private ComparisonResult Comparison()
        {
            int r = (state.GetCurrentValue() >> bit_Comparison) & 3;
            return (ComparisonResult)r;
        }

        /// <summary>
        /// Checks the type of shape element this amoebot represents.
        /// Can be nothing, node, edge or face.
        /// </summary>
        /// <returns>The current type of shape element.</returns>
        public ShapeElement ElementType()
        {
            int e = (state.GetCurrentValue() >> bit_Element) & 3;
            return (ShapeElement)e;
        }

        /// <summary>
        /// Checks the index of the element this amoebot represents.
        /// Use this in conjunction with <see cref="ElementType"/>.
        /// </summary>
        /// <returns>The index of this amoebot's shape element, if
        /// it exists, otherwise <c>-1</c>.</returns>
        public int ElementIndex()
        {
            return elementIndex.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether the current procedure is finished. Should be called after
        /// <see cref="ActivateReceive"/>.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// has finished.</returns>
        public bool IsFinished()
        {
            return GetStateBit(bit_Finished);
        }

        /// <summary>
        /// Checks whether the construction was successful.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// has finished successfully.</returns>
        public bool IsSuccessful()
        {
            return IsFinished() && GetStateBit(bit_Success);
        }

        /// <summary>
        /// Helper for setting the round counter.
        /// </summary>
        /// <param name="round">The new value of the round counter.</param>
        private void SetRound(int round)
        {
            state.SetValue((state.GetCurrentValue() & ~7 | round));
        }

        /// <summary>
        /// Helper for setting the rotation in the state integer.
        /// </summary>
        /// <param name="rotation">The new value of the rotation.</param>
        private void SetRotation(int rotation)
        {
            state.SetValue((state.GetCurrentValue() & ~(7 << bit_Rotation)) | (rotation << bit_Rotation));
        }

        /// <summary>
        /// Helper for setting the comparison result.
        /// </summary>
        /// <param name="result">The new value of the result.</param>
        private void SetComparison(ComparisonResult result)
        {
            int r = (int)result;
            state.SetValue(state.GetCurrentValue() & ~(3 << bit_Comparison) | (r << bit_Comparison));
        }

        /// <summary>
        /// Checks the type of shape element this amoebot represents.
        /// Can be nothing, node, edge or face.
        /// </summary>
        /// <returns>The current type of shape element.</returns>
        public void SetElementType(ShapeElement et)
        {
            int e = (int)et;
            state.SetValue(state.GetCurrentValue() & ~(3 << bit_Element) | (e << bit_Element));
        }

        /// <summary>
        /// Helper for getting the index of the current edge.
        /// </summary>
        /// <returns>The index of the edge in the current traversal.</returns>
        private int GetTraversalEdge()
        {
            return shape.traversal[index.GetCurrentValue()];
        }

        /// <summary>
        /// Helper for getting the direction of the given edge, taking the
        /// shape's rotation into account.
        /// </summary>
        /// <param name="edgeIdx">The index of the edge.</param>
        /// <returns>The rotated direction of the given edge.</returns>
        private Direction GetEdgeDirection(int edgeIdx)
        {
            Shape.Edge edge = shape.edges[edgeIdx];
            return ParticleSystem_Utils.VectorToDirection((Vector2Int)shape.nodes[edge.v] - shape.nodes[edge.u]).Rotate60(Rotation());
        }

        /// <summary>
        /// Wrapper for getting the planned pin configuration and
        /// throwing an exception if there is none.
        /// </summary>
        /// <returns>The currently planned pin configuration.</returns>
        private PinConfiguration GetPlannedPC()
        {
            PinConfiguration pc = algo.GetPlannedPinConfiguration();
            if (pc is null)
            {
                throw new InvalidActionException(particle, "Amoebot has no planned pin configuration");
            }
            return pc;
        }
    }

} // namespace AS2.Subroutines.BinaryOps
