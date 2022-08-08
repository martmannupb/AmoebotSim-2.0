using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePinGraphicState
{

    // General Data
    public int pinsPerSide;

    // State
    public bool[] hasNeighbor1 = new bool[6]; // like pins1 and pins2 (hasNeighbor2 is only relevant if particle is expanded), return true if there is a particle in that direction
    public bool[] hasNeighbor2 = new bool[6];
    public int neighbor1ToNeighbor2Direction = -1; // global direction from the neighbor1 to the neighbor2 (range 0 to 5), only set if particle is expanded (default -1)
    public List<PSetData> partitionSets;
    public bool isExpanded {
        get {
            return neighbor1ToNeighbor2Direction != -1;
        }
    }




    // Structs ====================

    public class PSetData
    {
        public Color color;
        // Data
        public List<PinDef> pins; // all pins that belong to this partition set
        // Beeping
        bool beepsThisRound;

        private PSetData()
        {
            this.pins = new List<PinDef>(10);
        }

        public void UpdatePSetData(Color color, bool beepsThisRound, params PinDef[] pins)
        {
            this.color = color;
            this.beepsThisRound = beepsThisRound;
            foreach (PinDef pin in pins)
            {
                this.pins.Add(pin);
            }
        }

        public void Clear()
        {
            this.color = new Color();
            this.pins.Clear();
            this.beepsThisRound = false;
        }


        // Pooling ===========

        private static Stack<PSetData> pool = new Stack<PSetData>();

        public static PSetData PoolCreate()
        {
            if (pool.Count > 0) return pool.Pop();
            else
            {
                return new PSetData();
            }
        }

        public static void PoolRelease(PSetData obj)
        {
            obj.Clear();
            pool.Push(obj);
        }

    }

    public struct PinDef
    {
        public int globalDir; // e.g. 0 to 5 for all directions (starting to the right counterclockwise)
        public int dirID; // e.g. 1,2 or 3 for 3 pins per side (or is 0,1,2 better?)
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
        partitionSets = new List<PSetData>(pinsPerSide * 6 * 2);
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
        if(pool.Count > 0)
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


