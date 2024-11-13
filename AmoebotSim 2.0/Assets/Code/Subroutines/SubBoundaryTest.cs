using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.PASC;

namespace AS2.Subroutines.BoundaryTest
{

    /// <summary>
    /// Subroutine implementation of <see cref="AS2.Algos.BoundaryTest.BoundaryTestParticle"/>.
    /// <para>
    /// <b>Usage:</b>
    /// <list type="bullet">
    /// <item>
    ///     Initialize by calling <see cref="Init(bool)"/>. All amoebots must participate in the subroutine.
    /// </item>
    /// <item>
    ///     Run <see cref="SetupPC(PinConfiguration)"/>, then <see cref="ParticleAlgorithm.SetNextPinConfiguration(PinConfiguration)"/>
    ///     (if necessary) and <see cref="ActivateSend"/> to start the procedure.
    /// </item>
    /// <item>
    ///     In the round immediately following a <see cref="ActivateSend"/> call, <see cref="ActivateReceive"/>
    ///     must be called. There can be an arbitrary break until the next pin configuration setup and
    ///     <see cref="ActivateSend"/> call. Continue this until the procedure is finished.
    /// </item>
    /// <item>
    ///     You can call <see cref="IsFinished"/> immediately after <see cref="ActivateReceive"/> to check
    ///     whether the procedure is finished. If it is, you can find the number of boundaries an amoebot is
    ///     part of using <see cref="NumBoundaries"/> and get boundary information with <see cref="IsBoundaryLeader(int)"/>,
    ///     <see cref="IsOuterBoundary(int)"/>, <see cref="GetBoundaryPredecessor(int)"/> and
    ///     <see cref="GetBoundarySuccessor(int)"/>.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>

    // Algorithm plan:
    //  1. Run a leader election on each boundary
    //      - Use the entire system for synchronization
    //  2. On each boundary, run an add tree procedure (with PASC) to compute the angular sums
    //  3. Let boundaries know what type they are (inner or outer) and let all amoebots know whether there is an inner boundary

    // Round plan:

    // Init:
    //  - Find out how many boundaries we have and where they are
    //  - Also set initial angles

    // 1. Leader election

    // Round 0:
    //  Send:
    //  - Establish two boundary circuits for each boundary
    //  - Leader candidates flip coin
    //      - Beep on first circuit for HEADS and second for TAILS
    //  Receive:
    //  - Receive on two circuits per boundary
    //  - If both circuits beeped:
    //      - Candidates that tossed TAILS revoke candidacy
    //  - Go to round 1

    // Round 1:
    //  Send:
    //  - Establish two global circuits
    //  - Aux. candidates flip coin
    //      - Beep on first circuit for HEADS and second for TAILS
    //  Receive:
    //  - Receive on two global circuits
    //  - If both circuits beeped:
    //      - Aux candidates that tossed TAILS revoke candidacy
    //  - Else:
    //      - Increment iteration counter
    //      - If counter has reached number of repetitions (kappa):
    //          - Initialize PASC
    //          - Go to round 2
    //      - Else:
    //          - Turn all amoebots into aux. candidates again
    //  - Go back to round 0

    // 2. Angle computation

    // Round 2:
    //  Send:
    //  - Establish PASC circuit on each boundary
    //  - Send PASC beep
    //  Receive:
    //  - Receive PASC beep

    // Round 3:
    //  Send:
    //  - Setup global circuit
    //  - Beep on global circuit if any PASC instance became passive
    //  Receive:
    //  - Receive on global circuit
    //  - If no beep:
    //      - Finished with angle computation, go to round 6
    //  - Else: Go to round 4

    // Round 4:
    //  Send:
    //  - Setup two boundary circuits, split at amoebots that are active and ones that just became passive
    //  - If our PASC instance became passive:
    //      - Send beep on first boundary circuit towards predecessor
    //      - Send first bit of our angle on the second circuit
    //  Receive:
    //  - Active amoebots receive on both successor circuits
    //  - If beep on first one:
    //      - Remember that successor exists
    //      - Remember bit on second circuit

    // Round 5:
    //  Send:
    //  - Setup two boundary circuits, split as before
    //  - If our PASC instance became passive:
    //      - Send two remaining bits of our angle on the two circuits towards the predecessor
    //  Receive:
    //  - If we are active and our successor exists:
    //      - Receive the two remaining bits
    //      - Update our angle sum
    //  - Go back to round 2

    // 3. Cleanup

