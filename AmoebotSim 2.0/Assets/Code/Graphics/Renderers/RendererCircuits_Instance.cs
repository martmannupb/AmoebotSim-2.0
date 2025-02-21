// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AS2.Visuals
{

    /// <summary>
    /// The circuit and bond renderer instance that handles all the
    /// data that is added in a round.
    /// Has render batches for circuits (colorized lines) and pins
    /// (colorized dots) grouped by data with the same properties (like
    /// type, color, etc.).
    /// These batches are all rendered when the draw loop is called.
    /// </summary>
    public class RendererCircuits_Instance
    {

        // Special batches for bonds to avoid unnecessary dictionary lookups
        public RendererCircuits_RenderBatch renderBatch_bondsHexagonal_static = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondHexagonal, false, false, false, Vector2.zero, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActiveOrPaused));
        public RendererCircuits_RenderBatch renderBatch_bondsHexagonal_animated = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondHexagonal, false, false, true, Vector2.zero, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActiveOrPaused));
        public RendererCircuits_RenderBatch renderBatch_bondsCircular_static = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondCircular, false, false, false, Vector2.zero, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActiveOrPaused));
        public RendererCircuits_RenderBatch renderBatch_bondsCircular_animated = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondCircular, false, false, true, Vector2.zero, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActiveOrPaused));
        // Stores all the batches containing bonds for easier iteration
        private readonly RendererCircuits_RenderBatch[] bondBatches;

        // Updated batch data structure:
        // There is a dictionary mapping colors to int indices
        // For batches rendering delayed lines, there is a list of arrays
        // For batches rendering non-delayed lines, there is a list of dictionaries mapping offset vectors to arrays
        // Each array stores six render batches
        // The index of each batch in the array describes the combination of the internal/external, beeping/not beeping and active/paused/activeOrPaused flags
        /// <summary>
        /// Maps colors to render batch list indices. Use
        /// <see cref="GetColorIndex(Color)"/> to access.
        /// </summary>
        private Dictionary<Color, int> colorIndices = new Dictionary<Color, int>();
        /// <summary>
        /// Contains the render batch arrays for delayed batches. Use
        /// <see cref="GetColorIndex(Color)"/> to get the index of the array
        /// belonging to the desired color. Use
        /// <see cref="GetBatchGroup(RendererCircuits_RenderBatch[], bool, bool, Color, Vector2, bool)"/>
        /// to find the correct batches or use <see cref="internal_notBeeping"/> etc. directly.
        /// </summary>
        private List<RendererCircuits_RenderBatch[]> delayedCircuitBatches = new List<RendererCircuits_RenderBatch[]>();
        /// <summary>
        /// Contains a dictionary mapping offset vectors to render batch arrays for
        /// non-delayed batches. Use <see cref="GetColorIndex(Color)"/> to get the index of the
        /// dictionary belonging to the desired color. Use
        /// <see cref="GetNonDelayedArray(int, Vector2)"/> using this index to get the batch array.
        /// Then use <see cref="GetBatchGroup(RendererCircuits_RenderBatch[], bool, bool, Color, Vector2, bool)"/>
        /// or <see cref="internal_notBeeping"/> etc. directly to get the specific batches.
        /// </summary>
        private List<Dictionary<Vector2, RendererCircuits_RenderBatch[]>> nonDelyedCircuitBatches = new List<Dictionary<Vector2, RendererCircuits_RenderBatch[]>>();
        /// <summary>
        /// Contains all line batches for easier iteration.
        /// </summary>
        private List<RendererCircuits_RenderBatch> lineBatches = new List<RendererCircuits_RenderBatch>();
        // Indices in batch arrays indicating property combinations
        private const int internal_notBeeping = 0;
        private const int internal_beeping_active = 1;
        private const int internal_beeping_paused = 2;
        private const int external_notBeeping = 3;
        private const int external_beeping_active = 4;
        private const int external_beeping_paused = 5;

        /// <summary>
        /// Map of render batches for all circle-like elements: Partition set
        /// handles, their beep highlights, and circles at circuit line
        /// junctions covering the sharp edges.
        /// </summary>
        public Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch> propertiesToPinRenderBatchMap = new Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch>();

        // Data
        private Dictionary<ParticleGraphicsAdapterImpl, ParticleCircuitData> circuitDataMap = new Dictionary<ParticleGraphicsAdapterImpl, ParticleCircuitData>();
        private List<ParticleBondGraphicState> bondData = new List<ParticleBondGraphicState>();
        // Flags
        public bool isRenderingActive = false;
        // Temporary
        private List<float> degreeList = new List<float>(16);
        private List<Vector2> vectorList = new List<Vector2>(16);
        private PriorityQueue<ParticlePinGraphicState.PSetData> pSetSortingList = new PriorityQueue<ParticlePinGraphicState.PSetData>();

        /// <summary>
        /// Represents the graphical data related to the circuit
        /// information of a single particle. Provides methods to
        /// check whether a partition set of the particle is near a
        /// given position and to update the partition set and
        /// circuit connector positions (used for the partition set
        /// dragging feature).
        /// </summary>
        public struct ParticleCircuitData
        {
            /// <summary>
            /// The render instance to which the data belongs.
            /// </summary>
            public RendererCircuits_Instance instance;
            /// <summary>
            /// The particle's graphical data.
            /// </summary>
            public ParticleGraphicsAdapterImpl particle;
            /// <summary>
            /// The particle's graphical circuit data.
            /// </summary>
            public ParticlePinGraphicState state;
            /// <summary>
            /// The particle's current position and movement snapshot.
            /// </summary>
            public ParticleGraphicsAdapterImpl.PositionSnap snap;

            public ParticleCircuitData(RendererCircuits_Instance instance, ParticleGraphicsAdapterImpl particle, ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap)
            {
                this.instance = instance;
                this.particle = particle;
                this.state = state;
                this.snap = snap;
            }

            /// <summary>
            /// Represents a single inner "pin", which can be a
            /// partition set handle or a circuit line connector.
            /// Stores the pin's position and a reference to the
            /// partition set data.
            /// </summary>
            public struct PSetInnerPinRef
            {
                /// <summary>
                /// Types of "pins" representing partition sets
                /// inside of a particle.
                /// </summary>
                public enum PinType
                {
                    /// <summary>
                    /// No role determined.
                    /// </summary>
                    None,
                    /// <summary>
                    /// Partition set handle in the particle's head.
                    /// </summary>
                    PSet1,
                    /// <summary>
                    /// Partition set handle in the particle's tail.
                    /// </summary>
                    PSet2,
                    /// <summary>
                    /// Line connector in the expanded particle's head.
                    /// </summary>
                    PConnector1,
                    /// <summary>
                    /// Line connector in the expanded particle's tail.
                    /// </summary>
                    PConnector2
                }

                /// <summary>
                /// The graphical data of the partition set to which
                /// the pin/handle belongs.
                /// </summary>
                public ParticlePinGraphicState.PSetData pSet;
                /// <summary>
                /// The type of the handle.
                /// </summary>
                public PinType pinType;
                /// <summary>
                /// The absolute world position of the handle.
                /// </summary>
                public Vector2 pinPos;

                public PSetInnerPinRef(ParticlePinGraphicState.PSetData pSet, PinType pinType, Vector2 pinPos)
                {
                    this.pSet = pSet;
                    this.pinType = pinType;
                    this.pinPos = pinPos;
                }
            }

            /// <summary>
            /// Tries to get the partition set handle or connector at the
            /// given world position.
            /// </summary>
            /// <param name="worldPos">The world position at which to
            /// look for the handle.</param>
            /// <returns>A <see cref="PSetInnerPinRef"/> referencing a partition
            /// set handle or connector close to the given position, if one
            /// is found, otherwise a null instance.</returns>
            public PSetInnerPinRef GetInnerPSetOrConnectorPinAtPosition(Vector2 worldPos)
            {
                float pinRadius = RenderSystem.const_circuitPinSize / 2f;
                foreach (var pSet in this.state.partitionSets)
                {
                    if (Vector2.Distance(pSet.graphicalData.active_position1, worldPos) <= pinRadius) return new PSetInnerPinRef(pSet, PSetInnerPinRef.PinType.PSet1, pSet.graphicalData.active_position1);
                    else if(snap.isExpanded)
                    {
                        if (Vector2.Distance(pSet.graphicalData.active_position2, worldPos) <= pinRadius) return new PSetInnerPinRef(pSet, PSetInnerPinRef.PinType.PSet2, pSet.graphicalData.active_position2);
                        if (Vector2.Distance(pSet.graphicalData.active_connector_position1, worldPos) <= pinRadius) return new PSetInnerPinRef(pSet, PSetInnerPinRef.PinType.PConnector1, pSet.graphicalData.active_connector_position1);
                        if (Vector2.Distance(pSet.graphicalData.active_connector_position2, worldPos) <= pinRadius) return new PSetInnerPinRef(pSet, PSetInnerPinRef.PinType.PConnector2, pSet.graphicalData.active_connector_position2);
                    }
                }
                return new PSetInnerPinRef(null, PSetInnerPinRef.PinType.None, Vector2.zero);
            }

            /// <summary>
            /// Changes the position of a partition set handle or
            /// connector. Used for dragging the handles with the mouse.
            /// <para>
            /// This method accesses the render batches rendering the
            /// lines that connect the given handle to its pins and/or
            /// connector and updates all the line positions according
            /// to the new handle position.
            /// </para>
            /// </summary>
            /// <param name="innerPin">A reference to the pin that
            /// should be moved.</param>
            /// <param name="worldPos">The new world position of the pin.</param>
            public void UpdatePSetOrConnectorPinPosition(PSetInnerPinRef innerPin, Vector2 worldPos)
            {
                RendererCircuitPins_RenderBatch batch_pins;
                ParticlePinGraphicState.PSetData.GraphicalData gd = innerPin.pSet.graphicalData;
                int offset = 0;

                // Get the render batches (all affected lines have the same properties)
                int colorIdx = instance.GetColorIndex(innerPin.pSet.color);
                RendererCircuits_RenderBatch[] batchesDelayed = instance.delayedCircuitBatches[colorIdx];
                RendererCircuits_RenderBatch[] batchesNonDelayed = instance.GetNonDelayedArray(colorIdx, gd.properties_line.animationOffset);

                // Find the correct batches beforehand
                // We only have internal lines
                RendererCircuits_RenderBatch[] batchesD = instance.GetBatchGroup(batchesDelayed, true, innerPin.pSet.beepsThisRound, innerPin.pSet.color, gd.properties_line.animationOffset, true);
                RendererCircuits_RenderBatch[] batchesND = instance.GetBatchGroup(batchesNonDelayed, true, innerPin.pSet.beepsThisRound, innerPin.pSet.color, gd.properties_line.animationOffset, false);
                RendererCircuits_RenderBatch batchBase = gd.properties_line.delayed ? batchesD[0] : batchesND[0];
                RendererCircuits_RenderBatch batchBeep = gd.properties_line_beep.delayed ? batchesD[2] : batchesND[2];

                switch (innerPin.pinType)
                {
                    case PSetInnerPinRef.PinType.None:
                        Log.Error("UpdatePSetOrConnectorPinPosition: PinType is None.");
                        return;
                    case PSetInnerPinRef.PinType.PSet1:
                        // Lines to Pins (and Connector1, if expanded)
                        // Save position
                        gd.active_position1 = worldPos;
                        // Pin
                        batch_pins = instance.GetBatch_Pin(gd.properties_pin);
                        batch_pins.UpdatePin(worldPos, false, gd.index_pSet1);
                        // Line to connector
                        offset = 0;
                        
                        if (state.IsExpanded && innerPin.pSet.HasPinsInHeadAndTail())
                        {
                            // Connector
                            batchBase.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lines1[0]);
                            if(innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lines1_beep[0]);
                            }
                            offset = 1;
                        }
                        // Lines
                        for (int i = 0; i < gd.pSet1_pins.Count; i++)
                        {
                            Vector2 pinPos = instance.CalculateGlobalPinPosition(snap.position1, gd.pSet1_pins[i], state.pinsPerSide);
                            batchBase.UpdateLine(worldPos, pinPos, gd.index_lines1[i + offset]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, pinPos, gd.index_lines1_beep[i + offset]);
                            }
                        }

                        // Beep Origin and faulty highlight
                        if (innerPin.pSet.beepOrigin)
                        {
                            // Pin
                            batch_pins = instance.GetBatch_Pin(gd.properties_pin_beep);
                            batch_pins.UpdatePin(worldPos, false, gd.index_pSet1_beep_origin);
                        }
                        break;
                    case PSetInnerPinRef.PinType.PSet2:
                        // Lines to Pins (and Connector2, if expanded)
                        if(state.IsExpanded == false)
                        {
                            Log.Error("UpdatePSetOrConnectorPinPosition: Trying to edit a partition set 2 for a particle that is contracted. This is not possible.");
                            return;
                        }
                        // Save position
                        gd.active_position2 = worldPos;
                        // Pin
                        batch_pins = instance.GetBatch_Pin(gd.properties_pin);
                        batch_pins.UpdatePin(worldPos, false, gd.index_pSet2);
                        // Connector
                        offset = 0;
                        if (innerPin.pSet.HasPinsInHeadAndTail())
                        {
                            batchBase.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lines2[0]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lines2_beep[0]);
                            }
                            offset = 1;
                        }
                        // Lines
                        for (int i = 0; i < gd.pSet2_pins.Count; i++)
                        {
                            Vector2 pinPos = instance.CalculateGlobalPinPosition(snap.position2, gd.pSet2_pins[i], state.pinsPerSide);
                            batchBase.UpdateLine(worldPos, pinPos, gd.index_lines2[i + offset]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, pinPos, gd.index_lines2_beep[i + offset]);
                            }
                        }

                        // Beeps and faults
                        if (innerPin.pSet.beepOrigin)
                        {
                            // Pin
                            batch_pins = instance.GetBatch_Pin(gd.properties_pin_beep);
                            batch_pins.UpdatePin(worldPos, false, gd.index_pSet2_beep_origin);
                        }
                        break;
                    case PSetInnerPinRef.PinType.PConnector1:
                        // Lines to Connector2 and PSet1
                        if(state.IsExpanded)
                        {
                            // Save position
                            gd.active_connector_position1 = worldPos;
                            // Connector Pos
                            batch_pins = instance.GetBatch_Pin(gd.properties_connectorPin);
                            batch_pins.UpdateConnectorPin(worldPos, gd.index_pSetConnectorPin1);
                            // Line to PSet
                            batchBase.UpdateLine(worldPos, gd.active_position1, gd.index_lines1[0]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, gd.active_position1, gd.index_lines1_beep[0]);
                            }
                            // Other connector
                            batchBase.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lineConnector);
                            if(innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lineConnector_beep);
                            }
                        }
                        else Log.Error("UpdatePSetOrConnectorPinPosition: Trying to edit connector 1 for a particle that is contracted. This is not possible.");
                        break;
                    case PSetInnerPinRef.PinType.PConnector2:
                        // Lines to Connector1 and PSet2
                        if (state.IsExpanded)
                        {
                            // Save position
                            gd.active_connector_position2 = worldPos;
                            // Connector Pos
                            batch_pins = instance.GetBatch_Pin(gd.properties_connectorPin);
                            batch_pins.UpdateConnectorPin(worldPos, gd.index_pSetConnectorPin2);
                            // Line to PSet
                            batchBase.UpdateLine(worldPos, gd.active_position2, gd.index_lines2[0]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, gd.active_position2, gd.index_lines2_beep[0]);
                            }
                            // Other connector
                            batchBase.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lineConnector);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batchBeep.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lineConnector_beep);
                            }
                        }
                        else Log.Error("UpdatePSetOrConnectorPinPosition: Trying to edit connector 2 for a particle that is contracted. This is not possible.");
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// A reference to graphical data belongig to a single
        /// partition set, combined with information on a line
        /// or partition set handle.
        /// </summary>
        public struct GDRef
        {
            // Defaults
            public static GDRef Empty = new GDRef(); // valid = false

            // Variables
            public ParticlePinGraphicState.PSetData.GraphicalData gd;
            public bool isLine;
            public bool isHead;
            public bool isConnector; // for both lines and pins
            // Line
            public int lineIndex;
            // Pin
            // -
            // Validity
            public bool valid;

            public GDRef(ParticlePinGraphicState.PSetData.GraphicalData gd, bool isLine, bool isHead, bool isConnector, int lineIndex = -1)
            {
                this.gd = gd;
                this.isLine = isLine;
                this.isHead = isHead;
                this.isConnector = isConnector;
                this.lineIndex = lineIndex;
                this.valid = true;
            }
        }

        public RendererCircuits_Instance()
        {
            bondBatches = new RendererCircuits_RenderBatch[] {
                renderBatch_bondsHexagonal_static,
                renderBatch_bondsHexagonal_animated,
                renderBatch_bondsCircular_static,
                renderBatch_bondsCircular_animated
            };
            lineBatches.Add(renderBatch_bondsHexagonal_static);
            lineBatches.Add(renderBatch_bondsHexagonal_animated);
            lineBatches.Add(renderBatch_bondsCircular_static);
            lineBatches.Add(renderBatch_bondsCircular_animated);
        }

        /// <summary>
        /// Calculates the degrees on which the partition sets are placed on a circle.
        /// </summary>
        /// <param name="circuitData">Contains the references to the data.</param>
        /// <param name="outputList">A list that has already been initialized.
        /// It will be cleared in this method.</param>
        /// <param name="isHead">Whether we are looking at head partition sets.</param>
        /// <param name="useRelaxationAlgorithm"><c>true</c> to make sure there is
        /// sufficient space between partition sets.</param>
        private void CalculateCircleLineDegreesForPartitionSets(ParticleCircuitData circuitData, List<float> outputList, bool isHead, bool useRelaxationAlgorithm = true)
        {
            outputList.Clear();
            for (int i = 0; i < circuitData.state.partitionSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = circuitData.state.partitionSets[i];
                if (isHead && pSet.graphicalData.hasPinsInHead || isHead == false && pSet.graphicalData.hasPinsInTail)
                {
                    // Calc average partition set position
                    Vector2 relPos = CalculateAverageRelativePinPosForPartitionSet(pSet, circuitData, isHead);
                    if (relPos == Vector2.zero) outputList.Add(0f);
                    else
                    {
                        // Convert relPos to degree
                        float degree = ((Mathf.Atan2(relPos.y, relPos.x) * Mathf.Rad2Deg - 90f) + 360f) % 360f;
                        outputList.Add(degree);
                    }
                }
            }
            if(useRelaxationAlgorithm) CircleDistributionCircleLine.DistributePointsOnCircle(outputList, Mathf.Min(0.8f * (360f / outputList.Count), 60f));
        }

        /// <summary>
        /// Calculates the positions at which the partition sets are placed in a circle.
        /// </summary>
        /// <param name="circuitData">Contains the references to the data.</param>
        /// <param name="outputList">A list that has already been initialized.
        /// It will be cleared in this method.</param>
        /// <param name="isHead">Whether we are looking at head partition sets.</param>
        /// <param name="useRelaxationAlgorithm"><c>true</c> to make sure there is
        /// sufficient space between partition sets.</param>
        private void CalculateCircleVectorCoordinatesForPartitionSets(ParticleCircuitData circuitData, List<Vector2> outputList, bool isHead, bool useRelaxationAlgorithm = true)
        {
            outputList.Clear();
            for (int i = 0; i < circuitData.state.partitionSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = circuitData.state.partitionSets[i];
                if (isHead && pSet.graphicalData.hasPinsInHead || isHead == false && pSet.graphicalData.hasPinsInTail)
                {
                    // Calc average partition set position
                    Vector2 relPos = CalculateAverageRelativePinPosForPartitionSet(pSet, circuitData, isHead);
                    outputList.Add(relPos);
                }
            }
            if (useRelaxationAlgorithm) CircleDistributionCircleArea.DistributePointsInCircle(outputList, 0.2f, 0.05f, 0.2f, 0.25f);
        }

        /// <summary>
        /// Calculates the average relative pin position of pins connected to a partition set.
        /// If the particle is expanded and there are pins belonging to the partition set that
        /// are in the other part of the particle, one extra virtual pin is added in direction
        /// of the other pins.
        /// </summary>
        /// <param name="pSet">The given partition set.</param>
        /// <param name="circuitData">Contains the references to the data.</param>
        /// <param name="isHead">If we are looking at head pins.</param>
        /// <returns>The average position of the pins belonging to <paramref name="pSet"/>,
        /// relative to the particle's head or tail.</returns>
        private Vector2 CalculateAverageRelativePinPosForPartitionSet(ParticlePinGraphicState.PSetData pSet, ParticleCircuitData circuitData, bool isHead)
        {
            if (isHead && pSet.graphicalData.hasPinsInHead || isHead == false && pSet.graphicalData.hasPinsInTail)
            {
                // Calc average partition set position 2
                Vector2 relPos = Vector2.zero;
                int virtualPinCount = 0;
                foreach (var pinDef in pSet.pins)
                {
                    if (isHead == pinDef.isHead)
                    {
                        relPos += AmoebotFunctions.CalculateRelativePinPosition(pinDef, circuitData.state.pinsPerSide, RenderSystem.setting_viewType);
                        virtualPinCount++;
                    }
                }
                Vector2 addVector = Vector2.zero;
                if (isHead && pSet.graphicalData.hasPinsInTail || isHead == false && pSet.graphicalData.hasPinsInHead)
                {
                    // Add one additional virtual position in direction of the center of the expanded particle
                    addVector = CalculateGlobalExpandedPartitionSetCenterNodePosition(
                        isHead ? circuitData.snap.position1 : circuitData.snap.position2,
                        1, 1,
                        isHead ? 60f * circuitData.state.neighbor1ToNeighbor2Direction : 60f * ((circuitData.state.neighbor1ToNeighbor2Direction + 3) % 6),
                        !isHead);
                    addVector -= AmoebotFunctions.GridToWorldPositionVector2(isHead ? circuitData.snap.position1 : circuitData.snap.position2);
                    
                    relPos += addVector;
                    virtualPinCount++;
                }
                relPos /= (float)virtualPinCount;
                if (relPos == Vector2.zero && (isHead && pSet.graphicalData.hasPinsInTail || isHead == false && pSet.graphicalData.hasPinsInHead))
                    relPos = (relPos + addVector) / 2f;
                return relPos;
            }
            else
                return Vector2.zero;
        }

        /// <summary>
        /// Sets the positions of all partition set handles in the given
        /// particle according to the positioning settings and placement type.
        /// </summary>
        /// <param name="circuitData">The circuit data belonging to a single particle.</param>
        /// <param name="pSetViewType_global">The current partition set view type
        /// set by the user.</param>
        private void CalculatePartitionSetPositions(ParticleCircuitData circuitData, PartitionSetViewType pSetViewType_global)
        {
            // Precalculations
            foreach (var pset in circuitData.state.partitionSets)
                pset.PrecalculatePinNumbersAndStoreInGD();

            //Log.Debug("PartitionSetViewType: Head: " + circuitData.state.codeOverrideType1.ToString() + ", Tail: " + (circuitData.state.isExpanded == false ? "-" : circuitData.state.codeOverrideType2.ToString()));

            // 1. Particle Head ====================
            {
                bool codeOverride_active = false;
                PartitionSetViewType pSetViewType = pSetViewType_global;
                // Get positioning type for both particle halves
                switch (pSetViewType)
                {
                    case PartitionSetViewType.Line:
                        break;
                    case PartitionSetViewType.Auto:
                        break;
                    case PartitionSetViewType.Auto_2DCircle:
                        break;
                    case PartitionSetViewType.CodeOverride:
                        switch (circuitData.state.codeOverrideType1)
                        {
                            case ParticlePinGraphicState.CodeOverrideType_Node.Automatic:
                                pSetViewType = PartitionSetViewType.Auto_2DCircle; // standard view type for this particle half
                                break;
                            case ParticlePinGraphicState.CodeOverrideType_Node.AutomaticLine:
                                pSetViewType = PartitionSetViewType.Line; // standard view type for this particle half
                                break;
                            case ParticlePinGraphicState.CodeOverrideType_Node.LineRotated:
                                pSetViewType = PartitionSetViewType.Line;
                                codeOverride_active = true;
                                break;
                            case ParticlePinGraphicState.CodeOverrideType_Node.ManualPlacement:
                                codeOverride_active = true;
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }

                int numberOfPartitionSetPinsInNode = -1;
                if (pSetViewType == PartitionSetViewType.Auto || pSetViewType == PartitionSetViewType.Auto_2DCircle)
                {
                    // Auto Placement 1. Iteration (PSet Degrees)
                    // 1. Head Pins
                    if (pSetViewType == PartitionSetViewType.Auto)
                        CalculateCircleLineDegreesForPartitionSets(circuitData, degreeList, true);
                    else if (pSetViewType == PartitionSetViewType.Auto_2DCircle)
                        CalculateCircleVectorCoordinatesForPartitionSets(circuitData, vectorList, true);
                    // Get number of PSets
                    numberOfPartitionSetPinsInNode = 0;
                    foreach (var pSet in circuitData.state.partitionSets)
                        if (pSet.graphicalData.hasPinsInHead)
                            numberOfPartitionSetPinsInNode++;
                }
                int counter = 0;
                for (int i = 0; i < circuitData.state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = circuitData.state.partitionSets[i];
                    switch (pSetViewType)
                    {
                        case PartitionSetViewType.Line:
                            // Default
                            float rot1 = 0f;
                            if (circuitData.state.IsExpanded)
                                rot1 = 60f * circuitData.state.neighbor1ToNeighbor2Direction;
                            if (codeOverride_active)
                                rot1 = circuitData.state.codeOverrideLineRotationDegrees1;
                            pSet.graphicalData.active_position1 = CalculateGlobalPartitionSetPinPosition(circuitData.snap.position1, i, circuitData.state.partitionSets.Count, rot1, false);
                            pSet.graphicalData.active_connector_position1 = CalculateGlobalExpandedPartitionSetCenterNodePosition(circuitData.snap.position1, i, circuitData.state.partitionSets.Count, rot1, false);
                            break;
                        case PartitionSetViewType.Auto:
                            // Auto
                            // Auto Placement 2. Iteration (PSet Positions)
                            if (pSet.graphicalData.hasPinsInHead)
                            {
                                // Convert degree to coordinate
                                float degree = degreeList[counter];
                                counter++;
                                Vector2 localPinPos = (degreeList.Count == 1 || numberOfPartitionSetPinsInNode == 1) ? Vector2.zero : Library.DegreeConstants.PolarToCartesian(degree, RenderSystem.global_particleScale * 0.3f, 90f);
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position1);
                                // Save position
                                pSet.graphicalData.active_position1 = posParticle + localPinPos;
                            }
                            break;
                        case PartitionSetViewType.Auto_2DCircle:
                            // Auto (2D Circle)
                            // Auto Placement 2. Iteration (PSet Positions)
                            if (pSet.graphicalData.hasPinsInHead)
                            {
                                // Convert degree to coordinate
                                Vector2 vector = vectorList[counter];
                                counter++;
                                Vector2 localPinPos = (vectorList.Count == 1 || numberOfPartitionSetPinsInNode == 1) ? Vector2.zero : vector;
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position1);
                                // Save position
                                pSet.graphicalData.active_position1 = posParticle + localPinPos;
                            }
                            break;
                        case PartitionSetViewType.CodeOverride:
                            // Code Override
                            // Manual Placement via Polar Coordinates
                            if (pSet.graphicalData.hasPinsInHead)
                            {
                                // Convert degree and radius to coordinate
                                counter++;
                                Vector2 localPinPos = Library.DegreeConstants.PolarToCartesian(pSet.graphicalData.codeOverride_coordinate1.angleDegrees, RenderSystem.global_particleScale * 0.5f * pSet.graphicalData.codeOverride_coordinate1.radiusPercentage, 90f);
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position1);
                                // Save position
                                pSet.graphicalData.active_position1 = posParticle + localPinPos;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            // 2. Particle Tail ====================
            if (circuitData.state.IsExpanded)
            {
                bool codeOverride_active = false;
                PartitionSetViewType pSetViewType = pSetViewType_global;
                // Get positioning type for both particle halves
                switch (pSetViewType)
                {
                    case PartitionSetViewType.Line:
                        break;
                    case PartitionSetViewType.Auto:
                        break;
                    case PartitionSetViewType.Auto_2DCircle:
                        break;
                    case PartitionSetViewType.CodeOverride:
                        switch (circuitData.state.codeOverrideType2)
                        {
                            case ParticlePinGraphicState.CodeOverrideType_Node.Automatic:
                                pSetViewType = PartitionSetViewType.Auto_2DCircle; // standard view type for this particle half
                                break;
                            case ParticlePinGraphicState.CodeOverrideType_Node.AutomaticLine:
                                pSetViewType = PartitionSetViewType.Line; // standard view type for this particle half
                                break;
                            case ParticlePinGraphicState.CodeOverrideType_Node.LineRotated:
                                pSetViewType = PartitionSetViewType.Line;
                                codeOverride_active = true;
                                break;
                            case ParticlePinGraphicState.CodeOverrideType_Node.ManualPlacement:
                                codeOverride_active = true;
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }

                int numberOfPartitionSetPinsInNode = -1;
                if (pSetViewType == PartitionSetViewType.Auto || pSetViewType == PartitionSetViewType.Auto_2DCircle)
                {
                    // Auto Placement 1. Iteration (PSet Degrees)
                    // 2. Tail Pins
                    if (pSetViewType == PartitionSetViewType.Auto)
                        CalculateCircleLineDegreesForPartitionSets(circuitData, degreeList, false);
                    else if (pSetViewType == PartitionSetViewType.Auto_2DCircle)
                        CalculateCircleVectorCoordinatesForPartitionSets(circuitData, vectorList, false);
                    // Get number of PSets
                    numberOfPartitionSetPinsInNode = 0;
                    foreach (var pSet in circuitData.state.partitionSets)
                        if (pSet.graphicalData.hasPinsInTail)
                            numberOfPartitionSetPinsInNode++;
                }
                int counter = 0;
                for (int i = 0; i < circuitData.state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = circuitData.state.partitionSets[i];
                    switch (pSetViewType)
                    {
                        case PartitionSetViewType.Line:
                            // Default
                            float rot2 = 60f * ((circuitData.state.neighbor1ToNeighbor2Direction + 3) % 6);
                            if (codeOverride_active)
                                rot2 = circuitData.state.codeOverrideLineRotationDegrees2;
                            pSet.graphicalData.active_position2 = CalculateGlobalPartitionSetPinPosition(circuitData.snap.position2, i, circuitData.state.partitionSets.Count, rot2, true);
                            pSet.graphicalData.active_connector_position2 = CalculateGlobalExpandedPartitionSetCenterNodePosition(circuitData.snap.position2, i, circuitData.state.partitionSets.Count, rot2, true);
                            break;
                        case PartitionSetViewType.Auto:
                            // Auto
                            // Auto Placement 2. Iteration (PSet Positions)
                            if (pSet.graphicalData.hasPinsInTail)
                            {
                                // Convert degree to coordinate
                                float degree = degreeList[counter];
                                counter++;
                                Vector2 localPinPos = (degreeList.Count == 1 || numberOfPartitionSetPinsInNode == 1) ? Vector2.zero : Library.DegreeConstants.PolarToCartesian(degree, RenderSystem.global_particleScale * 0.3f, 90f);
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position2);
                                // Save position
                                pSet.graphicalData.active_position2 = posParticle + localPinPos;
                            }
                            break;
                        case PartitionSetViewType.Auto_2DCircle:
                            // Auto (2D Circle)
                            // Auto Placement 2. Iteration (PSet Positions)
                            if (pSet.graphicalData.hasPinsInTail)
                            {
                                // Convert degree to coordinate
                                Vector2 vector = vectorList[counter];
                                counter++;
                                Vector2 localPinPos = (vectorList.Count == 1 || numberOfPartitionSetPinsInNode == 1) ? Vector2.zero : vector;
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position2);
                                // Save position
                                pSet.graphicalData.active_position2 = posParticle + localPinPos;
                            }
                            break;
                        case PartitionSetViewType.CodeOverride:
                            // Code Override
                            // Manual Placement via Polar Coordinates
                            if (pSet.graphicalData.hasPinsInTail)
                            {
                                // Convert degree and radius to coordinate
                                counter++;
                                Vector2 localPinPos = Library.DegreeConstants.PolarToCartesian(pSet.graphicalData.codeOverride_coordinate2.angleDegrees, RenderSystem.global_particleScale * 0.5f * pSet.graphicalData.codeOverride_coordinate2.radiusPercentage, 90f);
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position2);
                                // Save position
                                pSet.graphicalData.active_position2 = posParticle + localPinPos;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            // 3. Pin Connector Placement ====================
            if (circuitData.state.IsExpanded)
            {
                pSetSortingList.Clear();
                for (int i = 0; i < circuitData.state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = circuitData.state.partitionSets[i];
                    if (pSet.HasPinsInHeadAndTail(false))
                    {
                        // Calculate the average partition set positions
                        Vector2 averageSetPosition = (pSet.graphicalData.active_position1 + pSet.graphicalData.active_position2) / 2f;
                        // Distance to line through particles
                        float distanceToLineThroughParticleHalves = Library.DegreeConstants.ManuallyImplementedSignedOrthogonalDistancesOfPointToLineFromAToB(averageSetPosition, AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position1), AmoebotFunctions.GridToWorldPositionVector2(circuitData.snap.position2));
                        pSetSortingList.Enqueue(distanceToLineThroughParticleHalves, pSet);
                    }
                }
                List<Tuple<float, ParticlePinGraphicState.PSetData>> list = pSetSortingList.GetSortedList();
                for (int i = 0; i < pSetSortingList.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = list[i].Item2;
                    float rot1 = 60f * circuitData.state.neighbor1ToNeighbor2Direction;
                    float rot2 = 60f * ((circuitData.state.neighbor1ToNeighbor2Direction + 3) % 6);
                    pSet.graphicalData.active_connector_position1 = CalculateGlobalExpandedPartitionSetCenterNodePosition(circuitData.snap.position1, pSetSortingList.Count - 1 - i, pSetSortingList.Count, rot1, false);
                    pSet.graphicalData.active_connector_position2 = CalculateGlobalExpandedPartitionSetCenterNodePosition(circuitData.snap.position2, pSetSortingList.Count - 1 - i, pSetSortingList.Count, rot2, true);
                }
            }
            // ================================================================================================================================================================
        }

        /// <summary>
        /// Redraws the circuits with the given partition set view type.
        /// </summary>
        /// <param name="pSetViewType">The view type the circuits are drawn in.</param>
        public void Refresh(PartitionSetViewType pSetViewType)
        {
            Clear(true, true);
            foreach (var data in circuitDataMap.Values)
            {
                AddCircuits(data.particle, data.state, data.snap, pSetViewType, false);
            }
            foreach (var data in bondData)
            {
                AddBond(data, false);
            }
        }

        /// <summary>
        /// Adds the data of a particle's pin configuration to the system.
        /// Combines it with the position data of the particle itself to
        /// calculate all positions of the circuits and pins.
        /// </summary>
        /// <param name="particle">The particle the data belongs to.</param>
        /// <param name="state">The particle's graphical pin and partition set data.</param>
        /// <param name="snap">The particle's position and movement data.</param>
        /// <param name="pSetViewType">The view type that the circuits should be drawn with.</param>
        /// <param name="addToCircuitData">Whether the given input should be added to the
        /// circuit data. <c>true</c> by default, set to <c>false</c> when doing something like
        /// refreshing the circuits.</param>
        public void AddCircuits(ParticleGraphicsAdapterImpl particle, ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, PartitionSetViewType pSetViewType, bool addToCircuitData = true)
        {
            ParticleCircuitData circuitData;
            if (addToCircuitData)
            {
                circuitData = new ParticleCircuitData(this, particle, state, snap);
                circuitDataMap.Add(particle, circuitData);
            }
            else
                circuitData = circuitDataMap[particle];

            //bool delayed = RenderSystem.animationsOn && (circuitData.snap.jointMovementState.isJointMovement || (circuitData.snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || circuitData.snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting));
            bool delayed = snap.noAnimation == false && RenderSystem.animationsOn && (snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting);
            bool movement = RenderSystem.animationsOn && snap.jointMovementState.isJointMovement && delayed == false;
            Vector2 movementOffset = movement ? -AmoebotFunctions.GridToWorldPositionVector2(snap.jointMovementState.jointMovementOffset) : Vector2.zero;

            // 1. Calc PartitionSet Positions
            CalculatePartitionSetPositions(circuitData, pSetViewType);
            // 2. Generate Pins and Lines
            if (state.IsExpanded == false)
            {
                // Contracted
                // Add Internal Pins and Lines
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    ParticlePinGraphicState.PSetData.GraphicalData gd = pSet.graphicalData;
                    // 1. Add Pin
                    AddPin(pSet.graphicalData.active_position1, pSet.color, delayed, pSet.beepsThisRound, pSet.beepOrigin, pSet.isFaulty, movementOffset,
                        new GDRef(gd, false, true, false),
                        new GDRef(gd, false, true, false),
                        new GDRef(gd, false, true, false));
                    // 2. Add Lines
                    GDRef gdRef_lines = new GDRef(gd, true, true, false, 0);
                    AddLines_PartitionSetContracted(state, snap, pSet, pSet.graphicalData.active_position1, delayed, movementOffset, gdRef_lines);
                }
                // Add Singleton Lines
                for (int i = 0; i < state.singletonSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.singletonSets[i];
                    // 1. Add Lines
                    AddLines_SingletonSetContracted(state, snap, pSet, delayed, movementOffset);
                }
            }
            else
            {
                // Expanded
                // Add Internal Pins and Lines
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    ParticlePinGraphicState.PSetData.GraphicalData gd = pSet.graphicalData;
                    GDRef gdRef_lines1 = new GDRef(gd, true, true, false, 0);
                    GDRef gdRef_lines2 = new GDRef(gd, true, false, false, 0);
                    // 1. Add Pins + Connectors + Internal Lines
                    if (pSet.graphicalData.hasPinsInHead)
                        AddPin(pSet.graphicalData.active_position1, pSet.color, delayed, pSet.beepsThisRound, pSet.beepOrigin, pSet.isFaulty, movementOffset, new GDRef(gd, false, true, false), new GDRef(gd, false, true, false), new GDRef(gd, false, true, false));
                    if (pSet.graphicalData.hasPinsInTail)
                        AddPin(pSet.graphicalData.active_position2, pSet.color, delayed, pSet.beepsThisRound, pSet.beepOrigin, pSet.isFaulty, movementOffset, new GDRef(gd, false, false, false), new GDRef(gd, false, false, false), new GDRef(gd, false, false, false));
                    if (pSet.HasPinsInHeadAndTail())
                    {
                        // All of these connector lines have the same color and batch, only one dict lookup
                        int colorIdx = GetColorIndex(pSet.color);
                        // These are the batches for delayed lines
                        RendererCircuits_RenderBatch[] batchesDelayed = delayedCircuitBatches[colorIdx];
                        RendererCircuits_RenderBatch[] batchesNonDelayed = GetNonDelayedArray(colorIdx, movementOffset);

                        // Add 3 lines to the same batch (since they have the same properties)
                        // These lines connect the two partition set handles
                        AddLines(
                            new Vector2[] { pSet.graphicalData.active_position1, pSet.graphicalData.active_position2, pSet.graphicalData.active_connector_position1 },
                            new Vector2[] { pSet.graphicalData.active_connector_position1, pSet.graphicalData.active_connector_position2, pSet.graphicalData.active_connector_position2 },
                            pSet.color, false, delayed, pSet.beepsThisRound, movementOffset,
                            batchesDelayed, batchesNonDelayed,
                            new GDRef[] { gdRef_lines1, gdRef_lines2, new GDRef(gd, true, true, true) },
                            new GDRef[] { gdRef_lines1, gdRef_lines2, new GDRef(gd, true, true, true) }
                        );
                        gdRef_lines1.lineIndex++;
                        gdRef_lines2.lineIndex++;

                        AddConnectorPin(pSet.graphicalData.active_connector_position1, pSet.color, delayed, movementOffset, new GDRef(gd, false, true, true));
                        AddConnectorPin(pSet.graphicalData.active_connector_position2, pSet.color, delayed, movementOffset, new GDRef(gd, false, false, true));
                    }
                    // 2. Add Internal Lines + Connector Lines
                    AddLines_PartitionSetExpanded(state, snap, pSet, pSet.graphicalData.active_position1, pSet.graphicalData.active_position2, delayed, movementOffset, gdRef_lines1, gdRef_lines2);
                }
                // Add Singleton Lines
                for (int i = 0; i < state.singletonSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.singletonSets[i];
                    AddLines_SingletonSetExpanded(state, snap, pSet, delayed, movementOffset);
                }
            }
        }

        /// <summary>
        /// Adds the internal and external circuit lines belonging to a
        /// partition set of a contracted particle.
        /// </summary>
        /// <param name="state">The graphical circuit information belonging
        /// to the particle.</param>
        /// <param name="snap">The position snapshot of the particle.</param>
        /// <param name="pSet">The partition set whose lines should be added.</param>
        /// <param name="posPartitionSet">The global position of the
        /// partition set handle.</param>
        /// <param name="delayed">Whether the partition set and its lines should
        /// be displayed after a delay.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from
        /// the particle's end position after its movement to its start position.</param>
        /// <param name="gdRef">The graphical data struct in which the new graphical
        /// information should be stored.</param>
        private void AddLines_PartitionSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet, bool delayed, Vector2 movementOffset, GDRef gdRef)
        {
            // All lines of the pSet have the same color, only one dict lookup
            int colorIdx = GetColorIndex(pSet.color);
            // These are the batches for delayed lines
            RendererCircuits_RenderBatch[] batchesDelayed = delayedCircuitBatches[colorIdx];
            // These are for non-delayed lines
            RendererCircuits_RenderBatch[] batchesNonDelayed = GetNonDelayedArray(colorIdx, movementOffset);

            // Find the correct batches beforehand to avoid calculating the indices redundantly
            // Batches for the internal lines
            RendererCircuits_RenderBatch[] internalBatches = GetBatchGroup(delayed ? batchesDelayed : batchesNonDelayed, true, pSet.beepsThisRound, pSet.color, movementOffset, delayed);
            // Batches for the external lines
            RendererCircuits_RenderBatch[] externalBatchesD = GetBatchGroup(batchesDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, true);
            RendererCircuits_RenderBatch[] externalBatchesND = GetBatchGroup(batchesNonDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, false);

            foreach (var pin in pSet.pins)
            {
                // Inner Line
                Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                gdRef.gd.pSet1_pins.Add(pin);
                AddCircuitLineToBatches(posPartitionSet, posPin, internalBatches[0], pSet.beepsThisRound, internalBatches[1], internalBatches[2], gdRef, gdRef);
                gdRef.lineIndex++;
                // Outer Line
                if (state.hasNeighbor1[pin.globalDir])
                {
                    Vector2 posOuterLineCenter = CalculateGlobalOuterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                    RendererCircuits_RenderBatch[] arr = delayedState ? externalBatchesD : externalBatchesND;
                    AddCircuitLineToBatches(posPin, posOuterLineCenter,
                        arr[0], pSet.beepsThisRound, arr[1], arr[2],
                        GDRef.Empty, GDRef.Empty);
                }
            }
        }

        /// <summary>
        /// Adds the external circuit lines belonging to a singleton
        /// partition set of a contracted particle. Also adds a beep
        /// highlight if the partition set is a beep origin and a fault
        /// highlight if the partition set has a beep fault.
        /// </summary>
        /// <param name="state">The graphical circuit information belonging
        /// to the particle.</param>
        /// <param name="snap">The position snapshot of the particle.</param>
        /// <param name="pSet">The partition set whose lines should be added.</param>
        /// <param name="delayed">Whether the partition set and its lines should
        /// be displayed after a delay.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from
        /// the particle's end position after its movement to its start position.</param>
        private void AddLines_SingletonSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool delayed, Vector2 movementOffset)
        {
            // All lines of the pSet have the same color, only one dict lookup
            int colorIdx = GetColorIndex(pSet.color);
            RendererCircuits_RenderBatch[] batchesDelayed = delayedCircuitBatches[colorIdx];
            RendererCircuits_RenderBatch[] batchesNonDelayed = GetNonDelayedArray(colorIdx, movementOffset);

            // Find the correct batches beforehand to avoid calculating the indices redundantly
            // We only have external lines
            RendererCircuits_RenderBatch[] batchesD = GetBatchGroup(batchesDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, true);
            RendererCircuits_RenderBatch[] batchesND = GetBatchGroup(batchesNonDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, false);

            foreach (var pin in pSet.pins)
            {
                Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                // Outer Line
                if (state.hasNeighbor1[pin.globalDir])
                {
                    Vector2 posOuterLineCenter = CalculateGlobalOuterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                    RendererCircuits_RenderBatch[] arr = delayedState ? batchesD : batchesND;
                    AddCircuitLineToBatches(posPin, posOuterLineCenter,
                        arr[0], pSet.beepsThisRound, arr[1], arr[2],
                        GDRef.Empty, GDRef.Empty);
                }
                // Beep Origin
                if (pSet.beepOrigin)
                {
                    AddSingletonBeepOrigin(posPin, pSet.color, delayed, movementOffset);
                }
                if (pSet.isFaulty)
                {
                    AddSingletonFault(posPin, pSet.color, delayed, movementOffset);
                }
                else if (pSet.beepsThisRound)
                {
                    AddSingletonBeep(posPin, pSet.color, delayed, movementOffset);
                }
            }
        }

        /// <summary>
        /// Adds the internal and external circuit lines belonging to a
        /// partition set of an expanded particle.
        /// </summary>
        /// <param name="state">The graphical circuit information belonging
        /// to the particle.</param>
        /// <param name="snap">The position snapshot of the particle.</param>
        /// <param name="pSet">The partition set whose lines should be added.</param>
        /// <param name="posPartitionSet1">The global position of the
        /// partition set handle in the particle's head.</param>
        /// <param name="posPartitionSet2">The global position of the
        /// partition set handle in the particle's tail.</param>
        /// <param name="delayed">Whether the partition set and its lines should
        /// be displayed after a delay.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from
        /// the particle's end position after its movement to its start position.</param>
        /// <param name="gdRef_lines1">The graphical data struct in which the new graphical
        /// information for the head lines should be stored.</param>
        /// <param name="gdRef_lines2">The graphical data struct in which the new graphical
        /// information for the tail lines should be stored.</param>
        private void AddLines_PartitionSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet1, Vector2 posPartitionSet2, bool delayed, Vector2 movementOffset, GDRef gdRef_lines1, GDRef gdRef_lines2)
        {
            // All lines of the pSet have the same color, only one dict lookup
            int colorIdx = GetColorIndex(pSet.color);
            // These are the batches for delayed lines
            RendererCircuits_RenderBatch[] batchesDelayed = delayedCircuitBatches[colorIdx];
            RendererCircuits_RenderBatch[] batchesNonDelayed = GetNonDelayedArray(colorIdx, movementOffset);

            // Find the correct batches beforehand to avoid calculating the indices redundantly
            // Batches for the internal lines
            RendererCircuits_RenderBatch[] internalBatches = GetBatchGroup(delayed ? batchesDelayed : batchesNonDelayed, true, pSet.beepsThisRound, pSet.color, movementOffset, delayed);
            // Batches for the external lines
            RendererCircuits_RenderBatch[] externalBatchesD = GetBatchGroup(batchesDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, true);
            RendererCircuits_RenderBatch[] externalBatchesND = GetBatchGroup(batchesNonDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, false);

            foreach (var pin in pSet.pins)
            {
                // Inner Line
                Vector2 posPin;
                if (pin.isHead)
                {
                    posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                    gdRef_lines1.gd.pSet1_pins.Add(pin);
                    AddCircuitLineToBatches(posPartitionSet1, posPin, internalBatches[0], pSet.beepsThisRound, internalBatches[1], internalBatches[2], gdRef_lines1, gdRef_lines1);
                    gdRef_lines1.lineIndex++;
                }
                else
                {
                    posPin = CalculateGlobalPinPosition(snap.position2, pin, state.pinsPerSide);
                    gdRef_lines2.gd.pSet2_pins.Add(pin);
                    AddCircuitLineToBatches(posPartitionSet2, posPin, internalBatches[0], pSet.beepsThisRound, internalBatches[1], internalBatches[2], gdRef_lines2, gdRef_lines2);
                    gdRef_lines2.lineIndex++;
                }

                // Outer Line
                if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
                {
                    Vector2 posOuterLineCenter = CalculateGlobalOuterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || (pin.isHead ? state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn : state.neighborPinConnection2[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn));
                    RendererCircuits_RenderBatch[] arr = delayedState ? externalBatchesD : externalBatchesND;
                    AddCircuitLineToBatches(posPin, posOuterLineCenter, arr[0], pSet.beepsThisRound, arr[1], arr[2], GDRef.Empty, GDRef.Empty);
                }
            }
        }

        /// <summary>
        /// Adds the internal and external circuit lines belonging to a
        /// singleton partition set of an expanded particle. Also adds a
        /// beep highlight if the partition set is a beep origin and a
        /// fault highlight if the partition set has a beep fault.
        /// </summary>
        /// <param name="state">The graphical circuit information belonging
        /// to the particle.</param>
        /// <param name="snap">The position snapshot of the particle.</param>
        /// <param name="pSet">The partition set whose lines should be added.</param>
        /// <param name="delayed">Whether the partition set and its lines should
        /// be displayed after a delay.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from
        /// the particle's end position after its movement to its start position.</param>
        private void AddLines_SingletonSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool delayed, Vector2 movementOffset)
        {
            // All lines of the pSet have the same color, only one dict lookup
            int colorIdx = GetColorIndex(pSet.color);
            RendererCircuits_RenderBatch[] batchesDelayed = delayedCircuitBatches[colorIdx];
            RendererCircuits_RenderBatch[] batchesNonDelayed = GetNonDelayedArray(colorIdx, movementOffset);

            // Find the correct batches beforehand to avoid calculating the indices redundantly
            // We only have external lines
            RendererCircuits_RenderBatch[] batchesD = GetBatchGroup(batchesDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, true);
            RendererCircuits_RenderBatch[] batchesND = GetBatchGroup(batchesNonDelayed, false, pSet.beepsThisRound, pSet.color, movementOffset, false);

            foreach (var pin in pSet.pins)
            {
                Vector2 posPin = CalculateGlobalPinPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                // Outer Line
                if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
                {
                    Vector2 posOuterLineCenter = CalculateGlobalOuterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || (pin.isHead ? state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn : state.neighborPinConnection2[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn));
                    RendererCircuits_RenderBatch[] arr = delayedState ? batchesD : batchesND;
                    AddCircuitLineToBatches(posPin, posOuterLineCenter, arr[0], pSet.beepsThisRound, arr[1], arr[2], GDRef.Empty, GDRef.Empty);
                }
                // Beep Origin
                if (pSet.beepOrigin)
                {
                    AddSingletonBeepOrigin(posPin, pSet.color, delayed, movementOffset);
                }
                if (pSet.isFaulty)
                {
                    AddSingletonFault(posPin, pSet.color, delayed, movementOffset);
                }
                else if (pSet.beepsThisRound)
                {
                    AddSingletonBeep(posPin, pSet.color, delayed, movementOffset);
                }
            }
        }


        /// <summary>
        /// Adds lines representing a circuit line between the two
        /// given positions to the corresponding batches.
        /// </summary>
        /// <param name="globalLineStartPos">The start position of the line.</param>
        /// <param name="globalLineEndPos">The end position of the line.</param>
        /// <param name="batchBase">The render batch into which the non-beeping part of
        /// the line should be added.</param>
        /// <param name="beeping">Whether or not the circuit to which the line belongs
        /// is currently beeping.</param>
        /// <param name="batchBeepActive">The render batch to which the animated flashing
        /// part of the line should be added if the circuit is beeping. May be <c>null</c>
        /// if <paramref name="beeping"/> is <c>false</c>.</param>
        /// <param name="batchBeepPaused">The render batch to which the static part with
        /// the white center shown while the simulation is paused should be added if the
        /// circuit is beeping. May be <c>null</c> if <paramref name="beeping"/> is
        /// <c>false</c>.</param>
        /// <param name="gdRef">Graphical data reference for the non-beeping part.</param>
        /// <param name="gdRef_beep">Graphical data reference for the beeping part.</param>
        private void AddCircuitLineToBatches(Vector2 globalLineStartPos, Vector2 globalLineEndPos,
            RendererCircuits_RenderBatch batchBase, bool beeping, RendererCircuits_RenderBatch batchBeepActive, RendererCircuits_RenderBatch batchBeepPaused,
            GDRef gdRef, GDRef gdRef_beep)
        {
            // Normal Circuit
            RenderBatchIndex index = batchBase.AddLine(globalLineStartPos, globalLineEndPos);
            if (gdRef.valid)
            {
                StoreRenderBatchIndex(gdRef, index, true, false, false);
                gdRef.gd.properties_line = batchBase.properties;
            }
            // Beep
            if (beeping)
            {
                // Play Mode
                index = batchBeepActive.AddLine(globalLineStartPos, globalLineEndPos);
                // We only need to store the index for the paused mode
                // Pause Mode
                index = batchBeepPaused.AddLine(globalLineStartPos, globalLineEndPos);
                if (gdRef_beep.valid)
                {
                    StoreRenderBatchIndex(gdRef_beep, index, true, true, false);
                    gdRef_beep.gd.properties_line_beep = batchBeepPaused.properties;
                }
            }
        }

        /// <summary>
        /// Convenience version of <see cref="AddCircuitLineToBatches(Vector2, Vector2, RendererCircuits_RenderBatch, bool, RendererCircuits_RenderBatch, RendererCircuits_RenderBatch, GDRef, GDRef)"/>
        /// for the case that several lines have the exact same properties.
        /// </summary>
        /// <param name="globalLineStartPos">The start positions of the lines.</param>
        /// <param name="globalLineEndPos">The end positions of the lines. Must have the same
        /// length as <paramref name="globalLineStartPos"/>.</param>
        /// <param name="color">The color of the lines.</param>
        /// <param name="isConnectorLine">Whether the lines are connectors between particles
        /// or internal lines.</param>
        /// <param name="delayed">Whether the lines should be displayed with a delay or not.</param>
        /// <param name="beeping">Whether the lines should have a beeping effect or not.</param>
        /// <param name="movementOffset">The movement offset used to animate the line movements.
        /// Should be <c>Vector2.zero</c> if <paramref name="delayed"/> is <c>true</c>.</param>
        /// <param name="delayedBatches">The render batch array of size 6 containing the delayed
        /// batches for the color <paramref name="color"/>.</param>
        /// <param name="nonDelayedBatches">The render batch array of size 6 containing the
        /// non-delayed batches for the color <paramref name="color"/> and the movement offset
        /// <paramref name="movementOffset"/>.</param>
        /// <param name="gdRef">Graphical data references for the non-beeping lines. Must have
        /// the same length as <paramref name="globalLineStartPos"/>.</param>
        /// <param name="gdRef_beep">Graphical data references for the beeping lines. Must have
        /// the same length as <paramref name="globalLineStartPos"/>.</param>
        private void AddLines(Vector2[] globalLineStartPos, Vector2[] globalLineEndPos, Color color, bool isConnectorLine, bool delayed, bool beeping, Vector2 movementOffset,
            RendererCircuits_RenderBatch[] delayedBatches, RendererCircuits_RenderBatch[] nonDelayedBatches, GDRef[] gdRef, GDRef[] gdRef_beep)
        {
            // Normal Circuit
            int arrayIndex = GetArrayIndex(!isConnectorLine, false);
            RendererCircuits_RenderBatch[] arr = delayed ? delayedBatches : nonDelayedBatches;
            RendererCircuits_RenderBatch batch = GetArrayBatch(arr, arrayIndex, color, movementOffset, delayed);
            RenderBatchIndex index;
            for (int i = 0; i < globalLineStartPos.Length; i++)
            {
                index = batch.AddLine(globalLineStartPos[i], globalLineEndPos[i]);
                if (gdRef[i].valid)
                {
                    StoreRenderBatchIndex(gdRef[i], index, true, false, false);
                    gdRef[i].gd.properties_line = batch.properties;
                }
            }
            // Beep
            if (beeping)
            {
                int arrayIndexActive = GetArrayIndex(!isConnectorLine, true, true);
                int arrayIndexPaused = GetArrayIndex(!isConnectorLine, true, false);

                // Play Mode
                batch = GetArrayBatch(arr, arrayIndexActive, color, movementOffset, delayed);
                // Pause Mode
                RendererCircuits_RenderBatch batch2 = GetArrayBatch(arr, arrayIndexPaused, color, movementOffset, delayed);

                for (int i = 0; i < globalLineStartPos.Length; i++)
                {
                    batch.AddLine(globalLineStartPos[i], globalLineEndPos[i]);
                    // Only need to store the index for the paused mode
                    index = batch2.AddLine(globalLineStartPos[i], globalLineEndPos[i]);
                    if (gdRef_beep[i].valid)
                    {
                        StoreRenderBatchIndex(gdRef_beep[i], index, true, true, false);
                        gdRef_beep[i].gd.properties_line_beep = batch.properties;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a pin/partition set handle to the render system, using the corresponding
        /// render batch or creating a new one.
        /// </summary>
        /// <param name="pinPos">The global position of the pin.</param>
        /// <param name="color">The color in which the pin should be rendered.</param>
        /// <param name="delayed">Whether this pin should be displayed with a delay
        /// so that it only appears after any movement animations are finished.</param>
        /// <param name="beeping">Whether a beep should be displayed on this pin.</param>
        /// <param name="beepOrigin">Whether this pin is a beep origin.</param>
        /// <param name="faulty">Whether a beep fault should be displayed on this pin.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from the
        /// pin's end position after its movement to its start position.</param>
        /// <param name="gdRef">Graphical data belonging to the pin.</param>
        /// <param name="gdRef_beep_origin">Graphical data belonging to the beep origin. Should usually
        /// be the same as <paramref name="gdRef"/>.</param>
        /// <param name="gdRef_beep_fault">Graphical data belonging to the beep or fault highlight.
        /// Should usually be the same as <paramref name="gdRef"/>.</param>
        private void AddPin(Vector2 pinPos, Color color, bool delayed, bool beeping, bool beepOrigin, bool faulty, Vector2 movementOffset, GDRef gdRef, GDRef gdRef_beep_origin, GDRef gdRef_beep_fault)
        {
            // Base pin
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false, beeping, false, faulty, false, movementOffset);
            RenderBatchIndex index = batch.AddPin(pinPos, false);
            if(gdRef.valid)
            {
                StoreRenderBatchIndex(gdRef, index, false, false, false);
                gdRef.gd.properties_pin = batch.properties;
            }
            // Beep origin
            if (beepOrigin)
            {
                batch = GetBatch_Pin(color, delayed, false, false, true, false, false, movementOffset);
                index = batch.AddPin(pinPos, false);
                if (gdRef_beep_origin.valid)
                {
                    StoreRenderBatchIndex(gdRef_beep_origin, index, false, true, false);
                    gdRef_beep_origin.gd.properties_pin_beep = batch.properties;
                }
            }
        }


        /// <summary>
        /// Adds a beep origin highlight to the pin of a singleton partition set.
        /// </summary>
        /// <param name="pinPos">The global position of the pin.</param>
        /// <param name="color">The color of the partition set (will
        /// not be rendered; the beep highlight is always light gray).</param>
        /// <param name="delayed">Whether the pin and beep should appear delayed
        /// because the particle is performing a movement.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from
        /// the pin's end position after its movement to its start position.</param>
        private void AddSingletonBeepOrigin(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset)
        {
            // Beep
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, true, false, true, false, false, movementOffset);
            batch.AddPin(pinPos, true);
        }

        /// <summary>
        /// Adds a beep highlight to the pin of a singleton partition set.
        /// </summary>
        /// <param name="pinPos">The global position of the pin.</param>
        /// <param name="color">The color of the partition set (will
        /// not be rendered; the fault highlight is always red).</param>
        /// <param name="delayed">Whether the pin and fault should appear delayed
        /// because the particle is performing a movement.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from
        /// the pin's end position after its movement to its start position.</param>
        private void AddSingletonBeep(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset)
        {
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, true, true, false, false, false, movementOffset);
            batch.AddPin(pinPos, true);
        }

        /// <summary>
        /// Adds a fault highlight to the pin of a singleton partition set.
        /// </summary>
        /// <param name="pinPos">The global position of the pin.</param>
        /// <param name="color">The color of the partition set (will
        /// not be rendered; the fault highlight is always red).</param>
        /// <param name="delayed">Whether the pin and fault should appear delayed
        /// because the particle is performing a movement.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from
        /// the pin's end position after its movement to its start position.</param>
        private void AddSingletonFault(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset)
        {
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, true, false, false, true, false, movementOffset);
            batch.AddPin(pinPos, true);
        }

        /// <summary>
        /// Adds a colored circle at the position where two internal
        /// circuit lines meet to cover the sharp edges.
        /// </summary>
        /// <param name="pinPos">The global position where the circle
        /// should be drawn.</param>
        /// <param name="color">The color of the circuit to which the
        /// circle belongs.</param>
        /// <param name="delayed">Whether the circle should appear delayed
        /// because the particle is performing a movement.</param>
        /// <param name="movementOffset">The world coordinate vector pointing from the
        /// pin's end position after its movement to its start position.</param>
        /// <param name="gdRef">Graphical data belonging to the partition set.</param>
        private void AddConnectorPin(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset, GDRef gdRef)
        {
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false, false, false, false, true, movementOffset);
            RenderBatchIndex index = batch.AddConnectorPin(pinPos);
            if(gdRef.valid)
            {
                StoreRenderBatchIndex(gdRef, index, false, false, false);
                gdRef.gd.properties_connectorPin = batch.properties;
            }
        }

        /// <summary>
        /// Stores the given batch index in the corresponding
        /// field of the graphical data struct.
        /// </summary>
        /// <param name="gdRef">The graphical data struct in which the
        /// batch index should be stored.</param>
        /// <param name="index">The render batch index to be stored.</param>
        /// <param name="isLine">Whether the index belongs to a
        /// circuit line.</param>
        /// <param name="isBeepOrigin">Whether the index belongs to a
        /// beep origin highlight or to a beeping circuit line.</param>
        /// <param name="isBeepOrFault">Whether the index belongs to an
        /// object with a beep or fault highlight. Should never be true when
        /// <paramref name="isBeepOrigin"/>is also true.</param>
        private void StoreRenderBatchIndex(GDRef gdRef, RenderBatchIndex index, bool isLine, bool isBeepOrigin, bool isBeepOrFault)
        {
            if(gdRef.valid == false)
            {
                Log.Error("StoreRenderBatchIndex: GDRef is not valid.");
                return;
            }
            if(isLine)
            {
                // Lines
                if (gdRef.isLine == false)
                {
                    Log.Error("StoreRenderBatchIndex: isLine is set, but it is not set in GDRef");
                    return;
                }
                if(isBeepOrigin == false)
                {
                    if(gdRef.isConnector)
                    {
                        gdRef.gd.index_lineConnector = index;
                    }
                    else
                    {
                        if(gdRef.isHead) gdRef.gd.index_lines1.Add(index);
                        else gdRef.gd.index_lines2.Add(index);
                    }
                }
                else
                {
                    if (gdRef.isConnector)
                    {
                        gdRef.gd.index_lineConnector_beep = index;
                    }
                    else
                    {
                        if (gdRef.isHead) gdRef.gd.index_lines1_beep.Add(index);
                        else gdRef.gd.index_lines2_beep.Add(index);
                    }
                }
            }
            else
            {
                // Pins
                if(gdRef.isLine)
                {
                    Log.Error("StoreRenderBatchIndex: isLine is not set, but it is set in GDRef");
                    return;
                }
                if (isBeepOrigin)
                {
                    if (gdRef.isConnector)
                    {
                        // No beeps here
                        //if (gdRef.isHead) gdRef.gd.index_pSetConnectorPin1 = index;
                        //else gdRef.gd.index_pSetConnectorPin2 = index;
                    }
                    else
                    {
                        if (gdRef.isHead) gdRef.gd.index_pSet1_beep_origin = index;
                        else gdRef.gd.index_pSet2_beep_origin = index;
                    }
                }
                else if (isBeepOrFault)
                {
                    if (gdRef.isConnector)
                    {
                        // No highlight here
                    }
                    else
                    {
                        if (gdRef.isHead) gdRef.gd.index_pSet1_beep_or_fault = index;
                        else gdRef.gd.index_pSet2_beep_or_fault = index;
                    }
                }
                else
                {
                    if (gdRef.isConnector)
                    {
                        if (gdRef.isHead) gdRef.gd.index_pSetConnectorPin1 = index;
                        else gdRef.gd.index_pSetConnectorPin2 = index;
                    }
                    else
                    {
                        if (gdRef.isHead) gdRef.gd.index_pSet1 = index;
                        else gdRef.gd.index_pSet2 = index;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the fitting batch for rendering circuit lines.
        /// <para>
        /// WARNING: Use this only for special cases where only a few batches
        /// are required. Use the alternative indexing system for more frequent
        /// accesses to get better performance.
        /// </para>
        /// </summary>
        /// <param name="color">The color of the line.</param>
        /// <param name="lineType">The type of the line.</param>
        /// <param name="delayed">Whether the line should be shown delayed.</param>
        /// <param name="beeping">Whether the line should beep.</param>
        /// <param name="animated">Whether  this batch is updated manually each frame.</param>
        /// <param name="movementOffset">The offset for the joint movement. Set to
        /// <c>Vector2.zero</c> if no joint movement is present.</param>
        /// <param name="activeState">The state of the simulator for which the
        /// line should be drawn.</param>
        /// <returns>A render batch that renders all lines with the given properties.</returns>
        private RendererCircuits_RenderBatch GetBatch_CircuitLine(Color color, RendererCircuits_RenderBatch.PropertyBlockData.LineType lineType, bool delayed, bool beeping, bool animated, Vector2 movementOffset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState activeState = RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActiveOrPaused)
        {
            int colorIdx = GetColorIndex(color);
            int arrIdx = GetArrayIndex(lineType == RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, beeping, beeping ? activeState == RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActive : true);
            RendererCircuits_RenderBatch[] arr;
            if (delayed)
            {
                arr = delayedCircuitBatches[colorIdx];
            }
            else
            {
                arr = GetNonDelayedArray(colorIdx, movementOffset);
            }
            return GetArrayBatch(arr, arrIdx, color, movementOffset, delayed);
        }

        /// <summary>
        /// Returns the fitting batch for rendering pins.
        /// </summary>
        /// <param name="color">The color of the pin.</param>
        /// <param name="delayed">Whether the pin should be shown delayed.</param>
        /// <param name="singleton">Whether the pin is a singleton highlight.</param>
        /// <param name="beeping">Whether the pin should beep.</param>
        /// <param name="beepOrigin">Whether this is just a beep origin
        /// highlight.</param>
        /// <param name="connector">Whether this is a circuit line connector.</param>
        /// <param name="faulty">Whether the pin is faulty. Must not be true at
        /// the same time as <paramref name="beeping"/>.</param>
        /// <param name="movementOffset">The offset for the joint movement. Set to
        /// <c>Vector2.zero</c> if no joint movement is present.</param>
        /// <returns>A render batch that renders all pins with the given properties.</returns>
        private RendererCircuitPins_RenderBatch GetBatch_Pin(Color color, bool delayed, bool singleton, bool beeping, bool beepOrigin, bool faulty, bool connector, Vector2 movementOffset)
        {
            return GetBatch_Pin(new RendererCircuitPins_RenderBatch.PropertyBlockData(color, delayed, singleton, beeping, beepOrigin, faulty, connector, movementOffset));
        }

        /// <summary>
        /// Returns the fitting batch for rendering pins.
        /// </summary>
        /// <param name="properties">The properties of the batch.</param>
        /// <returns>A render batch that renders all pins with the given properties.</returns>
        private RendererCircuitPins_RenderBatch GetBatch_Pin(RendererCircuitPins_RenderBatch.PropertyBlockData propertyBlockData)
        {
            RendererCircuitPins_RenderBatch batch;
            if (propertiesToPinRenderBatchMap.TryGetValue(propertyBlockData, out batch) == false)
            {
                // Batch does not exist
                // Create Batch
                batch = new RendererCircuitPins_RenderBatch(propertyBlockData);
                propertiesToPinRenderBatchMap.Add(propertyBlockData, batch);
            }
            return batch;
        }

        /// <summary>
        /// Adds a single bond to the system.
        /// </summary>
        /// <param name="bondState">Graphical information belonging to the bond.</param>
        /// <param name="addToBondData">Whether this is a new bond that should be
        /// stored (set to <c>false</c> if this is just a refresh).</param>
        public void AddBond(ParticleBondGraphicState bondState, bool addToBondData = true)
        {
            // Store Data
            if(addToBondData) bondData.Add(bondState);

            // Convert Grid to World Space
            Vector2 prevBondPosWorld1 = AmoebotFunctions.GridToWorldPositionVector2(bondState.prevBondPos1);
            Vector2 prevBondPosWorld2 = AmoebotFunctions.GridToWorldPositionVector2(bondState.prevBondPos2);
            Vector2 curBondPosWorld1 = AmoebotFunctions.GridToWorldPositionVector2(bondState.curBondPos1);
            Vector2 curBondPosWorld2 = AmoebotFunctions.GridToWorldPositionVector2(bondState.curBondPos2);
            // Hexagonal
            RendererCircuits_RenderBatch batch = bondState.IsAnimated() ? renderBatch_bondsHexagonal_animated : renderBatch_bondsHexagonal_static;
            if (bondState.IsAnimated()) batch.AddManuallyUpdatedLine(prevBondPosWorld1, prevBondPosWorld2, curBondPosWorld1, curBondPosWorld2);
            else batch.AddLine(curBondPosWorld1, curBondPosWorld2);
            // Circular
            batch = bondState.IsAnimated() ? renderBatch_bondsCircular_animated : renderBatch_bondsCircular_static;
            if (bondState.IsAnimated()) batch.AddManuallyUpdatedLine(prevBondPosWorld1, prevBondPosWorld2, curBondPosWorld1, curBondPosWorld2);
            else batch.AddLine(curBondPosWorld1, curBondPosWorld2);
        }

        /// <summary>
        /// Finds the render batch list index for the given color.
        /// If there is no entry for this color yet, a new entry is created.
        /// <para>
        /// Use <see cref="GetArrayBatch(RendererCircuits_RenderBatch[], int, Color, Vector2, bool)"/>
        /// to get an actual render batch from an array obtained using this index..
        /// </para>
        /// </summary>
        /// <param name="c">The color whose batch list index should be found.</param>
        /// <returns>The list index of the batch array or dictionary belonging
        /// to color <paramref name="c"/>.</returns>
        private int GetColorIndex(Color c)
        {
            int idx;
            if (!colorIndices.TryGetValue(c, out idx))
            {
                // Don't know this color yet, add entry
                idx = colorIndices.Count;
                colorIndices[c] = idx;
                // Also extend data structures storing the batch references
                delayedCircuitBatches.Add(new RendererCircuits_RenderBatch[6]);
                nonDelyedCircuitBatches.Add(new Dictionary<Vector2, RendererCircuits_RenderBatch[]>());
            }
            return idx;
        }

        /// <summary>
        /// Calculates the array index of a render batch for circuit lines
        /// based on the line's type, beeping and activeState flags.
        /// </summary>
        /// <param name="isInternal">Whether the circuit line is internal
        /// (as opposed to being an external line between particles).</param>
        /// <param name="beeping">Whether the line belongs to a circuit that
        /// is currently beeping.</param>
        /// <param name="active">If <paramref name="beeping"/> is <c>true</c>,
        /// this differentiates between the animated flashing part of the line
        /// (<c>true</c>) and the static part with the white center that is
        /// shown while the simulation is paused (<c>false</c>).</param>
        /// <returns>The index of the desired batch in its array of size 6.</returns>
        private int GetArrayIndex(bool isInternal, bool beeping, bool active = true)
        {
            if (isInternal)
            {
                if (beeping)
                    return active ? internal_beeping_active : internal_beeping_paused;
                else
                    return internal_notBeeping;
            }
            else
            {
                if (beeping)
                    return active ? external_beeping_active : external_beeping_paused;
                else
                    return external_notBeeping;
            }
        }

        /// <summary>
        /// Finds the render batch array in the list of batches for non-delayed
        /// lines at the given index and for the given animation offset. If no
        /// entry for the given entry exists yet, a new array is added to the
        /// dictionary.
        /// </summary>
        /// <param name="colorIdx">The list index obtained from
        /// <see cref="GetColorIndex(Color)"/>.</param>
        /// <param name="offset">The animation offset vector of the
        /// desired render batch.</param>
        /// <returns>The array of size 6 that stores the render batches for
        /// lines with the specified color and animation offset and which
        /// are not delayed.
        /// <para>
        /// Use <see cref="GetArrayBatch(RendererCircuits_RenderBatch[], int, Color, Vector2, bool)"/>
        /// to get an actual render batch from the array.
        /// </para></returns>
        private RendererCircuits_RenderBatch[] GetNonDelayedArray(int colorIdx, Vector2 offset)
        {
            Dictionary<Vector2, RendererCircuits_RenderBatch[]> dict = nonDelyedCircuitBatches[colorIdx];
            RendererCircuits_RenderBatch[] arr;
            if (dict.TryGetValue(offset, out arr))
                return arr;
            arr = new RendererCircuits_RenderBatch[6];
            dict[offset] = arr;
            return arr;
        }

        /// <summary>
        /// Gets a render batch from the given array, at the given
        /// index and for the given parameters. If the array entry at
        /// index <paramref name="idx"/> is <c>null</c>, a new render
        /// batch is created using the given parameters and added to
        /// the list of circuit line batches.
        /// </summary>
        /// <param name="arr">The array of size 6 from which to get the render batch.</param>
        /// <param name="idx">The array index obtained from <see cref="GetArrayIndex(bool, bool, bool)"/>.</param>
        /// <param name="c">The color of the desired render batch (must be the one used
        /// to obtain the list index of the array or the dictionary).</param>
        /// <param name="offset">The animation offset of the desired render batch.
        /// Set to <c>Vector2.zero</c> if the batch is delayed anyway, otherwise it must be
        /// the offset that was given to <see cref="GetNonDelayedArray(int, Vector2)"/>
        /// to obtain the array <paramref name="arr"/>.</param>
        /// <param name="delayed">Whether the given array belongs to the delayed list or
        /// the non-delayed list of dictionaries.</param>
        /// <returns>The desired render batch from the given array.</returns>
        private RendererCircuits_RenderBatch GetArrayBatch(RendererCircuits_RenderBatch[] arr, int idx, Color c, Vector2 offset, bool delayed = true)
        {
            RendererCircuits_RenderBatch batch = arr[idx];
            if (batch != null)
                return batch;
            switch (idx)
            {
                case internal_notBeeping:
                    batch = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(c, RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, false, false, offset));
                    break;
                case internal_beeping_active:
                    batch = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(c, RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, true, false, offset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActive));
                    break;
                case internal_beeping_paused:
                    batch = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(c, RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, true, false, offset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimPaused));
                    break;
                case external_notBeeping:
                    batch = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(c, RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine, delayed, false, false, offset));
                    break;
                case external_beeping_active:
                    batch = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(c, RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine, delayed, true, false, offset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActive));
                    break;
                case external_beeping_paused:
                    batch = new RendererCircuits_RenderBatch(new RendererCircuits_RenderBatch.PropertyBlockData(c, RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine, delayed, true, false, offset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimPaused));
                    break;
            }
            arr[idx] = batch;
            lineBatches.Add(batch);
            return batch;
        }

        /// <summary>
        /// Convenience method getting the non-beeping and the two
        /// beeping render batches from the given array. This is useful
        /// because for beeping circuits, each connection is made up of
        /// 3 lines that have to be added to different batches.
        /// </summary>
        /// <param name="arr">The array of size 6 from which to get
        /// the desired batches.</param>
        /// <param name="isInternal">Whether the internal or the external
        /// batches should be returned.</param>
        /// <param name="beeping">Whether or not the circuit to which the
        /// lines belong is currently beeping.</param>
        /// <param name="c">The color of the lines.</param>
        /// <param name="movementOffset">The movement offset of the lines.</param>
        /// <param name="delayed">Whether the given array <paramref name="arr"/>
        /// belongs to the delayed or the non-delayed batch list.</param>
        /// <returns>A render batch array containing the non-beeping batch
        /// at index <c>0</c> and the beeping active and beeping paused batches
        /// at positions <c>1</c> and <c>2</c> respectively. If
        /// <paramref name="beeping"/> is <c>false</c>, the latter two entries
        /// are both <c>null</c>.</returns>
        private RendererCircuits_RenderBatch[] GetBatchGroup(RendererCircuits_RenderBatch[] arr, bool isInternal, bool beeping, Color c, Vector2 movementOffset, bool delayed)
        {
            RendererCircuits_RenderBatch[] batches = new RendererCircuits_RenderBatch[3];
            batches[0] = GetArrayBatch(arr, isInternal ? internal_notBeeping : external_notBeeping, c, movementOffset, delayed);
            if (beeping)
            {
                batches[1] = GetArrayBatch(arr, isInternal ? internal_beeping_active : external_beeping_active, c, movementOffset, delayed);
                batches[2] = GetArrayBatch(arr, isInternal ? internal_beeping_paused : external_beeping_paused, c, movementOffset, delayed);
            }

            return batches;
        }



        /// <summary>
        /// Reinitializes all batches. Helpful in case settings have been changed.
        /// </summary>
        public void ReinitBatches()
        {
            foreach (var batch in bondBatches)
            {
                batch.Init();
            }
            foreach (var batch in lineBatches)
            {
                batch.Init();
            }
            foreach (var batch in propertiesToPinRenderBatchMap.Values)
            {
                batch.Init();
            }
        }

        /// <summary>
        /// Renders everything stored in the render batches.
        /// </summary>
        /// <param name="type">The visualization mode that should
        /// be used to render the system. Determines what shape
        /// the particles have and whether circuits should be drawn.</param>
        public void Render(ViewType type)
        {
            bool firstRenderFrame = isRenderingActive == false;
            if (type == ViewType.Hexagonal || type == ViewType.HexagonalCirc)
            {
                renderBatch_bondsHexagonal_static.Render(type, firstRenderFrame);
                renderBatch_bondsHexagonal_animated.Render(type, firstRenderFrame);
            }
            else
            {
                renderBatch_bondsCircular_static.Render(type, firstRenderFrame);
                renderBatch_bondsCircular_animated.Render(type, firstRenderFrame);
            }
            foreach (var batch in lineBatches)
            {
                batch.Render(type, firstRenderFrame);
            }
            foreach (var batch in propertiesToPinRenderBatchMap.Values)
            {
                batch.Render(type, firstRenderFrame);
            }
            isRenderingActive = true;
        }

        /// <summary>
        /// Clears or nullifies the matrices to reset the data structures.
        /// </summary>
        /// <param name="keepCircuitData">Whether circuit data should be kept in the system.</param>
        /// <param name="keepBondData">Whether bond data should be kept in the system.</param>
        public void Clear(bool keepCircuitData = false, bool keepBondData = false)
        {
            foreach (var batch in bondBatches)
            {
                batch.ClearMatrices();
            }
            foreach (var batch in lineBatches)
            {
                batch.ClearMatrices();
            }
            foreach (var batch in propertiesToPinRenderBatchMap.Values)
            {
                batch.ClearMatrices();
            }
            
            if (keepCircuitData == false)
            {
                foreach (var data in circuitDataMap.Values)
                {
                    ParticlePinGraphicState.PoolRelease(data.state);
                }
                circuitDataMap.Clear();
            }
            else
            {
                // We need to clear the indices
                foreach (var data in circuitDataMap.Values)
                {
                    foreach (var pSet in data.state.partitionSets)
                    {
                        pSet.graphicalData.Clear(true);
                    }
                }
            }
            if(keepBondData == false) bondData.Clear();
            isRenderingActive = false;
        }


        // Helper methods

        /// <summary>
        /// Calculates the global position of a specific pin belonging
        /// to a particle.
        /// </summary>
        /// <param name="gridPosParticle">The grid coordinates of the particle
        /// that contains the pin.</param>
        /// <param name="pinDef">The pin whose position should be calculated.</param>
        /// <param name="pinsPerSide">The number of pins on each edge of the particle.</param>
        /// <returns>The absolute world coordinates of the pin.</returns>
        private Vector2 CalculateGlobalPinPosition(Vector2Int gridPosParticle, ParticlePinGraphicState.PinDef pinDef, int pinsPerSide)
        {
            Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(gridPosParticle);
            Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(pinDef, pinsPerSide, RenderSystem.setting_viewType);
            return posParticle + relPinPos;
        }

        /// <summary>
        /// Calculates the global position of a partition set handle
        /// inside of a particle by distributing the sets along a
        /// rotated line.
        /// </summary>
        /// <param name="gridPosParticle">The grid coordinates of the particle that
        /// contains the partition set.</param>
        /// <param name="partitionSetID">The index of the partition set in
        /// the considered part of the particle (head/tail).</param>
        /// <param name="amountOfPartitionSetsAtNode">The number of partition
        /// sets that should be distributed in this part of the particle.</param>
        /// <param name="rotationDegrees">The rotation of the line in degrees.
        /// 0 means the line is vertical, increasing degrees rotate the line
        /// counter-clockwise.</param>
        /// <param name="invertPositions">Whether the partition set indices
        /// should increase from bottom to top instead of top to bottom.</param>
        /// <returns>The absolute world coordinates of the partition set hanlde.</returns>
        private Vector2 CalculateGlobalPartitionSetPinPosition(Vector2Int gridPosParticle, int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees, bool invertPositions)
        {
            Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(gridPosParticle);
            return posParticle + CalculateRelativePartitionSetPinPosition(partitionSetID, amountOfPartitionSetsAtNode, rotationDegrees, invertPositions);
        }

        /// <summary>
        /// Calculates the global position of a circuit connector
        /// inside of an expanded particle by distributing the sets
        /// along a rotated and offset line.
        /// </summary>
        /// <param name="gridPosParticle">The grid coordinates of the particle
        /// containing the connector.</param>
        /// <param name="partitionSetID">The index of the partition set in
        /// the considered part of the particle (head/tail).</param>
        /// <param name="amountOfPartitionSetsAtNode">The number of partition
        /// set connectors that should be distributed in this part of the particle.</param>
        /// <param name="rotationDegrees">The rotation of the line in degrees.
        /// 0 means the line is vertical, increasing degrees rotate the line
        /// counter-clockwise.</param>
        /// <param name="invertPositions">Whether the partition set indices
        /// should increase from bottom to top instead of top to bottom.</param>
        /// <returns>The absolute world coordinates of the circuit connector.</returns>
        private Vector2 CalculateGlobalExpandedPartitionSetCenterNodePosition(Vector2Int gridPosParticle, int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees, bool invertPositions)
        {
            Vector2 posParticle = AmoebotFunctions.GridToWorldPositionVector2(gridPosParticle);
            return posParticle + CalculateRelativeExpandedPartitionSetCenterNodePosition(partitionSetID, amountOfPartitionSetsAtNode, rotationDegrees, invertPositions);
        }

        /// <summary>
        /// Calculates the global position of the center of a
        /// pin connection between two particles.
        /// </summary>
        /// <param name="gridPosParticle">The grid coordinates of
        /// one of the particles.</param>
        /// <param name="pinDef">The pin belonging to the first particle.</param>
        /// <param name="pinsPerSide">The number of pins each particle
        /// has on each edge.</param>
        /// <returns>The absolute world coordinates of the position exactly
        /// between the specified pin and its neighboring counterpart.</returns>
        private Vector2 CalculateGlobalOuterPinLineCenterPosition(Vector2Int gridPosParticle, ParticlePinGraphicState.PinDef pinDef, int pinsPerSide)
        {
            Vector2 pinPos = CalculateGlobalPinPosition(gridPosParticle, pinDef, pinsPerSide);

            // Calculate neighbor pin position
            ParticlePinGraphicState.PinDef pinDefNeighbor = new ParticlePinGraphicState.PinDef((pinDef.globalDir + 3) % 6, pinsPerSide - 1 - pinDef.dirID, pinDef.isHead);
            Vector2Int gridPosNeighbor = AmoebotFunctions.GetNeighborPosition(gridPosParticle, pinDef.globalDir);
            Vector2 pinPosNeighbor = CalculateGlobalPinPosition(gridPosNeighbor, pinDefNeighbor, pinsPerSide);
            return pinPos + ((pinPosNeighbor - pinPos) / 2f);
        }

        /// <summary>
        /// Calculates the relative position of a partition set handle
        /// inside of a particle by distributing the sets along a
        /// rotated line.
        /// </summary>
        /// <param name="partitionSetID">The index of the partition set in
        /// the considered part of the particle (head/tail).</param>
        /// <param name="amountOfPartitionSetsAtNode">The number of partition
        /// sets that should be distributed in this part of the particle.</param>
        /// <param name="rotationDegrees">The rotation of the line in degrees.
        /// 0 means the line is vertical, increasing degrees rotate the line
        /// counter-clockwise.</param>
        /// <param name="invertPositions">Whether the partition set indices
        /// should increase from bottom to top instead of top to bottom.</param>
        /// <returns>The world coordinates of the partition set hanlde relative
        /// to the particle center.</returns>
        private Vector2 CalculateRelativePartitionSetPinPosition(int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees, bool invertPositions)
        {
            if (amountOfPartitionSetsAtNode == 1) return Vector2.zero;
            else
            {
                float lineLength;
                // Use a longer line if there are more partition sets
                switch (amountOfPartitionSetsAtNode)
                {
                    case 2:
                        lineLength = RenderSystem.global_particleScale * 0.5f;
                        break;
                    case 3:
                        lineLength = RenderSystem.global_particleScale * 0.66f;
                        break;
                    default:
                        lineLength = RenderSystem.global_particleScale * 0.8f;
                        break;
                }
                float height;
                if (invertPositions)
                    height = (lineLength / 2f) - (amountOfPartitionSetsAtNode - partitionSetID - 1) * (lineLength / (amountOfPartitionSetsAtNode - 1));
                else
                    height = (lineLength / 2f) - partitionSetID * (lineLength / (amountOfPartitionSetsAtNode - 1));
                Vector2 position = new Vector2(0f, height);
                position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
                return position;
            }
        }

        /// <summary>
        /// Calculates the relative position of a circuit connector
        /// inside of an expanded particle by distributing the sets
        /// along a rotated and offset line.
        /// </summary>
        /// <param name="partitionSetID">The index of the partition set in
        /// the considered part of the particle (head/tail).</param>
        /// <param name="amountOfPartitionSetsAtNode">The number of partition
        /// set connectors that should be distributed in this part of the particle.</param>
        /// <param name="rotationDegrees">The rotation of the line in degrees.
        /// 0 means the line is vertical, increasing degrees rotate the line
        /// counter-clockwise.</param>
        /// <param name="invertPositions">Whether the partition set indices
        /// should increase from bottom to top instead of top to bottom.</param>
        /// <returns>The world coordinates of the circuit connector relative
        /// to the particle center.</returns>
        private Vector2 CalculateRelativeExpandedPartitionSetCenterNodePosition(int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees, bool invertPositions)
        {
            float relXPos = RenderSystem.global_particleScale * 0.5f * 0.85f;

            if (amountOfPartitionSetsAtNode == 1) return Quaternion.Euler(0f, 0f, rotationDegrees) * new Vector2(relXPos, 0f);
            else
            {
                float lineLength;
                switch (amountOfPartitionSetsAtNode)
                {
                    case 2:
                        lineLength = RenderSystem.global_particleScale * 0.3f;
                        break;
                    case 3:
                        lineLength = RenderSystem.global_particleScale * 0.4f;
                        break;
                    default:
                        lineLength = RenderSystem.global_particleScale * 0.4f;
                        break;
                }
                float height;
                if (invertPositions)
                    height = (lineLength / 2f) - (amountOfPartitionSetsAtNode - partitionSetID - 1) * (lineLength / (amountOfPartitionSetsAtNode - 1));
                else
                    height = (lineLength / 2f) - partitionSetID * (lineLength / (amountOfPartitionSetsAtNode - 1));
                Vector2 position = new Vector2(relXPos, height);
                position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
                return position;
            }
        }





        // Functions __________________________________

        /// <summary>
        /// Gets the circuit data belonging to the given particle.
        /// </summary>
        /// <param name="p">The particle whose circuit data to return.</param>
        /// <returns>The circuit data belonging to <paramref name="p"/>.</returns>
        public ParticleCircuitData GetParticleCircuitData(ParticleGraphicsAdapterImpl p)
        {
            ParticleCircuitData d;
            circuitDataMap.TryGetValue(p, out d);
            if (d.particle == null) Log.Error("RendererCircuits_Instance: GetParticleCircuitData for particle " + p.ToString() + " is not assigned!");
            return d;
        }


    }

    /// <summary>
    /// The various placement types for partition sets inside a particle.
    /// </summary>
    public enum PartitionSetViewType
    {
        /// <summary>
        /// Standard view type: Partition sets oriented in a vertical line.
        /// </summary>
        Line,
        /// <summary>
        /// Automatic view type: Partition sets oriented on a circle, automatically
        /// ordered by the average positions of their pins.
        /// </summary>
        Auto,
        /// <summary>
        /// Automatic view type: Partition sets oriented inside of a circle,
        /// automatically ordered by the average positions of their pins.
        /// </summary>
        Auto_2DCircle,
        /// <summary>
        /// Default view type: Prioritization of code positioning. If none has been
        /// set, we use auto positioning.
        /// </summary>
        CodeOverride,
    }

}
