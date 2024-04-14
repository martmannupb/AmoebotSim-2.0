using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.PASC;
using AS2.Subroutines.BinStateHelpers;

namespace AS2.Subroutines.ConvexShapePlacementSearch
{
    /// <summary>
    /// Valid placement search for parallelograms. Finds all valid
    /// placements for a parallelogram extending in the two given
    /// directions for two side lengths given in a binary counter,
    /// rotated by the specified amount.
    /// <para>
    /// It is assumed that the counter storing the side lengths consists of at
    /// least two amoebots (the start must not be equal to the end point).
    /// </para>
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge. This subroutine sometimes uses all pins on one side.
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <list type="bullet">
    /// <item>
    ///     Initialize by calling <see cref="Init(Direction, Direction, int, bool, Direction, Direction, bool, bool, bool, bool)"/>.
    ///     This assumes you have set up a binary counter storing the two shape parameters and knowing their MSBs.
    /// </item>
    /// <item>
    ///     Run <see cref="SetupPC(PinConfiguration)"/>, then <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/>
    ///     and <see cref="ActivateSend"/> to start the procedure.
    /// </item>
    /// <item>
    ///     In the round immediately following a <see cref="ActivateSend"/> call, <see cref="ActivateReceive"/>
    ///     must be called. There can be an arbitrary break until the next pin configuration setup and
    ///     <see cref="ActivateSend"/> call. Continue this until the procedure is finished.
    /// </item>
    /// <item>
    ///     You can call <see cref="IsFinished"/> immediately after <see cref="ActivateReceive"/> to check
    ///     whether the procedure is finished. If it is, you can check whether it was successful using
    ///     <see cref="Success"/> (available to all amoebots) and find the valid placement representatives
    ///     using <see cref="IsRepresentative"/>.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>

    // Round plan:

    // Init:
    //  - Place marker at counter start
    //  - Setup PASC in direction d^-1
    //  - Set comparison result to EQUAL

    // Round 0:
    //  Receive (Only reached after first send!):
    //  - Receive beeps on two global circuits
    //  - If no beep on first circuit:
    //      - If no beep on second circuit:
    //          - First PASC is finished
    //          - Set valid line flag based on comparison result
    //          - Go to round 3
    //      - Else:
    //          - Terminate with failure
    //  - Else:
    //      - If no beep on second circuit:
    //          - Perform PASC cutoff
    //          - Go to round 2
    //  Send:
    //  - Setup global circuit
    //  - Send PASC
    //  - Marker sends bit of d on global circuit
    //  - Marker sends beep to successor (unless it is MSB)

    // Round 1:
    //  Receive:
    //  - Receive PASC
    //  - Receive bit d on global circuit
    //  - Receive marker beep
    //  - Update comparison result and marker
    //  Send:
    //  - Setup 2 global circuits
    //  - Send beep on circuit 1 if we became passive
    //  - Current marker sends beep on global circuit 2
    //  - Go back to round 0

    // Round 2:
    //  Receive (Only reached after first send!):
    //  - Receive PASC cutoff beep
    //  - Update comparison result
    //  - Set valid line flag based on result
    //  - Go to round 3
    //  Send:
    //  - Setup PASC cutoff circuit and send beep

    // Round 3:
    //  Receive (Only reached after first send!):
    //  - Receive line circuit beep
    //      - If received and valid line point: Set segment limiter flag
    //      - (This means we have to interpret the PASC result differently later)
    //  - Receive second line circuit beep
    //      - Set have valid placement flag
    //      - (This means we have to run next phase on this segment)
    //  - Receive global circuit beep
    //      - If no beep: Terminate with failure
    //  - Place marker at counter start
    //  - Go to round 4
    //  Send:
    //  - Setup circuits along direction a
    //      - Split at invalid line points
    //      - Amoebots without neighbor in direction a send beep
    //  - Setup second circuits along direction a (complete)
    //      - Valid line points (*that are not excluded*) send beep
    //  - Setup a global circuit
    //      - Valid line points (*not excluded*) send beep

