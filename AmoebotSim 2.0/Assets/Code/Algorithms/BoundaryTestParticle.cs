using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoundaryTestAlgo
{

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
        private ParticleAttribute<bool> firstActivation;    // Flag used to setup data on the very first activation (only used once)
        private ParticleAttribute<int> round;               // Round counter used to synchronize the particles in all phases
        private ParticleAttribute<int> numBoundaries;       // Number of boundaries the particle is a part of. Can be 0-3
        private ParticleAttribute<int>[,] boundaryNbrs;     // Directions of predecessors and successors for each boundary. Has dimensions 3x2
        private ParticleAttribute<int>[] boundaryAngles;    // Angle of the turn from the boundary predecessor to the boundary successor
                                                            // Measured in number of 60° counter-clockwise turns mod 5
        private ParticleAttribute<bool> terminated;         // Final termination flag

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

            terminated = CreateAttributeBool("Terminated", false);

            SetMainColor(ColorData.Particle_Black);
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
                for (int boundary = 0; boundary < regionIdx; boundary++)
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

            SetPlannedPinConfiguration(pc);
        }
    }

} // namespace BoundaryTestAlgo
