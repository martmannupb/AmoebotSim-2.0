using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuits_Instance
{

    public Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch>();
    public Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch> propertiesToPinRenderBatchMap = new Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch>();

    // Data
    public bool isRenderingActive = false;
    // Temporary
    private bool[] globalDirLineSet1 = new bool[] { false, false, false, false, false, false };
    private bool[] globalDirLineSet2 = new bool[] { false, false, false, false, false, false };

    public void AddCircuits(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap)
    {
        bool delayed = RenderSystem.animationsOn && (snap.jointMovementState.isJointMovement || (snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting));
        int amountPartitionSets = state.partitionSets.Count;

        if(state.isExpanded == false)
        {
            // Contracted
            for (int i = 0; i < state.partitionSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                Vector2 posPartitionSet = CalculateGlobalPartitionSetPinPosition(snap.position1, i, amountPartitionSets, 0f, false);
                // 1. Add Pin
                AddPin(posPartitionSet, pSet.color, delayed, pSet.beepOrigin);
                // 2. Add Lines
                AddLines_PartitionSetContracted(state, snap, pSet, posPartitionSet, delayed);
            }
            for (int i = 0; i < state.singletonSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = state.singletonSets[i];
                // 1. Add Lines
                AddLines_SingletonSetContracted(state, snap, pSet, delayed);
            }
            // Add Connection Pins
            for (int i = 0; i < 6; i++)
            {
                if(state.hasNeighbor1[i] && globalDirLineSet1[i] == false)
                {
                    for (int j = 0; j < state.pinsPerSide; j++)
                    {
                        ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(i, j, true);
                        Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                        Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                        AddLine(posPin, posOutterLineCenter, Color.black, true, delayed, false);
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
                AddPin(posPartitionSet1, pSet.color, delayed, pSet.beepOrigin);
                AddPin(posPartitionSet2, pSet.color, delayed, pSet.beepOrigin);
                AddConnectorPin(posPartitionSetConnectorPin1, pSet.color, delayed);
                AddConnectorPin(posPartitionSetConnectorPin2, pSet.color, delayed);
                AddLine(posPartitionSet1, posPartitionSetConnectorPin1, pSet.color, false, delayed, pSet.beepsThisRound);
                AddLine(posPartitionSet2, posPartitionSetConnectorPin2, pSet.color, false, delayed, pSet.beepsThisRound);
                AddLine(posPartitionSetConnectorPin1, posPartitionSetConnectorPin2, pSet.color, false, delayed, pSet.beepsThisRound);
                // 2. Add Lines + Connector Lines
                AddLines_PartitionSetExpanded(state, snap, pSet, posPartitionSet1, posPartitionSet2, delayed);
                
            }
            for (int i = 0; i < state.singletonSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = state.singletonSets[i];
                AddLines_SingletonSetExpanded(state, snap, pSet, delayed);
            }
            AddLines_ExternalWithoutPartitionSet(state, snap, delayed);
            // Reset Temporary Data
            for (int j = 0; j < 6; j++)
            {
                globalDirLineSet1[j] = false;
                globalDirLineSet2[j] = false;
            }
        }
    }

    private void AddLines_PartitionSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet, bool delayed)
    {
        foreach (var pin in pSet.pins)
        {
            // Inner Line
            Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
            AddLine(posPartitionSet, posPin, pSet.color, false, delayed, pSet.beepsThisRound);
            // Outter Line
            if (state.hasNeighbor1[pin.globalDir])
            {
                Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                AddLine(posPin, posOutterLineCenter, pSet.color, true, delayed, pSet.beepsThisRound);
                globalDirLineSet1[pin.globalDir] = true;
            }
        }
    }

    private void AddLines_SingletonSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool delayed)
    {
        foreach (var pin in pSet.pins)
        {
            Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
            // Outter Line
            if (state.hasNeighbor1[pin.globalDir])
            {
                Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                AddLine(posPin, posOutterLineCenter, pSet.color, true, delayed, pSet.beepsThisRound);
                globalDirLineSet1[pin.globalDir] = true;
            }
            // Beep Origin
            if (pSet.beepOrigin)
            {
                AddSingletonBeep(posPin, pSet.color, delayed);
            }
        }
    }

    private void AddLines_PartitionSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet1, Vector2 posPartitionSet2, bool delayed)
    {
        foreach (var pin in pSet.pins)
        {
            // Inner Line
            Vector2 posPin = CalculateGlobalPinPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
            AddLine(pin.isHead ? posPartitionSet1 : posPartitionSet2, posPin, pSet.color, false, delayed, pSet.beepsThisRound);
            // Outter Line
            if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
            {
                Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                AddLine(posPin, posOutterLineCenter, pSet.color, true, delayed, pSet.beepsThisRound);
                if (pin.isHead) globalDirLineSet1[pin.globalDir] = true;
                else globalDirLineSet2[pin.globalDir] = true;
            }
        }
    }

    private void AddLines_SingletonSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool delayed)
    {
        foreach (var pin in pSet.pins)
        {
            Vector2 posPin = CalculateGlobalPinPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
            // Outter Line
            if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
            {
                Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                AddLine(posPin, posOutterLineCenter, pSet.color, true, delayed, pSet.beepsThisRound);
                if (pin.isHead) globalDirLineSet1[pin.globalDir] = true;
                else globalDirLineSet2[pin.globalDir] = true;
            }
            // Beep Origin
            if (pSet.beepOrigin)
            {
                AddSingletonBeep(posPin, pSet.color, delayed);
            }
        }
    }

    private void AddLines_ExternalWithoutPartitionSet(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, bool delayed)
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
                    AddLine(posPin, posOutterLineCenter, Color.black, true, delayed, false);
                }
            }
            if (state.hasNeighbor2[k] && globalDirLineSet2[k] == false && ((state.neighbor1ToNeighbor2Direction + 3) % 6) != k)
            {
                for (int j = 0; j < state.pinsPerSide; j++)
                {
                    ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(k, j, false);
                    Vector2 posPin = CalculateGlobalPinPosition(snap.position2, pin, state.pinsPerSide);
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position2, pin, state.pinsPerSide);
                    AddLine(posPin, posOutterLineCenter, Color.black, true, delayed, false);
                }
            }
        }
    }

    

    public void AddLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, Color color, bool isConnectorLine, bool delayed, bool beeping)
    {
        // Normal Circuit
        RendererCircuits_RenderBatch batch = GetBatch_Line(color, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, false, false);
        batch.AddLine(globalLineStartPos, globalLineEndPos);
        // Beep
        if(beeping)
        {
            batch = GetBatch_Line(Color.white, isConnectorLine ? RendererCircuits_RenderBatch.PropertyBlockData.LineType.ExternalLine : RendererCircuits_RenderBatch.PropertyBlockData.LineType.InternalLine, delayed, true, false);
            batch.AddLine(globalLineStartPos, globalLineEndPos);
        }
    }

    public void AddPin(Vector2 pinPos, Color color, bool delayed, bool beeping)
    {
        RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false);
        batch.AddPin(pinPos, false);
        // Beep
        if (beeping)
        {
            batch = GetBatch_Pin(color, delayed, true);
            batch.AddPin(pinPos, false);
        }
    }

    public void AddSingletonBeep(Vector2 pinPos, Color color, bool delayed)
    {
        // Beep
        RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, true);
        batch.AddPin(pinPos, true);
    }

    public void AddConnectorPin(Vector2 pinPos, Color color, bool delayed)
    {
        RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, delayed, false);
        batch.AddConnectorPin(pinPos);
    }

    public RendererCircuits_RenderBatch GetBatch_Line(Color color, RendererCircuits_RenderBatch.PropertyBlockData.LineType lineType, bool delayed, bool beeping, bool animated)
    {
        RendererCircuits_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuits_RenderBatch.PropertyBlockData(color, lineType, delayed, beeping, animated);
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

    public RendererCircuitPins_RenderBatch GetBatch_Pin(Color color, bool delayed, bool beeping)
    {
        RendererCircuitPins_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuitPins_RenderBatch.PropertyBlockData(color, delayed, beeping);
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

    public void AddBond(ParticleBondGraphicState bondState)
    {
        // Convert Grid to World Space
        Vector2 prevBondPosWorld1 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.prevBondPos1);
        Vector2 prevBondPosWorld2 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.prevBondPos2);
        Vector2 curBondPosWorld1 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.curBondPos1);
        Vector2 curBondPosWorld2 = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(bondState.curBondPos2);
        // Hexagonal
        RendererCircuits_RenderBatch batch = GetBatch_Line(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondHexagonal, false, false, bondState.IsAnimated());
        if (bondState.IsAnimated()) batch.AddAnimatedLine(prevBondPosWorld1, prevBondPosWorld2, curBondPosWorld1, curBondPosWorld2);
        else batch.AddLine(curBondPosWorld1, curBondPosWorld2);
        // Circular
        batch = GetBatch_Line(Color.black, RendererCircuits_RenderBatch.PropertyBlockData.LineType.BondCircular, false, false, bondState.IsAnimated());
        if (bondState.IsAnimated()) batch.AddAnimatedLine(prevBondPosWorld1, prevBondPosWorld2, curBondPosWorld1, curBondPosWorld2);
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
        Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(pinDef, pinsPerSide, RenderSystem.global_particleScale);
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
        Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(new ParticlePinGraphicState.PinDef(pinDef.globalDir, 0, pinDef.isHead), 1, RenderSystem.global_particleScale);
        //Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(pinDef, pinsPerSide, RenderSystem.global_particleScale);
        Vector2 lineCenterOffset = relPinPos / RenderSystem.global_particleScale - relPinPos;
        //if(pinDef.globalDir == 3)
        //{
            //Debug.Log("pinPos: "+pinPos+", relPinPos: " +relPinPos + ", lineCenterOffset: "+lineCenterOffset);
        //}
        return pinPos + lineCenterOffset;
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
            if(invertPositions) height = (lineLength / 2f) - (amountOfPartitionSetsAtNode - partitionSetID - 1) * (lineLength / (amountOfPartitionSetsAtNode - 1));
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
            if(invertPositions) height = (lineLength / 2f) - (amountOfPartitionSetsAtNode - partitionSetID - 1) * (lineLength / (amountOfPartitionSetsAtNode - 1));
            else height = (lineLength / 2f) - partitionSetID * (lineLength / (amountOfPartitionSetsAtNode - 1));
            Vector2 position = new Vector2(relXPos, height);
            position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
            return position;
        }
    }
}