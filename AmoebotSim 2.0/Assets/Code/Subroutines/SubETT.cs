// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.PASC;

namespace AS2.Subroutines.ETT
{

    /// <summary>
    /// Set of possible comparison results.
    /// </summary>
    public enum Comparison
    {
        LESS, EQUAL, GREATER
    }

    /// <summary>
    /// Implements the Euler Tour Technique (ETT).
    /// See https://doi.org/10.1145/3662158.3662776.<br/>
    /// Given a tree in the amoebot structure, we replace every edge with two
    /// directed edges in opposing directions. The resulting directed graph
    /// has an Euler cycle that traverses each original edge exactly twice.
    /// For example, from each amoebot, we can visit its neighbors in
    /// counter-clockwise order to create such a cycle. By splitting this
    /// cycle at one position, we obtain an Euler tour that starts and ends
    /// at the split position. The Euler Tour Technique allows the computation
    /// of various tree functions, which we can realize with circuits.
    /// In particular, we establish a PASC circuit along the Euler tour and
    /// treat each visit of an amoebot as one participant. Each amoebot can
    /// mark one of its edges/participants as active.
    /// <para>
    /// The subroutine is setup as follows:
    /// <list type="bullet">
    /// <item>
    ///     There is a list of neighbor directions with 1-6 elements. 
    ///     Each element identifies an outgoing and an incoming edge.
    ///     The order of the directions is counter-clockwise, i.e.,
    ///     the incoming edge of direction i is connected to the same
    ///     PASC instance as the outgoing edge of direction (i + 1) mod m,
    ///     where m is the number of neighbor directions.
    /// </item>
    /// <item>
    ///     One outgoing edge can be marked, making its PASC instance
    ///     initially active. The edge's direction is identified by its index
    ///     in the neighbor list.
    /// </item>
    /// <item>
    ///     The instance connecting the outgoing edge of the first neighbor
    ///     and the incoming edge of the last neighbor can be split to turn
    ///     the Euler cycle into an Euler tour. This should usually be done
    ///     for one amoebot on the cycle.
    /// </item>
    /// <item>
    ///     Each pair of incoming and outgoing edges has a primary and a
    ///     secondary partition set. If d is the direction of the outoing edge,
    ///     then the primary partition set has ID 2*d and the secondary
    ///     partition set has ID 2*d + 1. This means the IDs 0,...,11 are
    ///     reserved for the PASC partition sets of outgoing edges. In the
    ///     special case that the first outgoing and the last incoming edge
    ///     are split, the IDs 12 and 13 are used for the last incoming edge's
    ///     partition sets.
    /// </item>
    /// <item>
    ///     The PASC execution uses one round to send the main beep and a
    ///     second round to send a termination beep. The second beep is sent
    ///     on both circuits by all instances that became passive due to the
    ///     first beep. Once no beep is received in the termination round,
    ///     the procedure is finished.
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// This is how the subroutine should be used:
    /// <list type="bullet">
    /// <item>
    ///     Call <see cref="Init(Direction[], int, bool)"/> to setup the subroutine object for the
    ///     given directions.
    /// </item>
    /// <item>
    ///     Call <see cref="SetupPinConfig(PinConfiguration)"/> to construct all required
    ///     partition sets for the next beep. The constructed partition sets should not be changed,
    ///     otherwise <see cref="ActivateSend"/> might not work correctly.
    /// </item>
    /// <item>
    ///     Call <see cref="ActivateSend"/> after setting up the pin configuration to send the next
    ///     beep. In a termination round, this should be called by every participant. Otherwise, it
    ///     should only be called by the splitting amoebots that should beep (usually the root).
    ///     You can check the type of the current round using <see cref="IsTerminationRound"/>.
    /// </item>
    /// <item>
    ///     In the activation immediately after <see cref="ActivateSend"/> has been called,
    ///     <see cref="ActivateReceive"/> must be called. This will make the new bits and
    ///     comparison results available or terminate the procedure.
    /// </item>
    /// <item>
    ///     The procedure is finished as soon as <see cref="IsFinished"/> returns <c>true</c>.
    ///     This method can be called immediately after <see cref="ActivateReceive"/>.
    /// </item>
    /// <item>
    ///     The received bits and comparison results can be obtained from the various
    ///     inspection methods, e.g., <see cref="GetComparisonResult(Direction)"/>,
    ///     <see cref="GetDiffBit(Direction, bool)"/>.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    public class SubETT : Subroutine
    {

        ParticleAttribute<Direction>[] neighbors = new ParticleAttribute<Direction>[6];     // We have up to 6 neighbors, the order in this array tells us which belong together
        ParticleAttribute<int> numNeighbors;    // The number of neighbors (we store this for convenience)
        ParticleAttribute<int> markedEdge;      // The neighbor index of the outgoing edge that is marked
        ParticleAttribute<bool> split;          // Whether the edge going out into the first neighbor direction should be separated from the last incoming edge
        ParticleAttribute<bool> active;         // Whether the marked edge's PASC instance is still active
        ParticleAttribute<bool> becamePassive;  // Remember whether we became passive in the last iteration
        ParticleAttribute<bool> finished;       // Whether we are finished (happens when no beep is sent in the second round)
        ParticleAttribute<bool> terminationRound;   // Whether this is a round in which we send a termination beep

        ParticleAttribute<Comparison>[] comparisons = new ParticleAttribute<Comparison>[7]; // Comparison results for the 6 directions, comparing OUT to IN. The 7th entry compares the sum at the split edge (|Q|) to 0
        ParticleAttribute<string> bits;         // String storing the up to 12 bits. For direction d with int representation i, character 2*i stores the bit of OUT - IN at direction d
                                                // and character 2*i + 1 stores the bit of IN - OUT at direction d. A bit can be '0', '1' or '-' if it does not exist.
                                                // The additional bit at position 12 is the sum bit reserved for amoebots that split their first and last edge (computing |Q|)

        public SubETT(Particle p) : base(p)
        {
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = algo.CreateAttributeDirection(FindValidAttributeName("[ETT] Nbr" + i), Direction.NONE);
            }
            numNeighbors = algo.CreateAttributeInt(FindValidAttributeName("[ETT] Num nbrs"), 0);
            markedEdge = algo.CreateAttributeInt(FindValidAttributeName("[ETT] Marked edge"), -1);
            split = algo.CreateAttributeBool(FindValidAttributeName("[ETT] Split"), false);
            active = algo.CreateAttributeBool(FindValidAttributeName("[ETT] Active"), false);
            becamePassive = algo.CreateAttributeBool(FindValidAttributeName("[ETT] Became Passive"), false);
            finished = algo.CreateAttributeBool(FindValidAttributeName("[ETT] Finished"), false);
            terminationRound = algo.CreateAttributeBool(FindValidAttributeName("[ETT] Term. Round"), false);

            for (int i = 0; i < 7; i++)
            {
                comparisons[i] = algo.CreateAttributeEnum<Comparison>(FindValidAttributeName("[ETT] Comp." + i), Comparison.EQUAL);
            }
            bits = algo.CreateAttributeString(FindValidAttributeName("[ETT] Bits"), "-------------");
        }

