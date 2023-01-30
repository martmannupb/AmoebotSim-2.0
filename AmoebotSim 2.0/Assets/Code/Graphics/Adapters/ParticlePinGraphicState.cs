using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// A class that is created by the system to store a simplified version of the current state of the circuits of a single particle.
    /// This serves as a data container which updates the render system in each new configuration of the system.
    /// </summary>
    public class ParticlePinGraphicState
    {

        /// <summary>
        /// None = no pin connection during the round,
        /// Shown = shown pin connection during the round,
        /// ShownFadingIn = pin connection only visible after the round movement
        /// </summary>
        public enum NeighborPinConnection
        {
            None, Shown, ShownFadingIn
        }

        // General Data
        public int pinsPerSide;

        // State
        // INTERFACE ADDITION
        public NeighborPinConnection[] neighborPinConnection1 = new NeighborPinConnection[6]; // should replace/expand hasNeighbor1
        public NeighborPinConnection[] neighborPinConnection2 = new NeighborPinConnection[6]; // should replace/expand hasNeighbor2
        // Neighbors
        public bool[] hasNeighbor1 = new bool[6]; // like pins1 and pins2 (hasNeighbor2 is only relevant if particle is expanded), return true if there is a particle in that direction
        public bool[] hasNeighbor2 = new bool[6];
        public int neighbor1ToNeighbor2Direction = -1; // global direction from the neighbor1 to the neighbor2 (range 0 to 5), only set if particle is expanded (default -1)
        // Sets
        public List<PSetData> partitionSets;
        public List<PSetData> singletonSets;
        public bool isExpanded
        {
            get
            {
                return neighbor1ToNeighbor2Direction != -1;
            }
        }
        // Code Position Override
        public enum CodeOverrideType_Node
        {
            Automatic, AutomaticLine, LineRotated, ManualPlacement
        }
        public CodeOverrideType_Node codeOverrideType1 = CodeOverrideType_Node.Automatic;
        public CodeOverrideType_Node codeOverrideType2 = CodeOverrideType_Node.Automatic;
        public float codeOverrideLineRotationDegrees1 = 0f;
        public float codeOverrideLineRotationDegrees2 = 0f;



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

            // Graphics
            /// <summary>
            /// This struct stores graphical data.
            /// Please do not change the struct directly via the system, use the other methods in this class to assign coordinate values.
            /// </summary>
            public GraphicalData graphicalData = new GraphicalData();

            public class GraphicalData
            {
                // Latest Coordinate
                /// <summary>
                /// The active position of the partition set (at the end of the round).
                /// </summary>
                public Vector2 active_position1;
                /// <summary>
                /// The active position of the partition set of the expanded particle at the grid position 2 of the snap (at the end of the round).
                /// Only relevant for expanded particles.
                /// </summary>
                public Vector2 active_position2;
                /// <summary>
                /// The active connector position of the grid position 1 of the snap (for expanded particles).
                /// </summary>
                public Vector2 active_connector_position1;
                /// <summary>
                /// The active connector position of the grid position 2 of the snap (for expanded particles).
                /// </summary>
                public Vector2 active_connector_position2;

                // Indices (for pins and lines)
                // Idea: We store the positions of the pins and connector pins, so we can update each position if it is grabbed by the partition set move tool and override the matrices
                public RenderBatchIndex index_pSet1;
                public RenderBatchIndex index_pSet1_beep;
                public RenderBatchIndex index_pSet2;
                public RenderBatchIndex index_pSet2_beep;
                public List<RenderBatchIndex> index_lines1 = new List<RenderBatchIndex>(); // first values are pin lines, last value connector pin line (if expanded)
                public List<RenderBatchIndex> index_lines1_beep = new List<RenderBatchIndex>(); // first values are pin lines, last value connector pin line (if expanded)
                public List<RenderBatchIndex> index_lines2 = new List<RenderBatchIndex>(); // first values are pin lines, last value connector pin line (if expanded)
                public List<RenderBatchIndex> index_lines2_beep = new List<RenderBatchIndex>(); // first values are pin lines, last value connector pin line (if expanded)
                public RenderBatchIndex index_pSetConnectorPin1;
                //public RenderBatchIndex index_pSetConnectorPin1_beep;
                public RenderBatchIndex index_pSetConnectorPin2;
                //public RenderBatchIndex index_pSetConnectorPin2_beep;
                public RenderBatchIndex index_lineConnector;
                public RenderBatchIndex index_lineConnector_beep;
                public RendererCircuits_RenderBatch.PropertyBlockData properties_line;
                public RendererCircuits_RenderBatch.PropertyBlockData properties_line_beep;
                public RendererCircuitPins_RenderBatch.PropertyBlockData properties_pin;
                public RendererCircuitPins_RenderBatch.PropertyBlockData properties_pin_beep;
                public RendererCircuitPins_RenderBatch.PropertyBlockData properties_connectorPin;
                public List<PinDef> pSet1_pins = new List<PinDef>(10);
                public List<PinDef> pSet2_pins = new List<PinDef>(10);

                // Code Position Override
                public enum CodeOverrideType_PSet
                {
                    NotSet, Coordinate
                }
                public CodeOverrideType_PSet codeOverrideType1 = CodeOverrideType_PSet.NotSet;
                public CodeOverrideType_PSet codeOverrideType2 = CodeOverrideType_PSet.NotSet;
                /// <summary>
                /// A coordinate used to override the default position of the partition set in the view via code.
                /// For the head or contracted particle.
                /// </summary>
                public Polar2DCoordinate codeOverride_coordinate1;
                /// <summary>
                /// A coordinate used to override the default position of the partition set in the view via code.
                /// For the tail of the expanded particle.
                /// </summary>
                public Polar2DCoordinate codeOverride_coordinate2;

                // Pins
                /// <summary>
                /// If the particle has pins in the head. This value is set by the PrecalculateHasPinsInAndStoreInGD() method of the PSet.
                /// </summary>
                public bool hasPinsInHead = false;
                /// <summary>
                /// If the particle has pins in the tail. This value is set by the PrecalculateHasPinsInAndStoreInGD() method of the PSet.
                /// </summary>
                public bool hasPinsInTail = false;
                /// <summary>
                /// Number of pins in the head.
                /// </summary>
                public int pinsInHead = -1;
                /// <summary>
                /// Number of pins in the tail.
                /// </summary>
                public int pinsInTail = -1;

                public bool HasExpandedActivePosition()
                {
                    return active_position2 != new Vector2(float.MinValue, float.MinValue);
                }

                public void Clear(bool clearIndicesOnly = false)
                {
                    active_position1 = new Vector2(float.MinValue, float.MinValue);
                    active_position2 = new Vector2(float.MinValue, float.MinValue);
                    active_connector_position1 = new Vector2(float.MinValue, float.MinValue);
                    active_connector_position2 = new Vector2(float.MinValue, float.MinValue);
                    // Indices
                    index_pSet1.Discard();
                    index_pSet1_beep.Discard();
                    index_pSet2.Discard();
                    index_pSet2_beep.Discard();
                    index_lines1.Clear();
                    index_lines1_beep.Clear();
                    index_lines2.Clear();
                    index_lines2_beep.Clear();
                    index_pSetConnectorPin1.Discard();
                    //index_pSetConnectorPin1_beep.Discard();
                    index_pSetConnectorPin2.Discard();
                    //index_pSetConnectorPin2_beep.Discard();
                    index_lineConnector.Discard();
                    index_lineConnector_beep.Discard();
                    // Positions
                    pSet1_pins.Clear();
                    pSet2_pins.Clear();
                    
                    // Non indices
                    if(clearIndicesOnly == false)
                    {
                        // Manual Editing
                        codeOverrideType1 = CodeOverrideType_PSet.NotSet;
                        codeOverrideType2 = CodeOverrideType_PSet.NotSet;
                        codeOverride_coordinate1.Discard();
                        codeOverride_coordinate2.Discard();
                        // Pins
                        hasPinsInHead = false;
                        hasPinsInTail = false;
                        pinsInHead = -1;
                        pinsInTail = -1;
                    }
                    
                }

                // Updating ___________________

                public enum ParticleUpdatePinType
                {
                    PSet1,
                    PSet2,
                    PConnector1,
                    PConnector2,
                }

                // Pooling ___________________

                private static Stack<GraphicalData> pool;

                public static GraphicalData PoolCreate()
                {
                    if(pool.Count > 0)
                    {
                        return pool.Pop();
                    }
                    return new GraphicalData();
                }

                public static void PoolRelease(GraphicalData gd)
                {
                    gd.Clear();
                    pool.Push(gd);
                }

            }

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
            /// Returns if the partition set is connected to pins in the given part of the particle ('head' for contracted particles or the head, 'tail' for the tail of expanded particles).
            /// </summary>
            /// <param name="isHead">If a pin in the head should be searched. False if a pin in the tail should be searched.</param>
            /// <returns>True if a pin has been found.</returns>
            public bool HasPinsIn(bool isHead)
            {
                foreach (var pin in pins)
                {
                    if (isHead && pin.isHead) return true;
                    else if (isHead == false && pin.isHead == false) return true;
                }
                return false;
            }

            /// <summary>
            /// Calculates if the partition set has pins + the number of pins in the given part of the particle and stores it in variables in the graphical data for fast and easy access.
            /// </summary>
            public void PrecalculatePinNumbersAndStoreInGD()
            {
                graphicalData.pinsInHead = 0;
                graphicalData.pinsInTail = 0;
                foreach (var pin in pins)
                {
                    if (pin.isHead) graphicalData.pinsInHead++;
                    else graphicalData.pinsInTail++;
                }
                graphicalData.hasPinsInHead = graphicalData.pinsInHead > 0;
                graphicalData.hasPinsInTail = graphicalData.pinsInTail > 0;
            }

            public bool HasPinsInHeadAndTail(bool recalcPinNumbers = true)
            {
                if (recalcPinNumbers) PrecalculatePinNumbersAndStoreInGD();
                return graphicalData.hasPinsInHead && graphicalData.hasPinsInTail;
            }

            // Code Overrides ______________________________

            /// <summary>
            /// Code override of the position of the partition set by a polar coordinate.
            /// Replaces other code override values that have been set at this level of the object (like AutomaticRingPlacement).
            /// Different override methods for head and tail are allowed.
            /// Note that you also need to call the corresponding method in the ParticlePinGraphicState object to enable this mode.
            /// </summary>
            /// <param name="coordinate">The polar coordinate relative from the particle center.</param>
            /// <param name="isHead">True for contracted particles or the head of expanded particles.
            /// Set it to false if you want to position an expanded partition set at the tail.</param>
            public void CodePositionOverride_Coordinate(Polar2DCoordinate coordinate, bool isHead)
            {
                if (isHead)
                {
                    graphicalData.codeOverrideType1 = GraphicalData.CodeOverrideType_PSet.Coordinate;
                    graphicalData.codeOverride_coordinate1 = coordinate;
                }
                else
                {
                    graphicalData.codeOverrideType2 = GraphicalData.CodeOverrideType_PSet.Coordinate;
                    graphicalData.codeOverride_coordinate2 = coordinate;
                }
            }

            // ___________________________________________

            /// <summary>
            /// Clears the system for pooling.
            /// </summary>
            public void Clear()
            {
                this.color = new Color();
                this.pins.Clear();
                this.beepsThisRound = false;
                this.beepOrigin = false;
                this.graphicalData.Clear();
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

        /// <summary>
        /// Initializes the neighbor arrays with default values.
        /// </summary>
        private void InitPins()
        {
            for (int i = 0; i < 6; i++)
            {
                neighborPinConnection1[i] = NeighborPinConnection.None;
                neighborPinConnection2[i] = NeighborPinConnection.None;
                hasNeighbor1[i] = false;
                hasNeighbor2[i] = false;
            }
        }

        /// <summary>
        /// Calculates the amount of partition sets that have pins in both halves of the expanded particle.
        /// Returns 0 if the particle is contracted.
        /// </summary>
        /// <param name="recalcPinNumbers">If the pin numbers should be recalculated. Set this to false if you already did that for all particles to save time.</param>
        /// <returns></returns>
        public int CalculateAmountOfPSetsWithPinsInHeadAndTail(bool recalcPinNumbers = true)
        {
            if (isExpanded == false) return 0;
            else
            {
                int counter = 0;
                foreach (var pSet in partitionSets)
                {
                    if(recalcPinNumbers) pSet.PrecalculatePinNumbersAndStoreInGD();
                    if (pSet.HasPinsInHeadAndTail(recalcPinNumbers)) counter++;
                }
                return counter;
            }
        }

        // Code Overrides ______________________________

        /// <summary>
        /// Code override of the position of the partition set rotation of the standard partition set line.
        /// Expanded particles must set the rotation manually with this mode.
        /// Replaces other code override values that have been set at this level of the object (like AutomaticLine).
        /// </summary>
        /// <param name="rotationDegrees">The rotation degree, counterclockwise. It is recommended to use a value that is dividable by 60.</param>
        /// <param name="isHead">True for contracted particles or the head of expanded particles.</param>
        public void CodePositionOverride_LineRotated(float rotationDegrees, bool isHead)
        {
            if (isHead)
            {
                codeOverrideType1 = CodeOverrideType_Node.LineRotated;
                codeOverrideLineRotationDegrees1 = rotationDegrees;
            }
            else
            {
                codeOverrideType2 = CodeOverrideType_Node.LineRotated;
                codeOverrideLineRotationDegrees2 = rotationDegrees;
            }
        }

        /// <summary>
        /// Code override of the position of the partition sets by the use of the standard partition set line.
        /// Non rotated, except for the expanded particles where the line is oriented towards the other half of the particle.
        /// Replaces other code override values that have been set at this level of the object (like LineRotated).
        /// </summary>
        /// <param name="isHead">True for contracted particles or the head of expanded particles.</param>
        public void CodePositionOverride_AutomaticLine(bool isHead)
        {
            if (isHead)
            {
                codeOverrideType1 = CodeOverrideType_Node.AutomaticLine;
            }
            else
            {
                codeOverrideType2 = CodeOverrideType_Node.AutomaticLine;
            }
        }

        /// <summary>
        /// Code override of the position of the partition sets by the use of the standard partition set line.
        /// Non rotated, except for the expanded particles where the line is oriented towards the other half of the particle.
        /// Replaces other code override values that have been set at this level of the object (like LineRotated).
        /// </summary>
        /// <param name="isHead">True for contracted particles or the head of expanded particles.</param>
        public void CodePositionOverride_Automatic(bool isHead)
        {
            if(isHead) codeOverrideType1 = CodeOverrideType_Node.Automatic;
            else codeOverrideType2 = CodeOverrideType_Node.Automatic;
        }

        /// <summary>
        /// Code override of the position of the partition sets by the use of manually set polar coordinates that need to be defined in the partition sets.
        /// Replaces other code override values that have been set at this level of the object (like LineRotated).
        /// </summary>
        /// <param name="isHead"></param>
        public void CodePositionOverride_PolarCoordinatePlacement(bool isHead)
        {
            if (isHead) codeOverrideType1 = CodeOverrideType_Node.ManualPlacement;
            else codeOverrideType2 = CodeOverrideType_Node.ManualPlacement;
        }

        /// <summary>
        /// Removes any code overrides that have been set on this level of the object. You might not need this.
        /// </summary>
        public void ResetCodePositionOverride()
        {
            codeOverrideType1 = CodeOverrideType_Node.Automatic;
            codeOverrideType2 = CodeOverrideType_Node.Automatic;
            codeOverrideLineRotationDegrees1 = 0f;
            codeOverrideLineRotationDegrees2 = 0f;
        }

        // _____________________________________________

        /// <summary>
        /// Resets the object to the default values.
        /// Does not change number of pins.
        /// </summary>
        public void Reset()
        {
            neighbor1ToNeighbor2Direction = -1;
            for (int i = 0; i < 6; i++)
            {
                neighborPinConnection1[i] = NeighborPinConnection.None;
                neighborPinConnection2[i] = NeighborPinConnection.None;
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
            ResetCodePositionOverride();
        }

        /// <summary>
        /// Resets the object to the default values with a specific number of pins.
        /// </summary>
        /// <param name="pinsPerSide"></param>
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

}