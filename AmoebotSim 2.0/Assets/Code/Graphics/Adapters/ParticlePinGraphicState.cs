using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePinGraphicState
{
    /**
     * Also: Wir haben Pins und Circuits. Die Pins anzuzeigen ist kein Problem, die erschließen sich mit der Menge an Pins pro Seite.
     * Das Problem sind die Circuits: Wir haben zwei verschachtelte Arrays für die Pins, pins2 ist nur relevant, wenn das Partikel expandiert ist.
     * Die Arrays enthalten die Information, ob der jeweilige Pin zu einem Partition Set gehört (der index des Partition Sets wird gespeichert, also partitionSets[index]).
     * Wenn der Pin nicht existiert, ist der Index -1 und wird nicht zugeordnet, also z.B. die mittleren Pins bei einem expandierten Partikel. Diese Pins also einfach ignorieren.
     * 
     * Es sollte alles in globalen Richtungen gespeichert sein, also pins[0] ist rechts, pins[1] oben rechts, pins[2] oben links und so weiter... Das ist ok oder?
     * 
     * Also am besten sollten alle Pins ein Partition Set haben, da jeder Pin beepen kann. Dies ist auch nötig, um die richtige Farbe anzeigen zu können.
     * Einzig Singletons, die nicht mit einem Pin von einem Nachbarpartikel verbunden sind, bräuchten das nicht.
     * (Denn ich würde die Infos aus dieser Klasse auch nutzen, um die Linien zwischen den Partikeln anzuzeigen)
     * 
     * Wir nutzen Pooling um diese Klasse effizient wiederzuverwerten. Einfach ParticlePinGraphicState.PoolCreate(...) aufrufen, PoolRelease(...) wird dann von der Renderlogik gecallt, wenn das
     * Objekt nicht mehr benötigt wird. Ich dachte mir, dass wir das Objekt einmal nutzen, um die Circuits anzuzeigen, und dass du es danach modifizieren kannst, um mögliche Beeps hinzuzufügen. Dann
     * wird einmal global im Renderer aufgerufen, dass alle Beeps berechnet wurde, und ich würde diese Klasse nochmal auf Beeps überprüfen. Danach wird die Klasse irgendwann von mir wieder zum Pool
     * hinzugefügt.
     * 
     * Klingt nach einem Plan, oder? :p
     * Das ist wirklich ein ziemlicher Aufwand, das alle auszuarbeiten.
     */

    // General Data
    public int pinsPerSide;

    // State
    public bool isExpanded;
    public int[][] pins1 = new int[6][];
    public int[][] pins2 = new int[6][];
    public List<PSetData> partitionSets = new List<PSetData>();
    // Beeps
    public List<PSetBeep> beeps_pSets = new List<PSetBeep>();





    // Structs ====================

    public struct PSetData
    {
        public Color color;
        int amountOfConnectedPins;

        public PSetData(Color color, int amountOfConnectedPins)
        {
            this.color = color;
            this.amountOfConnectedPins = amountOfConnectedPins;
        }
    }

    public struct PSetBeep
    {
        public int index;

        public PSetBeep(int index)
        {
            this.index = index;
        }
    }

    //public struct SingletonBeep
    //{
    //    public bool inPins1; // true if pin is in pins1, false for pins2
    //    public Vector2Int pinIndex; // e.g. 2,3 for pins1[2][3], the third pin in the direction 2

    //    public SingletonBeep(bool inPins1, Vector2Int pinIndex)
    //    {
    //        this.inPins1 = inPins1;
    //        this.pinIndex = pinIndex;
    //    }
    //}






    // Logic ====================

    private ParticlePinGraphicState(int pinsPerSide)
    {
        this.pinsPerSide = pinsPerSide;
        // Init Pins
        InitPins();
    }

    private void InitPins()
    {
        pins1 = new int[6][];
        pins2 = new int[6][];
        for (int i = 0; i < 6; i++)
        {
            pins1[i] = new int[pinsPerSide];
            pins2[i] = new int[pinsPerSide];
            for (int j = 0; j < pinsPerSide; j++)
            {
                pins1[i][j] = -1;
                pins2[i][j] = -1;
            }
        }
    }

    public void Reset()
    {
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < pinsPerSide; j++)
            {
                pins1[i][j] = -1;
                pins2[i][j] = -1;
            }
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