        /// <summary>
        /// Initializes the subroutine for the given directions.
        /// </summary>
        /// <param name="nbrDirections">The directions of all neighboring edges,
        /// in counter-clockwise order. Each edge will be split into an outgoing
        /// and an incoming edge.</param>
        /// <param name="markedEdge">The index of the marked edge in the array
        /// of edges. If this is <c>-1</c>, no edge will be marked.</param>
        /// <param name="split">If <c>true</c>, do not connect the outgoing edge
        /// of the first neighbor to the incoming edge of the last neighbor. This
        /// splits the Euler cycle into an Euler tour if done by exactly one participant.</param>
        public void Init(Direction[] nbrDirections, int markedEdge = -1, bool split = false)
        {
            if (nbrDirections == null || nbrDirections.Length == 0 || nbrDirections.Length > 6)
            {
                throw new AlgorithmException(particle, "Invalid neighbor direction array: Must not be null and must have 1-6 elements");
            }

            numNeighbors.SetValue(nbrDirections.Length);
            for (int i = 0; i < nbrDirections.Length; i++)
            {
                neighbors[i].SetValue(nbrDirections[i]);
            }
            this.markedEdge.SetValue(markedEdge);
            this.split.SetValue(split);
            active.SetValue(markedEdge != -1);
            becamePassive.SetValue(false);
            finished.SetValue(false);
            terminationRound.SetValue(false);
            for (int i = 0; i < 7; i++)
            {
                comparisons[i].SetValue(Comparison.EQUAL);
            }
            bits.SetValue("-------------");
        }

