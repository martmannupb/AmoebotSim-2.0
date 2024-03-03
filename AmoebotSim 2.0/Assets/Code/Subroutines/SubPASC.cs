using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;

namespace AS2.Subroutines.PASC
{

    /// <summary>
    /// Implements the PASC (Primary-And-Secondary-Circuit) algorithm
    /// (https://drops.dagstuhl.de/opus/volltexte/2022/16793/).
    /// <para>
    /// For a sequence of particles p0,p1,...,pm with pp being the leader,
    /// iteratively deactivates every second active particle in the sequence.
    /// Takes O(log(m)) iterations to deactivate all particles. In the process,
    /// each particle pi receives the distance to p0 (= i) in binary, with bits
    /// arriving in increasing order.
    /// </para>
    /// <para>
    /// <b>Disclaimer:</b> Only works if the number of pins per edge is at most 5.
    /// </para>
    /// <para>
    /// Setup:<br/>
    /// Call <see cref="Init(bool, Direction, Direction, int, int, int, int, int, int, bool)"/>
    /// and tell each particle in the sequence whether it is p0, the directions to
    /// its predecessor and successor particles (<see cref="Direction.NONE"/> for
    /// the predecessor/successor of the first/last particle, resp.), which pins and
    /// partition set IDs to use for the two circuits, and whether it should start active
    /// (it is possible to start the procedure with some particles already passive).<br/>
    /// Every particle will use 2 partition sets and use 2 pins for its predecessor and
    /// 2 (other!) pins for its successor. Make sure that for neighboring particles
    /// p(i), p(i+1), the primary successor pin of p(i) is connected to the primary
    /// predecessor pin of p(i+1) and the same holds for the secondary pins.
    /// </para>
    /// <para>
    /// Usage:<br/>
    /// After initializing the sequence of particles, call
    /// <see cref="SetupPC(PinConfiguration)"/> to make each particle establish its
    /// partition sets. This does not plan the pin configuration yet, in the case
    /// that other changes are required. You can then call <see cref="ActivateSend"/>
    /// (after planning the pin configuration) to send the beeps. This is only necessary
    /// for the leader particle.<br/>
    /// In the next round, you have to call <see cref="ActivateReceive"/> before changing
    /// the pin configuration so that the received beeps can be processed. After this call,
    /// you can read the received bit and check if the particle became passive in
    /// this round. Then you can repeat the process.<br/>
    /// The subroutine does not include a termination check. You need to terminate
    /// manually when no particle of the sequence has become passive. It is no
    /// problem to keep the algorithm running after the last particle became passive.
    /// This allows you to run multiple instances of PASC and keep them running
    /// until each instance has finished.
    /// </para>
    /// <para>
    /// Early termination:<br/>
    /// If you use PASC to compare the distance bits to another sequence of bits which
    /// may be shorter than the PASC result (i.e., the compared sequence has a lower most
    /// significant bit than the number of PASC iterations), you can perform a cutoff to
    /// save a few rounds as follows:<br/>
    /// Instead of calling <see cref="SetupPC(PinConfiguration)"/>, call
    /// <see cref="SetupCutoffCircuit(PinConfiguration)"/> and plan the resulting pin
    /// configuration. This will setup a circuit where all active non-leader particles
    /// disconnect their predecessor and successor. After planning the pin configuration,
    /// call <see cref="SendCutoffBeep"/> instead of <see cref="ActivateSend"/> (but call
    /// it on all particles, not just the leader). This will make the active non-leader
    /// particles send a beep to their successor on both circuits, causing all particles
    /// after the first active non-leader particle to receive a beep. These are exactly
    /// the particles that will receive at least one 1-bit in the future PASC iterations,
    /// i.e., the particles whose PASC value will definitely be greater than the compared
    /// bit sequence. All particles that do not receive a beep (the leader and all
    /// connected passive particles) will only receive 0-bits, i.e., their comparison
    /// result is already finished since the compared sequence also has only 0-bits left.
    /// To read the result of this cutoff, call <see cref="ReceiveCutoffBeep"/> instead of
    /// <see cref="ActivateReceive"/>. The result of <see cref="GetReceivedBit"/> will be
    /// <c>1</c> if the particle has received the cutoff bit, and <c>0</c> otherwise.<br/>
    /// Afterwards, it is still possible to continue the PASC procedure where it was
    /// interrupted, starting with <see cref="SetupPC(PinConfiguration)"/>.
    /// </para>
    /// </summary>
    public class SubPASC : Subroutine
    {
        //     30           29               28          2724        2320       19 15         14 10         9   5         4   0
        // x   x            x                x           xxxx        xxxx       xxxxx         xxxxx         xxxxx         xxxxx
        //     Last bit 1   Became passive   Is active   Sec. PSet   Pri. PSet  Succ. Pin 2   Succ. Pin 1   Pred. Pin 2   Pred. Pin 1
        ParticleAttribute<int> state;

