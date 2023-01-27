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
    /// <para>
    /// A tree structure is used for merging circuits. When one
    /// circuit is merged into another, all of its children and
    /// the circuit itself are added as children to the other
    /// circuit. All children of a root circuit dispatch operations
    /// to their root.
    /// </para>
    /// </summary>
    public class Circuit
    {
        /// <summary>
        /// The ID given to this circuit while computing circuits in
        /// a particle system. Used to easily identify and refer to
        /// circuits while merging.
        /// </summary>
        public int id;
        /// <summary>
        /// Indicates whether a beep is sent on this circuit in this round.
        /// </summary>
        private bool hasBeep;
        /// <summary>
        /// The message that is sent via this circuit in this round.
        /// <c>null</c> if no message is sent.
        /// </summary>
        private Message message;
        /// <summary>
        /// The color in which this circuit should be displayed.
        /// </summary>
        private Color color;
        /// <summary>
        /// Indicates whether the circuit color has been set by a
        /// partition set.
        /// </summary>
        private bool colorOverride;
        /// <summary>
        /// The number of partition sets added directly to this circuit.
        /// Since this circuit might be merged into a larger one, this
        /// number is not always up to date.
        /// </summary>
        private int numPartitionSets = 0;
        /// <summary>
        /// The number of partition sets belonging to this circuit.
        /// </summary>
        public int NumPartitionSets
        {
            get { return GetRoot().numPartitionSets; }
        }

        /// <summary>
        /// The circuits merged into this one.
        /// </summary>
        private List<Circuit> children = new List<Circuit>();
        /// <summary>
        /// Reference to the root circuit that contains this one.
        /// <c>null</c> if this circuit is the root.
        /// </summary>
        private Circuit rootParent = null;

        private Circuit(int id)
        {
            Reset(id);
        }

        /// <summary>
        /// Completely reinitializes the circuit with a new ID.
        /// </summary>
        /// <param name="id">The new ID.</param>
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

        /// <summary>
        /// Checks whether this circuit is currently a root.
        /// </summary>
        /// <returns><c>true</c> if and only if this circuit does
        /// not have a root parent.</returns>
        public bool IsRoot()
        {
            return rootParent == null;
        }

        /// <summary>
        /// Checks whether a beep has been sent on this circuit.
        /// </summary>
        /// <returns><c>true</c> if and only if any partition set
        /// contained in this circuit sends a beep in this round.</returns>
        public bool HasBeep()
        {
            return GetRoot().hasBeep;
        }

        /// <summary>
        /// Gets the message sent on this circuit in this round.
        /// </summary>
        /// <returns>The message sent on this circuit in this round,
        /// or <c>null</c> if no message was sent.</returns>
        public Message GetMessage()
        {
            return GetRoot().message;
        }

        /// <summary>
        /// Checks whether the color of this circuit was changed
        /// by a partition set.
        /// </summary>
        /// <returns><c>true</c> if and only if a partition set
        /// in this circuit has defined a display color.</returns>
        public bool HasColorOverride()
        {
            return GetRoot().colorOverride;
        }

        /// <summary>
        /// Returns the color in which the circuit should be
        /// displayed. This may be one of the default colors or
        /// a color defined by a partition set.
        /// </summary>
        /// <returns>The desired display color of the circuit.</returns>
        public Color GetColor()
        {
            return GetRoot().color;
        }

        /// <summary>
        /// Sets the display color of this circuit.
        /// </summary>
        /// <param name="c">The new color in which the circuit
        /// should be displayed.</param>
        public void SetColor(Color c)
        {
            GetRoot().color = c;
        }

        /// <summary>
        /// Returns the root circuit in this circuit's tree.
        /// </summary>
        /// <returns>The root of the circuit tree to which this
        /// circuit belongs.</returns>
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
        /// If the partition set has a planned beep or message
        /// or has defined a display color, this information
        /// will be stored in the circuit.
        /// </para>
        /// </summary>
        /// <param name="ps">The partition set to be added.</param>
        public void AddPartitionSet(SysPartitionSet ps)
        {
            GetRoot().AddPartitionSetRoot(ps);
        }

        /// <summary>
        /// Counterpart to <see cref="AddPartitionSet(SysPartitionSet)"/>
        /// to be called on the root only.
        /// </summary>
        /// <param name="ps">The partition set to be added.</param>
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

            if (!colorOverride && ps.colorOverride)
            {
                colorOverride = true;
                color = ps.color;
            }
        }

        /// <summary>
        /// Checks whether this circuit is part of the same tree
        /// as the given circuit.
        /// </summary>
        /// <param name="other">The other circuit to be checked.</param>
        /// <returns><c>true</c> if and only if the trees of this
        /// circuit and the <paramref name="other"/> have the same root.</returns>
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
        /// have a planned beep. If any of the two circuits
        /// has a message, the message with the higher
        /// priority is sent on the resulting circuit. The
        /// color is determined by override if only one has
        /// a color set or by the larger one if both have a
        /// color override.
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

        /// <summary>
        /// Counterpart to <see cref="MergeWith(Circuit)"/> to be
        /// called on the root only.
        /// </summary>
        /// <param name="other">The other circuit to merge with
        /// this root.</param>
        private void MergeWithRoot(Circuit other)
        {
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

        /// <summary>
        /// A stack that contains the currently unused circuits.
        /// </summary>
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