    // Round 6:
    //  Send:
    //  - Setup one boundary circuit and a global circuit
    //  - Leader of outer boundary sends beep on boundary circuit
    //  - Leaders of inner boundaries send beeps on global circuit
    //  Receive:
    //  - Boundary circuit beep tells us whether we are on inner or outer boundary
    //  - Global beep tells us whether there is an inner boundary
    //  - Terminate

    public class SubBoundaryTest : Subroutine
    {
        private static readonly int kappa = 3;                                      // Number of repetitions of the leader election (one is always executed)
                                                                                    // Must not be greater than 8

        //      29           28  26           25                      8   76               5 3         2 0
        // xx   x            x x x            xxx xxx  xxx xxx  xxx xxx   xx               xxx         xxx
        //      Aux. cand.   Boundary cand.   Boundary pred/succ.         Num boundaries   Iteration   Round
        ParticleAttribute<int> state1;

        //             23              22         21                   20  18      17  15           14  12            11  9            8         0
        // xxxx xxxx   x               x          x                    x x x       x x x            x x x             x x x            xxx xxx xxx
        //             Control color   Finished   Inner boundary ex.   Coin toss   Successor bits   Successor flags   Inner boundary   Angles
        ParticleAttribute<int> state2;

        BinAttributeInt round;                                                      // Round counter
        BinAttributeInt iteration;                                                  // Leader election iteration counter
        BinAttributeInt numBoundaries;                                              // Number of boundaries we are part of
        BinAttributeDirection[] boundaryDirs = new BinAttributeDirection[6];        // Predecessor and successor directions for up to 3 boundaries
        BinAttributeBitField boundaryCandidate;                                     // Whether we are a leader candidate for up to 3 boundaries
        BinAttributeBool auxCandidate;                                              // Whether we are a helper candidate

        BinAttributeInt[] angles = new BinAttributeInt[3];                          // Angles for the boundaries
        BinAttributeBitField innerBoundary;                                         // Inner boundary flags
        BinAttributeBitField successorExists;                                       // Successor flags (for the angle transmission during the sum computation)
        BinAttributeBitField successorBit;                                          // The first successor bit (same situation as above)
        BinAttributeBitField coinTossHeads;                                         // Whether our last coin toss was HEADS
        BinAttributeBool innerBoundaryExists;                                       // Whether an inner boundary exists in the system
        BinAttributeBool finished;                                                  // Whether we are finished
        BinAttributeBool controlColor;                                              // Whether we should control the amoebot's color

        SubPASC[] pasc = new SubPASC[3];

        public SubBoundaryTest(Particle p, SubPASC[] pascInstances = null) : base(p)
        {
            state1 = algo.CreateAttributeInt(FindValidAttributeName("[BT] State 1"), 0);
            state2 = algo.CreateAttributeInt(FindValidAttributeName("[BT] State 2"), 0);

            round = new BinAttributeInt(state1, 0, 3);
            iteration = new BinAttributeInt(state1, 3, 3);
            numBoundaries = new BinAttributeInt(state1, 6, 2);
            for (int i = 0; i < 6; i++)
                boundaryDirs[i] = new BinAttributeDirection(state1, 8 + 3 * i);
            boundaryCandidate = new BinAttributeBitField(state1, 26, 3);
            auxCandidate = new BinAttributeBool(state1, 29);

            for (int i = 0; i < 3; i++)
                angles[i] = new BinAttributeInt(state2, 3 * i, 3);
            innerBoundary = new BinAttributeBitField(state2, 9, 3);
            successorExists = new BinAttributeBitField(state2, 12, 3);
            successorBit = new BinAttributeBitField(state2, 15, 3);
            coinTossHeads = new BinAttributeBitField(state2, 18, 3);
            innerBoundaryExists = new BinAttributeBool(state2, 21);
            finished = new BinAttributeBool(state2, 22);
            controlColor = new BinAttributeBool(state2, 23);

            if (pascInstances is null)
                pascInstances = new SubPASC[3];

            for (int i = 0; i < 3; i++)
            {
                if (i < pascInstances.Length && !(pascInstances[i] is null))
                    pasc[i] = pascInstances[i];
                else
                    pasc[i] = new SubPASC(p);
            }
        }