        private static readonly int invalidPin = 31;

        // We need no leader attribute because the leader has no predecessor => Its pred indices are the invalid pin value
        BinAttributeInt predPrimaryPin;          // Pin indices are directionIdx * PinsPerEdge + offset (need no direction attribute this way)
        BinAttributeInt predSecondaryPin;
        BinAttributeInt succPrimaryPin;
        BinAttributeInt succSecondaryPin;
        BinAttributeInt primaryPSetID;           // Partition set IDs so that multiple PASC instances do not interfere
        BinAttributeInt secondaryPSetID;

        BinAttributeBool isActive;
        BinAttributeBool becamePassive;          // Whether we became passive in the last round
        BinAttributeBool lastBitIs1;             // Whether the last received bit was a 1

        public SubPASC(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[PASC] State"), 0);

            predPrimaryPin = new BinAttributeInt(state, 0, 5);
            predSecondaryPin = new BinAttributeInt(state, 5, 5);
            succPrimaryPin = new BinAttributeInt(state, 10, 5);
            succSecondaryPin = new BinAttributeInt(state, 15, 5);
            primaryPSetID = new BinAttributeInt(state, 20, 4);
            secondaryPSetID = new BinAttributeInt(state, 24, 4);
            isActive = new BinAttributeBool(state, 28);
            becamePassive = new BinAttributeBool(state, 29);
            lastBitIs1 = new BinAttributeBool(state, 30);
        }

        /// <summary>
        /// Initializes the PASC subroutine.
        /// </summary>
        /// <param name="isLeader">Whether this particle is the first one in the sequence.</param>
        /// <param name="predDir">The direction of the predecessor in the sequence.</param>
        /// <param name="succDir">The direction of the successor in the sequence.</param>
        /// <param name="predPrimaryPin">The primary pin's offset in predecessor direction.</param>
        /// <param name="predSecondaryPin">The secondary pin's offset in predecessor direction.</param>
        /// <param name="succPrimaryPin">The primary pin's offset in successor direction.</param>
        /// <param name="succSecondaryPin">The secondary pin's offset in successor direction.</param>
        /// <param name="primaryPSet">The partition set ID to use for the primary circuit. Must be at most 15.</param>
        /// <param name="secondaryPSet">The partition set ID to use for the secondary circuit. Must be at most 15.</param>
        /// <param name="active">Whether this particle should start active.</param>
        public void Init(bool isLeader, Direction predDir, Direction succDir,
            int predPrimaryPin, int predSecondaryPin, int succPrimaryPin, int succSecondaryPin,
            int primaryPSet, int secondaryPSet,
            bool active = true)
        {
            int predDirInt = predDir.ToInt();
            int succDirInt = succDir.ToInt();
            if (isLeader)
            {
                this.predPrimaryPin.SetValue(invalidPin);
                this.predSecondaryPin.SetValue(invalidPin);
            }
            else
            {
                this.predPrimaryPin.SetValue(predDirInt * algo.PinsPerEdge + predPrimaryPin);
                this.predSecondaryPin.SetValue(predDirInt * algo.PinsPerEdge + predSecondaryPin);
            }
            if (succDir == Direction.NONE)
            {
                this.succPrimaryPin.SetValue(invalidPin);
                this.succSecondaryPin.SetValue(invalidPin);
            }
            else
            {
                this.succPrimaryPin.SetValue(succDirInt * algo.PinsPerEdge + succPrimaryPin);
                this.succSecondaryPin.SetValue(succDirInt * algo.PinsPerEdge + succSecondaryPin);
            }
            primaryPSetID.SetValue(primaryPSet);
            secondaryPSetID.SetValue(secondaryPSet);

            this.isActive.SetValue(active);
            this.becamePassive.SetValue(false);
            this.lastBitIs1.SetValue(false);
        }

