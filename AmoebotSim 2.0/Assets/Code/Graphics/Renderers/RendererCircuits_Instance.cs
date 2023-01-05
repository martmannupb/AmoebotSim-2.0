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
        public bool isRenderingActive = false;
        // Temporary
        private bool[] globalDirLineSet1 = new bool[] { false, false, false, false, false, false };
        private bool[] globalDirLineSet2 = new bool[] { false, false, false, false, false, false };

        /// <summary>
        /// Adds the data of a particle's partition set to the system. Combines it with the position data of the particle itself to calculate all positions of the circuits and pins.
        /// </summary>
        /// <param name="state">The particle's graphical pin and partition set data.</param>
        /// <param name="snap">The particle's position and movement data.</param>
        public void AddCircuits(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap)
        {
            //bool delayed = RenderSystem.animationsOn && (snap.jointMovementState.isJointMovement || (snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting));
            bool delayed = snap.noAnimation == false && RenderSystem.animationsOn && (snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting);
            bool movement = RenderSystem.animationsOn && snap.jointMovementState.isJointMovement && delayed == false;
            Vector2 movementOffset = movement ? -AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.jointMovementState.jointExpansionOffset) : Vector2.zero;
            int amountPartitionSets = state.partitionSets.Count;

            if (state.isExpanded == false)
            {
                // Contracted
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    Vector2 posPartitionSet = CalculateGlobalPartitionSetPinPosition(snap.position1, i, amountPartitionSets, 0f, false);
                    // 1. Add Pin
                    AddPin(posPartitionSet, pSet.color, delayed, pSet.beepOrigin, movementOffset);
                    // 2. Add Lines
                    AddLines_PartitionSetContracted(state, snap, pSet, posPartitionSet, delayed, movementOffset);
                }
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
                            AddLine(posPin, posOutterLineCenter, Color.black, true, delayedState, false, movementOffset);
                        }
                    }
                }
                for (int j = 0; j < 6; j++) globalDirLineSet1[j] = false;
            }
            else
            {
                // Expanded
                for (int i = 0; i < state.partitionSets.Count; i++)
                {
                    ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                    float rot1 = 60f * state.neighbor1ToNeighbor2Direction;
                    float rot2 = 60f * ((state.neighbor1ToNeighbor2Direction + 3) % 6);
                    Vector2 posPartitionSet1 = CalculateGlobalPartitionSetPinPosition(snap.position1, i, amountPartitionSets, rot1, false);
                    Vector2 posPartitionSet2 = CalculateGlobalPartitionSetPinPosition(snap.position2, i, amountPartitionSets, rot2, true);
                    Vector2 posPartitionSetConnectorPin1 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position1, i, amountPartitionSets, rot1, false);
                    Vector2 posPartitionSetConnectorPin2 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position2, i, amountPartitionSets, rot2, true);
                    // 1. Add Pins + Connectors + Internal Lines
                    AddPin(posPartitionSet1, pSet.color, delayed, pSet.beepOrigin, movementOffset);
                    AddPin(posPartitionSet2, pSet.color, delayed, pSet.beepOrigin, movementOffset);
                    AddConnectorPin(posPartitionSetConnectorPin1, pSet.color, delayed, movementOffset);
                    AddConnectorPin(posPartitionSetConnectorPin2, pSet.color, delayed, movementOffset);
                    AddLine(posPartitionSet1, posPartitionSetConnectorPin1, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset);
                    AddLine(posPartitionSet2, posPartitionSetConnectorPin2, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset);
                    AddLine(posPartitionSetConnectorPin1, posPartitionSetConnectorPin2, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset);
                    // 2. Add Lines + Connector Lines
                    AddLines_PartitionSetExpanded(state, snap, pSet, posPartitionSet1, posPartitionSet2, delayed, movementOffset);

                }
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

        private void AddLines_PartitionSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet, bool delayed, Vector2 movementOffset)
        {
            foreach (var pin in pSet.pins)
            {
                // Inner Line
                Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                AddLine(posPartitionSet, posPin, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset);
                // Outter Line
                if (state.hasNeighbor1[pin.globalDir])
                {
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn);
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset);
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
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset);
                    globalDirLineSet1[pin.globalDir] = true;
                }
                // Beep Origin
                if (pSet.beepOrigin)
                {
                    AddSingletonBeep(posPin, pSet.color, delayed, movementOffset);
                }
            }
        }

        private void AddLines_PartitionSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet1, Vector2 posPartitionSet2, bool delayed, Vector2 movementOffset)
        {
            foreach (var pin in pSet.pins)
            {
                // Inner Line
                Vector2 posPin = CalculateGlobalPinPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                AddLine(pin.isHead ? posPartitionSet1 : posPartitionSet2, posPin, pSet.color, false, delayed, pSet.beepsThisRound, movementOffset);
                // Outter Line
                if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
                {
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                    bool delayedState = snap.noAnimation == false && RenderSystem.animationsOn && (delayed || (pin.isHead ? state.neighborPinConnection1[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn : state.neighborPinConnection2[pin.globalDir] == ParticlePinGraphicState.NeighborPinConnection.ShownFadingIn));
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset);
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
                    AddLine(posPin, posOutterLineCenter, pSet.color, true, delayedState, pSet.beepsThisRound, movementOffset);
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
                        AddLine(posPin, posOutterLineCenter, Color.black, true, delayedState, false, movementOffset);
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
                        AddLine(posPin, posOutterLineCenter, Color.black, true, delayedState, false, movementOffset);
                    }
                }
            }
        }



        private void AddLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, Color color, bool isConnectorLine, bool delayed, bool beeping, Vector2 movementOffset)
        {
            // Normal Circuit
            RendererCircuits_RenderBatch batch = GetBatch_Line(color, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, false, false, movementOffset);
            batch.AddLine(globalLineStartPos, globalLineEndPos);
            // Beep
            if(beeping)
            {
                // Play Mode
                batch = GetBatch_Line(Color.white, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, true, false, movementOffset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimActive);
                batch.AddLine(globalLineStartPos, globalLineEndPos);
                // Pause Mode
                batch = GetBatch_Line(color, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, true, false, movementOffset, RendererCircuits_RenderBatch.PropertyBlockData.ActiveState.SimPaused);
                batch.AddLine(globalLineStartPos, globalLineEndPos);
            }
        }

        private void AddPin(Vector2 pinPos, Color color, bool delayed, bool beeping, Vector2 movementOffset)
        {
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false, movementOffset);
            batch.AddPin(pinPos, false);
            // Beep
            if (beeping)
            {
                batch = GetBatch_Pin(color, delayed, true, movementOffset);
                batch.AddPin(pinPos, false);
            }
        }

        private void AddSingletonBeep(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset)
        {
            // Beep
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, true, movementOffset);
            batch.AddPin(pinPos, true);
        }

        private void AddConnectorPin(Vector2 pinPos, Color color, bool delayed, Vector2 movementOffset)
        {
            RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false, movementOffset);
            batch.AddConnectorPin(pinPos);
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
            RendererCircuits_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuits_RenderBatch.PropertyBlockData(color, lineType, delayed, beeping, animated, movementOffset, activeState);
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
            RendererCircuitPins_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuitPins_RenderBatch.PropertyBlockData(color, delayed, beeping, movementOffset);
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
    }

}