        /// <summary>
        /// Initializes the subroutine. Must be called on
        /// all amoebots in the system.
        /// </summary>
        /// <param name="controlColor">Whether the subroutine should control the color
        /// of this amoebot.</param>
        public void Init(bool controlColor = false)
        {
            state1.SetValue(0);
            state2.SetValue(0);

            this.controlColor.SetValue(controlColor);

            // Find boundaries
            int nBoundaries = 0;
            bool[] occupied = new bool[6];
            int nOccupied = 0;
            for (int d = 0; d < 6; d++)
            {
                bool hasNbr = algo.HasNeighborAt(DirectionHelpers.Cardinal(d));
                occupied[d] = hasNbr;
                if (hasNbr)
                    nOccupied++;
            }

            if (nOccupied == 0)
            {
                // We are alone in the system!
                numBoundaries.SetValue(1);
                boundaryCandidate.SetValue(0, true);
                finished.SetValue(true);
            }
            else if (nOccupied == 6)
            {
                // We are not on any boundary
                numBoundaries.SetValue(0);
            }
            else
            {
                int firstBoundaryStart = -1;
                for (int d = 0; d < 6; d++)
                {
                    if (occupied[d] && !occupied[(d + 5) % 6])
                    {
                        firstBoundaryStart = d;
                        break;
                    }
                }
                nBoundaries = 1;
                boundaryDirs[0].SetValue(DirectionHelpers.Cardinal(firstBoundaryStart));
                bool onBoundary = true;
                for (int i = 1; i < 6; i++)
                {
                    int d = (firstBoundaryStart + 6 - i) % 6;
                    int next = (d + 5) % 6;
                    if (onBoundary)
                    {
                        // Check if this is the end of the boundary
                        if (!occupied[d] && occupied[next])
                        {
                            onBoundary = false;
                            Direction pred = boundaryDirs[2 * nBoundaries - 2].GetCurrentValue();
                            Direction succ = DirectionHelpers.Cardinal(next);
                            boundaryDirs[2 * nBoundaries - 1].SetValue(succ);
                            // Remember the angle
                            // Number of turns can be 0, 2, 3, 4, 5
                            // We map this to 3, 4, 0, 1, 2
                            int numTurns = (pred.DistanceTo(succ, true) / 2 + 3) % 6;
                            if (numTurns == 5)
                                numTurns--;
                            angles[nBoundaries - 1].SetValue(numTurns);
                        }
                    }
                    else
                    {
                        // Check if this is the start of a boundary
                        if (occupied[d] && !occupied[(d + 5) % 6])
                        {
                            onBoundary = true;
                            boundaryDirs[2 * nBoundaries].SetValue(DirectionHelpers.Cardinal(d));
                            nBoundaries++;
                        }
                    }
                }
                numBoundaries.SetValue(nBoundaries);
            }

            // Become candidates for all our boundaries
            for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                boundaryCandidate.SetValue(i, true);
            // Always start as helper candidate
            auxCandidate.SetValue(true);
        }