    // Round 4:
    //  Send:
    //  - Setup PASC circuits in direction a^-1
    //      - Only where we have a valid placement
    //      - Leaders are invalid placements and amoebots without neighbor in direction a
    //      - Send first beep
    //  - Setup global circuit
    //      - Marker sends current bit of a
    //  - Marker sends beep to successor (unless it is MSB)

    // Round 5 (similar to round 1):
    //  Receive:
    //  - (Only done by amoebots on line with valid line placement)
    //  - Receive PASC
    //  - Receive bit a on global circuit
    //  - Receive marker beep
    //  - Update comparison result and marker
    //  Send:
    //  - Setup 2 global circuits
    //  - Send beep on circuit 1 if we became passive
    //  - Current marker sends beep on global circuit 2

    // Round 6 (similar to round 0):
    //  Receive:
    //  - Receive beeps on two global circuits
    //  - If no beep on first circuit:
    //      - If no beep on second circuit:
    //          - First PASC is finished
    //          - Set valid placement flag based on comparison result
    //          - Go to round 8
    //      - Else:
    //          - Terminate with failure
    //  - Else:
    //      - If no beep on second circuit:
    //          - Perform PASC cutoff
    //          - Go to round 7
    //  Send:
    //  - Setup global circuit
    //  - Send PASC
    //  - Marker sends bit of a on global circuit
    //  - Marker sends beep to successor (unless it is MSB)
    //  - Go to round 5

    // Round 7 (similar to round 2):
    //  Receive (Only reached after first send!):
    //  - Receive PASC cutoff beep
    //  - Update comparison result
    //  - Set valid placement flag based on result
    //  - Go to round 8
    //  Send:
    //  - Setup PASC cutoff circuit and send beep

    // Round 8:
    //  Receive (Only reached after first send!):
    //  - Listen for beep on global circuit
    //  - If no beep received:
    //      - Terminate with failure
    //  - Otherwise:
    //      - Terminate with success
    //  Send:
    //  - Setup global circuit
    //  - Valid placements beep

    public class SubParallelogram : Subroutine
    {
        enum ComparisonResult
        {
            NONE = 0,
            EQUAL = 1,
            LESS = 2,
            GREATER = 3
        }

        // State:
        // 4 lowest bits: Round (values 0-8)
        // 2x3 bits for directions of sides a and d
        // 2x3 bits for predecessor and successor directions
        // 2 bits for comparison result
        // 2x1 bit for a and d in counter
        // 2x1 bit for MSBs of a and d
        // 1 bit for marker
        // 1 bit for valid line flag
        // 1 bit for "valid line on segment" flag, indicating we have a valid placement on our segment
        // 1 bit for segment limit flag, indicating that our PASC start is a boundary instead of an invalid line
        // 1 bit for representative flag
        // 1 bit for success flag
        // 1 bit for finished flag
        // 1 bit for color control flag
        // 1 bit for excluded flag (this amoebot cannot be a valid placement)
        // 31         30      29         28        27               26           25           24           23       22      21      20  19 1817          1614   1311   108     7 5     4  0
        // x          x       x          x         x                x            x            x            x        x       x       x   x   xx           xxx    xxx    xxx     xxx     xxxx
        // Excluded   Color   Finished   Success   Representative   Seg. limit   Valid seg.   Valid line   Marker   MSB d   MSB a   d   a   Comparison   Succ   Pred   Dir d   Dir a   Round
        ParticleAttribute<int> state;

        // Binary state wrappers
        BinAttributeInt round;
        BinAttributeDirection direction_a;
        BinAttributeDirection direction_d;
        BinAttributeDirection direction_pred;
        BinAttributeDirection direction_succ;
        BinAttributeEnum<ComparisonResult> comparison;
        BinAttributeBool bit_a;
        BinAttributeBool bit_d;
        BinAttributeBool msb_a;
        BinAttributeBool msb_d;
        BinAttributeBool marker;
        BinAttributeBool valid_line;
        BinAttributeBool valid_line_on_segment;
        BinAttributeBool segment_limit;
        BinAttributeBool is_representative;
        BinAttributeBool success;
        BinAttributeBool finished;
        BinAttributeBool controlColor;
        BinAttributeBool excluded;

