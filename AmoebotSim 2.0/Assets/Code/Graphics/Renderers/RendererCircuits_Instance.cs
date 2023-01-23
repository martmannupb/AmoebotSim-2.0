using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// The circuit and bond renderer instance that handles all the data that is added in a round.
    /// Has render batches for circuits (colorized lines) and pins (colorized dots) grouped by data with the same properties (like type, color, etc.).
    /// These batches are all rendered when the draw loop is called.
    /// </summary>
    public class RendererCircuits_Instance
    {

        public Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch>();
        public Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch> propertiesToPinRenderBatchMap = new Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch>();

        // Data
        private Dictionary<ParticleGraphicsAdapterImpl, ParticleCircuitData> circuitData = new Dictionary<ParticleGraphicsAdapterImpl, ParticleCircuitData>();
        // Flags
        public bool isRenderingActive = false;
        // Temporary
        private bool[] globalDirLineSet1 = new bool[] { false, false, false, false, false, false };
        private bool[] globalDirLineSet2 = new bool[] { false, false, false, false, false, false };
        private List<float> degreeList1 = new List<float>(16);
        private List<float> degreeList2 = new List<float>(16);
        private SortedList<float, ParticlePinGraphicState.PSetData> pSetSortingList = new SortedList<float, ParticlePinGraphicState.PSetData>();

        public struct ParticleCircuitData
        {
            public RendererCircuits_Instance instance;
            public ParticleGraphicsAdapterImpl particle;
            public ParticlePinGraphicState state;
            public ParticleGraphicsAdapterImpl.PositionSnap snap;

            public ParticleCircuitData(RendererCircuits_Instance instance, ParticleGraphicsAdapterImpl particle, ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap)
            {
                this.instance = instance;
                this.particle = particle;
                this.state = state;
                this.snap = snap;
            }

            public struct PSetInnerPinRef
            {
                public enum PinType
                {
                    None, PSet1, PSet2, PConnector1, PConnector2
                }

                public ParticlePinGraphicState.PSetData pSet;
                public PinType pinType;
                public Vector2 pinPos;

                public PSetInnerPinRef(ParticlePinGraphicState.PSetData pSet, PinType pinType, Vector2 pinPos)
                {
                    this.pSet = pSet;
                    this.pinType = pinType;
                    this.pinPos = pinPos;
                }
            }

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

            public void UpdatePSetOrConnectorPinPosition(PSetInnerPinRef innerPin, Vector2 worldPos)
            {
                RendererCircuitPins_RenderBatch batch_pins;
                RendererCircuits_RenderBatch batch_lines;
                ParticlePinGraphicState.PSetData.GraphicalData gd = innerPin.pSet.graphicalData;
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
                        // Lines
                        for (int i = 0; i < gd.pSet1_pins.Count; i++)
                        {
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            Vector2 pinPos = instance.CalculateGlobalPinPosition(snap.position1, gd.pSet1_pins[i], state.pinsPerSide);
                            batch_lines.UpdateLine(worldPos, pinPos, gd.index_lines1[i]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, pinPos, gd.index_lines1_beep[i]);
                            }
                        }
                        if(state.isExpanded)
                        {
                            // Connector
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            batch_lines.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lines1[gd.index_lines1.Count - 1]);
                            if(innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lines1_beep[gd.index_lines1_beep.Count - 1]);
                            }
                        }

                        // Beep Origin
                        if (innerPin.pSet.beepOrigin)
                        {
                            // Pin
                            batch_pins = instance.GetBatch_Pin(gd.properties_pin_beep);
                            batch_pins.UpdatePin(worldPos, false, gd.index_pSet1_beep);
                        }
                        break;
                    case PSetInnerPinRef.PinType.PSet2:
                        // Lines to Pins (and Connector2, if expanded)
                        // Save position
                        gd.active_position2 = worldPos;
                        // Pin
                        batch_pins = instance.GetBatch_Pin(gd.properties_pin);
                        batch_pins.UpdatePin(worldPos, false, gd.index_pSet2);
                        // Lines
                        for (int i = 0; i < gd.pSet2_pins.Count; i++)
                        {
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            Vector2 pinPos = instance.CalculateGlobalPinPosition(snap.position2, gd.pSet2_pins[i], state.pinsPerSide);
                            batch_lines.UpdateLine(worldPos, pinPos, gd.index_lines2[i]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, pinPos, gd.index_lines2_beep[i]);
                            }
                        }
                        if (state.isExpanded) // should be true
                        {
                            // Connector
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            batch_lines.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lines2[gd.index_lines2.Count - 1]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lines2_beep[gd.index_lines2_beep.Count - 1]);
                            }
                        }
                        else Log.Error("UpdatePSetOrConnectorPinPosition: Trying to edit a partition set 2 for a particle that is contracted. This is not possible.");

                        // Beeps
                        if (innerPin.pSet.beepOrigin)
                        {
                            // Pin
                            batch_pins = instance.GetBatch_Pin(gd.properties_pin_beep);
                            batch_pins.UpdatePin(worldPos, false, gd.index_pSet2_beep);
                        }
                        break;
                    case PSetInnerPinRef.PinType.PConnector1:
                        // Lines to Connector2 and PSet1
                        if(state.isExpanded)
                        {
                            // Save position
                            gd.active_connector_position1 = worldPos;
                            // Connector
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            batch_lines.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lines1[gd.index_lines1.Count - 1]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lines1_beep[gd.index_lines1_beep.Count - 1]);
                            }
                            // Other connector
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            batch_lines.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lineConnector);
                            if(innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lineConnector_beep);
                            }
                        }
                        else Log.Error("UpdatePSetOrConnectorPinPosition: Trying to edit connector 1 for a particle that is contracted. This is not possible.");
                        break;
                    case PSetInnerPinRef.PinType.PConnector2:
                        // Lines to Connector1 and PSet2
                        if (state.isExpanded)
                        {
                            // Save position
                            gd.active_connector_position2 = worldPos;
                            // Connector
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            batch_lines.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lines2[gd.index_lines2.Count - 1]);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, gd.active_connector_position2, gd.index_lines2_beep[gd.index_lines2_beep.Count - 1]);
                            }
                            // Other connector
                            batch_lines = instance.GetBatch_Line(gd.properties_line);
                            batch_lines.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lineConnector);
                            if (innerPin.pSet.beepsThisRound)
                            {
                                batch_lines = instance.GetBatch_Line(gd.properties_line_beep);
                                batch_lines.UpdateLine(worldPos, gd.active_connector_position1, gd.index_lineConnector_beep);
                            }
                        }
                        else Log.Error("UpdatePSetOrConnectorPinPosition: Trying to edit connector 1 for a particle that is contracted. This is not possible.");
                        break;
                    default:
                        break;
                }
            }
            
        }

        private void CalculatePartitionSetPositions(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, PartitionSetViewType pSetViewType)
        {
            // Precalculations
            foreach (var pset in state.partitionSets) pset.PrecalculatePinNumbersAndStoreInGD();

            // Partition Set Calculation
            if (state.isExpanded == false)
            {
                // 1. Contracted
                degreeList1.Clear();
                PartitionSetViewType particleViewType = pSetViewType;
                if (particleViewType == PartitionSetViewType.Auto)
                {
                    for (int i = 0; i < state.partitionSets.Count; i++)
                    {
                        ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                        // Calc average partition set position
                        Vector2 relPos = Vector2.zero;
                        foreach (var pinDef in pSet.pins)
                        {
                            relPos += AmoebotFunctions.CalculateRelativePinPosition(pinDef, state.pinsPerSide, RenderSystem.global_particleScale, RenderSystem.setting_viewType);
                        }
                        relPos /= (float)pSet.pins.Count;
                        if (relPos == Vector2.zero) degreeList1.Add(0f);
                        else
                        {
                            // Convert relPos to degree
                            float degree = ((Mathf.Atan2(relPos.y, relPos.x) * Mathf.Rad2Deg - 90f) + 360f) % 360f;
                            degreeList1.Add(degree);
                        }

                    }
                    CircleDistribution.DistributePointsOnCircle(degreeList1, Mathf.Min(0.8f * (360f / degreeList1.Count), 60f));
                }
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    switch (particleViewType)
                    {
                        case PartitionSetViewType.Default:
                            // Default
                            pSet.graphicalData.active_position1 = CalculateGlobalPartitionSetPinPosition(snap.position1, i, state.partitionSets.Count, 0f, false);
                            break;
                        case PartitionSetViewType.Auto:
                            // Auto
                            // Convert degree to coordinate
                            float degree = degreeList1[i];
                            Vector2 localPinPos = state.partitionSets.Count == 1 ? Vector2.zero : Engine.Library.DegreeConstants.DegreeToCoordinate(degree, RenderSystem.global_particleScale * 0.3f, 90f);
                            // Calc partition set position on the circle
                            Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.position1);
                            // Save position
                            pSet.graphicalData.active_position1 = posParticle + localPinPos;
                            break;
                        default:
                            break;
                    }
                }
            }

            // 2. Expanded
            if(state.isExpanded)
            {
                degreeList1.Clear();
                degreeList2.Clear();
                PartitionSetViewType particleViewType = pSetViewType;
                if (particleViewType == PartitionSetViewType.Auto)
                {
                    // Auto Placement 1. Iteration (PSet Degrees)
                    for (int i = 0; i < state.partitionSets.Count; i++)
                    {
                        ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                        // 1. Head Pins
                        if(pSet.graphicalData.hasPinsInHead)
                        {
                            // Calc average partition set position 1
                            Vector2 relPos = Vector2.zero;
                            int virtualPinCount = 0;
                            foreach (var pinDef in pSet.pins)
                            {
                                if(pinDef.isHead)
                                {
                                    relPos += AmoebotFunctions.CalculateRelativePinPosition(pinDef, state.pinsPerSide, RenderSystem.global_particleScale, RenderSystem.setting_viewType);
                                    virtualPinCount++;
                                }
                            }
                            if(pSet.graphicalData.hasPinsInTail)
                            {
                                // Add one additional virtual position in direction of the center of the expanded particle
                                relPos += CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position1, 1, 1, 60f * state.neighbor1ToNeighbor2Direction, false);
                                virtualPinCount++;
                            }
                            relPos /= (float)virtualPinCount;
                            if (relPos == Vector2.zero) degreeList1.Add(0f);
                            else
                            {
                                // Convert relPos to degree
                                float degree = ((Mathf.Atan2(relPos.y, relPos.x) * Mathf.Rad2Deg - 90f) + 360f) % 360f;
                                degreeList1.Add(degree);
                            }
                        }
                        // 2. Tail Pins
                        if (pSet.graphicalData.hasPinsInTail)
                        {
                            // Calc average partition set position 2
                            Vector2 relPos = Vector2.zero;
                            int virtualPinCount = 0;
                            foreach (var pinDef in pSet.pins)
                            {
                                if (pinDef.isHead == false)
                                {
                                    relPos += AmoebotFunctions.CalculateRelativePinPosition(pinDef, state.pinsPerSide, RenderSystem.global_particleScale, RenderSystem.setting_viewType);
                                    virtualPinCount++;
                                }
                            }
                            if (pSet.graphicalData.hasPinsInHead)
                            {
                                // Add one additional virtual position in direction of the center of the expanded particle
                                relPos += CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position2, 1, 1, 60f * ((state.neighbor1ToNeighbor2Direction + 3) % 6), true);
                                virtualPinCount++;
                            }
                            relPos /= (float)virtualPinCount;
                            if (relPos == Vector2.zero) degreeList2.Add(0f);
                            else
                            {
                                // Convert relPos to degree
                                float degree = ((Mathf.Atan2(relPos.y, relPos.x) * Mathf.Rad2Deg - 90f) + 360f) % 360f;
                                degreeList2.Add(degree);
                            }
                        }


                    }
                    CircleDistribution.DistributePointsOnCircle(degreeList1, Mathf.Min(0.8f * (360f / degreeList1.Count), 60f));
                    CircleDistribution.DistributePointsOnCircle(degreeList2, Mathf.Min(0.8f * (360f / degreeList2.Count), 60f));
                }
                int counter1 = 0;
                int counter2 = 0;
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    switch (particleViewType)
                    {
                        case PartitionSetViewType.Default:
                            // Default
                            pSet.graphicalData.active_position1 = CalculateGlobalPartitionSetPinPosition(snap.position1, i, state.partitionSets.Count, 0f, false);
                            float rot1 = 60f * state.neighbor1ToNeighbor2Direction;
                            float rot2 = 60f * ((state.neighbor1ToNeighbor2Direction + 3) % 6);
                            pSet.graphicalData.active_position1 = CalculateGlobalPartitionSetPinPosition(snap.position1, i, state.partitionSets.Count, rot1, false);
                            pSet.graphicalData.active_position2 = CalculateGlobalPartitionSetPinPosition(snap.position2, i, state.partitionSets.Count, rot2, true);
                            pSet.graphicalData.active_connector_position1 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position1, i, state.partitionSets.Count, rot1, false);
                            pSet.graphicalData.active_connector_position2 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position2, i, state.partitionSets.Count, rot2, true);
                            break;
                        case PartitionSetViewType.Auto:
                            // Auto
                            // Auto Placement 2. Iteration (PSet Positions)
                            if (pSet.graphicalData.hasPinsInHead)
                            {
                                // Convert degree to coordinate
                                float degree = degreeList1[counter1];
                                counter1++;
                                Vector2 localPinPos = degreeList1.Count == 1 ? Vector2.zero : Engine.Library.DegreeConstants.DegreeToCoordinate(degree, RenderSystem.global_particleScale * 0.3f, 90f);
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.position1);
                                // Save position
                                pSet.graphicalData.active_position1 = posParticle + localPinPos;
                            }
                            if (pSet.graphicalData.hasPinsInTail)
                            {
                                // Convert degree to coordinate
                                float degree = degreeList2[counter2];
                                counter2++;
                                Vector2 localPinPos = degreeList2.Count == 1 ? Vector2.zero : Engine.Library.DegreeConstants.DegreeToCoordinate(degree, RenderSystem.global_particleScale * 0.3f, 90f);
                                // Calc partition set position on the circle
                                Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.position2);
                                // Save position
                                pSet.graphicalData.active_position2 = posParticle + localPinPos;
                            }
                            break;
                        default:
                            break;
                    }
                }
                // Pin Connector Placement
                pSetSortingList.Clear();
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    if(pSet.HasPinsInHeadAndTail(false))
                    {
                        // Calculate the average partition set positions
                        Vector2 averageSetPosition = (pSet.graphicalData.active_position1 + pSet.graphicalData.active_position2) / 2f;
                        // Distance to line through particles
                        float distanceToLineThroughParticleHalves = Engine.Library.DegreeConstants.OrthogonalDistanceOfPointToLineFromAToB(averageSetPosition, AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.position1), AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.position2));
                        pSetSortingList.Add(distanceToLineThroughParticleHalves, pSet);
                    }
                }
                for (int i = 0; i < pSetSortingList.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = pSetSortingList.Values[i];
                    float rot1 = 60f * state.neighbor1ToNeighbor2Direction;
                    float rot2 = 60f * ((state.neighbor1ToNeighbor2Direction + 3) % 6);
                    pSet.graphicalData.active_connector_position1 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position1, i, pSetSortingList.Count, rot1, false);
                    pSet.graphicalData.active_connector_position2 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position2, i, pSetSortingList.Count, rot2, true);
                }
            }
        }

        /// <summary>
        /// Adds the data of a particle's partition set to the system. Combines it with the position data of the particle itself to calculate all positions of the circuits and pins.
        /// </summary>
        /// <param name="state">The particle's graphical pin and partition set data.</param>
        /// <param name="snap">The particle's position and movement data.</param>
        public void AddCircuits(ParticleGraphicsAdapterImpl particle, ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, PartitionSetViewType pSetViewType)
        {
            circuitData.Add(particle, new ParticleCircuitData(this, particle, state, snap));

            //bool delayed = RenderSystem.animationsOn && (snap.jointMovementState.isJointMovement || (snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting));
            bool delayed = snap.noAnimation == false && RenderSystem.animationsOn && (snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting);
            bool movement = RenderSystem.animationsOn && snap.jointMovementState.isJointMovement && delayed == false;
            Vector2 movementOffset = movement ? -AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.jointMovementState.jointExpansionOffset) : Vector2.zero;

            // 1. Calc PartitionSet Positions
            CalculatePartitionSetPositions(state, snap, pSetViewType);
            // 2. Generate Pins and Lines
            if (state.isExpanded == false)
            {
                // Contracted
                // Add Internal Pins and Lines
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    ParticlePinGraphicState.PSetData.GraphicalData gd = pSet.graphicalData;
                    // 1. Add Pin
                    AddPin(pSet.graphicalData.active_position1, pSet.color, delayed, pSet.beepOrigin, movementOffset, new GDRef(gd, false, true, false), new GDRef(gd, false, true, false));
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
                // Add Connection Pins
                for (int i = 0; i < 6; i++)
                {
                    if (state.hasNeighbor1[i] && globalDirLineSet1[i] == false)
                    {
                        bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection1[i] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                        for (int j = 0; j < state.pinsPerSide; j++)
                        {
                            ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(i, j, true);
                            Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                            Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                            AddLine(posPin, posOutterLineCenter, Color.black, true, delayedState, false, movementOffset, GDRef.Empty, GDRef.Empty);
                        }
                    }
                }
                for (int j = 0; j < 6; j++) globalDirLineSet1[j] = false;
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
                    GDRef gdRef_lines2 = new GDRef(gd, true, true, false, 0);
                    // 1. Add Pins + Connectors + Internal Lines
                    if(pSet.graphicalData.hasPinsInHead) AddPin(pSet.graphicalData.active_position1, pSet.color, delayed, pSet.beepOrigin, movementOffset, new GDRef(gd, false, true, false), new GDRef(gd, false, true, false));
                    if(pSet.graphicalData.hasPinsInTail) AddPin(pSet.graphicalData.active_position2, pSet.color, delayed, pSet.beepOrigin, movementOffset, new GDRef(gd, false, false, false), new GDRef(gd, false, false, false));
                    if(pSet.HasPinsInHeadAndTail())
                    {
                        AddConnectorPin(pSet.graphicalData.active_connector_position1, pSet.color, delayed, movementOffset, new GDRef(gd, false, true, true));
                        AddConnectorPin(pSet.graphicalData.active_connector_position2, pSet.color, delayed, movementOffset, new GDRef(gd, false, false, true));
                        AddLine(pSet.graphicalData.active_position1, pSet.graphicalData.active_connector_position1, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset, gdRef_lines1, gdRef_lines1);
                        AddLine(pSet.graphicalData.active_position2, pSet.graphicalData.active_connector_position2, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset, gdRef_lines2, gdRef_lines2);
                        gdRef_lines1.lineIndex++;
                        gdRef_lines2.lineIndex++;
                        AddLine(pSet.graphicalData.active_connector_position1, pSet.graphicalData.active_connector_position2, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset, new GDRef(gd, true, true, true), new GDRef(gd, true, true, true));
                    }
                    // 2. Add Lines + Connector Lines
                    AddLines_PartitionSetExpanded(state, snap, pSet, pSet.graphicalData.active_position1, pSet.graphicalData.active_position2, delayed, movementOffset, gdRef_lines1, gdRef_lines2);

                }
                // Add Singleton Lines
                for (int i = 0; i < state.singletonSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.singletonSets[i];
                    AddLines_SingletonSetExpanded(state, snap, pSet, delayed, movementOffset);
                }
                AddLines_ExternalWithoutPartitionSet(state, snap, delayed, movementOffset);
                // Reset Temporary Data
                for (int j = 0; j < 6; j++)
                {
                    globalDirLineSet1[j] = false;
                    globalDirLineSet2[j] = false;
                }
            }
        }

        private void AddLines_PartitionSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet, bool delayed, Vector2 movementOffset, GDRef gdRef)
        {
            foreach (var pin in pSet.pins)
            {
                // Inner Line
                Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                gdRef.gd.pSet1_pins.Add(pin);
                AddLine(posPartitionSet, posPin, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset, gdRef, gdRef);
                gdRef.lineIndex++;
                // Outter Line
                if (state.hasNeighbor1[pin.globalDir])
                {
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset, GDRef.Empty, GDRef.Empty);
                    globalDirLineSet1[pin.globalDir] = true;
                }
            }
        }

        private void AddLines_SingletonSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool delayed, Vector2 movementOffset)
        {
            foreach (var pin in pSet.pins)
            {
                Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                // Outter Line
                if (state.hasNeighbor1[pin.globalDir])
                {
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset, GDRef.Empty, GDRef.Empty);
                    globalDirLineSet1[pin.globalDir] = true;
                }
                // Beep Origin
                if (pSet.beepOrigin)
                {
                    AddSingletonBeep(posPin, pSet.color, delayed, movementOffset);
                }
            }
        }

        private void AddLines_PartitionSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet1, Vector2 posPartitionSet2, bool delayed, Vector2 movementOffset, GDRef gdRef_lines1, GDRef gdRef_lines2)
        {
            foreach (var pin in pSet.pins)
            {
                // Inner Line
                Vector2 posPin;
                if (pin.isHead)
                {
                    posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                    gdRef_lines1.gd.pSet1_pins.Add(pin);
                    AddLine(posPartitionSet1, posPin, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset, gdRef_lines1, gdRef_lines1);
                    gdRef_lines1.lineIndex++;
                    //gdRef_lines1_beep.lineIndex++;
                }
                else
                {
                    posPin = CalculateGlobalPinPosition(snap.position2, pin, state.pinsPerSide);
                    gdRef_lines2.gd.pSet2_pins.Add(pin);
                    AddLine(posPartitionSet2, posPin, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset, gdRef_lines2, gdRef_lines1);
                    gdRef_lines2.lineIndex++;
                    //gdRef_lines2_beep.lineIndex++;
                }

                // Outter Line
                if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
                {
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || (pin.isHead ? state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn : state.neighborPinConnection2[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn));
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset, GDRef.Empty, GDRef.Empty);
                    if (pin.isHead) globalDirLineSet1[pin.globalDir] = true;
                    else globalDirLineSet2[pin.globalDir] = true;
                }
            }
        }

        private void AddLines_SingletonSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool delayed, Vector2 movementOffset)
        {
            foreach (var pin in pSet.pins)
            {
                Vector2 posPin = CalculateGlobalPinPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                // Outter Line
                if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
                {
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || (pin.isHead ? state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn : state.neighborPinConnection2[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn));
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset, GDRef.Empty, GDRef.Empty);
                    if (pin.isHead) globalDirLineSet1[pin.globalDir] = true;
                    else globalDirLineSet2[pin.globalDir] = true;
                }
                // Beep Origin
                if (pSet.beepOrigin)
                {
                    AddSingletonBeep(posPin, pSet.color, delayed, movementOffset);
                }
            }
        }

        private void AddLines_ExternalWithoutPartitionSet(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, bool delayed, Vector2 movementOffset)
        {
            for (int k = 0; k < 6; k++)
            {
                if (state.hasNeighbor1[k] && globalDirLineSet1[k] == false && state.neighbor1ToNeighbor2Direction != k)
                {
                    for (int j = 0; j < state.pinsPerSide; j++)
                    {
                        ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(k, j, true);
                        Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                        Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                        bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection1[k] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                        AddLine(posPin, posOutterLineCenter, Color.black, true, delayedState, false, movementOffset, GDRef.Empty, GDRef.Empty);
                    }
                }
                if (state.hasNeighbor2[k] && globalDirLineSet2[k] == false && ((state.neighbor1ToNeighbor2Direction + 3) % 6) != k)
                {
                    for (int j = 0; j < state.pinsPerSide; j++)
                    {
                        ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(k, j, false);
                        Vector2 posPin = CalculateGlobalPinPosition(snap.position2, pin, state.pinsPerSide);
                        Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position2, pin, state.pinsPerSide);
                        bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection2[k] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                        AddLine(posPin, posOutterLineCenter, Color.black, true, delayedState, false, movementOffset, GDRef.Empty, GDRef.Empty);
                    }
                }
            }
        }



        private void AddLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, Color color, bool isConnectorLine, bool delayed, bool beeping, Vector2 movementOffset, GDRef gdRef, GDRef gdRef_beep)
        {
            // Normal Circuit
            RendererCircuits_RenderBatch batch = GetBatch_Line(color, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, false, false, movementOffset);
            RenderBatchIndex index = batch.AddLine(globalLineStartPos, globalLineEndPos);
            if(gdRef.valid)
            {
                StoreRenderBatchIndex(gdRef, index, true, false);
                gdRef.gd.properties_line = batch.properties;
            }
            // Beep
            if(beeping)
            {
                // Play Mode
                batch = GetBatch_Line(Color.white, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, true, false, movementOffset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActive);
                index = batch.AddLine(globalLineStartPos, globalLineEndPos);
                //StoreRenderBatchIndex(gdRef, index, true, true); // we only need to store the index for the paused mode
                // Pause Mode
                batch = GetBatch_Line(color, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, true, false, movementOffset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimPaused);
                index = batch.AddLine(globalLineStartPos, globalLineEndPos);
                if(gdRef_beep.valid)
                {
                    StoreRenderBatchIndex(gdRef_beep, index, true, true);
                    gdRef_beep.gd.properties_line_beep = batch.properties;
                }
            }
        }

        private void AddPin(Vector2 pinPos, Color color, bool delayed, bool beeping, Vector2 movementOffset, GDRef gdRef, GDRef gdRef_beep)
        {
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false, movementOffset);
            RenderBatchIndex index = batch.AddPin(pinPos, false);
            if(gdRef.valid)
            {
                StoreRenderBatchIndex(gdRef, index, false, false);
                gdRef.gd.properties_pin = batch.properties;
            }
            // Beep
            if (beeping)
            {
                batch = GetBatch_Pin(color, delayed, true, movementOffset);
                index = batch.AddPin(pinPos, false);
                if(gdRef_beep.valid)
                {
                    StoreRenderBatchIndex(gdRef_beep, index, false, true);
                    gdRef_beep.gd.properties_pin_beep = batch.properties;
                }
            }
        }


        private void AddSingletonBeep(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset)
        {
            // Beep
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, true, movementOffset);
            batch.AddPin(pinPos, true);
        }

        private void AddConnectorPin(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset, GDRef gdRef)
        {
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false, movementOffset);
            RenderBatchIndex index = batch.AddConnectorPin(pinPos);
            if(gdRef.valid)
            {
                StoreRenderBatchIndex(gdRef, index, false, false);
                gdRef.gd.properties_connectorPin = batch.properties;
            }
        }

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

        private void StoreRenderBatchIndex(GDRef gdRef, RenderBatchIndex index, bool isLine, bool isBeep)
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
                if(isBeep == false)
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
                if (isBeep == false)
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
                else
                {
                    if (gdRef.isConnector)
                    {
                        // No beeps here
                        //if (gdRef.isHead) gdRef.gd.index_pSetConnectorPin1 = index;
                        //else gdRef.gd.index_pSetConnectorPin2 = index;
                    }
                    else
                    {
                        if (gdRef.isHead) gdRef.gd.index_pSet1_beep = index;
                        else gdRef.gd.index_pSet2_beep = index;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the fitting batch for rendering lines.
        /// </summary>
        /// <param name="color">The color of the line.</param>
        /// <param name="lineType">The type of the line.</param>
        /// <param name="delayed">If the line should be shown delayed.</param>
        /// <param name="beeping">If the line should beep.</param>
        /// <param name="animated">If this batch is updated manually each frame.</param>
        /// <param name="movementOffset">The offset for the joint movement. Set to Vector2.zero if no jm is present.</param>
        /// <returns></returns>
        private RendererCircuits_RenderBatch GetBatch_Line(Color color, RendererCircuits_RenderBatch.PropertyBlockData.LineType lineType, bool delayed, bool beeping, bool animated, Vector2 movementOffset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState activeState = RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActiveOrPaused)
        {
            return GetBatch_Line(new RendererCircuits_RenderBatch.PropertyBlockData(color, lineType, delayed, beeping, animated, movementOffset, activeState));
        }

        /// <summary>
        /// Returns the fitting batch for rendering lines.
        /// </summary>
        /// <param name="propertyBlockData">The properties of the batch.</param>
        /// <returns></returns>
        private RendererCircuits_RenderBatch GetBatch_Line(RendererCircuits_RenderBatch.PropertyBlockData propertyBlockData)
        {
            RendererCircuits_RenderBatch batch;
            if (propertiesToRenderBatchMap.ContainsKey(propertyBlockData) == false)
            {
                // Batch does not exist
                // Create Batch
                batch = new RendererCircuits_RenderBatch(propertyBlockData);
                propertiesToRenderBatchMap.Add(propertyBlockData, batch);
            }
            else
            {
                propertiesToRenderBatchMap.TryGetValue(propertyBlockData, out batch);
            }
            return batch;
        }

        /// <summary>
        /// Returns the fitting batch for rendering pins.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="delayed"></param>
        /// <param name="beeping"></param>
        /// <param name="movementOffset">The offset for the joint movement. Set to Vector2.zero if no jm is present.</param>
        /// <returns></returns>
        private RendererCircuitPins_RenderBatch GetBatch_Pin(Color color, bool delayed, bool beeping, Vector2 movementOffset)
        {
            return GetBatch_Pin(new RendererCircuitPins_RenderBatch.PropertyBlockData(color, delayed, beeping, movementOffset));
        }

        /// <summary>
        /// Returns the fitting batch for rendering pins.
        /// </summary>
        /// <param name="properties">The properties of the batch.</param>
        /// <returns></returns>
        private RendererCircuitPins_RenderBatch GetBatch_Pin(RendererCircuitPins_RenderBatch.PropertyBlockData propertyBlockData)
        {
            RendererCircuitPins_RenderBatch batch;
            if (propertiesToPinRenderBatchMap.ContainsKey(propertyBlockData) == false)
            {
                // Batch does not exist
                // Create Batch
                batch = new RendererCircuitPins_RenderBatch(propertyBlockData);
                propertiesToPinRenderBatchMap.Add(propertyBlockData, batch);
            }
            else
            {
                propertiesToPinRenderBatchMap.TryGetValue(propertyBlockData, out batch);
            }
            return batch;
        }

        /// <summary>
        /// Adds a single bond to the system.
        /// </summary>
        /// <param name="bondState"></param>
        public void AddBond(ParticleBondGraphicState bondState)
        {
            // Convert Grid to World Space
            Vector2 prevBondPosWorld1 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.prevBondPos1);
            Vector2 prevBondPosWorld2 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.prevBondPos2);
            Vector2 curBondPosWorld1 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.curBondPos1);
            Vector2 curBondPosWorld2 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.curBondPos2);
            // Hexagonal
            RendererCircuits_RenderBatch batch = GetBatch_Line(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondHexagonal, false, false, bondState.IsAnimated(), Vector2.zero);
            if (bondState.IsAnimated()) batch.AddManuallyUpdatedLine(prevBondPosWorld1, prevBondPosWorld2, curBondPosWorld1, curBondPosWorld2);
            else batch.AddLine(curBondPosWorld1, curBondPosWorld2);
            // Circular
            batch = GetBatch_Line(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondCircular, false, false, bondState.IsAnimated(), Vector2.zero);
            if (bondState.IsAnimated()) batch.AddManuallyUpdatedLine(prevBondPosWorld1, prevBondPosWorld2, curBondPosWorld1, curBondPosWorld2);
            else batch.AddLine(curBondPosWorld1, curBondPosWorld2);
        }



        /// <summary>
        /// Reinitializes the Batches. Helpful in case settings have been changed.
        /// </summary>
        public void ReinitBatches()
        {
            foreach (var batch in propertiesToRenderBatchMap.Values)
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
    /// <param name="type"></param>
    public void Render(ViewType type)
    {
        bool firstRenderFrame = isRenderingActive == false;
        foreach (var batch in propertiesToRenderBatchMap.Values)
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
        public void Clear()
        {
            foreach (var batch in propertiesToRenderBatchMap.Values)
            {
                batch.ClearMatrices();
            }
            foreach (var batch in propertiesToPinRenderBatchMap.Values)
            {
                batch.ClearMatrices();
            }
            foreach (var data in circuitData.Values)
            {
                ParticlePinGraphicState.PoolRelease(data.state);
            }
            circuitData.Clear();
            isRenderingActive = false;
        }









        private Vector2 CalculateGlobalPinPosition(Vector2Int gridPosParticle, ParticlePinGraphicState.PinDef pinDef, int pinsPerSide)
        {
            Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
            Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(pinDef, pinsPerSide, RenderSystem.global_particleScale, RenderSystem.setting_viewType);
            return posParticle + relPinPos;
        }

        private Vector2 CalculateGlobalPartitionSetPinPosition(Vector2Int gridPosParticle, int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees, bool invertPositions)
        {
            Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
            return posParticle + CalculateRelativePartitionSetPinPosition(partitionSetID, amountOfPartitionSetsAtNode, rotationDegrees, invertPositions);
        }

        private Vector2 CalculateGlobalExpandedPartitionSetCenterNodePosition(Vector2Int gridPosParticle, int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees, bool invertPositions)
        {
            Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
            return posParticle + CalculateRelativeExpandedPartitionSetCenterNodePosition(partitionSetID, amountOfPartitionSetsAtNode, rotationDegrees, invertPositions);
        }

        private Vector2 CalculateGlobalOutterPinLineCenterPosition(Vector2Int gridPosParticle, ParticlePinGraphicState.PinDef pinDef, int pinsPerSide)
        {
            Vector2 pinPos = CalculateGlobalPinPosition(gridPosParticle, pinDef, pinsPerSide);
            //Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(new ParticlePinGraphicState.PinDef(pinDef.globalDir, pinDef.dirID, pinDef.isHead), pinsPerSide, RenderSystem.global_particleScale, RenderSystem.setting_viewType);

            // Calculate neighbor pin position
            ParticlePinGraphicState.PinDef pinDefNeighbor = new ParticlePinGraphicState.PinDef((pinDef.globalDir + 3) % 6, pinsPerSide - 1 - pinDef.dirID, pinDef.isHead);
            Vector2Int gridPosNeighbor = AmoebotFunctions.GetNeighborPosition(gridPosParticle, pinDef.globalDir);
            Vector2 pinPosNeighbor = CalculateGlobalPinPosition(gridPosNeighbor, pinDefNeighbor, pinsPerSide);
            //Vector2 relNeighborPinPos = AmoebotFunctions.CalculateRelativePinPosition(pinDefNeighbor, pinsPerSide, RenderSystem.global_particleScale, RenderSystem.setting_viewType);
            return pinPos + ((pinPosNeighbor - pinPos) / 2f);
        }

        private Vector2 CalculateRelativePartitionSetPinPosition(int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees, bool invertPositions)
        {
            if (amountOfPartitionSetsAtNode == 1) return Vector2.zero;
            else
            {
                float lineLength;
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
                if (invertPositions) height = (lineLength / 2f) - (amountOfPartitionSetsAtNode - partitionSetID - 1) * (lineLength / (amountOfPartitionSetsAtNode - 1));
                else height = (lineLength / 2f) - partitionSetID * (lineLength / (amountOfPartitionSetsAtNode - 1));
                Vector2 position = new Vector2(0f, height);
                position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
                return position;
            }
        }

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
                if (invertPositions) height = (lineLength / 2f) - (amountOfPartitionSetsAtNode - partitionSetID - 1) * (lineLength / (amountOfPartitionSetsAtNode - 1));
                else height = (lineLength / 2f) - partitionSetID * (lineLength / (amountOfPartitionSetsAtNode - 1));
                Vector2 position = new Vector2(relXPos, height);
                position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
                return position;
            }
        }





        // Functions __________________________________

        public ParticleCircuitData GetParticleCircuitData(ParticleGraphicsAdapterImpl p)
        {
            ParticleCircuitData d;
            circuitData.TryGetValue(p, out d);
            if (d.particle == null) Log.Error("RendererCircuits_Instance: GetParticleCircuitData for particle " + p.ToString() + " is not assigned!");
            return d;
        }


    }

    public enum PartitionSetViewType
    {
        /// <summary>
        /// Standard view type: Partition Sets oriented in a row.
        /// </summary>
        Default,
        /// <summary>
        /// Automatic view type: Partition Sets oriented on a circle, automaticaly ordered by the median coordinates.
        /// </summary>
        Auto,
        /// <summary>
        /// Code override view type: Partition Sets are oriented based on input from the code. If none is available we use auto positioning.
        /// </summary>
        CodeOverride,
    }

}