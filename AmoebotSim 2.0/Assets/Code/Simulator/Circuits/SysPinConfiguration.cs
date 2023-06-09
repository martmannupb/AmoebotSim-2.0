using System;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// System-side implementation of the abstract base class
    /// <see cref="PinConfiguration"/>, which declares the API
    /// for the developer.
    /// <para>
    /// Can generate a compressed representation to enable saving
    /// and loading pin configurations, see <see cref="PinConfigurationSaveData"/>.
    /// </para>
    /// </summary>
    public class SysPinConfiguration : PinConfiguration
    {
        /// <summary>
        /// The particle to which this pin configuration belongs.
        /// </summary>
        public Particle particle;
        /// <summary>
        /// The number of pins on each edge.
        /// </summary>
        private int pinsPerEdge;
        /// <summary>
        /// The local head direction if this pin configuration is expanded.
        /// </summary>
        private Direction headDirection;
        /// <summary>
        /// The total number of pins.
        /// </summary>
        private int numPins;

        /// <summary>
        /// The pins contained in this pin configuration.
        /// </summary>
        public SysPin[] pins;
        /// <summary>
        /// Pins sorted by global IDs. These are the same pins as
        /// the ones stored in <see cref="pins"/>.
        /// </summary>
        public SysPin[] pinsGlobal;
        /// <summary>
        /// The partition sets defining this pin configuration.
        /// </summary>
        public SysPartitionSet[] partitionSets;

        // State information for receiving and sending beeps and messages
        /// <summary>
        /// Indicates whether this is the current pin configuration.
        /// </summary>
        public bool isCurrent = false;  // If true, give access to received data
        /// <summary>
        /// Indicates whether this is the planned pin configuration.
        /// </summary>
        public bool isPlanned = false;  // If true, allow sending data

        // Visualization info
        /// <summary>
        /// The selected partition set placement mode in the particle's head.
        /// </summary>
        public PSPlacementMode placementModeHead = PSPlacementMode.NONE;
        /// <summary>
        /// The selected partition set placement mode in the particle's tail.
        /// </summary>
        public PSPlacementMode placementModeTail = PSPlacementMode.NONE;
        /// <summary>
        /// The global angle of the line along which partition sets are
        /// placed in the particle's head.
        /// </summary>
        public float lineRotationHead = 0f;
        /// <summary>
        /// The global angle of the line along which partition sets are
        /// placed in the particle's tail.
        /// </summary>
        public float lineRotationTail = 0f;

        public SysPinConfiguration(Particle particle, int pinsPerEdge, Direction headDirection = Direction.NONE)
        {
            this.particle = particle;
            this.pinsPerEdge = pinsPerEdge;
            this.headDirection = headDirection;

            numPins = headDirection == Direction.NONE ? (6 * pinsPerEdge) : (10 * pinsPerEdge);

            partitionSets = new SysPartitionSet[numPins];
            pins = new SysPin[numPins];
            pinsGlobal = new SysPin[numPins];

            Direction comDir = particle.comDir;
            bool chirality = particle.chirality;

            // Initialize partition sets and pins
            // Default is singleton: Each pin is its own partition set
            // Store each pin in its local position and its global position
            if (headDirection == Direction.NONE)
            {
                for (int d = 0; d < 6; d++)
                {
                    Direction dir = DirectionHelpers.Cardinal(d);
                    Direction globalDir = ParticleSystem_Utils.LocalToGlobalDir(dir, comDir, chirality);
                    int globalDirInt = globalDir.ToInt();
                    for (int idx = 0; idx < pinsPerEdge; idx++)
                    {
                        int idxGlobal = chirality ? idx : pinsPerEdge - 1 - idx;
                        int id = d * pinsPerEdge + idx;
                        int idGlobal = globalDirInt * pinsPerEdge + idxGlobal;
                        SysPartitionSet ps = new SysPartitionSet(this, id, numPins);
                        SysPin pin = new SysPin(ps, id, dir, globalDirInt, true, idx, idxGlobal);
                        ps.AddPinBasic(id);
                        partitionSets[id] = ps;
                        pins[id] = pin;
                        pinsGlobal[idGlobal] = pin;
                    }
                }
            }
            else
            {
                for (int label = 0; label < 10; label++)
                {
                    Direction direction = ParticleSystem_Utils.GetDirOfLabel(label, headDirection);
                    Direction globalDir = ParticleSystem_Utils.LocalToGlobalDir(direction, comDir, chirality);
                    bool isHead = ParticleSystem_Utils.IsHeadLabel(label, headDirection);
                    int globalLabel = ParticleSystem_Utils.GetLabelInDir(globalDir, ParticleSystem_Utils.LocalToGlobalDir(headDirection, comDir, chirality), isHead);
                    for (int idx = 0; idx < pinsPerEdge; idx++)
                    {
                        int idxGlobal = chirality ? idx : pinsPerEdge - 1 - idx;
                        int id = label * pinsPerEdge + idx;
                        int idGlobal = globalLabel * pinsPerEdge + idxGlobal;
                        SysPartitionSet ps = new SysPartitionSet(this, id, numPins);
                        SysPin pin = new SysPin(ps, id, direction, globalLabel, isHead, idx, idxGlobal);
                        ps.AddPinBasic(id);
                        partitionSets[id] = ps;
                        pins[id] = pin;
                        pinsGlobal[idGlobal] = pin;
                    }
                }
            }
        }

        /// <summary>
        /// Resets current and planned flags to <c>false</c>.
        /// </summary>
        private void UpdateFlagsAfterChange()
        {
            isCurrent = false;
            isPlanned = false;
        }

        /// <summary>
        /// Computes the ID of the pin on the specified edge with the
        /// given offset.
        /// <para>
        /// The formula for the pin ID is <c>label * pinsPerEdge +
        /// <paramref name="offset"/></c>, where <c>label</c> is computed
        /// using <paramref name="direction"/> and <paramref name="head"/>.
        /// </para>
        /// </summary>
        /// <param name="direction">The local direction of the edge.</param>
        /// <param name="offset">The edge offset of the pin.</param>
        /// <param name="head">If the pin configuration represents the
        /// expanded state, this flag indicates whether the edge belongs to
        /// the particle's head or not.</param>
        /// <returns>The ID of the pin in the location specified by an edge
        /// and an edge offset.</returns>
        public int GetPinId(Direction direction, int offset, bool head = true)
        {
            return ParticleSystem_Utils.GetLabelInDir(direction, headDirection, head) * pinsPerEdge + offset;
        }

        /// <summary>
        /// Returns the pin with the given ID.
        /// </summary>
        /// <param name="pinId">The ID of the pin to return.</param>
        /// <returns>The pin with ID <paramref name="pinId"/>.</returns>
        public SysPin GetPin(int pinId)
        {
            return pins[pinId];
        }

        /// <summary>
        /// Returns the partition set with the given ID.
        /// <para>
        /// This is the system-side version of <see cref="GetPartitionSet(int)"/>,
        /// which is part of the algorithm developer API.
        /// </para>
        /// </summary>
        /// <param name="partitionSetId">The ID of the partition set to return.</param>
        /// <returns>The partition set with ID <paramref name="partitionSetId"/>.</returns>
        public SysPartitionSet GetPartitionSetWithId(int partitionSetId)
        {
            return partitionSets[partitionSetId];
        }

        /// <summary>
        /// Tries to remove the specified pin from its
        /// partition set and insert it into an empty
        /// partition set.
        /// <para>
        /// Throws a <see cref="System.InvalidOperationException"/> if
        /// no empty partition set can be found.
        /// </para>
        /// </summary>
        /// <param name="pinId">The ID of the pin to be
        /// removed from its partition set.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the pin is the only one in this partition set and
        /// no other partition set is empty.
        /// </exception>
        public void TryRemovePin(int pinId)
        {
            // Find empty partition set
            for (int i = 0; i < numPins; i++)
            {
                SysPartitionSet ps = partitionSets[i];
                if (ps.IsEmpty())
                {
                    ps.AddPin(pinId);
                    UpdateFlagsAfterChange();
                    return;
                }
            }
            throw new System.InvalidOperationException("Pin with ID " + pinId + " cannot be removed from its partition set: No other partition set is empty.");
        }

        /// <summary>
        /// Tries to remove the specified pins from their
        /// partition set and insert them into empty
        /// partition sets.
        /// <para>
        /// It is assumed that the pins should all be removed
        /// from the same partition set.
        /// </para>
        /// <para>
        /// Throws a <see cref="System.InvalidOperationException"/> if at
        /// any point, no empty partition set can be found.
        /// </para>
        /// </summary>
        /// <param name="pinIds">The IDs of the pins to be removed from
        /// their partition set. Must all be from the same partition set.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if one of the pins is the last one in the partition set
        /// and no other partition set is empty.
        /// </exception>
        public void TryRemovePins(int[] pinIds)
        {
            // Try to find an empty partition set for each pin
            int i = 0;
            foreach (int pinId in pinIds)
            {
                bool foundSet = false;
                for (; i < numPins; i++)
                {
                    SysPartitionSet ps = partitionSets[i];
                    if (ps.IsEmpty())
                    {
                        ps.AddPin(pinId);
                        foundSet = true;
                        UpdateFlagsAfterChange();
                        break;
                    }
                }
                if (!foundSet)
                {
                    throw new System.InvalidOperationException("Pin with ID " + pinId + " cannot be removed from its partition set: No other partition set is empty.");
                }
            }
        }

        // TODO: There is probably a much more efficient way to do this
        /// <summary>
        /// Creates a full copy of this pin configuration.
        /// <para>
        /// All <see cref="SysPin"/>s and <see cref="SysPartitionSet"/>s contained in
        /// this pin configuration are copied as well. It is not a deep copy
        /// because the reference to the containing <see cref="Particle"/> stays
        /// the same.
        /// </para>
        /// </summary>
        /// <returns>A copy of this pin configuration.</returns>
        public SysPinConfiguration Copy()
        {
            SysPinConfiguration copy = new SysPinConfiguration(particle, pinsPerEdge, headDirection);
            for (int i = 0; i < numPins; i++)
            {
                if (!partitionSets[i].IsEmpty())
                {
                    copy.MakePartitionSet(partitionSets[i].GetPins(), i);
                }
            }
            copy.placementModeHead = placementModeHead;
            copy.placementModeTail = placementModeTail;
            copy.lineRotationHead = lineRotationHead;
            copy.lineRotationTail = lineRotationTail;
            for (int i = 0; i < numPins; i++)
            {
                SysPartitionSet spCopy = copy.partitionSets[i];
                SysPartitionSet spMine = partitionSets[i];
                spCopy.color = spMine.color;
                spCopy.colorOverride = spMine.colorOverride;
                spCopy.positionHead = spMine.positionHead;
                spCopy.positionTail = spMine.positionTail;
                spCopy.drawSingletonHandle = spMine.drawSingletonHandle;
            }
            copy.isCurrent = isCurrent;
            copy.isPlanned = isPlanned;
            return copy;
        }

        /*
         * Comparison operators for comparing pin configurations easily
         */

        public static bool operator ==(SysPinConfiguration pc1, SysPinConfiguration pc2)
        {
            // Two nulls are equal
            if (pc1 is null && pc2 is null)
            {
                return true;
            }
            // Unequal if one is null or head direction or number of pins per edge differ
            else if (pc1 is null || pc2 is null || pc1.headDirection != pc2.headDirection || pc1.pinsPerEdge != pc2.pinsPerEdge)
            {
                return false;
            }
            // Unequal if placement mode or parameters are different
            if (pc1.placementModeHead != pc2.placementModeHead || pc1.placementModeTail != pc2.placementModeTail || pc1.lineRotationHead != pc2.lineRotationHead || pc1.lineRotationTail != pc2.lineRotationTail)
                return false;
            // Unequal if any partition sets are not equal
            for (int i = 0; i < pc1.numPins; i++)
            {
                if (pc1.partitionSets[i] != pc2.partitionSets[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool operator !=(SysPinConfiguration pc1, SysPinConfiguration pc2)
        {
            // Opposite of ==
            if (pc1 is null && pc2 is null)
            {
                return false;
            }
            else if (pc1 is null || pc2 is null || pc1.headDirection != pc2.headDirection || pc1.pinsPerEdge != pc2.pinsPerEdge)
            {
                return true;
            }
            if (pc1.placementModeHead != pc2.placementModeHead || pc1.placementModeTail != pc2.placementModeTail || pc1.lineRotationHead != pc2.lineRotationHead || pc1.lineRotationTail != pc2.lineRotationTail)
                return true;
            for (int i = 0; i < pc1.numPins; i++)
            {
                if (pc1.partitionSets[i] != pc2.partitionSets[i])
                {
                    return true;
                }
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            return obj is SysPinConfiguration other && this == other;
        }

        // TODO: Make sure this is correct if it is ever used
        public override int GetHashCode()
        {
            return System.HashCode.Combine(pinsPerEdge, headDirection, partitionSets, placementModeHead, placementModeTail, lineRotationHead, lineRotationTail);
        }


        /*
         * PinConfiguration: Developer API implementation
         */

        public override Direction HeadDirection
        {
            get { return headDirection; }
        }

        public override int PinsPerEdge
        {
            get { return pinsPerEdge; }
        }

        public override int NumPins
        {
            get { return numPins; }
        }

        public override Pin GetPinAt(Direction direction, int offset, bool head = true)
        {
            int pinId = GetPinId(direction, offset, head);
            return pins[pinId];
        }

        public override Pin[] GetPinsAtEdge(Direction direction, bool head = true)
        {
            Pin[] ipins = new Pin[pinsPerEdge];
            int pinId = GetPinId(direction, 0, head);
            for (int i = 0; i < pinsPerEdge; i++)
            {
                ipins[i] = pins[pinId];
                pinId++;
            }
            return ipins;
        }

        public override PartitionSet GetPartitionSet(int index)
        {
            return partitionSets[index];
        }

        public override PartitionSet[] GetPartitionSets()
        {
            return partitionSets;
        }

        public override PartitionSet[] GetNonEmptyPartitionSets()
        {
            List<SysPartitionSet> nonEmptySets = new List<SysPartitionSet>();
            foreach (SysPartitionSet ps in partitionSets)
            {
                if (!ps.IsEmpty())
                {
                    nonEmptySets.Add(ps);
                }
            }
            return nonEmptySets.ToArray();
        }

        public override void SetToSingleton()
        {
            for (int id = 0; id < numPins; id++)
            {
                SysPartitionSet ps = partitionSets[id];
                SysPin pin = pins[id];
                ps.ClearInternal();
                ps.AddPinBasic(id);
                pin.partitionSet = ps;
            }
            UpdateFlagsAfterChange();
        }

        public override void SetToGlobal(int partitionSetId = 0)
        {
            SysPartitionSet psGlobal = partitionSets[partitionSetId];
            psGlobal.ClearInternal();
            for (int id = 0; id < numPins; id++)
            {
                SysPartitionSet ps = partitionSets[id];
                SysPin pin = pins[id];
                if (id != partitionSetId)
                {
                    ps.ClearInternal();
                }
                psGlobal.AddPinBasic(id);
                pin.partitionSet = psGlobal;
            }
            UpdateFlagsAfterChange();
        }

        public override void SetToGlobal(PartitionSet partitionSet)
        {
            SetToGlobal(partitionSet.Id);
        }

        public override void SetStarConfig(int offset, int partitionSetIndex)
        {
            SetStarConfig(offset, new bool[headDirection == Direction.NONE ? 6 : 10], partitionSetIndex);
        }

        public override void SetStarConfig(int offset, PartitionSet partitionSet)
        {
            SetStarConfig(offset, new bool[headDirection == Direction.NONE ? 6 : 10], partitionSet.Id);
        }

        public override void SetStarConfig(int offset, bool[] inverted, int partitionSetIndex)
        {
            // First add the correct pins, then remove the ones that have to be removed
            // This order avoids exceptions in all cases
            SysPartitionSet ps = partitionSets[partitionSetIndex];
            List<int> pinsToRemove = new List<int>();
            int numLabels = headDirection == Direction.NONE ? 6 : 10;
            for (int label = 0; label < numLabels; label++)
            {
                for (int os = 0; os < pinsPerEdge; os++)
                {
                    int pinId = label * pinsPerEdge + os;
                    if ((!inverted[label] && os == offset) || (inverted[label] && os == pinsPerEdge - 1 - offset))
                    {
                        ps.AddPin(pinId);
                    }
                    else if (ps.ContainsPin(pinId))
                    {
                        pinsToRemove.Add(pinId);
                    }
                }
            }
            TryRemovePins(pinsToRemove.ToArray());
            UpdateFlagsAfterChange();
        }

        public override void SetStarConfig(int offset, bool[] inverted, PartitionSet partitionSet)
        {
            SetStarConfig(offset, inverted, partitionSet.Id);
        }

        public override void MakePartitionSet(int[] pinIds, int partitionSetIndex)
        {
            // First add the correct pins, then remove the ones that have to be removed
            // This order avoids exceptions in all cases except the one where an error
            // is unavoidable
            SysPartitionSet ps = partitionSets[partitionSetIndex];
            List<int> pinsToRemove = new List<int>();
            System.Array.Sort(pinIds);
            int pinIdx = 0;
            int numLabels = headDirection == Direction.NONE ? 6 : 10;
            for (int label = 0; label < numLabels; label++)
            {
                for (int offset = 0; offset < pinsPerEdge; offset++)
                {
                    int pinId = label * pinsPerEdge + offset;
                    if (pinIdx < pinIds.Length && pinIds[pinIdx] == pinId)
                    {
                        ps.AddPin(pinId);
                        pinIdx++;
                    }
                    else if (ps.ContainsPin(pinId))
                    {
                        pinsToRemove.Add(pinId);
                    }
                }
            }
            TryRemovePins(pinsToRemove.ToArray());
            UpdateFlagsAfterChange();
        }

        public override void MakePartitionSet(Pin[] pins, int partitionSetIndex)
        {
            int[] pinIds = new int[pins.Length];
            for (int i = 0; i < pins.Length; i++)
            {
                pinIds[i] = pins[i].Id;
            }
            MakePartitionSet(pinIds, partitionSetIndex);
        }

        public override void MakePartitionSet(int[] pinIds, PartitionSet partitionSet)
        {
            MakePartitionSet(pinIds, partitionSet.Id);
        }

        public override void MakePartitionSet(Pin[] pins, PartitionSet partitionSet)
        {
            int[] pinIds = new int[pins.Length];
            for (int i = 0; i < pins.Length; i++)
            {
                pinIds[i] = pins[i].Id;
            }
            MakePartitionSet(pinIds, partitionSet.Id);
        }

        public override bool ReceivedBeepOnPartitionSet(int partitionSetIndex)
        {
            if (!isCurrent)
            {
                throw new InvalidOperationException("Cannot check for received beeps in non-current pin configuration.");
            }
            return particle.HasReceivedBeep(partitionSetIndex);
        }

        public override void SendBeepOnPartitionSet(int partitionSetIndex)
        {
            if (!isPlanned)
            {
                throw new InvalidOperationException("Cannot send beeps in non-planned pin configuration.");
            }
            particle.PlanBeep(partitionSetIndex, this);
        }

        public override bool ReceivedMessageOnPartitionSet(int partitionSetIndex)
        {
            if (!isCurrent)
            {
                throw new InvalidOperationException("Cannot check for received messages in non-current pin configuration.");
            }
            return particle.HasReceivedMessage(partitionSetIndex);
        }

        public override Message GetReceivedMessageOfPartitionSet(int partitionSetIndex)
        {
            if (!isCurrent)
            {
                throw new InvalidOperationException("Cannot get received messages from non-current pin configuration.");
            }
            return particle.GetReceivedMessage(partitionSetIndex);
        }

        public override void SendMessageOnPartitionSet(int partitionSetIndex, Message msg)
        {
            if (!isPlanned)
            {
                throw new InvalidOperationException("Cannot send messages in non-planned pin configuration.");
            }
            particle.PlanMessage(partitionSetIndex, msg != null ? msg.Copy() : null, this);
        }

        public override void SetPartitionSetColor(int partitionSetIndex, Color color)
        {
            partitionSets[partitionSetIndex].SetColor(color);

            // If the pin configuration is marked as planned, apply the same change
            // to the particle's planned PC
            if (isPlanned)
            {
                SysPinConfiguration planned = particle.PlannedPinConfiguration;
                if (planned != this)
                    planned.SetPartitionSetColor(partitionSetIndex, color);
            }
        }

        public override void ResetPartitionSetColor(int partitionSetIndex)
        {
            partitionSets[partitionSetIndex].ResetColor();

            // If the pin configuration is marked as planned, apply the same change
            // to the particle's planned PC
            if (isPlanned)
            {
                SysPinConfiguration planned = particle.PlannedPinConfiguration;
                if (planned != this)
                    planned.ResetPartitionSetColor(partitionSetIndex);
            }
        }

        public override void ResetAllPartitionSetColors()
        {
            foreach (SysPartitionSet sp in partitionSets)
            {
                sp.ResetColor();
            }

            // If the pin configuration is marked as planned, apply the same change
            // to the particle's planned PC
            if (isPlanned)
            {
                SysPinConfiguration planned = particle.PlannedPinConfiguration;
                if (planned != this)
                    planned.ResetAllPartitionSetColors();
            }
        }

        public override void SetPartitionSetPosition(int partitionSetIndex, Vector2 polarCoords, bool head = true)
        {
            partitionSets[partitionSetIndex].SetPosition(polarCoords, head);

            // If the pin configuration is marked as planned, apply the same change
            // to the particle's planned PC
            if (isPlanned)
            {
                SysPinConfiguration planned = particle.PlannedPinConfiguration;
                if (planned != this)
                    planned.SetPartitionSetPosition(partitionSetIndex, polarCoords, head);
            }
        }

        public override void SetPSPlacementMode(PSPlacementMode mode, bool head = true)
        {
            if (head)
                placementModeHead = mode;
            else
                placementModeTail = mode;

            // If the pin configuration is marked as planned, apply the same change
            // to the particle's planned PC
            if (isPlanned)
            {
                SysPinConfiguration planned = particle.PlannedPinConfiguration;
                if (planned != this)
                    planned.SetPSPlacementMode(mode, head);
            }
        }

        public override void SetLineRotation(float angle, bool head = true)
        {
            // Must convert angle from local to global
            float angleLocal = particle.CompassDir().ToInt() * 60f + (particle.chirality ? angle : -angle);
            if (head)
            {
                lineRotationHead = angleLocal;
                placementModeHead = PSPlacementMode.LINE_ROTATED;
            }
            else
            {
                lineRotationTail = angleLocal;
                placementModeTail = PSPlacementMode.LINE_ROTATED;
            }

            // If the pin configuration is marked as planned, apply the same change
            // to the particle's planned PC
            if (isPlanned)
            {
                SysPinConfiguration planned = particle.PlannedPinConfiguration;
                if (planned != this)
                    planned.SetLineRotation(angle, head);
            }
        }

        public override void ResetPartitionSetPlacement(bool head = true)
        {
            foreach (SysPartitionSet sp in partitionSets)
            {
                sp.SetPosition(Vector2.zero, head);
            }
            if (head)
                placementModeHead = PSPlacementMode.NONE;
            else
                placementModeTail = PSPlacementMode.NONE;

            // If the pin configuration is marked as planned, apply the same change
            // to the particle's planned PC
            if (isPlanned)
            {
                SysPinConfiguration planned = particle.PlannedPinConfiguration;
                if (planned != this)
                    planned.ResetPartitionSetPlacement(head);
            }
        }

        public override void SetPartitionSetDrawHandle(int partitionSetIndex, bool drawHandle)
        {
            partitionSets[partitionSetIndex].drawSingletonHandle = drawHandle;
        }

        public override void ResetPartitionSetDrawHandle(bool head = true)
        {
            foreach (SysPartitionSet ps in partitionSets)
                ps.drawSingletonHandle = false;
        }

        /*
         * Saving and loading functionality, also used for histories.
         */

        /// <summary>
        /// Creates a serializable object containing all data needed
        /// to restore the pin configuration.
        /// </summary>
        /// <returns>A serializable representation of this
        /// pin configuration.</returns>
        public PinConfigurationSaveData GenerateSaveData()
        {
            PinConfigurationSaveData data = new PinConfigurationSaveData();

            data.headDirection = headDirection;
            data.placementModeHead = placementModeHead;
            data.placementModeTail = placementModeTail;
            data.lineRotationHead = lineRotationHead;
            data.lineRotationTail = lineRotationTail;
            data.pinPartitionSets = new int[numPins];
            data.partitionSetColors = new Color[numPins];
            data.partitionSetColorOverrides = new bool[numPins];
            data.partitionSetHeadPositions = new Vector2[numPins];
            data.partitionSetTailPositions = new Vector2[numPins];
            data.partitionSetDrawHandleFlags = new bool[numPins];
            for (int i = 0; i < numPins; i++)
            {
                data.pinPartitionSets[i] = pins[i].partitionSet.id;
                SysPartitionSet ps = partitionSets[i];
                data.partitionSetColors[i] = ps.color;
                data.partitionSetColorOverrides[i] = ps.colorOverride;
                data.partitionSetHeadPositions[i] = ps.positionHead;
                data.partitionSetTailPositions[i] = ps.positionTail;
                data.partitionSetDrawHandleFlags[i] = ps.drawSingletonHandle;
            }

            return data;
        }

        /// <summary>
        /// Recovers a pin configuration from its serializable representation.
        /// It is expected that the given particle already has an algorithm
        /// attached to it and that it matches the given pin configuration
        /// save data.
        /// </summary>
        /// <param name="data">The serializable representation of the
        /// pin configuration.</param>
        /// <param name="p">The particle to which the pin configuration
        /// should belong.</param>
        public SysPinConfiguration(PinConfigurationSaveData data, Particle p)
        {
            particle = p;
            pinsPerEdge = p.algorithm.PinsPerEdge;
            headDirection = data.headDirection;

            numPins = headDirection == Direction.NONE ? (6 * pinsPerEdge) : (10 * pinsPerEdge);

            partitionSets = new SysPartitionSet[numPins];
            pins = new SysPin[numPins];
            pinsGlobal = new SysPin[numPins];

            Direction comDir = particle.comDir;
            bool chirality = particle.chirality;

            // Initialize partition sets and pins
            // Default is singleton: Each pin is its own partition set
            // Store each pin in its local position and its global position
            if (headDirection == Direction.NONE)
            {
                for (int d = 0; d < 6; d++)
                {
                    Direction direction = DirectionHelpers.Cardinal(d);
                    Direction globalDir = ParticleSystem_Utils.LocalToGlobalDir(direction, comDir, chirality);
                    int globalDirInt = globalDir.ToInt();
                    for (int idx = 0; idx < pinsPerEdge; idx++)
                    {
                        int idxGlobal = chirality ? idx : pinsPerEdge - 1 - idx;
                        int id = d * pinsPerEdge + idx;
                        int idGlobal = globalDirInt * pinsPerEdge + idxGlobal;
                        SysPartitionSet ps = new SysPartitionSet(this, id, numPins);
                        SysPin pin = new SysPin(ps, id, direction, globalDirInt, true, idx, idxGlobal);
                        partitionSets[id] = ps;
                        pins[id] = pin;
                        pinsGlobal[idGlobal] = pin;
                    }
                }
            }
            else
            {
                for (int label = 0; label < 10; label++)
                {
                    Direction direction = ParticleSystem_Utils.GetDirOfLabel(label, headDirection);
                    Direction globalDir = ParticleSystem_Utils.LocalToGlobalDir(direction, comDir, chirality);
                    bool isHead = ParticleSystem_Utils.IsHeadLabel(label, headDirection);
                    int globalLabel = ParticleSystem_Utils.GetLabelInDir(globalDir, ParticleSystem_Utils.LocalToGlobalDir(headDirection, comDir, chirality), isHead);
                    for (int idx = 0; idx < pinsPerEdge; idx++)
                    {
                        int idxGlobal = chirality ? idx : pinsPerEdge - 1 - idx;
                        int id = label * pinsPerEdge + idx;
                        int idGlobal = globalLabel * pinsPerEdge + idxGlobal;
                        SysPartitionSet ps = new SysPartitionSet(this, id, numPins);
                        SysPin pin = new SysPin(ps, id, direction, globalLabel, isHead, idx, idxGlobal);
                        partitionSets[id] = ps;
                        pins[id] = pin;
                        pinsGlobal[idGlobal] = pin;
                    }
                }
            }
            // Add the pins to their partition sets
            for (int i = 0; i < data.pinPartitionSets.Length; i++)
            {
                int psIdx = data.pinPartitionSets[i];
                partitionSets[psIdx].AddPinBasic(i);
                pins[i].partitionSet = partitionSets[psIdx];
            }
            // Set partition set visualization info
            for (int i = 0; i < numPins; i++)
            {
                SysPartitionSet ps = partitionSets[i];
                ps.color = data.partitionSetColors[i];
                ps.colorOverride = data.partitionSetColorOverrides[i];
                ps.positionHead = data.partitionSetHeadPositions[i];
                ps.positionTail = data.partitionSetTailPositions[i];
                ps.drawSingletonHandle = data.partitionSetDrawHandleFlags[i];
            }
            // Set placement mode
            placementModeHead = data.placementModeHead;
            placementModeTail = data.placementModeTail;
            lineRotationHead = data.lineRotationHead;
            lineRotationTail = data.lineRotationTail;
        }

        // <<<FOR DEBUGGING>>>
        public void Print()
        {
            string s = "Pin Configuration for head direction " + headDirection + " with " + pinsPerEdge + " pins per edge and " + numPins + " pins:\n";
            //Debug.Log("Pin Configuration for head direction " + headDirection + " with " + pinsPerEdge + " pins per edge and " + numPins + " pins:");
            for (int i = 0; i < numPins; i++)
            {
                //Debug.Log("Partition set " + i + ":");
                s += "Partition set " + i + ":\n" + partitionSets[i].Print() + "\n";
                //partitionSets[i].Print();
            }
            Debug.Log(s);
        }
    }

} // namespace AS2.Sim