        /// <summary>
        /// The first half of the subroutine activation. Must be called
        /// in the round immediately after <see cref="ActivateSend"/>
        /// was called.
        /// </summary>
        public void ActivateReceive()
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        // Candidates receive beeps on both circuits and revoke candidacy if both circuits
                        // have beeped and they have tossed TAILS
                        if (boundaryCandidate.GetCurrentOr())
                        {
                            for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                            {
                                if (boundaryCandidate.GetCurrentValue(i) && !coinTossHeads.GetCurrentValue(i) && algo.ReceivedBeepOnPartitionSet(2 * i) && algo.ReceivedBeepOnPartitionSet(2 * i + 1))
                                {
                                    boundaryCandidate.SetValue(i, false);
                                }
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 1:
                    {
                        // Receive beeps on both global circuit
                        if (algo.ReceivedBeepOnPartitionSet(0) && algo.ReceivedBeepOnPartitionSet(1))
                        {
                            // Helper candidates that tossed TAILS revoke candidacy
                            if (auxCandidate.GetCurrentValue() && !coinTossHeads.GetCurrentValue(0))
                                auxCandidate.SetValue(false);
                        }
                        else
                        {
                            // This iteration is over, check if we are finished
                            int reps = iteration.GetCurrentValue() + 1;
                            if (reps >= kappa)
                            {
                                // Finished
                                // Initialize PASC and continue with next phase
                                for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                                {
                                    Direction pred = boundaryDirs[2 * i].GetCurrentValue();
                                    Direction succ = boundaryDirs[2 * i + 1].GetCurrentValue();
                                    bool leader = boundaryCandidate.GetCurrentValue(i);

                                    pasc[i].Init(leader, leader ? Direction.NONE : pred, succ, 0, 1, algo.PinsPerEdge - 1, algo.PinsPerEdge - 2, 2 * i, 2 * i + 1);
                                }
                                round.SetValue(2);
                                break;
                            }
                            else
                            {
                                // Not finished, start next iteration
                                iteration.SetValue(reps);
                                auxCandidate.SetValue(true);
                            }
                        }
                        round.SetValue(0);
                    }
                    break;
                case 2:
                    {
                        // Receive PASC beep
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                            pasc[i].ActivateReceive();
                        round.SetValue(r + 1);
                    }
                    break;
                case 3:
                    {
                        // Listen for global circuit beep telling us that we have to continue
                        if (algo.ReceivedBeepOnPartitionSet(0))
                        {
                            // Continue angle computation
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // No beep received: Finished angle computation
                            round.SetValue(6);
                        }
                    }
                    break;
                case 4:
                case 5:
                    {
                        // Active amoebots receive on both successor circuits
                        // Round 4: Existence and first bit of successor
                        // Round 5: Remaining two bits of successor
                        PinConfiguration pc = algo.GetCurrPinConfiguration();
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                        {
                            if (pasc[i].IsActive())
                            {
                                Direction succ = boundaryDirs[2 * i + 1].GetCurrentValue();
                                bool beep1 = pc.GetPinAt(succ, algo.PinsPerEdge - 1).ReceivedBeep();
                                bool beep2 = pc.GetPinAt(succ, algo.PinsPerEdge - 2).ReceivedBeep();
                                if (r == 4)
                                {
                                    if (beep1)
                                    {
                                        successorExists.SetValue(i, true);
                                        successorBit.SetValue(i, beep2);
                                    }
                                    else
                                        successorExists.SetValue(i, false);
                                }
                                else if (successorExists.GetCurrentValue(i))
                                {
                                    // Update our angle sum
                                    int angle = angles[i].GetCurrentValue();
                                    if (successorBit.GetCurrentValue(i))
                                        angle = (angle + 4) % 5;
                                    else
                                    {
                                        angle = (angle + (beep1 ? 1 : 0) + (beep2 ? 2 : 0)) % 5;
                                    }
                                    angles[i].SetValue(angle);
                                }
                            }
                        }
                        if (r == 4)
                            round.SetValue(r + 1);
                        else
                            round.SetValue(2);
                    }
                    break;
                case 6:
                    {
                        // Boundary circuit beep tells us that we are on the outer boundary
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                        {
                            innerBoundary.SetValue(i, !algo.ReceivedBeepOnPartitionSet(2 * i));
                        }

                        // Global circuit beep tells us whether an inner boundary exists
                        innerBoundaryExists.SetValue(algo.ReceivedBeepOnPartitionSet(6));
                        
                        finished.SetValue(true);
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Sets up the pin configuration required for the
        /// <see cref="ActivateSend"/> call. The next pin configuration
        /// is not set to another object by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        public void SetupPC(PinConfiguration pc)
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                            SetupBoundaryCircuit(pc, i, true);
                    }
                    break;
                case 1:
                    {
                        SetupGlobalCircuit(pc, 0, 0);
                        SetupGlobalCircuit(pc, 1, 1);
                    }
                    break;
                case 2:
                    {
                        // Setup PASC circuits
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                            pasc[i].SetupPC(pc);
                    }
                    break;
                case 3:
                    {
                        SetupGlobalCircuit(pc, 0, 0);
                    }
                    break;
                case 4:
                case 5:
                    {
                        // Setup two boundary circuits but split at active amoebots and ones that became passive
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                        {
                            if (!pasc[i].IsActive() && !pasc[i].BecamePassive())
                                SetupBoundaryCircuit(pc, i, true);
                        }
                    }
                    break;
                case 6:
                    {
                        // Setup one boundary circuit and one global circuit
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                            SetupBoundaryCircuit(pc, i, false);
                        SetupGlobalCircuit(pc, 1, 6);
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
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        // Leader candidates flip coin and beep
                        if (boundaryCandidate.GetCurrentOr())
                        {
                            for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                            {
                                if (boundaryCandidate.GetCurrentValue(i))
                                {
                                    bool heads = Random.Range(0f, 1f) < 0.5f;
                                    algo.SendBeepOnPartitionSet(heads ? 2 * i : 2 * i + 1);
                                    coinTossHeads.SetValue(i, heads);
                                }
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        // Helper candidates flip coin and beep on global circuit
                        if (auxCandidate.GetCurrentValue())
                        {
                            bool heads = Random.Range(0f, 1f) < 0.5f;
                            algo.SendBeepOnPartitionSet(heads ? 0 : 1);
                            coinTossHeads.SetValue(0, heads);
                        }
                    }
                    break;
                case 2:
                    {
                        // Send PASC beep
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                            pasc[i].ActivateSend();
                    }
                    break;
                case 3:
                    {
                        // Beep on global circuit if any PASC instance became passive
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                        {
                            if (pasc[i].BecamePassive())
                            {
                                algo.SendBeepOnPartitionSet(0);
                                break;
                            }
                        }
                    }
                    break;
                case 4:
                case 5:
                    {
                        // PASC instances that became passive send existence beep and angle bits to predecessor
                        // Existence and first bit in round 4, remaining bits in round 5
                        PinConfiguration pc = algo.GetNextPinConfiguration();
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                        {
                            if (pasc[i].BecamePassive())
                            {
                                Direction pred = boundaryDirs[2 * i].GetCurrentValue();
                                if (r == 4)
                                {
                                    pc.GetPinAt(pred, 0).SendBeep();
                                    // Beep on second circuit if our angle is 4
                                    if (angles[i].GetCurrentValue() == 4)
                                        pc.GetPinAt(pred, 1).SendBeep();
                                }
                                else
                                {
                                    // Send lower bit on first circuit, higher bit on second
                                    int angle = angles[i].GetCurrentValue();
                                    if (angle % 2 == 1)
                                        pc.GetPinAt(pred, 0).SendBeep();
                                    if (angle > 1 && angle < 4)
                                        pc.GetPinAt(pred, 1).SendBeep();
                                }
                            }
                        }
                    }
                    break;
                case 6:
                    {
                        // Leader of outer boundary sends beep on boundary circuit
                        // Other leaders send beep on global circuit
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                        {
                            if (boundaryCandidate.GetCurrentValue(i))
                            {
                                if (angles[i].GetCurrentValue() == 1)
                                    algo.SendBeepOnPartitionSet(2 * i);
                                else
                                    algo.SendBeepOnPartitionSet(6);
                            }
                        }
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Checks whether the procedure is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if all boundaries
        /// have been identified.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this amoebot is on the outer boundary.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot
        /// is part of the outer boundary.</returns>
        public bool OnOuterBoundary()
        {
            if (!IsFinished())
                return false;
            for (int i = 0; i < numBoundaries.GetValue(); i++)
            {
                if (!innerBoundary.GetCurrentValue(i))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether this amoebot is on the inner boundary.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot
        /// is part of an inner boundary.</returns>
        public bool OnInnerBoundary()
        {
            if (!IsFinished())
                return false;
            for (int i = 0; i < numBoundaries.GetValue(); i++)
            {
                if (innerBoundary.GetCurrentValue(i))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether this amoebot is on the outer boundary leader.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot
        /// is the unique leader of the outer boundary.</returns>
        public bool IsOuterBoundaryLeader()
        {
            if (!IsFinished())
                return false;
            for (int i = 0; i < numBoundaries.GetValue(); i++)
            {
                if (!innerBoundary.GetCurrentValue(i) && boundaryCandidate.GetCurrentValue(i))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Finds the number of boundaries this amoebot is on. If m is
        /// the number of boundaries, the boundary indices are 0,...,m-1.
        /// </summary>
        /// <returns>The number of boundaries this amoebot is a part
        /// of, or <c>0</c> if the procedure is not finished yet.</returns>
        public int NumBoundaries()
        {
            return IsFinished() ? numBoundaries.GetCurrentValue() : 0;
        }

        /// <summary>
        /// Checks whether this amoebot is the leader of the
        /// indicated boundary.
        /// </summary>
        /// <param name="idx">The boundary index to check.</param>
        /// <returns><c>true</c> if and only if the procedure is
        /// finished and this amoebot is the leader of the boundary
        /// with index <paramref name="idx"/>.</returns>
        public bool IsBoundaryLeader(int idx)
        {
            return IsFinished() && boundaryCandidate.GetCurrentValue(idx);
        }

        /// <summary>
        /// Checks whether the boundary with the given index is
        /// the outer boundary.
        /// </summary>
        /// <param name="idx">The boundary index to check.</param>
        /// <returns><c>true</c> if and only if the procedure is
        /// finished and the boundary with index <paramref name="idx"/>
        /// of this amoebot is the outer boundary.</returns>
        public bool IsOuterBoundary(int idx)
        {
            return IsFinished() && !innerBoundary.GetCurrentValue(idx);
        }

        /// <summary>
        /// Gets the predecessor direction on the given boundary.
        /// </summary>
        /// <param name="idx">The boundary index to check.</param>
        /// <returns>The direction pointing from this amoebot to its
        /// predecessor in a clockwise traversal of the boundary with
        /// index <paramref name="idx"/>.
        /// Will be <see cref="Direction.NONE"/> if this amoebot does not
        /// have this boundary or the procedure is not finished.</returns>
        public Direction GetBoundaryPredecessor(int idx)
        {
            if (!IsFinished())
                return Direction.NONE;
            return boundaryDirs[2 * idx].GetCurrentValue();
        }

        /// <summary>
        /// Gets the successor direction on the given boundary.
        /// </summary>
        /// <param name="idx">The boundary index to check.</param>
        /// <returns>The direction pointing from this amoebot to its
        /// successor in a clockwise traversal of the boundary with
        /// index <paramref name="idx"/>.
        /// Will be <see cref="Direction.NONE"/> if this amoebot does not
        /// have this boundary or the procedure is not finished.</returns>
        public Direction GetBoundarySuccessor(int idx)
        {
            if (!IsFinished())
                return Direction.NONE;
            return boundaryDirs[2 * idx + 1].GetCurrentValue();
        }

        /// <summary>
        /// Checks whether an inner boundary was found during the procedure.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure is finished and
        /// there is at least one inner boundary in the system.</returns>
        public bool InnerBoundaryExists()
        {
            return IsFinished() && innerBoundaryExists.GetCurrentValue();
        }

        private void SetColor()
        {
            if (!controlColor.GetCurrentValue())
                return;

            if (numBoundaries.GetCurrentValue() == 0)
                algo.SetMainColor(ColorData.Particle_Black);
            else
            {
                int r = round.GetCurrentValue();
                if (r < 2)
                {
                    if (boundaryCandidate.GetCurrentOr())
                        algo.SetMainColor(ColorData.Particle_Green);
                    else if (auxCandidate.GetCurrentValue())
                        algo.SetMainColor(ColorData.Particle_Blue);
                    else
                        algo.SetMainColor(ColorData.Particle_BlueDark);
                }
                else if (r < 6)
                {
                    if (boundaryCandidate.GetCurrentOr())
                        algo.SetMainColor(ColorData.Particle_Green);
                    else
                    {
                        bool pascActive = false;
                        for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                        {
                            if (pasc[i].IsActive())
                            {
                                pascActive = true;
                                break;
                            }
                        }
                        if (pascActive)
                            algo.SetMainColor(new Color(1, 1, 0));
                        else
                            algo.SetMainColor(ColorData.Particle_BlueDark);
                    }
                }
            }
        }

        /// <summary>
        /// Sets up one or two boundary circuits for the given boundary.
        /// The first circuit's partition set ID is <paramref name="boundaryIndex"/> * 2
        /// and the second ID is <paramref name="boundaryIndex"/> * 2 + 1.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="boundaryIndex">The index of the boundary for which to
        /// setup the circuit.</param>
        /// <param name="addInner">Whether the second "inner" circuit should be
        /// setup as well.</param>
        private void SetupBoundaryCircuit(PinConfiguration pc, int boundaryIndex, bool addInner = true)
        {
            int i = boundaryIndex;
            Direction pred = boundaryDirs[2 * i].GetCurrentValue();
            Direction succ = boundaryDirs[2 * i + 1].GetCurrentValue();
            float angle = pred == succ ? pred.ToInt() * 60f : (succ.ToInt() + succ.DistanceTo(pred) / 4.0f) * 60f; 
            pc.MakePartitionSet(new int[] { pc.GetPinAt(pred, 0).Id, pc.GetPinAt(succ, algo.PinsPerEdge - 1).Id }, 2 * i);
            pc.SetPartitionSetPosition(2 * i, new Vector2(angle, pred == succ ? 0.3f : 0.7f));
            if (addInner)
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(pred, 1).Id, pc.GetPinAt(succ, algo.PinsPerEdge - 2).Id }, 2 * i + 1);
                pc.SetPartitionSetPosition(2 * i + 1, new Vector2(angle, pred == succ ? 0.7f : 0.4f));
            }
        }

        /// <summary>
        /// Sets up a global circuit using the given pin offset.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="offset">The pin offset.</param>
        /// <param name="pSet">The partition set ID.</param>
        private void SetupGlobalCircuit(PinConfiguration pc, int offset, int pSet)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(offset, inverted, pSet);
        }
    }

} // namespace AS2.Subroutines.BoundaryTest