        /// <summary>
        /// Sets up the required partition sets for the next
        /// beep in the given pin configuration.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.
        /// It has to be set as the next configuration before
        /// <see cref="ActivateSend"/> is called.</param>
        public void SetupPinConfig(PinConfiguration pc)
        {
            int nNbrs = numNeighbors.GetCurrentValue();
            int pinsPerEdge = algo.PinsPerEdge;
            for (int i = 0; i < nNbrs; i++)
            {
                Direction d = neighbors[i].GetCurrentValue();
                Direction dPred = neighbors[(i + nNbrs - 1) % nNbrs].GetCurrentValue();
                bool isActive = (active.GetCurrentValue() && markedEdge.GetCurrentValue() == i);
                bool separate = split.GetCurrentValue() && i == 0;
                SubPASC.SetupPascConfig(pc,
                    separate ? Direction.NONE : dPred,      // If we split here, we have no predecessor direction
                    d, pinsPerEdge - 1, pinsPerEdge - 2,
                    0, 1, d.ToInt() * 2, d.ToInt() * 2 + 1, isActive);
                // Additional partition sets if we have to separate
                if (separate)
                    SubPASC.SetupPascConfig(pc, dPred, Direction.NONE, pinsPerEdge - 1, pinsPerEdge - 2, -1, -1, 12, 13, false);
            }
        }

        /// <summary>
        /// Sends either a PASC or a termination beep, depending on what kind of
        /// round this is. If called in a termination round, all participants
        /// that became passive in the last iteration will send a beep to prevent
        /// termination. In a PASC round, all participants that split the Euler
        /// tour send a beep on their primary partition set. Special case: If
        /// the first outgoing edge of a splitting participant is marked, it will
        /// send a beep on its secondary partition set to count this edge.
        /// </summary>
        public void ActivateSend()
        {
            if (terminationRound.GetCurrentValue())
            {
                // If we became passive, send a beep on both marked circuits
                if (becamePassive.GetCurrentValue())
                {
                    Direction d = neighbors[markedEdge.GetCurrentValue()].GetCurrentValue();
                    algo.SendBeepOnPartitionSet(d.ToInt() * 2);
                    algo.SendBeepOnPartitionSet(d.ToInt() * 2 + 1);
                }
            }
            else
            {
                // If split, send beep on primary partition set
                // Special case: If the edge is marked and active, send on secondary partition set instead
                if (split.GetCurrentValue())
                {
                    if (markedEdge.GetCurrentValue() == 0 && active.GetCurrentValue())
                        algo.SendBeepOnPartitionSet(2 * neighbors[0].GetCurrentValue().ToInt() + 1);
                    else
                        algo.SendBeepOnPartitionSet(2 * neighbors[0].GetCurrentValue().ToInt());
                }
            }
        }

