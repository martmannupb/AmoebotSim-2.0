// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.



namespace AS2.Sim
{

    /// <summary>
    /// System-side implementation of the abstract base class
    /// <see cref="Pin"/>, which declares the API
    /// for the developer.
    /// </summary>
    public class SysPin : Pin
    {
        /// <summary>
        /// The partition set to which this pin belongs.
        /// </summary>
        public SysPartitionSet partitionSet;
        /// <summary>
        /// The unique ID within the pin configuration.
        /// </summary>
        public int id;
        /// <summary>
        /// The local direction of the edge on which this pin lies.
        /// </summary>
        public Direction localDir;
        /// <summary>
        /// The global label of the edge on which this pin lies.
        /// </summary>
        public int globalLabel;
        /// <summary>
        /// Indicates whether the pin lies on the particle's head.
        /// </summary>
        public bool head;
        /// <summary>
        /// The index of the pin on its edge relative to the
        /// particle's chirality.
        /// </summary>
        public int localEdgeOffset;
        /// <summary>
        /// The index of the pin on its edge relative to the
        /// global counter-clockwise direction.
        /// </summary>
        public int globalEdgeOffset;

        public SysPin(SysPartitionSet partitionSet, int id, Direction localDir, int globalLabel, bool head, int localEdgeOffset, int globalEdgeOffset)
        {
            this.partitionSet = partitionSet;
            this.id = id;
            this.localDir = localDir;
            this.globalLabel = globalLabel;
            this.head = head;
            this.localEdgeOffset = localEdgeOffset;
            this.globalEdgeOffset = globalEdgeOffset;
        }


        /*
         * Pin: Developer API implementation
         */

        public override PartitionSet PartitionSet
        {
            get { return partitionSet; }
        }

        public override int Id
        {
            get { return id; }
        }

        public override Direction Direction
        {
            get { return localDir; }
        }

        public override int Offset
        {
            get { return localEdgeOffset; }
        }

        public override bool IsOnHead
        {
            get { return head; }
        }

        public override bool IsOnTail
        {
            get { return !head; }
        }

        public override bool ReceivedBeep()
        {
            return partitionSet.ReceivedBeep();
        }

        public override void SendBeep()
        {
            partitionSet.pinConfig.SendBeepOnPin(id);
        }

        public override bool HasReceivedMessage()
        {
            return partitionSet.HasReceivedMessage();
        }

        public override Message GetReceivedMessage()
        {
            return partitionSet.GetReceivedMessage();
        }

        public override void SendMessage(Message msg)
        {
            partitionSet.pinConfig.SendMessageOnPin(id, msg);
        }



        // <<<FOR DEBUGGING>>>
        public string Print()
        {
            return "Pin with ID " + id + ": Direction " + localDir + " (global label: " + globalLabel + "), Offset: " + localEdgeOffset + " (" + globalEdgeOffset + "), On Head: " + head;
        }
    }

} // namespace AS2.Sim
