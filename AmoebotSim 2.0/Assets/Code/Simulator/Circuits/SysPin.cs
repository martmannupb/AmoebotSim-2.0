
namespace AS2.Sim
{

    /// <summary>
    /// System-side implementation of the abstract base class
    /// <see cref="Pin"/>, which declares the API
    /// for the developer.
    /// </summary>
    public class SysPin : Pin
    {
        public SysPartitionSet partitionSet;
        public int id;
        public Direction localDir;
        public int globalLabel;
        public bool head;
        public int localEdgeOffset;
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


        /**
         * Pin: Developer API
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




        // <<<TEMPORARY, FOR DEBUGGING>>>
        public string Print()
        {
            return "Pin with ID " + id + ": Direction " + localDir + " (global label: " + globalLabel + "), Offset: " + localEdgeOffset + " (" + globalEdgeOffset + "), On Head: " + head;
        }
    }

} // namespace AS2.Sim