        SubPASC2 pasc;

        public SubParallelogram(Particle p, SubPASC2 pasc_instance = null) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Paral.] State"), 0);

            round = new BinAttributeInt(state, 0, 4);
            direction_a = new BinAttributeDirection(state, 5);
            direction_d = new BinAttributeDirection(state, 8);
            direction_pred = new BinAttributeDirection(state, 11);
            direction_succ = new BinAttributeDirection(state, 14);
            comparison = new BinAttributeEnum<ComparisonResult>(state, 17, 2);
            bit_a = new BinAttributeBool(state, 19);
            bit_d = new BinAttributeBool(state, 20);
            msb_a = new BinAttributeBool(state, 21);
            msb_d = new BinAttributeBool(state, 22);
            marker = new BinAttributeBool(state, 23);
            valid_line = new BinAttributeBool(state, 24);
            valid_line_on_segment = new BinAttributeBool(state, 25);
            segment_limit = new BinAttributeBool(state, 26);
            is_representative = new BinAttributeBool(state, 27);
            success = new BinAttributeBool(state, 28);
            finished = new BinAttributeBool(state, 29);
            controlColor = new BinAttributeBool(state, 30);
            excluded = new BinAttributeBool(state, 31);

            if (pasc_instance is null)
            {
                pasc = new SubPASC2(p);
            }
            else
            {
                pasc = pasc_instance;
            }
        }

        /// <summary>
        /// Initializes the subroutine. Assumes that a binary counter stores
        /// the width <c>a</c> and height <c>d</c> of the parallelogram, as
        /// well as its MSBs. Each amoebot on the counter can only store one bit.
        /// </summary>
        /// <param name="dirA">The direction in which side <c>a</c> extends.</param>
        /// <param name="dirD">The direction in which side <c>d</c> extends.</param>
        /// <param name="rotation">The number of counter-clockwise 60 degree rotations
        /// by which the shape should be rotated around its origin.</param>
        /// <param name="controlColor">Whether the subroutine should control the
        /// amoebot color to visualize its progress.</param>
        /// <param name="excluded">Whether this amoebot should be excluded from
        /// being a valid placement.</param>
        /// <param name="counterPred">The predecessor direction of the counter.
        /// Should be <see cref="Direction.NONE"/> for the counter start and amoebots
        /// that are not on the counter.</param>
        /// <param name="counterSucc">The successor direction of the counter.
        /// Should be <see cref="Direction.NONE"/> for the counter end and amoebots
        /// that are not on the counter.</param>
        /// <param name="bitA">If the amoebot is on the counter, stores the bit of
        /// side length <c>a</c>.</param>
        /// <param name="bitD">If the amoebot is on the counter, stores the bit of
        /// side length <c>d</c>.</param>
        /// <param name="msbA">Whether this is the MSB of <c>a</c> if the amoebot
        /// is on the counter.</param>
        /// <param name="msbD">Whether this is the MSB of <c>d</c> if the amoebot
        /// is on the counter.</param>
        public void Init(Direction dirA, Direction dirD, int rotation, bool controlColor = false, bool excluded = false, Direction counterPred = Direction.NONE, Direction counterSucc = Direction.NONE,
            bool bitA = false, bool bitD = false, bool msbA = false, bool msbD = false)
        {
            state.SetValue(0);

            // Apply rotation
            if (rotation > 0)
            {
                dirA = dirA.Rotate60(rotation);
                dirD = dirD.Rotate60(rotation);
            }

            this.controlColor.SetValue(controlColor);
            this.excluded.SetValue(excluded);

            direction_a.SetValue(dirA);
            direction_d.SetValue(dirD);
            direction_pred.SetValue(counterPred);
            direction_succ.SetValue(counterSucc);
            if (counterPred != Direction.NONE || counterSucc != Direction.NONE)
            {
                bit_a.SetValue(bitA);
                bit_d.SetValue(bitD);
                msb_a.SetValue(msbA);
                msb_d.SetValue(msbD);
                // Place marker at each counter start
                if (counterPred == Direction.NONE)
                    marker.SetValue(true);
            }

            // Already initialize comparison and setup PASC
            comparison.SetValue(ComparisonResult.EQUAL);
            bool hasPredPASC = algo.HasNeighborAt(dirD);
            pasc.Init(hasPredPASC ? new List<Direction>() { dirD } : null, new List<Direction>() { dirD.Opposite() }, 0, 1, 0, 1, !hasPredPASC);
        }

        /// <summary>
        /// The first half of the subroutine activation. Must be called
        /// in the round immediately after <see cref="ActivateSend"/>
        /// was called.
        /// </summary>
        public void ActivateReceive()
        {
            if (finished.GetCurrentValue())
                return;
            int r = round.GetValue();
            switch (r)
            {
                case 0:
                case 6:
                    {
                        // Receive beeps on two global circuits
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        bool beep1 = pc.ReceivedBeepOnPartitionSet(0);
                        bool beep2 = pc.ReceivedBeepOnPartitionSet(1);
                        if (!beep1)
                        {
                            // PASC is finished everywhere
                            if (!beep2)
                            {
                                // Counter is finished too!
                                if (r == 0)
                                {
                                    valid_line.SetValue(comparison.GetValue() != ComparisonResult.LESS);
                                    round.SetValue(3);
                                }
                                else // r == 6
                                {
                                    if (valid_line_on_segment.GetValue())
                                    {
                                        if (segment_limit.GetCurrentValue())
                                            is_representative.SetValue(comparison.GetValue() != ComparisonResult.LESS && !excluded.GetValue());
                                        else
                                            is_representative.SetValue(comparison.GetValue() == ComparisonResult.GREATER && !excluded.GetValue());
                                    }
                                    round.SetValue(8);
                                }
                            }
                            else
                            {
                                // Counter is not yet finished: All lines are too short
                                finished.SetValue(true);
                                success.SetValue(false);
                            }
                        }
                        else
                        {
                            // PASC is not finished yet
                            if (!beep2)
                            {
                                // Counter is finished: PASC cutoff
                                round.SetValue(r == 0 ? 2 : 7);
                            }
                            // Otherwise we just continue
                        }
                    }
                    break;
                case 1:
                case 5:
                    {
                        // Receive PASC, bit of d (or a) on global circuit and marker bit
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Run PASC always in round 1 or on valid segment in round 5
                        if (r == 1 || valid_line_on_segment.GetCurrentValue())
                        {
                            pasc.ActivateReceive();
                            bool bitPasc = pasc.GetReceivedBit() > 0;
                            bool bitCounter = pc.ReceivedBeepOnPartitionSet(2);
                        
                            // Update comparison result
                            if (bitPasc && !bitCounter)
                                comparison.SetValue(ComparisonResult.GREATER);
                            else if (bitCounter && !bitPasc)
                                comparison.SetValue(ComparisonResult.LESS);
                        }

                        // Update marker
                        Direction pred = direction_pred.GetValue();
                        // PASC line direction depends on round
                        int markerPin = GetMarkerPin(false, pred.Opposite(), r == 1 ? direction_d.GetCurrentValue() : direction_a.GetCurrentValue());
                        bool markerBeep = pred != Direction.NONE && pc.GetPinAt(pred, markerPin).PartitionSet.ReceivedBeep();

                        marker.SetValue(markerBeep);
                        if (markerBeep)
                            algo.SetMainColor(ColorData.Particle_Orange);
                        else
                            algo.SetMainColor(ColorData.Particle_Black);
                    }
                    break;
                case 2:
                case 7:
                    {
                        // Receive PASC cutoff beep
                        if (r == 2 || valid_line_on_segment.GetValue())
                        {
                            pasc.ReceiveCutoffBeep();
                            // Update final comparison result
                            if (pasc.GetReceivedBit() > 0)
                                comparison.SetValue(ComparisonResult.GREATER);

                            // Set valid line or placement flag based on the result
                            if (r == 2)
                            {
                                valid_line.SetValue(comparison.GetCurrentValue() != ComparisonResult.LESS);
                            }
                            else
                            {
                                if (segment_limit.GetValue())
                                    is_representative.SetValue(comparison.GetCurrentValue() != ComparisonResult.LESS && !excluded.GetValue());
                                else
                                    is_representative.SetValue(comparison.GetCurrentValue() == ComparisonResult.GREATER && !excluded.GetValue());
                                marker.SetValue(false);
                            }
                        }
                        round.SetValue(r == 2 ? 3 : 8);
                    }
                    break;
                case 3:
                    {
                        // Receive beeps on two line circuits and global circuit
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // No beep on global circuit: Terminate with failure
                        if (!pc.ReceivedBeepOnPartitionSet(2))
                        {
                            success.SetValue(false);
                            finished.SetValue(true);
                            break;
                        }

                        Direction dirA = direction_a.GetValue();
                        // Beep on whole line circuit: This line must run PASC in the next phase
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            valid_line_on_segment.SetValue(true);
                            // If we are valid and received a beep from the right: Set segment limiter flag
                            // (our PASC start will be a boundary amoebot instead of an invalid line start)
                            if (pc.GetPinAt(dirA, 2).PartitionSet.ReceivedBeep())
                                segment_limit.SetValue(true);
                        }
                        else
                        {
                            valid_line_on_segment.SetValue(false);
                        }

                        // Already setup PASC, marker etc.
                        marker.SetValue(direction_succ.GetValue() != Direction.NONE && direction_pred.GetValue() == Direction.NONE);
                        Direction dirOpp = dirA.Opposite();
                        if (valid_line_on_segment.GetCurrentValue())
                        {
                            bool pascLeader = !valid_line.GetCurrentValue() || !algo.HasNeighborAt(dirA);
                            pasc.Init(pascLeader ? null : new List<Direction>() { dirA }, new List<Direction>() { dirOpp }, 0, 1, 0, 1, pascLeader);
                        }

                        // Reset comparison
                        comparison.SetValue(ComparisonResult.EQUAL);

                        round.SetValue(4);
                    }
                    break;
                // No case 4
                case 8:
                    {
                        // Listen for beep on global circuit and terminate
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            success.SetValue(true);
                        }
                        else
                        {
                            success.SetValue(false);
                        }
                        finished.SetValue(true);
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Sets up the pin configuration required for the
        /// <see cref="ActivateSend"/> call. The pin configuration
        /// is not planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        public void SetupPC(PinConfiguration pc)
        {
            if (finished.GetCurrentValue())
                return;
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                case 6:
                    {
                        // Setup PASC and global circuit
                        SetupPASCAndGlobalCircuit(pc, r == 0 ? direction_d.GetCurrentValue() : direction_a.GetCurrentValue(), r == 0 || valid_line_on_segment.GetCurrentValue());
                    }
                    break;
                case 1:
                case 5:
                case 8:
                    {
                        // Setup 2 global circuits
                        SetupGlobalCircuits(pc);
                    }
                    break;
                case 2:
                case 7:
                    {
                        // Just setup PASC cutoff
                        if (r == 2 || valid_line_on_segment.GetValue())
                            pasc.SetupCutoffCircuit(pc);
                    }
                    break;
                case 3:
                    {
                        // Setup 2 circuits along direction a and one global circuit
                        SetupLineAndGlobalCircuits(pc);
                    }
                    break;
                case 4:
                    {
                        // Setup PASC and global circuits (PASC only where necessary)
                        SetupPASCAndGlobalCircuit(pc, direction_a.GetValue(), valid_line_on_segment.GetCurrentValue());
                    }
                    break;
            }
        }

        /// <summary>
        /// The second half of the subroutine activation. Before this
        /// can be called, the pin configuration set up by
        /// <see cref="SetupPC(PinConfiguration)"/> must be planned.
        /// </summary>
        public void ActivateSend()
        {
            if (finished.GetCurrentValue())
                return;
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                case 6:
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Send PASC beep
                        if (r == 0 || valid_line_on_segment.GetValue())
                            pasc.ActivateSend();
                        if (marker.GetCurrentValue())
                        {
                            // Marker sends bit of d or a on global circuit
                            if (r == 0 && bit_d.GetCurrentValue() || r == 6 && bit_a.GetCurrentValue())
                            {
                                pc.SendBeepOnPartitionSet(2);
                            }
                            // Marker sends beep to successor unless it is MSB
                            Direction succ = direction_succ.GetCurrentValue();
                            if (succ != Direction.NONE && (r == 0 && !msb_d.GetCurrentValue() || r == 6 && !msb_a.GetCurrentValue()))
                            {
                                int pin = GetMarkerPin(true, succ, r == 0 ? direction_d.GetCurrentValue() : direction_a.GetCurrentValue());
                                pc.GetPinAt(succ, pin).PartitionSet.SendBeep();
                            }
                        }
                        round.SetValue(r == 0 ? 1 : 5);
                    }
                    break;
                case 1:
                case 5:
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Send beep on circuit 1 if we became passive (only necessary for some amoebots in round 5)
                        if ((r == 1 || valid_line_on_segment.GetCurrentValue()) && pasc.BecamePassive())
                            pc.SendBeepOnPartitionSet(0);
                        // Current marker sends beep on global circuit 2
                        if (marker.GetCurrentValue())
                            pc.SendBeepOnPartitionSet(1);
                        round.SetValue(r == 1 ? 0 : 6);
                    }
                    break;
                case 2:
                case 7:
                    {
                        // Send PASC cutoff beep
                        if (r == 2 || valid_line_on_segment.GetValue())
                            pasc.SendCutoffBeep();
                    }
                    break;
                case 3:
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Valid line points send beep on first line circuit and global circuit
                        // (but only if they are not excluded)
                        if (valid_line.GetCurrentValue())
                        {
                            if (!excluded.GetCurrentValue())
                            {
                                pc.SendBeepOnPartitionSet(0);
                                pc.SendBeepOnPartitionSet(2);
                            }

                            // Amoebots with no neighbor in direction a send beep on second line circuit
                            Direction dirA = direction_a.GetValue();
                            if (!algo.HasNeighborAt(dirA))
                                pc.GetPinAt(dirA.Opposite(), 1).PartitionSet.SendBeep();
                        }
                    }
                    break;
                case 4:
                    {
                        // Send first PASC beep
                        if (valid_line_on_segment.GetCurrentValue())
                            pasc.ActivateSend();
                        // Marker sends beep on global circuit and to successor
                        if (marker.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            if (bit_a.GetValue())
                                pc.SendBeepOnPartitionSet(2);
                            if (!msb_a.GetValue())
                            {
                                Direction succ = direction_succ.GetValue();
                                if (succ != Direction.NONE)
                                {
                                    int pin = GetMarkerPin(true, succ, direction_a.GetValue());
                                    pc.GetPinAt(succ, pin).PartitionSet.SendBeep();
                                }
                            }
                        }
                        round.SetValue(5);
                    }
                    break;
                case 8:
                    {
                        // Valid placements send beep on global circuit
                        if (is_representative.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(0);
                        }
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Checks whether the procedure is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if all valid placements
        /// were found or ruled out.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether the procedure finished successfully, i.e.,
        /// there is a valid placement in the system.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// is finished and there is at least one valid placement.</returns>
        public bool Success()
        {
            return IsFinished() && success.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this amoebot is a representative of a
        /// valid placement after the procedure has finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// is finished and this amoebot was determined as a
        /// valid placement.</returns>
        public bool IsRepresentative()
        {
            return IsFinished() && is_representative.GetCurrentValue();
        }

        /// <summary>
        /// Helper setting the amoebot color if this option
        /// is active. The base color is black, the marker is
        /// highlighted in orange, representatives are green,
        /// checking segments are blue (light blue for valid
        /// line placements, dark for invalid ones) and the
        /// whole system is red if no placement was found.
        /// </summary>
        private void SetColor()
        {
            if (!controlColor.GetCurrentValue())
                return;
            bool isFinished = finished.GetCurrentValue();
            if (marker.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Orange);
            else if (isFinished && is_representative.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Green);
            else if (isFinished && !success.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Red);
            else if (!isFinished && valid_line_on_segment.GetCurrentValue())
            {
                if (valid_line.GetCurrentValue())
                    algo.SetMainColor(ColorData.Particle_Blue);
                else
                    algo.SetMainColor(ColorData.Particle_BlueDark);
            }
            else
                algo.SetMainColor(ColorData.Particle_Black);
        }

        /// <summary>
        /// Helper setting up a pin configuration for the PASC
        /// phases. Sets up a PASC circuit if required and a
        /// global circuit on partition set 2 that does not
        /// interfere with PASC.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="pascLineDir">The direction of the side for which
        /// PASC is being executed (the actual PASC direction will be
        /// the opposite of this).</param>
        /// <param name="setupPasc">Whether the PASC pin configuration
        /// should be setup as well.</param>
        private void SetupPASCAndGlobalCircuit(PinConfiguration pc, Direction pascLineDir, bool setupPasc = true)
        {
            if (setupPasc)
                pasc.SetupPC(pc);

            bool[] inverted = new bool[6];
            int d = pascLineDir.ToInt();
            for (int i = 0; i < 3; i++)
            {
                inverted[(d + i) % 6] = true;
            }
            pc.SetStarConfig(2, inverted, 2);
        }

        /// <summary>
        /// Sets up two global circuits on partition sets 0 and 1.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void SetupGlobalCircuits(PinConfiguration pc)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(0, inverted, 0);
            pc.SetStarConfig(1, inverted, 1);
        }

        /// <summary>
        /// Sets up two line circuits on each segment as well as
        /// a global circuit. The first line circuit connects the
        /// whole segment and has partition set ID 0. The second
        /// line circuit is split at invalid line placements and
        /// uses pins 2 and 1 in the <c>a</c> side direction and
        /// its opposite, resp. The global circuit has partition
        /// set ID 2.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void SetupLineAndGlobalCircuits(PinConfiguration pc)
        {
            Direction dirA = direction_a.GetValue();
            Direction dirOpp = dirA.Opposite();

            // First line circuit is complete along the line
            pc.MakePartitionSet(new int[] { pc.GetPinAt(dirA, 3).Id, pc.GetPinAt(dirOpp, 0).Id }, 0);

            // Other line circuit is split at invalid line points
            if (valid_line.GetCurrentValue() && algo.HasNeighborAt(dirA) && algo.HasNeighborAt(dirOpp))
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(dirA, 2).Id, pc.GetPinAt(dirOpp, 1).Id }, 1);
            }

            // Global circuit that does not interfere
            bool[] inverted = new bool[6];
            int d = dirA.ToInt();
            for (int i = 0; i < 3; i++)
            {
                inverted[(d + i) % 6] = true;
            }
            pc.SetStarConfig(2, inverted, 2);
        }

        /// <summary>
        /// Helper to determine the free pin on which to send/receive
        /// the marker beep.
        /// </summary>
        /// <param name="outgoing">Whether the outgoing pin should be
        /// returned rather than the incoming pin.</param>
        /// <param name="succDir">The direction in which the marker should move.</param>
        /// <param name="lineDir">The direction of the line on which PASC is
        /// being executed (shape line direction).</param>
        /// <returns>The offset of the free pin.</returns>
        private int GetMarkerPin(bool outgoing, Direction succDir, Direction lineDir)
        {
            if (succDir == lineDir)
                return outgoing ? 0 : 3;
            else
                return outgoing ? 3 : 0;
        }
    }

} // namespace AS2.Subroutines.ConvexShapePlacementSearch
