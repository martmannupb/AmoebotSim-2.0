using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoundaryTestAlgo
{
    public enum Phase { LE1, LE2, SUM }

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
        private static readonly Color candidateColor = ColorData.Particle_Green;
        private static readonly Color retiredColor = ColorData.Particle_BlueDark;
        private static readonly Color phase2CandColor = ColorData.Particle_Blue;

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
        private static readonly int kappa = 3;                  // Number of repetitions of the second phase (one iteration is always executed)
        private ParticleAttribute<bool>[] isCandidate;          // Stores candidate flag for each of the boundaries
        private ParticleAttribute<bool>[] isPhase2Candidate;    // Stores phase 2 candidate flag for each of the boundaries
        private ParticleAttribute<bool>[] heads;                // Last coin toss result for each boundary
        private ParticleAttribute<bool>[] receivedHeadsBeep;    // HEADS flag for remembering coin toss beep for each boundary
        private ParticleAttribute<int> phase2Count;             // Counter to execute the second phase kappa times

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

            SetMainColor(startColor);
        }

        public override int PinsPerEdge => 4;

        public override void Activate()
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
        }

        private void FirstActivation()
        {
            // Determine how many boundaries we are on and set up the initial pin configuration
            // First find all neighbors and the direction of the first encountered neighbor
            bool[] nbrs = new bool[6];
            int firstNbrDir = -1;
            for (int dir = 0; dir < 6; dir++)
            {
                nbrs[dir] = HasNeighborAt(dir);
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
                    int oppositePredDir = (boundaryNbrs[regionIdx, 0].GetValue_After() + 3) % 6;
                    int numTurns = ((curDir + 1) + 6 - oppositePredDir) % 6;
                    // 0, 1, 2, 3 turns means positive angle, 5 means negative angle (-1 = 4 mod 5)
                    boundaryAngles[regionIdx].SetValue(numTurns < 4 ? numTurns : 4);
                    regionIdx++;
                }

                curDir = (curDir + 1) % 6;
            }
            numBoundaries.SetValue(regionIdx);

            // Now setup the pin configuration based on the result
            PinConfiguration pc = GetCurrentPinConfiguration();

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
                SetMainColor(candidateColor);
                SetupBoundaryCircuit(pc);
            }

            SetPlannedPinConfiguration(pc);

            // Initialize and start first phase of leader election
            for (int boundary = 0; boundary < numBoundaries.GetValue_After(); boundary++)
            {
                // Start as candidate
                isCandidate[boundary].SetValue(true);
                // Toss coin and send beep if we got HEADS
                if (TossCoin(boundary))
                {
                    pc.SendBeepOnPartitionSet(boundary);
                }
            }

            // Continue with round 1
            round.SetValue(1);
        }

        private void LE1Activate0()
        {
            // Receive termination beep on global circuit
            PinConfiguration pc = GetCurrentPinConfiguration();
            bool rcvGlobalBeep = pc.ReceivedBeepOnPartitionSet(0);
            // First setup boundary circuit again (partition set ID = boundary index)
            SetupBoundaryCircuit(pc);
            SetPlannedPinConfiguration(pc);

            // if nobody beeped, continue to Phase 2
            if (!rcvGlobalBeep)
            {
                phase.SetValue(Phase.LE2);
                Debug.Log("PROCEED TO PHASE 2");
            }

            // Every candidate has to toss a coin again and beep if the result is HEADS
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (isCandidate[boundary])
                {
                    if (TossCoin(boundary))
                    {
                        pc.SendBeepOnPartitionSet(boundary);
                    }
                }

                // If we have to start phase 2, become a phase 2 candidate
                if (!rcvGlobalBeep)
                {
                    isPhase2Candidate[boundary].SetValue(true);
                }
            }

            // Visualization
            if (!rcvGlobalBeep && numBoundaries > 0)
            {
                bool isACandidate = false;
                for (int i = 0; i < numBoundaries; i++)
                {
                    if (isCandidate[i])
                    {
                        isACandidate = true;
                        break;
                    }
                }
                if (!isACandidate)
                {
                    SetMainColor(phase2CandColor);
                }
            }

            round.SetValue(1);
        }

        private void LE1Activate1()
        {
            // Receive HEADS beep on each boundary
            PinConfiguration pc = GetCurrentPinConfiguration();
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (pc.ReceivedBeepOnPartitionSet(boundary))
                {
                    receivedHeadsBeep[boundary].SetValue(true);
                }
                // Send beep if we are a candidate and have tossed TAILS
                if (isCandidate[boundary] && !heads[boundary])
                {
                    SetPlannedPinConfiguration(pc);
                    pc.SendBeepOnPartitionSet(boundary);
                }
            }

            round.SetValue(2);
        }

        private void LE1Activate2()
        {
            // Receive TAILS beep on each boundary
            PinConfiguration pc = GetCurrentPinConfiguration();
            bool beepOnGlobal = false;
            bool isCandidateOnABoundary = false;
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                bool rcvTails = pc.ReceivedBeepOnPartitionSet(boundary);
                // If both HEADS and TAILS were received: Phase is not finished yet
                // Candidate with TAILS must retire and beep must be sent
                if (receivedHeadsBeep[boundary] && rcvTails)
                {
                    beepOnGlobal = true;
                    if (isCandidate[boundary] && !heads[boundary])
                    {
                        isCandidate[boundary].SetValue(false);
                    }
                }

                // Reset flag
                receivedHeadsBeep[boundary].SetValue(false);

                // Visualization
                if (isCandidate[boundary].GetValue_After())
                {
                    isCandidateOnABoundary = true;
                }
            }

            // Visualization
            if (numBoundaries > 0 && !isCandidateOnABoundary)
            {
                SetMainColor(retiredColor);
            }

            // Establish global circuit and beep if we are not finished yet
            if (numBoundaries > 0)
            {
                pc.SetToGlobal();
                SetPlannedPinConfiguration(pc);
                if (beepOnGlobal)
                {
                    pc.SendBeepOnPartitionSet(0);
                }
            }

            round.SetValue(0);
        }

        private void LE2Activate0()
        {
            // Receive termination beep on global circuit
            PinConfiguration pc = GetCurrentPinConfiguration();
            bool rcvGlobalBeep = pc.ReceivedBeepOnPartitionSet(0);
            // First setup boundary circuit again (partition set ID = boundary index)
            SetupBoundaryCircuit(pc);
            SetPlannedPinConfiguration(pc);

            // If nobody beeped, check if we have to start the next iteration
            bool nextIteration = false;
            if (!rcvGlobalBeep)
            {
                phase2Count.SetValue(phase2Count + 1);
                nextIteration = phase2Count.GetValue_After() < kappa;
            }

            // Proceed to Sum Computation phase if the leader election is over
            if (!rcvGlobalBeep && !nextIteration)
            {
                phase.SetValue(Phase.SUM);
                Debug.Log("PROCEED TO SUM COMPUTATION");
                if (numBoundaries > 0)
                {
                    int numCandidacies = 0;
                    for (int i = 0; i < numBoundaries; i++)
                    {
                        if (isCandidate[i])
                        {
                            numCandidacies++;
                        }
                    }
                    if (numCandidacies == 0)
                        SetMainColor(retiredColor);
                    else if (numCandidacies == 1)
                        SetMainColor(candidateColor);
                    else if (numCandidacies == 2)
                        SetMainColor(ColorData.Particle_Red);
                    else if (numCandidacies == 3)
                        SetMainColor(ColorData.Particle_Purple);
                }
                return;
            }

            // Did not proceed to Sum Computation, so carry on or start next iteration
            // Have to continue or start next iteration
            // Every candidate has to toss a coin again and beep if the result is HEADS
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (isCandidate[boundary])
                {
                    if (TossCoin(boundary))
                    {
                        pc.SendBeepOnPartitionSet(boundary);
                    }
                }

                // If we start a new iteration, become a Phase 2 candidate again
                if (nextIteration)
                {
                    isPhase2Candidate[boundary].SetValue(true);
                }
            }

            // Visualization
            if (!rcvGlobalBeep && numBoundaries > 0)
            {
                bool isACandidate = false;
                bool isAPhase2Candidate = false;
                for (int i = 0; i < numBoundaries; i++)
                {
                    if (isCandidate[i])
                    {
                        isACandidate = true;
                    }
                    if (isPhase2Candidate[i].GetValue_After())
                    {
                        isAPhase2Candidate = true;
                    }
                }
                if (!isACandidate)
                {
                    if (isAPhase2Candidate)
                    {
                        SetMainColor(phase2CandColor);
                    }
                    else
                    {
                        SetMainColor(retiredColor);
                    }
                }
            }

            round.SetValue(1);
        }

        private void LE2Activate1()
        {
            // Receive HEADS beep on each boundary
            PinConfiguration pc = GetCurrentPinConfiguration();
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (pc.ReceivedBeepOnPartitionSet(boundary))
                {
                    receivedHeadsBeep[boundary].SetValue(true);
                }
                // Send beep if we are a candidate and have tossed TAILS
                if (isCandidate[boundary] && !heads[boundary])
                {
                    SetPlannedPinConfiguration(pc);
                    pc.SendBeepOnPartitionSet(boundary);
                }
            }

            round.SetValue(2);
        }

        private void LE2Activate2()
        {
            // Receive TAILS beep on each boundary
            PinConfiguration pc = GetCurrentPinConfiguration();
            bool isACandidate = false;
            bool isAPhase2Candidate = false;
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                bool rcvTails = pc.ReceivedBeepOnPartitionSet(boundary);
                // If both HEADS and TAILS were received: Candidate with TAILS must retire
                // Candidate with TAILS must retire and beep must be sent
                if (receivedHeadsBeep[boundary] && rcvTails)
                {
                    if (isCandidate[boundary] && !heads[boundary])
                    {
                        isCandidate[boundary].SetValue(false);
                    }
                }

                // Reset flag
                receivedHeadsBeep[boundary].SetValue(false);
            }

            // Phase 2 candidates toss coins and send beep on HEADS
            SetPlannedPinConfiguration(pc);
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (isPhase2Candidate[boundary])
                {
                    if (TossCoin(boundary))
                    {
                        pc.SendBeepOnPartitionSet(boundary);
                    }
                }

                // Visualization
                if (isCandidate[boundary].GetValue_After())
                {
                    isACandidate = true;
                }
                if (isPhase2Candidate[boundary].GetValue_After())
                {
                    isAPhase2Candidate = true;
                }
            }

            // Visualization
            if (numBoundaries > 0)
            {
                if (!isACandidate)
                {
                    if (isAPhase2Candidate)
                    {
                        SetMainColor(phase2CandColor);
                    }
                    else
                    {
                        SetMainColor(retiredColor);
                    }
                }
            }

            round.SetValue(3);
        }

        private void LE2Activate3()
        {
            // Receive HEADS beep on each boundary
            PinConfiguration pc = GetCurrentPinConfiguration();
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                if (pc.ReceivedBeepOnPartitionSet(boundary))
                {
                    receivedHeadsBeep[boundary].SetValue(true);
                }
                // Send beep if we are a Phase 2 candidate and have tossed TAILS
                if (isPhase2Candidate[boundary] && !heads[boundary])
                {
                    SetPlannedPinConfiguration(pc);
                    pc.SendBeepOnPartitionSet(boundary);
                }
            }

            round.SetValue(4);
        }

        private void LE2Activate4()
        {
            // Receive TAILS beep on each boundary
            PinConfiguration pc = GetCurrentPinConfiguration();
            bool beepOnGlobal = false;
            bool isACandidate = false;
            bool isAPhase2Candidate = false;
            for (int boundary = 0; boundary < numBoundaries; boundary++)
            {
                bool rcvTails = pc.ReceivedBeepOnPartitionSet(boundary);
                // If both HEADS and TAILS were received: Phase is not finished yet
                // Phase 2 candidate with TAILS must retire and beep must be sent
                if (receivedHeadsBeep[boundary] && rcvTails)
                {
                    beepOnGlobal = true;
                    if (isPhase2Candidate[boundary] && !heads[boundary])
                    {
                        isPhase2Candidate[boundary].SetValue(false);
                    }
                }

                // Reset flag
                receivedHeadsBeep[boundary].SetValue(false);

                // Visualization
                if (isCandidate[boundary].GetValue_After())
                {
                    isACandidate = true;
                }
                if (isPhase2Candidate[boundary].GetValue_After())
                {
                    isAPhase2Candidate = true;
                }
            }

            // Visualization
            if (numBoundaries > 0 && !isACandidate)
            {
                if (isAPhase2Candidate)
                {
                    SetMainColor(phase2CandColor);
                }
                else
                {
                    SetMainColor(retiredColor);
                }
            }

            // Establish global circuit and beep if we are not finished yet
            if (numBoundaries > 0)
            {
                pc.SetToGlobal();
                SetPlannedPinConfiguration(pc);
                if (beepOnGlobal)
                {
                    pc.SendBeepOnPartitionSet(0);
                }
            }

            round.SetValue(0);
        }

        // Changes the pin configuration such that partition set i is part
        // of the boundary circuit for boundary i. Has no effect for
        // particles that have no boundaries
        private void SetupBoundaryCircuit(PinConfiguration pc)
        {
            for (int boundary = 0; boundary < numBoundaries.GetValue_After(); boundary++)
            {
                int predDir = boundaryNbrs[boundary, 0].GetValue_After();
                int succDir = boundaryNbrs[boundary, 1].GetValue_After();
                pc.MakePartitionSet(new Pin[] {
                        pc.GetPinAt(predDir, 2),
                        pc.GetPinAt(predDir, 3),
                        pc.GetPinAt(succDir, 0),
                        pc.GetPinAt(succDir, 1)
                    }, boundary);
            }
        }

        private bool TossCoin(int boundary)
        {
            bool result = Random.Range(0.0f, 1.0f) <= 0.5f;
            heads[boundary].SetValue(result);
            return result;
        }
    }

} // namespace BoundaryTestAlgo
