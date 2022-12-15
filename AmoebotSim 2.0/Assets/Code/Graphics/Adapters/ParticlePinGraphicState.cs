using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Graphics
{

    /// <summary>
    /// A class that is created by the system to store a simplified version of the current state of the circuits of a single particle.
    /// This serves as a data container which updates the render system in each new configuration of the system.
    /// </summary>
    public class ParticlePinGraphicState
    {

        // General Data
        public int pinsPerSide;

        // State
        public bool[] hasNeighbor1 = new bool[6]; // like pins1 and pins2 (hasNeighbor2 is only relevant if particle is expanded), return true if there is a particle in that direction
        public bool[] hasNeighbor2 = new bool[6];
        public int neighbor1ToNeighbor2Direction = -1; // global direction from the neighbor1 to the neighbor2 (range 0 to 5), only set if particle is expanded (default -1)
        public List<PSetData> partitionSets;
        public List<PSetData> singletonSets;
        public bool isExpanded
        {
            get
            {
                return neighbor1ToNeighbor2Direction != -1;
            }
        }




        // Structs ====================

        /// <summary>
        /// Stores the references to pins in a single partition set.
        /// </summary>
        public class PSetData
        {
            public Color color;
            // Data
            public List<PinDef> pins; // all pins that belong to this partition set
                                      // Beeping
            public bool beepsThisRound;
            public bool beepOrigin;

            private PSetData()
            {
                this.pins = new List<PinDef>(10);
            }

            /// <summary>
            /// Updates the state of this container.
            /// </summary>
            /// <param name="color">The color the partition set lines should have.</param>
            /// <param name="beepsThisRound">True if there is a beep in this round.</param>
            /// <param name="pins">An array of pin references that show the pins contained in this system.</param>
            public void UpdatePSetData(Color color, bool beepsThisRound, params PinDef[] pins)
            {
                UpdatePSetData(color, beepsThisRound, false, pins);
            }

            /// <summary>
            /// Updates the state of this container.
            /// </summary>
            /// <param name="color">The color the partition set lines should have.</param>
            /// <param name="beepsThisRound">True if there is a beep in this round.</param>
            /// <param name="beepOrigin">True if the origin of the beep came from this particle.</param>
            /// <param name="pins">An array of pin references that show the pins contained in this system.</param>
            public void UpdatePSetData(Color color, bool beepsThisRound, bool beepOrigin, params PinDef[] pins)
            {
                this.color = color;
                this.beepsThisRound = beepsThisRound;
                this.beepOrigin = beepOrigin;
                foreach (PinDef pin in pins)
                {
                    this.pins.Add(pin);
                }
            }

            /// <summary>
            /// Clears the system for pooling.
            /// </summary>
            public void Clear()
            {
                this.color = new Color();
                this.pins.Clear();
                this.beepsThisRound = false;
                this.beepOrigin = false;
            }


            // Pooling ===========

            private static Stack<PSetData> pool = new Stack<PSetData>();

            /// <summary>
            /// Instantiates an object or uses pooling to recycle an old object.
            /// </summary>
            /// <returns></returns>
            public static PSetData PoolCreate()
            {
                if (pool.Count > 0) return pool.Pop();
                else
                {
                    return new PSetData();
                }
            }

            /// <summary>
            /// Clears and adds the object instance to the pool.
            /// </summary>
            /// <param name="obj"></param>
            public static void PoolRelease(PSetData obj)
            {
                obj.Clear();
                pool.Push(obj);
            }

        }

        /// <summary>
        /// The definition of a single pin. Contains directions, id and if this pin is in the head of the particle.
        /// </summary>
        public struct PinDef
        {
            public int globalDir; // e.g. 0 to 5 for all directions (starting to the right counterclockwise)
            public int dirID; // e.g. 0,1 or 2 for 3 pins per side
            public bool isHead; // true for contracted particle or head on expanded particle, false if is tail on expanded particle

            public PinDef(int globalDir, int dirID, bool isHead)
            {
                this.globalDir = globalDir;
                this.dirID = dirID;
                this.isHead = isHead;
            }
        }





        // Logic ====================

        private ParticlePinGraphicState(int pinsPerSide)
        {
            this.pinsPerSide = pinsPerSide;
            partitionSets = new List<PSetData>(pinsPerSide * 10);
            singletonSets = new List<PSetData>(pinsPerSide * 10);
            // Init Pins
            InitPins();
        }

        private void InitPins()
        {
            for (int i = 0; i < 6; i++)
            {
                hasNeighbor1[i] = false;
                hasNeighbor2[i] = false;
            }
        }

        public void Reset()
        {
            neighbor1ToNeighbor2Direction = -1;
            for (int i = 0; i < 6; i++)
            {
                hasNeighbor1[i] = false;
                hasNeighbor2[i] = false;
            }
            foreach (PSetData pSet in partitionSets)
            {
                PSetData.PoolRelease(pSet);
            }
            partitionSets.Clear();
            foreach (PSetData pSet in singletonSets)
            {
                PSetData.PoolRelease(pSet);
            }
            singletonSets.Clear();
        }

        public void Reset(int pinsPerSide)
        {
            if (this.pinsPerSide == pinsPerSide) Reset();
            else
            {
                this.pinsPerSide = pinsPerSide;
                InitPins();
                Reset();
            }
        }








        // Pooling ====================

        private static Stack<ParticlePinGraphicState> pool = new Stack<ParticlePinGraphicState>();

        public static ParticlePinGraphicState PoolCreate(int pinsPerSide)
        {
            if (pool.Count > 0)
            {
                // Take from Pool
                ParticlePinGraphicState state = pool.Pop();
                state.Reset(pinsPerSide);
                return state;
            }
            else
            {
                // Instantiate
                return new ParticlePinGraphicState(pinsPerSide);
            }
        }

        public static void PoolRelease(ParticlePinGraphicState state)
        {
            pool.Push(state);
        }

    }

} // namespace AS2.Graphics
