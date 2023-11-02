using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

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
    /// Setup: Call <see cref="Init(bool, Direction, Direction, int, int, int, int, int, int, bool)"/>
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
    /// Usage: After initializing the sequence of particles, call
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
    /// Warning: The algorithm only works properly if there are at least two particles!
    /// </para>
    /// </summary>
    public class SubPASC : Subroutine
    {

        // We need no leader attribute because the leader has no predecessor => Its pred indices are -1
        ParticleAttribute<int> predPrimaryPin;          // Pin indices are directionIdx * PinsPerEdge + offset (need no direction attribute this way)
        ParticleAttribute<int> predSecondaryPin;
        ParticleAttribute<int> succPrimaryPin;
        ParticleAttribute<int> succSecondaryPin;
        ParticleAttribute<int> primaryPSetID;           // Partition set IDs so that multiple PASC instances do not interfere
        ParticleAttribute<int> secondaryPSetID;

        ParticleAttribute<bool> isActive;
        ParticleAttribute<bool> becamePassive;          // Whether we became passive in the last round
        ParticleAttribute<bool> lastBitIs1;             // Whether the last received bit was a 1

        public SubPASC(Particle p) : base(p)
        {
            predPrimaryPin = algo.CreateAttributeInt(FindValidAttributeName("[PASC] Pin Pred P"), -1);
            predSecondaryPin = algo.CreateAttributeInt(FindValidAttributeName("[PASC] Pin Pred S"), -1);
            succPrimaryPin = algo.CreateAttributeInt(FindValidAttributeName("[PASC] Pin Succ P"), -1);
            succSecondaryPin = algo.CreateAttributeInt(FindValidAttributeName("[PASC] Pin Succ S"), -1);
            primaryPSetID = algo.CreateAttributeInt(FindValidAttributeName("[PASC] Primary PSet"), -1);
            secondaryPSetID = algo.CreateAttributeInt(FindValidAttributeName("[PASC] Secondary PSet"), -1);

            isActive = algo.CreateAttributeBool(FindValidAttributeName("[PASC] Active"), false);
            becamePassive = algo.CreateAttributeBool(FindValidAttributeName("[PASC] Became passive"), false);
            lastBitIs1 = algo.CreateAttributeBool(FindValidAttributeName("[PASC] Last Bit 1"), false);
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
        /// <param name="primaryPSet">The partition set ID to use for the primary circuit.</param>
        /// <param name="secondaryPSet">The partition set ID to use for the secondary circuit.</param>
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
                this.predPrimaryPin.SetValue(-1);
                this.predSecondaryPin.SetValue(-1);
            }
            else
            {
                this.predPrimaryPin.SetValue(predDirInt * algo.PinsPerEdge + predPrimaryPin);
                this.predSecondaryPin.SetValue(predDirInt * algo.PinsPerEdge + predSecondaryPin);
            }
            if (succDir == Direction.NONE)
            {
                this.succPrimaryPin.SetValue(-1);
                this.succSecondaryPin.SetValue(-1);
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
            // TODO: Could simplify this by storing the pin IDs directly


            if (predPrimaryPin.GetCurrentValue() == -1)
            {
                // We have no predecessor => Only need successor pins
                Direction succDir = DirectionHelpers.Cardinal(succPrimaryPin.GetCurrentValue() / algo.PinsPerEdge);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(succDir, succPrimaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, primaryPSetID.GetCurrentValue());
                pc.MakePartitionSet(new int[] { pc.GetPinAt(succDir, succSecondaryPin.GetCurrentValue() % algo.PinsPerEdge).Id }, secondaryPSetID.GetCurrentValue());
            }
            else if (succPrimaryPin.GetCurrentValue() == -1)
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
        /// Processes the received beep that was sent in
        /// the last round. Must be called in the next round
        /// after sending the beep and before the current
        /// pin configuration changes.
        /// </summary>
        public void ActivateReceive()
        {
            // If we became passive in the previous round, we
            // did not become passive in this round
            if (becamePassive)
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
                if (isActive)
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
        /// Whether this particle is the sequence's leader.
        /// </summary>
        /// <returns><c>true</c> if and only if this particle
        /// has no predecessor.</returns>
        public bool IsLeader()
        {
            return predPrimaryPin.GetCurrentValue() == -1;
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