        /// <summary>
        /// Receives the beeps from the last <see cref="ActivateSend"/> call
        /// and updates the comparison results as well as the type of round.
        /// After this call, the next bits and comparison results are available.
        /// </summary>
        public void ActivateReceive()
        {
            // If we became passive in the previous round, we
            // did not become passive in this round
            if (becamePassive)
                becamePassive.SetValue(false);

            if (terminationRound.GetCurrentValue())
            {
                // Terminate if no beep was received
                if (!algo.ReceivedBeepOnPartitionSet(neighbors[0].GetCurrentValue().ToInt() * 2))
                {
                    finished.SetValue(true);
                }
            }
            else
            {
                int nNbrs = numNeighbors.GetCurrentValue();
                // Store the incoming and outgoing bits in these arrays (indices are direction ints)
                int[] bitsIN = new int[6];
                int[] bitsOUT = new int[6];
                for (int i = 0; i < nNbrs; i++)
                {
                    Direction d = neighbors[i].GetCurrentValue();
                    Direction dPred = neighbors[(i + nNbrs - 1) % nNbrs].GetCurrentValue();
                    int pSetPrimary = d.ToInt() * 2;
                    int pSetSecondary = d.ToInt() * 2 + 1;
                    bool isMarkedAndActive = (i == markedEdge.GetCurrentValue() && active.GetCurrentValue());

                    int bit = 0;

                    if (algo.ReceivedBeepOnPartitionSet(pSetSecondary))
                    {
                        bit = 1;

                        // If this is the marked edge, check whether we have to become passive
                        if (isMarkedAndActive)
                        {
                            active.SetValue(false);
                            becamePassive.SetValue(true);
                        }
                    }

                    bitsOUT[d.ToInt()] = bit;
                    // Special case: If the outgoing edge is marked and active,
                    // the bit of the connected incoming edge must be flipped
                    bitsIN[dPred.ToInt()] = isMarkedAndActive ? 1 - bit : bit;
                }
                // Remaining special case
                // We have to split: IN bit is determined by other partition sets
                // This also overrides the bit flip due to a marked edge (which would have been wrong)
                char sumBit = '-';
                if (split.GetCurrentValue())
                {
                    Direction d = neighbors[nNbrs - 1].GetCurrentValue();
                    int bit = algo.ReceivedBeepOnPartitionSet(13) ? 1 : 0;
                    bitsIN[d.ToInt()] = bit;
                    sumBit = bit.ToString()[0];
                }

                // We now have the incoming and outgoing bits, continue by updating the subtraction
                // and comparison results
                char[] b = bits.GetCurrentValue().ToCharArray();
                for (int i = 0; i < nNbrs; i++)
                {
                    Direction d = neighbors[i].GetCurrentValue();
                    int dirInt = d.ToInt();

                    int bitIN = bitsIN[dirInt];
                    int bitOUT = bitsOUT[dirInt];
                    Comparison prevComp = comparisons[dirInt].GetCurrentValue();
                    Comparison nextComp = prevComp;
                    int bitOUTminusIN;
                    int bitINminusOUT;
                    // Determine basic comparison bits and next comparison result
                    if (bitOUT == bitIN)
                    {
                        bitOUTminusIN = 0;
                        bitINminusOUT = 0;
                    }
                    else
                    {
                        bitOUTminusIN = 1;
                        bitINminusOUT = 1;
                        nextComp = bitOUT > bitIN ? Comparison.GREATER : Comparison.LESS;
                    }
                    comparisons[dirInt].SetValue(nextComp);

                    // The previous comparison result tells us which side has a borrowed 1:
                    // If the result was GREATER, we borrow a 1 from IN - OUT
                    // If the result was LESS, we borrow a 1 from OUT - IN
                    if (prevComp == Comparison.GREATER)
                        bitINminusOUT = 1 - bitINminusOUT;
                    else if (prevComp == Comparison.LESS)
                        bitOUTminusIN = 1 - bitOUTminusIN;

                    b[dirInt * 2] = bitOUTminusIN.ToString()[0];
                    b[dirInt * 2 + 1] = bitINminusOUT.ToString()[0];
                }
                b[12] = sumBit;
                if (sumBit > 0)
                    comparisons[6].SetValue(Comparison.GREATER);
                bits.SetValue(new string(b));
            }
            terminationRound.SetValue(!terminationRound.GetCurrentValue());
        }