        /// <summary>
        /// Sets up the primary and secondary partition sets
        /// for this particle. The pin configuration is not
        /// planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.
        /// This will only modify the pins and partition sets
        /// specified for this subroutine.</param>
        public void SetupPC(PinConfiguration pc)
        {
            SetupPinConfig(pc, false);
        }

        private void SetupPinConfig(PinConfiguration pc, bool cutoff = false)
        {
            // TODO: Could simplify this by storing the pin IDs directly


            if (predPrimaryPin.GetCurrentValue() == invalidPin
                // Cutoff: Active particles do not connect their predecessor to their successor
                || cutoff && isActive.GetCurrentValue())
            {
                // We have no predecessor => Only need successor pins
                Direction succDir = DirectionHelpers.Cardinal(succPrimaryPin.GetCurrentValue() / algo.PinsPerEdge);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(succDir, succPrimaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, primaryPSetID.GetCurrentValue());
                pc.MakePartitionSet(new int[] { pc.GetPinAt(succDir, succSecondaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, secondaryPSetID.GetCurrentValue());
            }
            else if (succPrimaryPin.GetCurrentValue() == invalidPin)
            {
                // We have no successor => Only need predecessor pins
                int pSetPrimary = primaryPSetID.GetCurrentValue();
                int pSetSecondary = secondaryPSetID.GetCurrentValue();
                if (isActive.GetCurrentValue())
                {
                    // Switch the two sets if we are active
                    int tmp = pSetPrimary;
                    pSetPrimary = pSetSecondary;
                    pSetSecondary = tmp;
                }
                Direction predDir = DirectionHelpers.Cardinal(predPrimaryPin.GetCurrentValue() / algo.PinsPerEdge);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(predDir, predPrimaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, pSetPrimary);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(predDir, predSecondaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, pSetSecondary);
            }
            else
            {
                // Have both predecessor and successor => Connect
                Direction predDir = DirectionHelpers.Cardinal(predPrimaryPin.GetCurrentValue() / algo.PinsPerEdge);
                Direction succDir = DirectionHelpers.Cardinal(succPrimaryPin.GetCurrentValue() / algo.PinsPerEdge);
                int predPinPrimary = pc.GetPinAt(predDir, predPrimaryPin.GetCurrentValue() % algo.PinsPerEdge).Id;      // Pred pin connected to primary PSet
                int predPinSecondary = pc.GetPinAt(predDir, predSecondaryPin.GetCurrentValue() % algo.PinsPerEdge).Id;  // Pred pin connected to secondary PSet
                if (isActive.GetCurrentValue())
                {
                    // Switch the pins if we are active
                    int tmp = predPinPrimary;
                    predPinPrimary = predPinSecondary;
                    predPinSecondary = tmp;
                }
                pc.MakePartitionSet(new int[] { predPinPrimary, pc.GetPinAt(succDir, succPrimaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, primaryPSetID.GetCurrentValue());
                pc.MakePartitionSet(new int[] { predPinSecondary, pc.GetPinAt(succDir, succSecondaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, secondaryPSetID.GetCurrentValue());
                // Also place the partition sets nicely
                float pinsPerEdgeFactor = ((algo.PinsPerEdge - 1) / 2.0f);
                float primaryPin = ((predPrimaryPin.GetCurrentValue() % algo.PinsPerEdge) - pinsPerEdgeFactor) / pinsPerEdgeFactor;
                float secondaryPin = ((predSecondaryPin.GetCurrentValue() % algo.PinsPerEdge) - pinsPerEdgeFactor) / pinsPerEdgeFactor;
                float maxAngle = 45.0f - 90.0f / (algo.PinsPerEdge + 1);
                pc.SetPartitionSetPosition(primaryPSetID.GetCurrentValue(), new Vector2(predDir.ToInt() * 60.0f + primaryPin * maxAngle, 0.7f));
                pc.SetPartitionSetPosition(secondaryPSetID.GetCurrentValue(), new Vector2(predDir.ToInt() * 60.0f + secondaryPin * maxAngle, 0.7f));
            }
        }

        /// <summary>
        /// Sets up the cutoff circuit for early termination.
        /// The pin configuration is not planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.
        /// This will only modify the pins and partition sets
        /// specified for this subroutine.</param>
        public void SetupCutoffCircuit(PinConfiguration pc)
        {
            SetupPinConfig(pc, true);
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
            if (becamePassive.GetCurrentValue())
                becamePassive.SetValue(false);

            // Check where we have received a beep
            PinConfiguration pc = algo.GetCurrentPinConfiguration();
            bool beepOnPrimary = pc.ReceivedBeepOnPartitionSet(primaryPSetID.GetCurrentValue());
            bool beepOnSecondary = pc.ReceivedBeepOnPartitionSet(secondaryPSetID.GetCurrentValue());

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
                if (isActive.GetCurrentValue())
                {
                    isActive.SetValue(false);
                    becamePassive.SetValue(true);
                }
                // Bit for secondary circuit is 1
                lastBitIs1.SetValue(true);
            }
            else
            {
                // Beep on primary circuit: Bit is 0
                lastBitIs1.SetValue(false);
            }
        }

        /// <summary>
        /// Causes the leader of the particle sequence to send the
        /// beep that deactivates half of the active particles.
        /// Must only be called after the final pin configuration
        /// has been planned.
        /// </summary>
        public void ActivateSend()
        {
            // Leader sends beep on primary partition set
            if (IsLeader())
            {
                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                pc.SendBeepOnPartitionSet(primaryPSetID.GetCurrentValue());
            }
        }

        /// <summary>
        /// Causes the active non-leader particles to send the
        /// cutoff beep that is received by all particles that will
        /// receive at least one 1-bit in a future PASC iteration.
        /// Must only be called after the final pin configuration
        /// setup by <see cref="SetupCutoffCircuit(PinConfiguration)"/>
        /// has been planned.
        /// </summary>
        public void SendCutoffBeep()
        {
            if (isActive.GetCurrentValue() && !IsLeader())
            {
                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                pc.SendBeepOnPartitionSet(primaryPSetID.GetCurrentValue());
                pc.SendBeepOnPartitionSet(secondaryPSetID.GetCurrentValue());
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
            lastBitIs1.SetValue(pc.ReceivedBeepOnPartitionSet(primaryPSetID.GetCurrentValue()));
        }

        /// <summary>
        /// Whether this particle is the sequence's leader.
        /// </summary>
        /// <returns><c>true</c> if and only if this particle
        /// has no predecessor.</returns>
        public bool IsLeader()
        {
            return predPrimaryPin.GetCurrentValue() == invalidPin;
        }

        /// <summary>
        /// Whether this particle is active. The leader
        /// is always active.
        /// </summary>
        /// <returns><c>true</c> if and only if this particle
        /// is active.</returns>
        public bool IsActive()
        {
            return isActive.GetCurrentValue();
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
            return becamePassive.GetCurrentValue();
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
            return lastBitIs1.GetCurrentValue() ? 1 : 0;
        }
    }

} // namespace AS2.Subroutines.PASC
