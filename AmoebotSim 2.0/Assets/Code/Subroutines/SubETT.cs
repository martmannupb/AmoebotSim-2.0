using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.PASC;

namespace AS2.Subroutines.ETT
{

    public enum Comparison
    {
        LESS, EQUAL, GREATER
    }

    /// <summary>
    /// Implements the Euler Tour Technique (ETT).
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

        ParticleAttribute<Comparison>[] comparisons = new ParticleAttribute<Comparison>[6]; // Comparison results for the 6 directions, comparing OUT to IN
        ParticleAttribute<string> bits;         // String storing the up to 12 bits. For direction d with int representation i, character 2*i stores the bit of OUT - IN at direction d
                                                // and character 2*i + 1 stores the bit of IN - OUT at direction d. A bit can be '0', '1' or '-' if it does not exist


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

            for (int i = 0; i < 6; i++)
            {
                comparisons[i] = algo.CreateAttributeEnum<Comparison>(FindValidAttributeName("[ETT] Comp." + i), Comparison.EQUAL);
            }
            bits = algo.CreateAttributeString(FindValidAttributeName("[ETT] Bits"), "------------");
        }

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
            for (int i = 0; i < 6; i++)
            {
                comparisons[i].SetValue(Comparison.EQUAL);
            }
            bits.SetValue("------------");
        }

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

        public void ActivateSend()
        {
            if (terminationRound.GetCurrentValue())
            {
                // If we became passive, send a beep on both marked circuits
                if (becamePassive.GetCurrentValue())
                {
                    Direction d = neighbors[markedEdge.GetCurrentValue()].GetCurrentValue();
                    PinConfiguration pc = algo.GetPlannedPinConfiguration();
                    pc.SendBeepOnPartitionSet(d.ToInt() * 2);
                    pc.SendBeepOnPartitionSet(d.ToInt() * 2 + 1);
                }
            }
            else
            {
                // If split, send beep on primary partition set
                // Special case: If the edge is marked and active, send on secondary partition set instead
                if (split.GetCurrentValue())
                {
                    PinConfiguration pc = algo.GetPlannedPinConfiguration();
                    if (markedEdge.GetCurrentValue() == 0 && active.GetCurrentValue())
                        pc.SendBeepOnPartitionSet(2 * neighbors[0].GetCurrentValue().ToInt() + 1);
                    else
                        pc.SendBeepOnPartitionSet(2 * neighbors[0].GetCurrentValue().ToInt());
                }
            }
        }

        public void ActivateReceive()
        {
            // If we became passive in the previous round, we
            // did not become passive in this round
            if (becamePassive)
                becamePassive.SetValue(false);

            if (terminationRound.GetCurrentValue())
            {
                // Terminate if no beep was received
                PinConfiguration pc = algo.GetCurrentPinConfiguration();
                if (!pc.ReceivedBeepOnPartitionSet(neighbors[0].GetCurrentValue().ToInt() * 2))
                {
                    finished.SetValue(true);
                }
            }
            else
            {
                PinConfiguration pc = algo.GetCurrentPinConfiguration();
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

                    if (pc.ReceivedBeepOnPartitionSet(pSetSecondary))
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
                if (split.GetCurrentValue())
                {
                    Direction d = neighbors[nNbrs - 1].GetCurrentValue();
                    int bit = pc.ReceivedBeepOnPartitionSet(13) ? 1 : 0;
                    bitsIN[d.ToInt()] = bit;
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
                bits.SetValue(new string(b));
            }
            terminationRound.SetValue(!terminationRound.GetCurrentValue());
        }

        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        public bool IsTerminationRound()
        {
            return terminationRound.GetCurrentValue();
        }

        public int GetBit(Direction d, bool outgoing = true)
        {
            return bits.GetCurrentValue()[d.ToInt() * 2 + (outgoing ? 0 : 1)] == '1' ? 1 : 0;
        }

        // TODO: Compute bits of the sum in the root amoebot
    }

} // namespace AS2.Subroutines.ETT
