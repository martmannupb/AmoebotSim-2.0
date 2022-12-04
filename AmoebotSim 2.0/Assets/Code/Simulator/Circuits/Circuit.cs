using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Represents a collection of partition sets that are connected
    /// into a single circuit.
    /// <para>
    /// Use the pooling mechanism (<see cref="Get(int)"/> and
    /// <see cref="Free(Circuit)"/>) to obtain and free instances.
    /// </para>
    /// </summary>
    public class Circuit
    {
        public int id;
        private bool hasBeep;
        private Message message;
        private Color color;
        private bool colorOverride;
        private int numPartitionSets = 0;
        /// <summary>
        /// The number of partition sets belonging to this circuit.
        /// </summary>
        public int NumPartitionSets
        {
            get { return GetRoot().numPartitionSets; }
        }

        private List<Circuit> children = new List<Circuit>();
        private Circuit rootParent = null;

        private Circuit(int id)
        {
            Reset(id);
        }

        private void Reset(int id)
        {
            this.id = id;
            hasBeep = false;
            message = null;
            color = Color.black;
            colorOverride = false;
            numPartitionSets = 0;
            children.Clear();
            rootParent = null;
        }

        public bool IsRoot()
        {
            return rootParent == null;
        }

        public bool HasBeep()
        {
            return GetRoot().hasBeep;
        }

        public Message GetMessage()
        {
            return GetRoot().message;
        }

        public bool HasColorOverride()
        {
            return GetRoot().colorOverride;
        }

        public Color GetColor()
        {
            return GetRoot().color;
        }

        public void SetColor(Color c)
        {
            GetRoot().color = c;
        }

        private Circuit GetRoot()
        {
            if (rootParent != null)
                return rootParent;
            else
                return this;
        }

        /// <summary>
        /// Adds the given partition set to this circuit.
        /// <para>
        /// If the partition set has a planned beep, this information
        /// is stored in the circuit.
        /// </para>
        /// </summary>
        /// <param name="ps">The partition set to be added.</param>
        public void AddPartitionSet(SysPartitionSet ps)
        {
            GetRoot().AddPartitionSetRoot(ps);
        }

        private void AddPartitionSetRoot(SysPartitionSet ps)
        {
            ps.circuit = id;
            numPartitionSets++;
            if (!hasBeep && ps.pinConfig.particle.HasPlannedBeep(ps.Id))
            {
                hasBeep = true;
            }

            if (message == null)
            {
                if (ps.pinConfig.particle.HasPlannedMessage(ps.Id))
                {
                    message = ps.pinConfig.particle.GetPlannedMessage(ps.Id);
                }
            }
            else if (ps.pinConfig.particle.HasPlannedMessage(ps.Id))
            {
                Message msg = ps.pinConfig.particle.GetPlannedMessage(ps.Id);
                if (msg.GreaterThan(message))
                {
                    message = msg;
                }
            }

            if (!colorOverride && ps.pinConfig.particle.PartitionSetColorsOverride[ps.Id])
            {
                colorOverride = true;
                color = ps.pinConfig.particle.PartitionSetColors[ps.Id];
            }
        }

        public bool SameCircuitAs(Circuit other)
        {
            return GetRoot() == other.GetRoot();
        }

        /// <summary>
        /// Merges this circuit with the given other circuit.
        /// <para>
        /// Nothing happens if the other circuit is the same
        /// as this one. Otherwise, the smaller circuit is
        /// merged into the bigger one and the smaller one is
        /// set to be inactive. If one of the circuits has a
        /// planned beep, the resulting circuit will also
        /// have a planned beep.
        /// </para>
        /// </summary>
        /// <param name="other">The circuit to merge this one
        /// with.</param>
        public void MergeWith(Circuit other)
        {
            if (SameCircuitAs(other))
                return;

            GetRoot().MergeWithRoot(other.GetRoot());
        }

        private void MergeWithRoot(Circuit other)
        {
            //if (children.Count >= other.children.Count)
            if (numPartitionSets >= other.numPartitionSets)
            {
                MergeOther(other);
            }
            else
            {
                other.MergeOther(this);
            }
        }

        /// <summary>
        /// Merges the given other circuit into this one. If one of the circuits
        /// has a color override, that color is used. If both circuits have a
        /// color override, the color of this circuit is used.
        /// </summary>
        /// <param name="other">The circuit to be merged into
        /// this one. It will be set to inactive after the merge.</param>
        private void MergeOther(Circuit other)
        {
            foreach (Circuit c in other.children)
            {
                c.rootParent = this;
                children.Add(c);
            }
            other.children.Clear();
            other.rootParent = this;
            children.Add(other);
            numPartitionSets += other.numPartitionSets;

            hasBeep = hasBeep || other.hasBeep;

            if (other.message != null)
            {
                if (message == null)
                    message = other.message;
                else
                    message = other.message.GreaterThan(message) ? other.message : message;
            }

            if (!colorOverride && other.colorOverride)
            {
                color = other.color;
                colorOverride = true;
            }
        }

        // Pooling

        private static Stack<Circuit> pool = new Stack<Circuit>();

        /// <summary>
        /// Returns a new circuit instance with the given ID.
        /// <para>
        /// Use <see cref="Free(Circuit)"/> on the instance
        /// when it is not needed anymore so that it can be
        /// reused later.
        /// </para>
        /// </summary>
        /// <param name="id">The ID for the new circuit.</param>
        /// <returns>A new circuit instance obtained from the
        /// pool or newly created.</returns>
        public static Circuit Get(int id)
        {
            if (pool.Count > 0)
            {
                Circuit c = pool.Pop();
                c.Reset(id);
                return c;
            }
            else
            {
                return new Circuit(id);
            }
        }

        /// <summary>
        /// Returns the given circuit instance to the pool.
        /// </summary>
        /// <param name="c">The circuit to be returned to the pool.</param>
        public static void Free(Circuit c)
        {
            if (c != null)
                pool.Push(c);
        }
    }

} // namespace AS2.Sim
