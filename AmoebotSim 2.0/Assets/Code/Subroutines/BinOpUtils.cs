using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{

    /// <summary>
    /// General helper methods for binary operations on amoebot chains.
    /// </summary>
    public static class BinOpUtils
    {
        /// <summary>
        /// Sets up a chain circuit by connecting or disconnecting the
        /// predecessor from the successor at the given pin offset.
        /// If partition sets are created, their IDs will always be one
        /// of the contained pin IDs.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        /// <param name="predDir">The direction of the chain predecessor.
        /// <see cref="Direction.NONE"/> means there is no predecessor.</param>
        /// <param name="succDir">The direction of the chain successor.
        /// <see cref="Direction.NONE"/> means there is no successor.</param>
        /// <param name="offset">The pin distance of the connections. The offset is applied to
        /// the predecessor pin and inverted for the successor.</param>
        /// <param name="pinsPerEdge">The number of pins per edge used by the algorithm.</param>
        /// <param name="connected">Whether the pins of the predecessor and successor should be
        /// connected. There will be no connection if the predecessor or successor direction
        /// is <see cref="Direction.NONE"/>.</param>
        public static void MakeChainCircuit(PinConfiguration pc, Direction predDir, Direction succDir, int offset, int pinsPerEdge, bool connected)
        {
            if (connected)
            {
                List<int> pins = new List<int>();
                if (predDir != Direction.NONE)
                    pins.Add(pc.GetPinAt(predDir, offset).Id);
                if (succDir != Direction.NONE)
                    pins.Add(pc.GetPinAt(succDir, pinsPerEdge - 1 - offset).Id);
                if (pins.Count > 0)
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
                    int id2 = pc.GetPinAt(succDir, pinsPerEdge - 1 - offset).Id;
                    pc.MakePartitionSet(new int[] { id2 }, id2);
                }
            }
        }

        /// <summary>
        /// Helper for getting the partition set IDs of connected chain circuits.
        /// This is useful at the start and end of the chain, where one of the two
        /// ends is not connected, so we cannot easily use the pin to find the partition set.
        /// </summary>
        /// <param name="pc">The pin configuration from which to get the partition set IDs.</param>
        /// <param name="predDir">The direction of the predecessor.
        /// <see cref="Direction.NONE"/> means there is no predecessor.</param>
        /// <param name="succDir">The direction of the chain successor.
        /// <see cref="Direction.NONE"/> means there is no successor.</param>
        /// <param name="offset">The pin distance of the connections. The offset is applied to
        /// the predecessor pin and inverted for the successor.</param>
        /// <param name="pinsPerEdge">The number of pins per edge used by the algorithm.</param>
        /// <returns>The ID of the chain circuit connecting the predecessor and successor at
        /// the given <paramref name="offset"/>. <c>-1</c> if there is neither a predecessor nor
        /// a successor.</returns>
        public static int GetChainPSetID(PinConfiguration pc, Direction predDir, Direction succDir, int offset, int pinsPerEdge)
        {
            if (succDir != Direction.NONE)
            {
                return pc.GetPinAt(succDir, pinsPerEdge - 1 - offset).PartitionSet.Id;
            }
            else if (predDir != Direction.NONE)
            {
                return pc.GetPinAt(predDir, offset).PartitionSet.Id;
            }
            return -1;
        }
    }

} // namespace AS2.Subroutines.BinaryOps