        /// <summary>
        /// Checks whether the ETT procedure has finished.
        /// </summary>
        /// <returns><c>true</c> if and only if no termination
        /// beep was received in the last termination round.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this round is a termination round.
        /// If <c>true</c>, the next beep will be sent by all
        /// participants that became passive to continue the
        /// procedure with another iteration.
        /// </summary>
        /// <returns><c>true</c> if and only this round is
        /// a termination round.</returns>
        public bool IsTerminationRound()
        {
            return terminationRound.GetCurrentValue();
        }

        /// <summary>
        /// Returns the last received bit of the given edge's
        /// difference.
        /// </summary>
        /// <param name="d">The direction of the edge.</param>
        /// <param name="outgoing">If <c>true</c>, return the bit of
        /// the OUT - IN difference, otherwise return the bit of the
        /// IN - OUT difference (which might not be valid if the number
        /// turns out to be negative).</param>
        /// <returns></returns>
        public int GetDiffBit(Direction d, bool outgoing = true)
        {
            return bits.GetCurrentValue()[d.ToInt() * 2 + (outgoing ? 0 : 1)] == '1' ? 1 : 0;
        }

        /// <summary>
        /// Returns the neighbor directions with which the procedure was initialized.
        /// This is a convenience method so that the directions don't have to be
        /// stored in separate attributes again.
        /// </summary>
        /// <returns>An array containing exactly the directions given to
        /// <see cref="Init(Direction[], int, bool)"/>.</returns>
        public Direction[] GetNeighborDirections()
        {
            int nNbrs = numNeighbors.GetCurrentValue();
            Direction[] directions = new Direction[nNbrs];
            for (int i = 0; i < nNbrs; i++)
                directions[i] = neighbors[i].GetCurrentValue();
            return directions;
        }

        /// <summary>
        /// Returns the last received bit of the incoming edge
        /// belonging to the last neighbor direction in case
        /// this participant splits the cycle. This is exactly the
        /// number of marked edges on the Euler tour and is usually
        /// used to determine |Q|, in the notation of the paper.
        /// </summary>
        /// <returns>The last bit of the PASC result computing the
        /// number of marked edges on the Euler tour. If this
        /// participant does not split the tour, this will always
        /// return <c>-1</c>.</returns>
        public int GetSumBit()
        {
            if (split.GetCurrentValue())
                return bits.GetCurrentValue()[12] == '1' ? 1 : 0;
            return -1;
        }

        /// <summary>
        /// Returns the difference comparison result belonging
        /// to the given direction's edge. Note that this result
        /// is always up to date with the latest received bits.
        /// </summary>
        /// <param name="d">The direction of the edge.</param>
        /// <returns>The result of comparing OUT - IN to 0 on the
        /// edge in direction <paramref name="d"/>. Note that to
        /// get the result of IN - OUT, the comparison result just
        /// has to be flipped.</returns>
        public Comparison GetComparisonResult(Direction d)
        {
            return comparisons[d.ToInt()].GetCurrentValue();
        }

        /// <summary>
        /// Returns the result of comparing the sum of all
        /// marked edges to 0. This result is always up to
        /// date with the latest received bits.
        /// </summary>
        /// <returns>The result of comparing the PASC sum
        /// to 0. Will never be <see cref="Comparison.LESS"/>.</returns>
        public Comparison GetSumComparisonResult()
        {
            return comparisons[6].GetCurrentValue();
        }
    }

} // namespace AS2.Subroutines.ETT
