using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// A class that is created by the system to store a simplified
    /// version of the current state of the circuits of a single particle.
    /// This serves as a data container which updates the render system in
    /// each new configuration of the system.
    /// </summary>
    public class ParticlePinGraphicState
    {

        /// <summary>
        /// Visibility types of connections to neighboring particles.
        /// </summary>
        public enum NeighborPinConnection
        {
            /// <summary>
            /// No pin connection during the round.
            /// </summary>
            None,
            /// <summary>
            /// Shown pin connection during the round.
            /// </summary>
            Shown,
            /// <summary>
            /// Pin connection only visible after the movement phase.
            /// </summary>
            ShownFadingIn
        }

        // General Data
        /// <summary>
        /// The number of pins on each edge of a particle.
        /// </summary>
        public int pinsPerSide;

        // State
        // INTERFACE ADDITION
        /// <summary>
        /// Types of pins connected to the particle's head.
        /// Indices are global directions.
        /// </summary>
        public NeighborPinConnection[] neighborPinConnection1 = new NeighborPinConnection[6];
        /// <summary>
        /// Types of pins connected to the particle's tail.
        /// Only relevant if the particle is expanded.
        /// Indices are global directions.
        /// </summary>
        public NeighborPinConnection[] neighborPinConnection2 = new NeighborPinConnection[6];

        // Neighbors
        /// <summary>
        /// Neighbor flags for the particle's head. Indices are
        /// global directions.
        /// </summary>
        public bool[] hasNeighbor1 = new bool[6];
        /// <summary>
        /// Neighbor flags for the particle's tail.
        /// Only relevant if the particle is expanded.
        /// Indices are global directions.
        /// </summary>
        public bool[] hasNeighbor2 = new bool[6];
        /// <summary>
        /// Global direction from the particle's head to its tail. Is
        /// <c>-1</c> if the particle is contracted.
        /// </summary>
        public int neighbor1ToNeighbor2Direction = -1;

        // Sets
        /// <summary>
        /// Non-singleton and non-empty partition sets in the pin configuration.
        /// </summary>
        public List<PSetData> partitionSets;
        /// <summary>
        /// Singleton partition sets in the pin configuration.
        /// </summary>
        public List<PSetData> singletonSets;

        /// <summary>
        /// Checks whether the pin configuration belongs to an expanded particle.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return neighbor1ToNeighbor2Direction != -1;
            }
        }

        /// <summary>
        /// Types of partition set placement modes set by
        /// code override.
        /// </summary>
        public enum CodeOverrideType_Node
        {
            /// <summary>
            /// Use the default automatic partition set positioning.
            /// </summary>
            Automatic,
            /// <summary>
            /// Place partition sets on a vertical line.
            /// </summary>
            AutomaticLine,
            /// <summary>
            /// Place partition sets on a rotated line.
            /// </summary>
            LineRotated,
            /// <summary>
            /// Use custom polar coordinates for all partition sets.
            /// </summary>
            ManualPlacement
        }
        /// <summary>
        /// Partition set placement type in the head.
        /// </summary>
        public CodeOverrideType_Node codeOverrideType1 = CodeOverrideType_Node.Automatic;
        /// <summary>
        /// Partition set placement type in the tail.
        /// </summary>
        public CodeOverrideType_Node codeOverrideType2 = CodeOverrideType_Node.Automatic;
        /// <summary>
        /// Rotation of partition set placement line in the head.
        /// </summary>
        public float codeOverrideLineRotationDegrees1 = 0f;
        /// <summary>
        /// Rotation of partition set placement line in the head.
        /// </summary>
        public float codeOverrideLineRotationDegrees2 = 0f;



        // Structs ====================

        /// <summary>
        /// Stores information belonging to a single partition set.
        /// </summary>
        public class PSetData
        {
            /// <summary>
            /// The partition set's color.
            /// </summary>
            public Color color;
            /// <summary>
            /// The pins belonging to the partition set.
            /// </summary>
            public List<PinDef> pins;
            /// <summary>
            /// Whether the partition set receives a beep in the current round.
            /// </summary>
            public bool beepsThisRound;
            /// <summary>
            /// Whether the partition set sends a beep in the current round.
            /// </summary>
            public bool beepOrigin;
            /// <summary>
            /// Whether the partition set has suffered a beep reception
            /// failure in the current round.
            /// </summary>
            public bool isFaulty;

            // Graphics
            /// <summary>
            /// This class stores graphical data.
            /// Please do not change the instance directly via the system,
            /// use the other methods in this class to assign coordinate values.
            /// </summary>
            public GraphicalData graphicalData = new GraphicalData();

            /// <summary>
            /// Stores internal graphical data belonging to a
            /// <see cref="PSetData"/> instance.
            /// </summary>
            public class GraphicalData
            {
                // Latest Coordinates
                /// <summary>
                /// The active position of the partition set (at the end of the round).
                /// </summary>
                public Vector2 active_position1;
                /// <summary>
                /// The active position of the partition set of the expanded particle at
                /// the grid position 2 of the snap (at the end of the round).
                /// Only relevant for expanded particles.
                /// </summary>
                public Vector2 active_position2;
                /// <summary>
                /// The active connector position of the grid position 1 of the snap
                /// (for expanded particles).
                /// </summary>
                public Vector2 active_connector_position1;
                /// <summary>
                /// The active connector position of the grid position 2 of the snap
                /// (for expanded particles).
                /// </summary>
                public Vector2 active_connector_position2;

                // Indices (for pins and lines)
                // Idea: We store the positions of the pins and connector pins, so we can
                // update each position if it is grabbed by the partition set move tool
                // and override the matrices
                // We need to store beeping lines separately because they are rendered
                // by different batches
                /// <summary>
                /// Batch index of the head partition set handle.
                /// </summary>
                public RenderBatchIndex index_pSet1;
                /// <summary>
                /// Batch index of the head partition set handle's beep origin highlight.
                /// </summary>
                public RenderBatchIndex index_pSet1_beep_origin;
                /// <summary>
                /// Batch index of the head partition set handle's beep or
                /// fault highlight.
                /// </summary>
                public RenderBatchIndex index_pSet1_beep_or_fault;
                /// <summary>
                /// Batch index of the tail partition set handle.
                /// Only used for expanded particles.
                /// </summary>
                public RenderBatchIndex index_pSet2;
                /// <summary>
                /// Batch index of the tail partition set handle's beep origin highlight.
                /// Only used for expanded particles.
                /// </summary>
                public RenderBatchIndex index_pSet2_beep_origin;
                /// <summary>
                /// Batch index of the tail partition set handle's beep or
                /// fault highlight.
                /// Only used for expanded particles.
                /// </summary>
                public RenderBatchIndex index_pSet2_beep_or_fault;
                /// <summary>
                /// Batch indices for circuit lines belonging to the head partition
                /// set handle. The last entry belongs to the line between the handle
                /// and the connector if the particle is expanded.
                /// </summary>
                public List<RenderBatchIndex> index_lines1 = new List<RenderBatchIndex>();
                /// <summary>
                /// Batch indices for beeping circuit lines belonging to the head
                /// partition set handle. The last entry belongs to the line between
                /// the handle and the connector if the particle is expanded.
                /// </summary>
                public List<RenderBatchIndex> index_lines1_beep = new List<RenderBatchIndex>();
                /// <summary>
                /// Batch indices for circuit lines belonging to the tail partition
                /// set handle, if the particle is expanded. The last entry belongs to
                /// the line between the handle and the connector.
                /// </summary>
                public List<RenderBatchIndex> index_lines2 = new List<RenderBatchIndex>();
                /// <summary>
                /// Batch indices for beeping circuit lines belonging to the tail partition
                /// set handle, if the particle is expanded. The last entry belongs to
                /// the line between the handle and the connector.
                /// </summary>
                public List<RenderBatchIndex> index_lines2_beep = new List<RenderBatchIndex>();
                /// <summary>
                /// Batch index of the head connector position if the particle
                /// is expanded.
                /// </summary>
                public RenderBatchIndex index_pSetConnectorPin1;
                /// <summary>
                /// Batch index of the tail connector position if the particle
                /// is expanded.
                /// </summary>
                public RenderBatchIndex index_pSetConnectorPin2;
                /// <summary>
                /// Batch index of the connector line if the particle is expanded.
                /// </summary>
                public RenderBatchIndex index_lineConnector;
                /// <summary>
                /// Batch index of the beeping connector line if the particle
                /// is expanded.
                /// </summary>
                public RenderBatchIndex index_lineConnector_beep;

                // Material properties
                /// <summary>
                /// Material properties for circuit lines.
                /// </summary>
                public RendererCircuits_RenderBatch.PropertyBlockData properties_line;
                /// <summary>
                /// Material properties for beeping circuit lines.
                /// </summary>
                public RendererCircuits_RenderBatch.PropertyBlockData properties_line_beep;
                /// <summary>
                /// Material properties for partition set handles.
                /// </summary>
                public RendererCircuitPins_RenderBatch.PropertyBlockData properties_pin;
                /// <summary>
                /// Material properties for beeping partition set handles.
                /// </summary>
                public RendererCircuitPins_RenderBatch.PropertyBlockData properties_pin_beep;
                /// <summary>
                /// Material properties for faulty partition set handles.
                /// </summary>
                public RendererCircuitPins_RenderBatch.PropertyBlockData properties_pin_faulty;
                /// <summary>
                /// Material properties for circuit line connectors.
                /// </summary>
                public RendererCircuitPins_RenderBatch.PropertyBlockData properties_connectorPin;

                // Pins
                /// <summary>
                /// Pins belonging to the partition set that are at
                /// the particle's head.
                /// </summary>
                public List<PinDef> pSet1_pins = new List<PinDef>(10);
                /// <summary>
                /// Pins belonging to the partition set that are at
                /// the particle's tail if it is expanded.
                /// </summary>
                public List<PinDef> pSet2_pins = new List<PinDef>(10);

                /// <summary>
                /// Types of partition set placement overrides.
                /// </summary>
                public enum CodeOverrideType_PSet
                {
                    /// <summary>
                    /// No override set.
                    /// </summary>
                    NotSet,
                    /// <summary>
                    /// Polar coordinates set.
                    /// </summary>
                    Coordinate
                }
                /// <summary>
                /// Type of partition set placement override in the particle's head.
                /// </summary>
                public CodeOverrideType_PSet codeOverrideType1 = CodeOverrideType_PSet.NotSet;
                /// <summary>
                /// Type of partition set placement override in the particle's tail
                /// if the particle is expanded.
                /// </summary>
                public CodeOverrideType_PSet codeOverrideType2 = CodeOverrideType_PSet.NotSet;
                /// <summary>
                /// A coordinate used to override the default position of
                /// the partition set in the view via code.
                /// For the head or contracted particle.
                /// </summary>
                public Polar2DCoordinate codeOverride_coordinate1;
                /// <summary>
                /// A coordinate used to override the default position of
                /// the partition set in the view via code.
                /// For the tail of the expanded particle.
                /// </summary>
                public Polar2DCoordinate codeOverride_coordinate2;

                // Pins
                /// <summary>
                /// Whether the particle has pins in the head. This value
                /// is set by the <see cref="PrecalculatePinNumbersAndStoreInGD"/>
                /// method of the PSet.
                /// </summary>
                public bool hasPinsInHead = false;
                /// <summary>
                /// Whether the particle has pins in the tail. This value
                /// is set by the <see cref="PrecalculatePinNumbersAndStoreInGD"/>
                /// method of the PSet.
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

                /// <summary>
                /// Clears the graphical data by resetting all batch indices
                /// and coordinates.
                /// </summary>
                /// <param name="clearIndicesOnly">If <c>false</c>,
                /// code override and pin info is also reset.</param>
                public void Clear(bool clearIndicesOnly = false)
                {
                    active_position1 = new Vector2(float.MinValue, float.MinValue);
                    active_position2 = new Vector2(float.MinValue, float.MinValue);
                    active_connector_position1 = new Vector2(float.MinValue, float.MinValue);
                    active_connector_position2 = new Vector2(float.MinValue, float.MinValue);
                    // Indices
                    index_pSet1.Discard();
                    index_pSet1_beep_origin.Discard();
                    index_pSet1_beep_or_fault.Discard();
                    index_pSet2.Discard();
                    index_pSet2_beep_origin.Discard();
                    index_pSet2_beep_or_fault.Discard();
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
            }

            private PSetData()
            {
                this.pins = new List<PinDef>(10);
            }

            /// <summary>
            /// Updates the state of this container.
            /// </summary>
            /// <param name="color">The color the partition set lines should have.</param>
            /// <param name="beepsThisRound"><c>true</c> if there is a beep in this round.</param>
            /// <param name="pins">An array of pin references that show the pins contained
            /// in this system.</param>
            public void UpdatePSetData(Color color, bool beepsThisRound, params PinDef[] pins)
            {
                UpdatePSetData(color, beepsThisRound, false, false, pins);
            }

            /// <summary>
            /// Updates the state of this container.
            /// </summary>
            /// <param name="color">The color the partition set lines should have.</param>
            /// <param name="beepsThisRound"><c>true</c> if there is a beep in this round.</param>
            /// <param name="beepOrigin"><c>true</c> if the origin of the beep came from
            /// this particle.</param>
            /// <param name="faulty"><c>true</c> if this particle has not received a beep
            /// due to a failure.</param>
            /// <param name="pins">An array of pin references that show the pins contained
            /// in this system.</param>
            public void UpdatePSetData(Color color, bool beepsThisRound, bool beepOrigin, bool faulty, params PinDef[] pins)
            {
                this.color = color;
                this.beepsThisRound = beepsThisRound;
                this.beepOrigin = beepOrigin;
                this.isFaulty = faulty;
                foreach (PinDef pin in pins)
                {
                    this.pins.Add(pin);
                }
            }

            /// <summary>
            /// Returns whether the partition set is connected to pins in the given part of
            /// the particle ('head' for contracted particles or the head, 'tail' for the
            /// tail of expanded particles).
            /// </summary>
            /// <param name="isHead">Whether a pin in the head should be searched.
            /// <c>false</c> if a pin in the tail should be searched.</param>
            /// <returns><c>true</c> if a pin has been found.</returns>
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
            /// Calculates if the partition set has pins + the number of pins
            /// in the given part of the particle and stores it in variables in
            /// the graphical data for fast and easy access.
            /// </summary>
            public void PrecalculatePinNumbersAndStoreInGD()
            {
                graphicalData.pinsInHead = 0;
                graphicalData.pinsInTail = 0;
                foreach (var pin in pins)
                {
                    if (pin.isHead)
                        graphicalData.pinsInHead++;
                    else
                        graphicalData.pinsInTail++;
                }
                graphicalData.hasPinsInHead = graphicalData.pinsInHead > 0;
                graphicalData.hasPinsInTail = graphicalData.pinsInTail > 0;
            }

            /// <summary>
            /// Checks whether the partition set contains pins in the particle's
            /// head and tail (if the particle is expanded).
            /// </summary>
            /// <param name="recalcPinNumbers">If <c>true</c>, recalculate
            /// the pin counts before checking (necessary if the partition set
            /// data has recently changed.</param>
            /// <returns><c>true</c> if and only if the partition set contains
            /// pins both in the particle's head and in its tail.</returns>
            public bool HasPinsInHeadAndTail(bool recalcPinNumbers = true)
            {
                if (recalcPinNumbers)
                    PrecalculatePinNumbersAndStoreInGD();
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
                this.isFaulty = false;
                this.graphicalData.Clear();
            }


            // Pooling ===========

            private static Stack<PSetData> pool = new Stack<PSetData>();

            /// <summary>
            /// Instantiates an object or uses pooling to recycle an old object.
            /// </summary>
            /// <returns>A fresh <see cref="PSetData"/> instance.</returns>
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
            /// <param name="obj">The instance to be recycled.</param>
            public static void PoolRelease(PSetData obj)
            {
                obj.Clear();
                pool.Push(obj);
            }

        }

        /// <summary>
        /// The definition of a single pin. Contains directions,
        /// id and if this pin is in the head of the particle.
        /// </summary>
        public struct PinDef
        {
            /// <summary>
            /// Global direction of the edge on which the pin is
            /// located. Values are <c>0,...,5</c> with <c>0</c>
            /// being East and values increasing in counter-clockwise
            /// direction.
            /// </summary>
            public int globalDir;
            /// <summary>
            /// Local edge index of the pin. Values are
            /// <c>0,...,n-1</c>, where <c>n</c> is the number
            /// of pins per edge.
            /// </summary>
            public int dirID;
            /// <summary>
            /// <c>true</c> if the pin belongs to a contracted particle
            /// or the head of an expanded particle, <c>false</c> if the
            /// pin is on the tail of an expanded particle.
            /// </summary>
            public bool isHead;

            public PinDef(int globalDir, int dirID, bool isHead)
            {
                this.globalDir = globalDir;
                this.dirID = dirID;
                this.isHead = isHead;
            }
        }


        // Logic ====================

        // Private constructor used for pooling
        private ParticlePinGraphicState(int pinsPerSide)
        {
            this.pinsPerSide = pinsPerSide;
            partitionSets = new List<PSetData>(pinsPerSide * 10);
            singletonSets = new List<PSetData>(pinsPerSide * 10);
            // Init Pins
            InitNeighbors();
        }

        /// <summary>
        /// Initializes the neighbor arrays with default values
        /// (indicating no neighbors).
        /// </summary>
        private void InitNeighbors()
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
        /// Calculates the amount of partition sets that have pins
        /// in both halves of the expanded particle.
        /// Returns <c>0</c> if the particle is contracted.
        /// </summary>
        /// <param name="recalcPinNumbers">Whether the pin numbers
        /// should be recalculated. Set this to <c>false</c> if you
        /// already did that for all particles to save time.</param>
        /// <returns>The number of partition sets that contain pins
        /// in both the particle's head and tail, if the particle
        /// is expanded.</returns>
        public int CalculateAmountOfPSetsWithPinsInHeadAndTail(bool recalcPinNumbers = true)
        {
            if (IsExpanded == false) return 0;
            else
            {
                int counter = 0;
                foreach (var pSet in partitionSets)
                {
                    if (pSet.HasPinsInHeadAndTail(recalcPinNumbers))
                        counter++;
                }
                return counter;
            }
        }

        // Code Overrides ______________________________

        /// <summary>
        /// Code override of the position of the partition set
        /// rotation of the standard partition set line.
        /// Expanded particles must set the rotation manually with this mode.
        /// Replaces other code override values that have been set at this
        /// level of the object (like <see cref="CodeOverrideType_Node.AutomaticLine"/>).
        /// </summary>
        /// <param name="rotationDegrees">The rotation degree, counterclockwise.
        /// It is recommended to use a value that is divisible by 60.
        /// 0 degrees means the line is vertical.</param>
        /// <param name="isHead"><c>true</c> for contracted particles or the head
        /// of expanded particles.</param>
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
        /// Code override of the position of the partition sets by the use
        /// of the standard partition set line. Non rotated, except for the
        /// expanded particles where the line is oriented orthogonally to
        /// the other half of the particle. Replaces other code override
        /// values that have been set at this level of the object (like
        /// <see cref="CodeOverrideType_Node.LineRotated"/>).
        /// </summary>
        /// <param name="isHead"><c>true</c> for contracted particles or the
        /// head of expanded particles.</param>
        public void CodePositionOverride_AutomaticLine(bool isHead)
        {
            if (isHead)
                codeOverrideType1 = CodeOverrideType_Node.AutomaticLine;
            else
                codeOverrideType2 = CodeOverrideType_Node.AutomaticLine;
        }

        /// <summary>
        /// Code override of the position of the partition sets by the use
        /// of the default placement algorithm. Replaces other code override
        /// values that have been set at this level of the object (like
        /// <see cref="CodeOverrideType_Node.LineRotated"/>).
        /// </summary>
        /// <param name="isHead"><c>true</c> for contracted particles or the
        /// head of expanded particles.</param>
        public void CodePositionOverride_Automatic(bool isHead)
        {
            if (isHead)
                codeOverrideType1 = CodeOverrideType_Node.Automatic;
            else
                codeOverrideType2 = CodeOverrideType_Node.Automatic;
        }

        /// <summary>
        /// Code override of the position of the partition sets by the use
        /// of manually set polar coordinates that need to be defined in the
        /// partition sets. Replaces other code override values that have
        /// been set at this level of the object (like
        /// <see cref="CodeOverrideType_Node.LineRotated"/>).
        /// </summary>
        /// <param name="isHead"><c>true</c> for contracted particles or the
        /// head of expanded particles.</param>
        public void CodePositionOverride_PolarCoordinatePlacement(bool isHead)
        {
            if (isHead)
                codeOverrideType1 = CodeOverrideType_Node.ManualPlacement;
            else
                codeOverrideType2 = CodeOverrideType_Node.ManualPlacement;
        }

        /// <summary>
        /// Removes any code overrides that have been set on this level
        /// of the object. You might not need this.
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
        /// <param name="pinsPerSide">The new number of pins.</param>
        public void Reset(int pinsPerSide)
        {
            if (this.pinsPerSide != pinsPerSide)
            {
                this.pinsPerSide = pinsPerSide;
                InitNeighbors();
            }
            Reset();
        }


        // Pooling ====================

        private static Stack<ParticlePinGraphicState> pool = new Stack<ParticlePinGraphicState>();

        /// <summary>
        /// Recycles a <see cref="ParticlePinGraphicState"/> instance
        /// from the pool or creates a new one.
        /// </summary>
        /// <param name="pinsPerSide">The number of pins with which to
        /// initialize the new instance.</param>
        /// <returns>A <see cref="ParticlePinGraphicState"/> instance
        /// initialized with <paramref name="pinsPerSide"/> pins.</returns>
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

        /// <summary>
        /// Reinserts the given <see cref="ParticlePinGraphicState"/>
        /// instance into the pool so that it can be reused later.
        /// </summary>
        /// <param name="state">The instanced to be entered into
        /// the pool.</param>
        public static void PoolRelease(ParticlePinGraphicState state)
        {
            pool.Push(state);
        }

    }

}
