using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{
    /// <summary>
    /// Implements binary multiplication for two numbers stored
    /// in the same binary counter.
    /// </summary>
    // Init:
    //  - Give the bits of a and b
    //  - Set the result bit to 0
    //  - Give the chain's starting point the token
    //  - Mark the MSBs of both numbers
    //  - Give the predecessor and successor direction
    // Round 0:
    //  Send:
    //  - Establish two full chain circuits
    //  - Beep on the second circuit if we perform another iteration
    //  - Transmit bit of a on the first circuit (only necessary if another iteration should be done)
    // Round 1:
    //  Receive:
    //  - If no beep on circuit 2: Finished
    //  - Otherwise:
    //      - Compute local sum
    //  Send:
    //  - Setup chain circuit 1 and beep for carry bits
    //  - Setup neighbor circuit 2 and send bits of b
    // Round 2:
    //  Receive:
    //  - Receive carry bits and update sum bits
    //  - Receive b bits to shift b
    public class SubMultiplication : Subroutine
    {
        // This int represents the state of this amoebot
        // Since the standard int type is a 32-bit signed int, we use the
        // 32 bits to encode the entire state:
        // The lowest 2 bits represent the round counter (possible values 0, 1, 2)
        // Bits 2, 3, 4 store the current bits of a, b and c (result) stored in this amoebot
        // Bit 5 is the flag for the token in a
        // Bits 6 and 7 are the MSB flags for a and b
        // Bits 8-10 store the direction of the predecessor (0-5 and 6 means no predecessor)
        // Bits 11-13 store the direction of the successor
        // Bit 14 is the termination flag
        //                         14      1311        108         76        5       432   10
        // xxxx xxxx xxxx xxxx x   x       xxx         xxx         xx        x       xxx   xx
        //                         Term.   Succ. dir   Pred. dir   ba MSBs   Token   cba   Round
        ParticleAttribute<int> state;

        public SubMultiplication(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Mult] State"), 0);
        }

        public void Init(bool a, bool b, bool start, bool msbA, bool msbB, Direction predDir, Direction succDir)
        {
            // Encode the starting information in the state
            state.SetValue(
                0 |                     // Round
                (a ? 4 : 0) |           // Bits of a, b and c (c is 0)
                (b ? 8 : 0) |
                (start ? 32 : 0) |      // Token
                (msbA ? 64 : 0) |       // MSBs of a and b
                (msbB ? 128 : 0) |
                (predDir != Direction.NONE ? (predDir.ToInt() << 8) : (6 << 8)) |   // Predecessor and successor direction
                (succDir != Direction.NONE ? (succDir.ToInt() << 11) : (6 << 11)));
        }

        public void ActivateReceive()
        {
            int round = Round();
            if (round == 1)
            {
                // Check if there was a beep on circuit 2
                PinConfiguration pc = algo.GetCurrentPinConfiguration();
                GetPsetIds(pc, out int pSet1, out int pSet2);
                if (pc.ReceivedBeepOnPartitionSet(pSet2))
                {
                    // Start the iteration
                    // Compute local sum
                    Debug.Log("A, B before: " + Bit_A() + ", " + Bit_B());
                    SetBit_C(Bit_A() ^ Bit_B());
                    Debug.Log("A, B after: " + Bit_A() + ", " + Bit_B());
                }
                else
                {
                    // Terminate
                    SetFinished(true);
                }
            }
        }

        public void SetupPinConfig(PinConfiguration pc)
        {
            int round = Round();
            Direction predDir = PredDir();
            Direction succDir = SuccDir();
            if (round == 0)
            {
                // Establish two full chain circuits
                MakePartitionSets(pc, predDir, succDir, 0, true);
                MakePartitionSets(pc, predDir, succDir, 1, true);
            }
            else if (round == 1)
            {
                // Circuit 1 is chain circuit for carry bits
                // Connect iff the two bits of a and b are different
                if (Bit_A() ^ Bit_B())
                {
                    Debug.Log("Connecting because a != b");
                }
                else
                {
                    Debug.Log("Not connecting");
                }
                MakePartitionSets(pc, predDir, succDir, 0, Bit_A() ^ Bit_B());

                // Circuit 2 is singleton to transmit bits of b
                MakePartitionSets(pc, predDir, succDir, 1, false);
            }
        }

        public void ActivateSend()
        {
            int round = Round();
            if (round == 0)
            {
                // If we have the token: Beep on second circuit and send a's bit on first circuit
                if (HaveToken())
                {
                    PinConfiguration pc = GetPlannedPC();

                    GetPsetIds(pc, out int pSet1, out int pSet2);
                    pc.SendBeepOnPartitionSet(pSet2);
                    if (Bit_A())
                        pc.SendBeepOnPartitionSet(pSet1);
                }
                SetRound(1);
            }
            else if (round == 1)
            {
                PinConfiguration pc = GetPlannedPC();
                Direction succDir = SuccDir();
                if (succDir != Direction.NONE)
                {
                    // Beep for carry bit on circuit 1
                    if (Bit_A() && Bit_B())
                    {
                        pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.SendBeep();
                    }

                    // Transmit bit of b
                    if (Bit_B())
                    {
                        pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.SendBeep();
                    }
                }
                SetRound(2);
            }
        }

        private int Round()
        {
            return state.GetCurrentValue() & 3;
        }

        private bool Bit_A()
        {
            return (state.GetCurrentValue() & 4) != 0;
        }

        private bool Bit_B()
        {
            return (state.GetCurrentValue() & 8) != 0;
        }

        private bool Bit_C()
        {
            return (state.GetCurrentValue() & 16) != 0;
        }

        private bool HaveToken()
        {
            return (state.GetCurrentValue() & 32) != 0;
        }

        private Direction PredDir()
        {
            int d = (state.GetCurrentValue() >> 8) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        private Direction SuccDir()
        {
            int d = (state.GetCurrentValue() >> 11) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        public bool Finished()
        {
            return (state.GetCurrentValue() & (1 << 14)) != 0;
        }

        private void SetRound(int round)
        {
            state.SetValue((state.GetCurrentValue() & ~3 | round));
        }

        private void SetBit_C(bool bit)
        {
            state.SetValue(bit ? state.GetCurrentValue() | (1 << 4) : state.GetCurrentValue() & ~(1 << 4));
        }

        private void SetFinished(bool finished)
        {
            state.SetValue(finished ? state.GetCurrentValue() | (1 << 14) : state.GetCurrentValue() & ~(1 << 14));
        }

        private void GetPsetIds(PinConfiguration pc, out int pSet1, out int pSet2)
        {
            Direction succDir = SuccDir();
            Direction predDir = PredDir();

            if (succDir != Direction.NONE)
            {
                pSet1 = pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.Id;
                pSet2 = pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.Id;
            }
            else
            {
                pSet1 = pc.GetPinAt(predDir, 0).PartitionSet.Id;
                pSet2 = pc.GetPinAt(predDir, 1).PartitionSet.Id;
            }
        }

        private void MakePartitionSets(PinConfiguration pc, Direction predDir, Direction succDir, int offset, bool connected)
        {
            if (connected)
            {
                List<int> pins = new List<int>();
                if (predDir != Direction.NONE)
                    pins.Add(pc.GetPinAt(predDir, offset).Id);
                if (succDir != Direction.NONE)
                    pins.Add(pc.GetPinAt(succDir, algo.PinsPerEdge - 1 - offset).Id);
                pc.MakePartitionSet(pins.ToArray(), pins[0]);
            }
            else
            {
                if (predDir != Direction.NONE)
                {
                    int id1 = pc.GetPinAt(predDir, offset).Id;
                    pc.MakePartitionSet(new int[] { id1 }, id1);
                }
                if (succDir != Direction.NONE)
                {
                    int id2 = pc.GetPinAt(succDir, algo.PinsPerEdge - 1 - offset).Id;
                    pc.MakePartitionSet(new int[] { id2 }, id2);
                }
            }
        }

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
