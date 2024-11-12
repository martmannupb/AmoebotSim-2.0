using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.BoundaryTest
{
    public enum Phase { LE1, LE2, SUM }
    // Operation modes of the Sum Computation phase
    // COMPUTE: We are still computing the sum of the angles
    // FINAL: This is the last iteration and the leader transmits the final result
    // WAIT: The leader has already transmitted the final result and we are waiting for termination
    public enum SCMode { COMPUTE, FINAL, WAIT }

    /*
     * Structure
     * 
     * Leader Election Phase 1
     * 0: Receive termination beep.
     *  If beep received: Establish boundary circuit, candidates toss
     *  coin and beep on HEADS.
     *  If no beep: Initialize Leader Election Phase 2
     * 1: Receive HEADS beep and store result.
     *  Candidates with TAILS send beep.
     * 2: Receive TAILS beep and establish global circuit.
     *  If both HEADS and TAILS beeped: Candidates with TAILS retire, send beep.
     *  If only one result beeped: Send no beep.
     *  
     * Leader Election Phase 2
     * 0: Receive termination beep.
     *  If beep received: Establish boundary circuit, candidates toss
     *  coin and beep on HEADS.
     *  If no beep: Initialize next iteration of Phase 2 or start Boundary Test.
     * 1: Receive HEADS beep and store result.
     *  Candidates with TAILS send beep.
     * 2: Receive TAILS beep.
     *  If both HEADS and TAILS beeped: Candidates with TAILS retire.
     *  Phase 2 candidates toss coin and beep on HEADS.
     * 3: Receive Phase 2 HEADS and store result.
     *  Phase 2 candidates with TAILS beep.
     * 4: Receive Phase 2 TAILS beep and establish global circuit.
     *  If both HEADS and TAILS beeped: Phase 2 candidates with TAILS
     *  retire, send beep.
     *  If only one result beeped: Send no beep.
     * 
     * Sum Computation
     * 0: Receive global termination beep
     *  If beep received: Terminate algorithm.
     *  If no beep: Setup pin configuration, leader beeps
     *  on primary partition set.
     * 1: Active particles listen for beep on secondary
     *  partition set, send echo if they received it and
     *  store this information.
     * 2: All particles listen for echo.
     *  If no echo: Algorithm has terminated, setup boundary circuit
     *  again and leader prepares transmission of final result.
     *  If echo: Setup pin configuration for transmission of
     *  intermediate result and start sending.
     * 3-5: Send and receive partial sums, active particles add
     *  received angles to their own.
     * 6: Receive final part of partial sum, establish global
     *  circuit and beep if the procedure is not done yet. Active
     *  particles that have a flag to become passive in this iteration
     *  become passive now and reset the flag.
     *  If this is the end of the final result transmission, the
     *  particles determine whether or not they are on the outer
     *  boundary and will not send a beep in round 6 in any future
     *  iteration.
     */

    /// <summary>
    /// Implementation of the inner outer boundary test from
    /// https://arxiv.org/abs/2205.02610v1.
    /// <para>
    /// Common chirality and compass alignment are assumed. The particles
    /// determine whether or not they are part of a boundary, elect a leader
    /// on each boundary, and then test whether their boundary is an inner
    /// or the outer boundary.
    /// </para>
    /// <para>
    /// The phases are synchronized by periodic beeps on the global circuit
    /// by particles that have not yet finished their current phase. All
    /// particles terminate once the boundary test has finished on each
    /// boundary.
    /// </para>
    /// <para>
    /// The outer boundary's direction is counter-clockwise while the
    /// direction of each inner boundary is clockwise.
    /// </para>
    /// </summary>
    public class BoundaryTestParticle : ParticleAlgorithm
    {
        // Visualization data
        private static readonly Color startColor = ColorData.Particle_Black;
        private static readonly Color candidate1Color = ColorData.Particle_Green;
        private static readonly Color candidate2Color = ColorData.Particle_Red;
        private static readonly Color candidate3Color = ColorData.Particle_Purple;
        private static readonly Color retiredColor = ColorData.Particle_BlueDark;
        private static readonly Color phase2CandColor = ColorData.Particle_Blue;
        private static readonly Color activeColor = new Color(1, 1, 0);
        private static readonly Color passiveColor = ColorData.Particle_BlueDark;

        // Attributes
        // General
        private ParticleAttribute<bool> firstActivation;        // Flag used to setup data on the very first activation (only used once)
        private ParticleAttribute<int> round;                   // Round counter used to synchronize the particles in all phases
        private ParticleAttribute<int> numBoundaries;           // Number of boundaries the particle is a part of. Can be 0-3
        private ParticleAttribute<int>[,] boundaryNbrs;         // Directions of predecessors and successors for each boundary. Has dimensions 3x2
        private ParticleAttribute<int>[] boundaryAngles;        // Angle of the turn from the boundary predecessor to the boundary successor
                                                                // Measured in number of 60° counter-clockwise turns mod 5
        private ParticleAttribute<Phase> phase;                 // The current phase of the algorithm
        private ParticleAttribute<bool> terminated;             // Final termination flag

        // Leader Election
        private static readonly int kappa = 2;                  // Number of repetitions of the second phase (one iteration is always executed)
        private ParticleAttribute<bool>[] isCandidate;          // Stores candidate flag for each of the boundaries
        private ParticleAttribute<bool>[] isPhase2Candidate;    // Stores phase 2 candidate flag for each of the boundaries
        private ParticleAttribute<bool>[] heads;                // Last coin toss result for each boundary
        private ParticleAttribute<bool>[] receivedHeadsBeep;    // HEADS flag for remembering coin toss beep for each boundary
        private ParticleAttribute<int> phase2Count;             // Counter to execute the second phase kappa times

        private ParticleAttribute<PinConfiguration> boundaryPC; // PinConfiguration to setup whenever we want to have one circuit for each boundary

        // Sum computation
        private ParticleAttribute<bool>[] isActive;             // Flag indicating whether the particle is active on each boundary
        private ParticleAttribute<bool>[] becomePassive;        // Flag indicating whether the particle should become passive in the current iteration
        private ParticleAttribute<SCMode>[] scMode;             // Mode of the Sum Computation on each boundary

        public BoundaryTestParticle(Particle p) : base(p)
        {
            firstActivation = CreateAttributeBool("First Activation", true);
            round = CreateAttributeInt("Round", 0);
            numBoundaries = CreateAttributeInt("# Boundaries", -1);
            boundaryNbrs = new ParticleAttribute<int>[3, 2];
            for (int boundaryIdx = 0; boundaryIdx < 3; boundaryIdx++)
            {
                for (int predSuc = 0; predSuc < 2; predSuc++)
                {
                    boundaryNbrs[boundaryIdx, predSuc] = CreateAttributeInt("Boundary " + (boundaryIdx + 1) + " " + (predSuc == 0 ? "Pred" : "Succ"), -1);
                }
            }
            boundaryAngles = new ParticleAttribute<int>[3];
            for (int i = 0; i < 3; i++)
            {
                boundaryAngles[i] = CreateAttributeInt("Boundary " + (i + 1) + " Angle", -1);
            }
            phase = CreateAttributeEnum<Phase>("Phase", Phase.LE1);
            terminated = CreateAttributeBool("Terminated", false);

            isCandidate = new ParticleAttribute<bool>[3];
            isPhase2Candidate = new ParticleAttribute<bool>[3];
            heads = new ParticleAttribute<bool>[3];
            receivedHeadsBeep = new ParticleAttribute<bool>[3];
            phase2Count = CreateAttributeInt("Phase 2 Iterations", 0);
            for (int i = 0; i < 3; i++)
            {
                isCandidate[i] = CreateAttributeBool("Boundary " + (i + 1) + " Candidate", false);
                isPhase2Candidate[i] = CreateAttributeBool("Boundary " + (i + 1) + " Phase 2 Cand.", false);
                heads[i] = CreateAttributeBool("Boundary " + (i + 1) + " HEADS", false);
                receivedHeadsBeep[i] = CreateAttributeBool("Boundary " + (i + 1) + " HEADS beep", false);
            }
            boundaryPC = CreateAttributePinConfiguration("Boundary PC", null);

            isActive = new ParticleAttribute<bool>[3];
            becomePassive = new ParticleAttribute<bool>[3];
            scMode = new ParticleAttribute<SCMode>[3];
            for (int i = 0; i < 3; i++)
            {
                isActive[i] = CreateAttributeBool("Boundary " + (i + 1) + " Active", false);
                becomePassive[i] = CreateAttributeBool("Boundary " + (i + 1) + " Become Passive", false);
                scMode[i] = CreateAttributeEnum<SCMode>("Boundary " + (i + 1) + " SC Mode", SCMode.COMPUTE);
            }

            SetMainColor(startColor);
        }

        public override int PinsPerEdge => 4;

        public static new string Name => "Boundary Test";

        public override bool IsFinished()
        {
            return terminated;
        }

        public override void ActivateMove()
        {

        }

        public override void ActivateBeep()
        {
            if (firstActivation)
            {
                FirstActivation();
                firstActivation.SetValue(false);
                return;
            }
            else if (terminated)
            {
                return;
            }

            // Actual algorithm execution
            if (phase == Phase.LE1)
            {
                switch (round)
                {
                    case 0: LE1Activate0();
                        break;
                    case 1: LE1Activate1();
                        break;
                    case 2: LE1Activate2();
                        break;
                    default: Debug.LogError("Error: Round " + round.GetValue() + " undefined for Leader Election Phase 1");
                        break;
                }
            }
            else if (phase == Phase.LE2)
            {
                switch (round)
                {
                    case 0:
                        LE2Activate0();
                        break;
                    case 1:
                        LE2Activate1();
                        break;
                    case 2:
                        LE2Activate2();
                        break;
                    case 3:
                        LE2Activate3();
                        break;
                    case 4:
                        LE2Activate4();
                        break;
                    default:
                        Debug.LogError("Error: Round " + round.GetValue() + " undefined for Leader Election Phase 2");
                        break;
                }
            }
            else
            {
                switch (round)
                {
                    case 0:
                        SCActivate0();
                        break;
                    case 1:
                        SCActivate1();
                        break;
                    case 2:
                        SCActivate2();
                        break;
                    case 3:
                    case 4:
                    case 5:
                        SCActivate345();
                        break;
                    case 6:
                        SCActivate6();
                        break;
                    default:
                        Debug.LogError("Error: Round " + round.GetValue() + " undefined for Sum Computation phase");
                        break;
                }
            }

            // Finally, update the visuals
            SetColor();
        }

        private void FirstActivation()
        {
            // Determine how many boundaries we are on and set up the initial pin configuration
            // First find all neighbors and the direction of the first encountered neighbor
            bool[] nbrs = new bool[6];
            int firstNbrDir = -1;
            for (int dir = 0; dir < 6; dir++)
            {
                nbrs[dir] = HasNeighborAt(DirectionHelpers.Cardinal(dir));
                if (firstNbrDir == -1 && nbrs[dir])
                {
                    firstNbrDir = dir;
                }
            }

            // If we have no neighbors: Terminate immediately
            if (firstNbrDir == -1)
            {
                terminated.SetValue(true);
                return;
            }

            // We have at least one neighbor: Find all empty regions we are adjacent to
            int regionIdx = 0;
            int curDir = firstNbrDir;
            for (int i = 0; i < 6; i++)
            {
                bool nbrAtCurrent = nbrs[curDir];
                bool nbrAtNext = nbrs[(curDir + 1) % 6];

                if (nbrAtCurrent && !nbrAtNext)
                {
                    // This is our boundary predecessor
                    boundaryNbrs[regionIdx, 0].SetValue(curDir);
                }
                else if (!nbrAtCurrent && nbrAtNext)
                {
                    // The next neighbor is our boundary successor
                    boundaryNbrs[regionIdx, 1].SetValue((curDir + 1) % 6);
                    // Compute the angle as number of 60° counter-clockwise turns mod 5
                    int oppositePredDir = (boundaryNbrs[regionIdx, 0].GetCurrentValue() + 3) % 6;
                    int numTurns = ((curDir + 1) + 6 - oppositePredDir) % 6;
                    // 0, 1, 2, 3 turns means positive angle, 4 means negative angle (-1 = 4 mod 5)
                    boundaryAngles[regionIdx].SetValue(numTurns < 4 ? numTurns : 4);
                    regionIdx++;
                }

                curDir = (curDir + 1) % 6;
            }
            numBoundaries.SetValue(regionIdx);

            // Now setup the pin configuration based on the result
            PinConfiguration pc = GetPrevPinConfiguration();

            // If we have no boundaries, we are an inner particle
            // Simply establish the global circuit
            if (regionIdx == 0)
            {
                pc.SetToGlobal();
            }
            // Otherwise setup a partition set for each boundary
            // Partition set index will be equal to boundary index
            else
            {
                SetMainColor(candidate1Color);
                SetupBoundaryCircuit(ref pc);
            }

            SetPlannedPinConfiguration(pc); // This is necessary because pc might be a different object

            // Initialize and start first phase of leader election
            for (int boundary = 0; boundary < numBoundaries.GetCurrentValue(); boundary++)
            {
                // Start as candidate
                isCandidate[boundary].SetValue(true);
                // Toss coin and send beep if we got HEADS
                if (TossCoin(boundary))
                {
                    SendBeepOnPartitionSet(boundary);
                }
            }

            // Continue with round 1
            round.SetValue(1);
        }

        #region LeaderElection

        private void LE1Activate0()
        {
            // Receive termination beep on global circuit
            PinConfiguration pc = GetPrevPinConfiguration();
            bool rcvGlobalBeep = ReceivedBeepOnPartitionSet(0);
            // First setup boundary circuit again (partition set ID = boundary index)
            SetupBoundaryCircuit(ref pc);
            SetPlannedPinConfiguration(pc);

            // if nobody beeped, continue to Phase 2
            if (!rcvGlobalBeep)
            {
                phase.SetValue(Phase.LE2);
                Debug.Log("PROCEED TO PHASE 2");
            }

            // Every candidate has to toss a coin again and beep if the result is HEADS
            // If we are starting Phase 2, make every boundary particle a Phase 2 candidate
            StartLERound(!rcvGlobalBeep);

            round.SetValue(1);
        }

        private void LE1Activate1()
        {
            // Receive HEADS beep and send TAILS beep
            LEActivate1();
            round.SetValue(2);
        }

        private void LE1Activate2()
        {
            // Receive TAILS beep on each boundary
            PinConfiguration pc = GetPrevPinConfiguration();
            bool beepOnGlobal = LEReceiveTails();

            // Establish global circuit and beep if we are not finished yet
            SetGlobalCircuitAndBeep(beepOnGlobal);

            round.SetValue(0);
        }

        private void LE2Activate0()
        {
            // Receive termination beep on global circuit
            bool rcvGlobalBeep = ReceivedBeepOnPartitionSet(0);

            // If nobody beeped, check if we have to start the next iteration
            bool nextIteration = false;
            if (!rcvGlobalBeep)
            {
                phase2Count.SetValue(phase2Count + 1);
                nextIteration = phase2Count.GetCurrentValue() < kappa;
            }

            // Proceed to Sum Computation phase if the leader election is over
            if (!rcvGlobalBeep && !nextIteration)
            {
                phase.SetValue(Phase.SUM);
                Debug.Log("PROCEED TO SUM COMPUTATION");

                // Setup Sum Computation
                // Particles on at least one boundary become active
                for (int boundary = 0; boundary < numBoundaries; boundary++)
                {
                    isActive[boundary].SetValue(true);
                }
                // Setup the pin configuration
                SetPASCPinConfig();

                // If we are the leader of the boundary: Beep on primary partition set
                for (int boundary = 0; boundary < numBoundaries; boundary++)
                {
                    if (isCandidate[boundary])
                        SendBeepOnPartitionSet(boundary * 2);
                }

                round.SetValue(1);
                return;
            }

            // Did not proceed to Sum Computation, so carry on or start next iteration
            // Have to continue or start next iteration
            // First setup boundary circuit again (partition set ID = boundary index)
            PinConfiguration pc = GetNextPinConfiguration();
            SetupBoundaryCircuit(ref pc);
            SetPlannedPinConfiguration(pc);
            // Every candidate has to toss a coin again and beep if the result is HEADS
            // If we start a new iteration, become a Phase 2 candidate again
            StartLERound(nextIteration);

            round.SetValue(1);
        }

        private void LE2Activate1()
        {
            // Receive HEADS beep and send TAILS beep
            LEActivate1();
            round.SetValue(2);
        }

        private void LE2Activate2()
        {
            // Receive TAILS beep on each boundary
            LEReceiveTails();

            // Phase 2 candidates toss coins and send beep on HEADS
            StartLERound(false, true);

            round.SetValue(3);
        }

        private void LE2Activate3()
        {
            LEActivate1(true);
            round.SetValue(4);
        }

        private void LE2Activate4()
        {
            // Receive TAILS beep on each boundary
            bool beepOnGlobal = LEReceiveTails(true);

            // Establish global circuit and beep if we are not finished yet
            SetGlobalCircuitAndBeep(beepOnGlobal);

            round.SetValue(0);
        }

        // Makes each candidate or Phase 2 candidate toss a coin and send a
        // beep on the corresponding partition set if it tossed HEADS. Also
        // makes every boundary particle a Phase 2 candidate if required.
        private void StartLERound(bool becomePhase2Cand = false, bool isPhase2Cand = false)
        {
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (!isPhase2Cand && isCandidate[boundary] || isPhase2Cand && isPhase2Candidate[boundary])
                {
                    if (TossCoin(boundary))
                    {
                        SendBeepOnPartitionSet(boundary);
                    }
                }

                // If we start a new iteration, become a Phase 2 candidate again
                if (becomePhase2Cand)
                {
                    isPhase2Candidate[boundary].SetValue(true);
                }
            }
        }

        // Procedure in round 1 is shared between Phase 1 and 2.
        // Can also be reused for Phase 2 candidates
        private void LEActivate1(bool phase2 = false)
        {
            // Receive HEADS beep on each boundary
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (ReceivedBeepOnPartitionSet(boundary))
                {
                    receivedHeadsBeep[boundary].SetValue(true);
                }
                // Send beep if we are a candidate and have tossed TAILS
                if (!heads[boundary] && (!phase2 && isCandidate[boundary] || phase2 && isPhase2Candidate[boundary]))
                {
                    SendBeepOnPartitionSet(boundary);
                }
            }
        }

        // Receives the TAILS beep for each boundary and revokes candidacy
        // if both HEADS and TAILS were received for that boundary. Also
        // resets the HEADS storage and returns true if HEADS and TAILS
        // were received on any boundary. Can be used by Phase 1 and
        // Phase 2 candidates
        private bool LEReceiveTails(bool phase2 = false)
        {
            bool rcvHeadsAndTails = false;
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                bool rcvTails = ReceivedBeepOnPartitionSet(boundary);
                // If both HEADS and TAILS were received: Phase is not finished yet
                // Candidate with TAILS must retire and beep must be sent
                if (receivedHeadsBeep[boundary] && rcvTails)
                {
                    rcvHeadsAndTails = true;
                    if (!heads[boundary])
                    {
                        // Revoke Phase 1 or Phase 2 candidacy
                        if (!phase2 && isCandidate[boundary])
                        {
                            isCandidate[boundary].SetValue(false);
                        }
                        else if (phase2 && isPhase2Candidate[boundary])
                        {
                            isPhase2Candidate[boundary].SetValue(false);
                        }
                    }
                }

                // Reset flag
                receivedHeadsBeep[boundary].SetValue(false);
            }
            return rcvHeadsAndTails;
        }

        #endregion

        #region SumComputation

        private void SCActivate0()
        {
            // Receive global termination beep (no beep = we are finished)
            if (!ReceivedBeepOnPartitionSet(0))
            {
                terminated.SetValue(true);
                return;
            }

            // No termination yet: Continue with next iteration
            SetPASCPinConfig();

            // Leader particles send beep on primary partition sets
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (isCandidate[boundary])
                {
                    SendBeepOnPartitionSet(boundary * 2);
                }
            }

            round.SetValue(1);
        }

        private void SCActivate1()
        {
            // Active particles listen for beep on secondary circuit
            // If they receive a beep, they send an echo and remember
            // to become passive in this iteration
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (scMode[boundary] != SCMode.COMPUTE) continue;
                if (isActive[boundary] && ReceivedBeepOnPartitionSet(boundary * 2 + 1))
                {
                    SendBeepOnPartitionSet(boundary * 2 + 1);
                    becomePassive[boundary].SetValue(true);
                }
            }

            round.SetValue(2);
        }

        private void SCActivate2()
        {
            // All boundary particles listen for echo beep on primary and secondary partition set
            // If a beep was sent, active particles becoming passive will cut
            // their connections to their successors and start transmitting
            // their partial sum to the predecessors
            // If no beep was sent, the leader has the final result and will
            // start transmitting it now

            PinConfiguration planned = GetNextPinConfiguration();
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (scMode[boundary] != SCMode.COMPUTE) continue;
                bool rcvEcho = ReceivedBeepOnPartitionSet(boundary * 2) || ReceivedBeepOnPartitionSet(boundary * 2 + 1);
                if (rcvEcho)
                {
                    // Active particles becoming passive setup pin configuration for
                    // transmission of partial sum and transmit 1 if that is the value
                    if (isActive[boundary] && becomePassive[boundary])
                    {
                        // Turn primary partition set into one set containing only
                        // connections to the predecessor
                        Direction d = DirectionHelpers.Cardinal(boundaryNbrs[boundary, 0]);
                        planned.MakePartitionSet(new Pin[] {
                            planned.GetPinAt(d, 2),
                            planned.GetPinAt(d, 3)
                        }, boundary * 2);
                        planned.SetPartitionSetPosition(boundary * 2, new Vector2(d.ToInt() * 60 + 30, 0.6f));

                        // Send beep if current angle value is 1
                        if (boundaryAngles[boundary] == 1)
                        {
                            SendBeepOnPartitionSet(boundary * 2);
                        }
                    }
                }
                else
                {
                    // Termination: Leader starts transmitting final result
                    scMode[boundary].SetValue(SCMode.FINAL);
                    if (isCandidate[boundary])
                    {
                        // Connect primary and secondary circuit
                        Direction d = DirectionHelpers.Cardinal(boundaryNbrs[boundary, 1]);
                        planned.MakePartitionSet(new Pin[] {
                            planned.GetPinAt(d, 0),
                            planned.GetPinAt(d, 1)
                        }, boundary * 2);
                        planned.SetPartitionSetPosition(boundary * 2, new Vector2(d.ToInt() * 60 - 30, 0.6f));
                        // Send beep if final angle result is 1
                        if (boundaryAngles[boundary] == 1)
                        {
                            SendBeepOnPartitionSet(boundary * 2);
                        }
                    }
                    else
                    {
                        // Set final result to 0 in case no beep is sent
                        boundaryAngles[boundary].SetValue(0);
                    }
                }
            }

            round.SetValue(3);
        }

        private void SCActivate345()
        {
            // In COMPUTE phase: Active boundary particles send and receive
            // partial sums on primary partition set
            // In FINAL phase: All boundary particles listen for final result
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (scMode[boundary] == SCMode.WAIT) continue;
                if (scMode[boundary] == SCMode.COMPUTE)
                {
                    if (isActive[boundary])
                    {
                        // Particles that stay active listen for beeps,
                        // particles that will become passive send them
                        if (!becomePassive[boundary] && ReceivedBeepOnPartitionSet(boundary * 2))
                        {
                            // Update partial sum mod 5 (round 3 receives angle 1, round 4 receives angle 2, etc.)
                            boundaryAngles[boundary].SetValue((boundaryAngles[boundary] + round - 2) % 5);
                        }
                        else if (becomePassive[boundary] && boundaryAngles[boundary] == round - 1)
                        {
                            SendBeepOnPartitionSet(boundary * 2);
                        }
                    }
                }
                else
                {
                    // SC mode is FINAL
                    // Leader sends final result, other particles listen
                    if (isCandidate[boundary] && boundaryAngles[boundary] == round - 1)
                    {
                        SendBeepOnPartitionSet(boundary * 2);
                    }
                    else if (!isCandidate[boundary] && ReceivedBeepOnPartitionSet(boundary * 2))
                    {
                        boundaryAngles[boundary].SetValue(round - 2);
                    }
                }
            }

            round.SetValue(round + 1);
        }

        private void SCActivate6()
        {
            // Receive angle value 4 if we are computing or receiving the final result
            //PinConfiguration pc = GetCurrentPinConfiguration();
            bool beepOnGlobal = false;
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (scMode[boundary] == SCMode.WAIT) continue;
                if (scMode[boundary] == SCMode.COMPUTE)
                {
                    beepOnGlobal = true;
                    if (isActive[boundary])
                    {
                        // Particles that stay active listen for beeps
                        if (!becomePassive[boundary] && ReceivedBeepOnPartitionSet(boundary * 2))
                        {
                            // Update partial sum mod 5 (round 6 receives angle 4)
                            boundaryAngles[boundary].SetValue((boundaryAngles[boundary] + 4) % 5);
                        }
                        // Particles that become passive do so now
                        if (becomePassive[boundary])
                        {
                            isActive[boundary].SetValue(false);
                            becomePassive[boundary].SetValue(false);
                        }
                    }
                }
                else
                {
                    // SC mode is FINAL
                    // Receive final bit of result
                    if (ReceivedBeepOnPartitionSet(boundary * 2))
                    {
                        boundaryAngles[boundary].SetValue(4);
                    }
                    scMode[boundary].SetValue(SCMode.WAIT);
                }
            }

            // Establish global circuit and beep if we are not done yet
            if (numBoundaries > 0)
            {
                PinConfiguration pc = GetNextPinConfiguration();
                pc.SetToGlobal();
                pc.ResetPartitionSetPlacement();
                if (beepOnGlobal)
                    SendBeepOnPartitionSet(0);
            }

            round.SetValue(0);
        }

        #endregion


        // Changes the pin configuration such that partition set i is part
        // of the boundary circuit for boundary i. Has no effect for
        // particles that have no boundaries
        private void SetupBoundaryCircuit(ref PinConfiguration pc)
        {
            if (numBoundaries.GetCurrentValue() == 0)
                return;

            PinConfiguration loadedPC = boundaryPC;
            if (loadedPC is null)
            {
                for (int boundary = 0; boundary < numBoundaries.GetCurrentValue(); boundary++)
                {
                    Direction predDir = DirectionHelpers.Cardinal(boundaryNbrs[boundary, 0].GetCurrentValue());
                    Direction succDir = DirectionHelpers.Cardinal(boundaryNbrs[boundary, 1].GetCurrentValue());
                    pc.MakePartitionSet(new Pin[] {
                        pc.GetPinAt(predDir, 2),
                        pc.GetPinAt(predDir, 3),
                        pc.GetPinAt(succDir, 0),
                        pc.GetPinAt(succDir, 1)
                    }, boundary);
                    // Place the partition set close to the boundary
                    float angle = (predDir.ToInt() + predDir.DistanceTo(succDir) / 4f) * 60f;
                    float dist = 0.5f;
                    if (predDir == succDir)
                        dist = 0f;
                    pc.SetPartitionSetPosition(boundary, new Vector2(angle, dist));
                }
                boundaryPC.SetValue(pc);
            }
            else
            {
                pc = loadedPC;
            }
        }

        // Sets up the PASC pin configuration based on whether the particle is active
        // and a leader or not. For boundary i, the partition set with ID 2*i is the
        // primary partition set and the partition set with ID 2*i + 1 is the secondary
        // partition set. The primary partition set is always connected to the successor
        // on the pin that is closest to the empty region.
        private void SetPASCPinConfig()
        {
            if (numBoundaries == 0)
                return;

            PinConfiguration pc = GetNextPinConfiguration();
            pc.SetToSingleton();
            pc.ResetPartitionSetPlacement();
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                Direction ds = DirectionHelpers.Cardinal(boundaryNbrs[boundary, 1]);
                Direction dp = DirectionHelpers.Cardinal(boundaryNbrs[boundary, 0]);
                if (isCandidate[boundary])
                {
                    // If we are still a candidate, that means we are the leader of the boundary
                    // Then we do not connect any pins, we just mark pins as primary and
                    // secondary circuits
                    pc.MakePartitionSet(new Pin[] { pc.GetPinAt(ds, 0) }, boundary * 2);
                    pc.MakePartitionSet(new Pin[] { pc.GetPinAt(ds, 1) }, boundary * 2 + 1);
                }
                else
                {
                    // We are not the leader of the boundary
                    Pin pSucc = pc.GetPinAt(ds, 0);
                    Pin sSucc = pc.GetPinAt(ds, 1);
                    Pin pPred = pc.GetPinAt(dp, 3);
                    Pin sPred = pc.GetPinAt(dp, 2);
                    if (isActive[boundary].GetCurrentValue())
                    {
                        // Active particles cross their connections
                        pc.MakePartitionSet(new Pin[] { pSucc, sPred }, boundary * 2);
                        pc.MakePartitionSet(new Pin[] { sSucc, pPred }, boundary * 2 + 1);
                    }
                    else
                    {
                        // Passive particles set their connections parallel
                        pc.MakePartitionSet(new Pin[] { pSucc, pPred }, boundary * 2);
                        pc.MakePartitionSet(new Pin[] { sSucc, sPred }, boundary * 2 + 1);
                    }
                }

                // Set placement of the partition sets
                // The two sets have the same angle but the primary partition set
                // is closer to the boundary
                float angleP = (dp.ToInt() + dp.DistanceTo(ds) / 4f) * 60f;
                float angleS = angleP;
                float distP = 0.7f;
                float distS = 0.4f;
                // If we are an endpoint (only one neighbor) and the two
                // circuits cross: Put the partition sets next to each other
                if (dp == ds)
                {
                    if (isActive[boundary].GetCurrentValue())
                    {
                        angleP -= 15;
                        angleS += 15;
                        distP = 0.6f;
                        distS = 0.6f;
                    }
                    else
                    {
                        distP = 0.3f;
                        distS = 0.7f;
                    }
                }
                pc.SetPartitionSetPosition(boundary * 2, new Vector2(angleP, distP));
                pc.SetPartitionSetPosition(boundary * 2 + 1, new Vector2(angleS, distS));
            }
        }

        // Tosses a fair coin for the given boundary and sets the corresponding
        // HEADS flag to true if the result is HEADS. Also returns the result.
        private bool TossCoin(int boundary)
        {
            bool result = Random.Range(0.0f, 1.0f) <= 0.5f;
            heads[boundary].SetValue(result);
            return result;
        }

        // Sets the given PinConfiguration to the global configuration,
        // makes it the planned one, and sends a beep if specified.
        // Only affects particles with at least one boundary.
        private void SetGlobalCircuitAndBeep(bool beep)
        {
            if (numBoundaries > 0)
            {
                PinConfiguration pc = GetNextPinConfiguration();
                pc.SetToGlobal();
                pc.ResetPartitionSetPlacement();
                if (beep)
                {
                    SendBeepOnPartitionSet(0);
                }
            }
        }

        private void SetColor()
        {
            if (numBoundaries.GetCurrentValue() == 0)
                return;
            
            if (terminated.GetCurrentValue())
            {
                // Set final color
                // Find boundary and leader status
                // 0 = inner boundary
                // 1 = outer boundary
                // 2 = inner boundary leader
                // 3 = outer boundary leader
                int score = 0;
                for (int b = 0; b < numBoundaries; b++)
                {
                    bool leader = isCandidate[b];
                    bool outer = boundaryAngles[b] == 1;
                    int s = (leader ? 2 : 0) + (outer ? 1 : 0);
                    if (s > score)
                        score = s;
                }
                Color[] colors = new Color[] {
                    ColorData.Particle_Orange,
                    ColorData.Particle_Aqua,
                    ColorData.Particle_Red,
                    ColorData.Particle_Green
                };
                SetMainColor(colors[score]);
                
                return;
            }

            if (phase.GetCurrentValue() == Phase.LE1 || phase.GetCurrentValue() == Phase.LE2)
            {
                // Candidate color if we are a candidate on 1-3 boundaries
                // P2 candidate color if we are only a Phase 2 candidate on any boundary
                // Retired color otherwise
                int numCandidacies = 0;
                bool isAP2Candidate = false;
                for (int b = 0; b < numBoundaries.GetCurrentValue(); b++)
                {
                    if (isCandidate[b].GetCurrentValue())
                        numCandidacies++;
                    if (isPhase2Candidate[b].GetCurrentValue())
                        isAP2Candidate = true;
                }
                if (numCandidacies > 0)
                {
                    SetMainColor(numCandidacies == 1 ? candidate1Color : (numCandidacies == 2 ? candidate2Color : candidate3Color));
                }
                else
                    SetMainColor(isAP2Candidate ? phase2CandColor : retiredColor);
            }
            else
            {
                // Candidate color is determined by number of candidacies
                int numCandidacies = 0;
                for (int i = 0; i < numBoundaries; i++)
                {
                    if (isCandidate[i])
                    {
                        numCandidacies++;
                    }
                }
                if (numCandidacies == 1)
                    SetMainColor(candidate1Color);
                else if (numCandidacies == 2)
                    SetMainColor(candidate2Color);
                else if (numCandidacies == 3)
                    SetMainColor(candidate3Color);
                else
                {
                    // Not a candidate: Check if we are active on any boundary
                    bool active = false;
                    for (int i = 0; i < numBoundaries; i++)
                    {
                        if (isActive[i].GetCurrentValue())
                        {
                            active = true;
                            break;
                        }
                    }
                    SetMainColor(active ? activeColor : passiveColor);
                }
            }
        }
    }
} // namespace AS2.Algos.BoundaryTest
