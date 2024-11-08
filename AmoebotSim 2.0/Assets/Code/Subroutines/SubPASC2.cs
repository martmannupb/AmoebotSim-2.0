using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.PASC
{

    /// <summary>
    /// Implements the PASC (Primary-And-Secondary-Circuit) algorithm
    /// (https://drops.dagstuhl.de/opus/volltexte/2022/16793/) with a focus
    /// on simple stripe and tree structures. See <see cref="SubPASC"/> for
    /// a detailed description of the original algorithm.
    /// <para>
    /// In this version, every amoebot defines each direction as successor,
    /// predecessor, neighbor or nothing. Neighbors are handled such that
    /// the primary and secondary partition sets are always connected directly,
    /// allowing stripes to share partition sets.
    /// Only two pin offsets are required, which will be used for outgoing
    /// edges (successors and neighbors in neighbor direction). For other
    /// connections (predecessors and neighbors in opposite neighbor direction),
    /// the pin offsets are inverted. The remaining functionality of the algorithm
    /// is exactly the same.
    /// </para>
    /// <para>
    /// <b>Disclaimer: The cutoff functionality might not work correctly if the
    /// leader has active predecessors.</b>
    /// </para>
    /// <para>
    /// Setup:<br/>
    /// Call <see cref="Init(List{Direction}, List{Direction}, int, int, int, int, bool, bool, Direction, bool, bool)"/>
    /// and tell each amoebot the directions of its predecessors and successors, the pin
    /// offsets for outgoing connections, the partition set IDs, the leader and active
    /// state info, the direction of the stripe axis, and the flags for the two stripe
    /// neighbor connections. No direction should appear as predecessor and successor
    /// and no direction parallel to the stripe axis should be a successor or
    /// predecessor.
    /// </para>
    /// <para>
    /// Usage:<br/>
    /// After initializing the subroutine, call <see cref="SetupPC(PinConfiguration, List{Direction})"/>
    /// to make each amoebot establish its partition sets. This does not plan the pin
    /// configuration yet, in the case that other changes are required. You can then call
    /// <see cref="ActivateSend"/> (after planning the pin configuration) to send the beeps.
    /// This is only necessary for the leader amoebots (but it is no problem to call it
    /// on all amoebots).<br/>
    /// In the next round, you have to call <see cref="ActivateReceive"/> before changing
    /// the pin configuration so that the received beeps can be processed. After this call,
    /// you can read the received bit and check if the amoebot became passive in
    /// this round. Then you can repeat the process.<br/>
    /// The subroutine does not include a termination check. You need to terminate
    /// manually when no amoebot in the structure has become passive. It is no
    /// problem to keep the algorithm running after the last amoebot became passive.
    /// This allows you to run multiple instances of PASC and keep them running
    /// until each instance has finished.
    /// </para>
    /// <para>
    /// Early termination:<br/>
    /// <b>WIP: This will not work properly on stripe structures where the leader
    /// stripe has active predecessors.</b><br/>
    /// If you use PASC to compare the distance bits to another sequence of bits which
    /// may be shorter than the PASC result (i.e., the compared sequence has a lower most
    /// significant bit than the number of PASC iterations), you can perform a cutoff to
    /// save a few rounds as follows:<br/>
    /// Instead of calling <see cref="SetupPC(PinConfiguration, List{Direction})"/>, call
    /// <see cref="SetupCutoffCircuit(PinConfiguration, List{Direction})"/> and plan the
    /// resulting pin configuration. This will setup a circuit where all active non-leader
    /// amoebots disconnect their predecessors. After planning the pin configuration, call
    /// <see cref="SendCutoffBeep"/> instead of <see cref="ActivateSend"/> (but call
    /// it on all amoebots, not just the leader). This will make the active non-leader
    /// amoebots send a beep to their successor on both circuits, causing all amoebots
    /// after the first active non-leader amoebot to receive a beep. These are exactly
    /// the amoebots that will receive at least one 1-bit in the future PASC iterations,
    /// i.e., the amoebots whose PASC value will definitely be greater than the compared
    /// bit sequence. All amoebots that do not receive a beep (the leader and all
    /// connected passive amoebots) will only receive 0-bits, i.e., their comparison
    /// result is already finished since the compared sequence also has only 0-bits left.
    /// To read the result of this cutoff, call <see cref="ReceiveCutoffBeep"/> instead of
    /// <see cref="ActivateReceive"/>. The result of <see cref="GetReceivedBit"/> will be
    /// <c>1</c> if the amoebot has received the cutoff bit, and <c>0</c> otherwise.<br/>
    /// Afterwards, it is still possible to continue the PASC procedure where it was
    /// interrupted, starting with <see cref="SetupPC(PinConfiguration, List{Direction})"/>.
    /// </para>
    /// </summary>
    public class SubPASC2 : Subroutine
    {
        private enum NbrType
        {
            NONE = 0,
            PREDECESSOR = 1,
            SUCCESSOR = 2,
            NEIGHBOR = 3
        }

        // We use an integer to store all state information more compactly
        // State 1:
        // Lowest 6 * 2 bits store the neighbor types (only 4 possibilities)
        // Next 4 bits store leader, active, became passive and received bit flags
        // Next 2 * 3 bits store pin offsets 1 and 2 (numbers up to 7)
        // Last 2 * 5 bits store partition set IDs 1 and 2 (numbers up to 31)
        // 31  27   26  22   2119    1816    15    14        13       12      1110     98      76      54      32      10
        // xxxxx    xxxxx    xxx     xxx     x     x         x        x        xx      xx      xx      xx      xx      xx
        // PSet S   PSet P   Pin 2   Pin 1   Bit   Passive   Active   Leader   Nbr 5   Nbr 4   Nbr 3   Nbr 2   Nbr 1   Nbr 0
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_Leader = 12;
        private const int bit_Active = 13;
        private const int bit_Passive = 14;
        private const int bit_ReceivedBit = 15;
        private const int bit_Pin1 = 16;
        private const int bit_Pin2 = 19;
        private const int bit_PSet1 = 22;
        private const int bit_PSet2 = 27;
        private const int nbrBitWidth = 2;

        public SubPASC2(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[PASC2] State"), 0);
        }

        /// <summary>
        /// Initializes the PASC2 subroutine. Successors, predecessors and neighbors
        /// must not overlap (the last encountered one will be taken).
        /// </summary>
        /// <param name="predecessors">The directions where predecessors should be connected.</param>
        /// <param name="successors">The directions where successors should be connected.</param>
        /// <param name="pin1">The primary pin offset for outgoing connections. Must be at most 7.</param>
        /// <param name="pin2">The secondary pin offset for outgoing connections. Must be at most 7.</param>
        /// <param name="pSet1">The primary partition set ID. Must be at most 31.</param>
        /// <param name="pSet2">The secondary partition set ID. Must be at most 31.</param>
        /// <param name="isLeader">Whether this amoebot is the leader. <b>All leaders should be
        /// on the same stripe.</b></param>
        /// <param name="isActive">Whether this amoebot is initially active.</param>
        /// <param name="neighborDir">The direction of the neighbor axis.</param>
        /// <param name="connectNbr1">Whether the neighbor in direction <paramref name="neighborDir"/>
        /// should be connected.</param>
        /// <param name="connectNbr2">Whether the neighbor in the direction opposite opposite
        /// of <paramref name="neighborDir"/> should be connected.</param>
        public void Init(List<Direction> predecessors, List<Direction> successors,
            int pin1, int pin2, int pSet1, int pSet2, bool isLeader,
            bool isActive = true, Direction neighborDir = Direction.NONE, bool connectNbr1 = true, bool connectNbr2 = true)
        {
            state.SetValue(0);

            if (predecessors != null)
            {
                for (int i = 0; i < predecessors.Count; i++)
                {
                    SetStateNbrType(predecessors[i], NbrType.PREDECESSOR);
                }
            }

            if (successors != null)
            {
                for (int i = 0; i < successors.Count; i++)
                {
                    SetStateNbrType(successors[i], NbrType.SUCCESSOR);
                }
            }

            if (neighborDir != Direction.NONE)
            {
                if (connectNbr1)
                    SetStateNbrType(neighborDir, NbrType.SUCCESSOR);        // Neighbor in neighbor direction can be treated like successor
                if (connectNbr2)
                    SetStateNbrType(neighborDir.Opposite(), NbrType.NEIGHBOR);
            }

            Pin1 = pin1;
            Pin2 = pin2;
            PSet1 = pSet1;
            PSet2 = pSet2;
            Leader = isLeader;
            Active = isActive;
        }

        /// <summary>
        /// Sets up the primary and secondary partition sets
        /// for this amoebot. The pin configuration is not
        /// planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.
        /// This will only modify the pins and partition sets
        /// specified for this subroutine.</param>
        /// <param name="invertedDirections">Directions in which the
        /// pin offsets should be inverted. This can be used to adjust
        /// the "side" on which the beeps are sent if the specified default
        /// pins are used for something else in some directions.</param>
        public void SetupPC(PinConfiguration pc, List<Direction> invertedDirections = null)
        {
            SetupPinConfig(pc, false, invertedDirections);
        }

        private void SetupPinConfig(PinConfiguration pc, bool cutoff = false, List<Direction> invertedDirections = null)
        {
            int pin1Orig = Pin1;
            int pin2Orig = Pin2;
            int pin1RevOrig = algo.PinsPerEdge - 1 - pin1Orig;
            int pin2RevOrig = algo.PinsPerEdge - 1 - pin2Orig;
            int pSet1 = PSet1;
            int pSet2 = PSet2;

            if (invertedDirections is null)
                invertedDirections = new List<Direction>();

            bool havePartitionSets = false;

            // Collect some information for nice partition set placement
            Direction firstPredDir = Direction.NONE;
            Direction firstSuccDir = Direction.NONE;
            Direction firstNbrDir = Direction.NONE;

            for (int d = 0; d < 6; d++)
            {
                Direction dir = DirectionHelpers.Cardinal(d);

                NbrType t = GetStateNbrType(dir);
                if (t == NbrType.NONE)
                    continue;

                // If cutoff: Active amoebots do not connect predecessor
                if (cutoff && Active && t == NbrType.PREDECESSOR)
                    continue;

                // Collect placement info
                if (t == NbrType.PREDECESSOR && firstPredDir == Direction.NONE)
                    firstPredDir = dir;
                if (t == NbrType.SUCCESSOR && firstSuccDir == Direction.NONE)
                    firstSuccDir = dir;
                if (t == NbrType.NEIGHBOR && firstNbrDir == Direction.NONE)
                    firstNbrDir = dir;

                // Determine which pins to add
                int pin1 = pin1Orig;
                int pin2 = pin2Orig;
                int pin1Rev = pin1RevOrig;
                int pin2Rev = pin2RevOrig;
                if (invertedDirections.Contains(dir))
                {
                    pin1 = pin1RevOrig;
                    pin2 = pin2RevOrig;
                    pin1Rev = pin1Orig;
                    pin2Rev = pin2Orig;
                }
                int pinPrimary;
                int pinSecondary;
                if (t == NbrType.SUCCESSOR)
                {
                    // Outgoing edge, always connect without crossing
                    pinPrimary = pc.GetPinAt(dir, pin1).Id;
                    pinSecondary = pc.GetPinAt(dir, pin2).Id;
                }
                else if (Active && t == NbrType.PREDECESSOR)
                {
                    // Incoming edge that has to be crossed
                    pinPrimary = pc.GetPinAt(dir, pin2Rev).Id;
                    pinSecondary = pc.GetPinAt(dir, pin1Rev).Id;
                }
                else
                {
                    // Incoming edge that does not have to be crossed
                    pinPrimary = pc.GetPinAt(dir, pin1Rev).Id;
                    pinSecondary = pc.GetPinAt(dir, pin2Rev).Id;
                }

                // Now add the pins to the partition set
                if (!havePartitionSets)
                {
                    // Must create partition sets first
                    pc.MakePartitionSet(new int[] { pinPrimary }, pSet1);
                    pc.MakePartitionSet(new int[] { pinSecondary }, pSet2);
                    havePartitionSets = true;
                }
                else
                {
                    // Can add to existing partition sets
                    pc.GetPartitionSet(pSet1).AddPin(pinPrimary);
                    pc.GetPartitionSet(pSet2).AddPin(pinSecondary);
                }
            }

            // TODO: What if we still don't have any partition sets?

            // Place partition sets nicely
            Direction placeDir = firstPredDir != Direction.NONE ? firstPredDir : (firstSuccDir != Direction.NONE ? firstSuccDir : firstNbrDir);
            if ((placeDir == firstSuccDir && !invertedDirections.Contains(placeDir)) || (placeDir != firstSuccDir && invertedDirections.Contains(placeDir)))
            {
                pin1RevOrig = pin1Orig;
                pin2RevOrig = pin2Orig;
            }
            if (pc.GetPartitionSet(pSet1).GetPins().Length > 1 && placeDir != Direction.NONE)
            {
                float pinsPerEdgeFactor = ((algo.PinsPerEdge - 1) / 2.0f);
                float primaryPin = (pin1RevOrig - pinsPerEdgeFactor) / pinsPerEdgeFactor;
                float secondaryPin = (pin2RevOrig - pinsPerEdgeFactor) / pinsPerEdgeFactor;
                float maxAngle = 45.0f - 90.0f / (algo.PinsPerEdge + 1);
                pc.SetPartitionSetPosition(pSet1, new Vector2(placeDir.ToInt() * 60.0f + primaryPin * maxAngle, 0.7f));
                pc.SetPartitionSetPosition(pSet2, new Vector2(placeDir.ToInt() * 60.0f + secondaryPin * maxAngle, 0.7f));
            }
        }

        /// <summary>
        /// Sets up the cutoff circuit for early termination.
        /// The pin configuration is not planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.
        /// This will only modify the pins and partition sets
        /// specified for this subroutine.</param>
        /// <param name="invertedDirections">Directions in which the
        /// pin offsets should be inverted. This can be used to adjust
        /// the "side" on which the beeps are sent if the specified default
        /// pins are used for something else in some directions.</param>
        public void SetupCutoffCircuit(PinConfiguration pc, List<Direction> invertedDirections = null)
        {
            SetupPinConfig(pc, true, invertedDirections);
        }

        /// <summary>
        /// Processes the received beep that was sent in
        /// the last round. Must be called in the next round
        /// after sending the beep and before the current
        /// pin configuration changes.
        /// </summary>
        public void ActivateReceive()
        {
            // If we became passive in the previous round, we
            // did not become passive in this round
            if (Passive)
                Passive = false;

            // Check where we have received a beep
            PinConfiguration pc = algo.GetCurrentPinConfiguration();
            bool beepOnPrimary = pc.ReceivedBeepOnPartitionSet(PSet1);
            bool beepOnSecondary = pc.ReceivedBeepOnPartitionSet(PSet2);

            if (!beepOnPrimary && !beepOnSecondary)
            {
                Log.Error("PASC Error: Did not receive beep at all!");
            }
            else if (beepOnPrimary && beepOnSecondary)
            {
                Log.Error("PASC Error: Received beep on both circuits!");
            }
            else if (beepOnSecondary)
            {
                // If we are active and received a beep on the secondary circuit: Become passive
                if (Active)
                {
                    Active = false;
                    Passive = true;
                }
                // Bit for secondary circuit is 1
                ReceivedBit = true;
            }
            else
            {
                // Beep on primary circuit: Bit is 0
                ReceivedBit = false;
            }
        }

        /// <summary>
        /// Causes the leader of the PASC structure to send the
        /// beep that deactivates half of the active participants.
        /// Must only be called after the final pin configuration
        /// has been planned.
        /// </summary>
        public void ActivateSend()
        {
            // Leader sends beep on primary partition set
            if (Leader)
            {
                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                pc.SendBeepOnPartitionSet(PSet1);
            }
        }

        /// <summary>
        /// Causes the active non-leader amoebots to send the
        /// cutoff beep that is received by all amoebots that will
        /// receive at least one 1-bit in a future PASC iteration.
        /// Must only be called after the final pin configuration
        /// setup by <see cref="SetupCutoffCircuit(PinConfiguration, List{Direction})"/>
        /// has been planned.
        /// </summary>
        public void SendCutoffBeep()
        {
            if (Active && !Leader)
            {
                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                pc.SendBeepOnPartitionSet(PSet1);
                pc.SendBeepOnPartitionSet(PSet2);
            }
        }

        /// <summary>
        /// Processes the received cutoff beep that was sent in
        /// the last round. Must be called in the next round
        /// after sending the cutoff beep and before the current
        /// pin configuration changes. Afterwards,
        /// <see cref="GetReceivedBit"/> will be <c>1</c> if and
        /// only if we received the cutoff beep.
        /// </summary>
        public void ReceiveCutoffBeep()
        {
            PinConfiguration pc = algo.GetCurrentPinConfiguration();
            ReceivedBit = pc.ReceivedBeepOnPartitionSet(PSet1);
        }

        /// <summary>
        /// Whether this particle is the sequence's leader.
        /// </summary>
        /// <returns><c>true</c> if and only if this particle
        /// has no predecessor.</returns>
        public bool IsLeader()
        {
            return Leader;
        }

        /// <summary>
        /// Whether this particle is active. The leader
        /// is always active.
        /// </summary>
        /// <returns><c>true</c> if and only if this particle
        /// is active.</returns>
        public bool IsActive()
        {
            return Active;
        }

        /// <summary>
        /// Whether this particle became passive in the last
        /// iteration.
        /// </summary>
        /// <returns><c>true</c> if and only if the last call
        /// of <see cref="ActivateReceive"/> has made this
        /// particle passive.</returns>
        public bool BecamePassive()
        {
            return Passive;
        }

        /// <summary>
        /// The bit received in the last iteration.
        /// Bits are received in ascending order and
        /// represent the number of preceding active
        /// particles including the leader.
        /// </summary>
        /// <returns><c>1</c> if and only if in the last call
        /// of <see cref="ActivateReceive"/>, a beep was
        /// received on the secondary circuit, otherwise <c>0</c>.</returns>
        public int GetReceivedBit()
        {
            return ReceivedBit ? 1 : 0;
        }



        #region Int State Helpers

        /// <summary>
        /// Whether we are the leader.
        /// </summary>
        private bool Leader
        {
            get { return GetStateBool(bit_Leader); }
            set { SetStateBool(bit_Leader, value); }
        }

        /// <summary>
        /// Whether we are currently active.
        /// </summary>
        private bool Active
        {
            get { return GetStateBool(bit_Active); }
            set { SetStateBool(bit_Active, value); }
        }

        /// <summary>
        /// Whether we became passive in the last iteration.
        /// </summary>
        private bool Passive
        {
            get { return GetStateBool(bit_Passive); }
            set { SetStateBool(bit_Passive, value); }
        }

        /// <summary>
        /// The bit received in the last iteration.
        /// </summary>
        private bool ReceivedBit
        {
            get { return GetStateBool(bit_ReceivedBit); }
            set { SetStateBool(bit_ReceivedBit, value); }
        }

        /// <summary>
        /// The first pin offset.
        /// </summary>
        private int Pin1
        {
            get { return GetStateInt3(bit_Pin1); }
            set { SetStateInt3(bit_Pin1, value); }
        }

        /// <summary>
        /// The second pin offset.
        /// </summary>
        private int Pin2
        {
            get { return GetStateInt3(bit_Pin2); }
            set { SetStateInt3(bit_Pin2, value); }
        }

        /// <summary>
        /// The primary partition set ID.
        /// </summary>
        private int PSet1
        {
            get { return GetStateInt5(bit_PSet1); }
            set { SetStateInt5(bit_PSet1, value); }
        }

        /// <summary>
        /// The secondary partition set ID.
        /// </summary>
        private int PSet2
        {
            get { return GetStateInt5(bit_PSet2); }
            set { SetStateInt5(bit_PSet2, value); }
        }

        /// <summary>
        /// Helper for reading a single bit from the state int.
        /// </summary>
        /// <param name="bit">The position of the bit.</param>
        /// <returns>The value of the state bit at position <paramref name="bit"/>.</returns>
        private bool GetStateBool(int bit)
        {
            return (state.GetCurrentValue() & (1 << bit)) != 0;
        }

        /// <summary>
        /// Helper for setting a single bit from the state int.
        /// </summary>
        /// <param name="bit">The position of the bit.</param>
        /// <param name="value">The new value of the bit.</param>
        private void SetStateBool(int bit, bool value)
        {
            state.SetValue(value ? state.GetCurrentValue() | (1 << bit) : state.GetCurrentValue() & ~(1 << bit));
        }

        /// <summary>
        /// Helper for reading a 3-bit integer from the state int.
        /// </summary>
        /// <param name="bit">Lowest bit of the integer to read.</param>
        /// <returns>The 3-bit integer stored at the given location.</returns>
        private int GetStateInt3(int bit)
        {
            return (state.GetCurrentValue() >> bit) & 7;
        }

        /// <summary>
        /// Helper for writing a 3-bit integer to the state int.
        /// </summary>
        /// <param name="bit">Lowest bit position to write to.</param>
        /// <param name="value">The new 3-bit integer value.</param>
        private void SetStateInt3(int bit, int value)
        {
            state.SetValue((state.GetCurrentValue() & ~(7 << bit)) | (value << bit));
        }

        /// <summary>
        /// Helper for reading a 5-bit integer from the state int.
        /// </summary>
        /// <param name="bit">Lowest bit of the integer to read.</param>
        /// <returns>The 5-bit integer stored at the given location.</returns>
        private int GetStateInt5(int bit)
        {
            return (state.GetCurrentValue() >> bit) & 31;
        }

        /// <summary>
        /// Helper for writing a 5-bit integer to the state int.
        /// </summary>
        /// <param name="bit">Lowest bit position to write to.</param>
        /// <param name="value">The new 5-bit integer value.</param>
        private void SetStateInt5(int bit, int value)
        {
            state.SetValue((state.GetCurrentValue() & ~(31 << bit)) | (value << bit));
        }

        /// <summary>
        /// Helper for reading neighbor types from the state int.
        /// </summary>
        /// <param name="dir">The direction of the neighbor.</param>
        /// <returns>The assigned type of the neighbor.</returns>
        private NbrType GetStateNbrType(Direction dir)
        {
            int t = (state.GetCurrentValue() >> (nbrBitWidth * dir.ToInt())) & 3;
            return (NbrType)t;
        }

        /// <summary>
        /// Helper for setting neighbor types in the state int.
        /// </summary>
        /// <param name="dir">The direction of the neighbor.</param>
        /// <param name="type">The new type of the neighbor.</param>
        private void SetStateNbrType(Direction dir, NbrType type)
        {
            int t = (int)type;
            int idx = nbrBitWidth * dir.ToInt();
            state.SetValue((state.GetCurrentValue() & ~(3 << idx)) | (t << idx));
        }

        #endregion
    }

} // namespace AS2.Subroutines.PASC
