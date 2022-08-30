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
        bool moving = snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding || snap.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting;
        int amountPartitionSets = state.partitionSets.Count;

        if(state.isExpanded == false)
        {
            // Contracted
            for (int i = 0; i < state.partitionSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                Vector2 posPartitionSet = CalculateGlobalPartitionSetPinPosition(snap.position1, i, amountPartitionSets, 0f, false);
                // 1. Add Pin
                AddPin(posPartitionSet, pSet.color, moving, pSet.beepOrigin);
                // 2. Add Lines
                foreach (var pin in pSet.pins)
                {
                    // Inner Line
                    Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                    AddLine(posPartitionSet, posPin, pSet.color, false, moving, pSet.beepsThisRound);
                    // Outter Line
                    if (state.hasNeighbor1[pin.globalDir])
                    {
                        Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                        AddLine(posPin, posOutterLineCenter, pSet.color, true, moving, pSet.beepsThisRound);
                        globalDirLineSet1[pin.globalDir] = true;
                    }
                }
            }
            for (int i = 0; i < state.singletonSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = state.singletonSets[i];
                // Add Singleton Connections
                AddLines_SingletonSetContracted(state, snap, pSet, moving);
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
                        AddLine(posPin, posOutterLineCenter, Color.black, true, moving, false);
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
                AddPin(posPartitionSet1, pSet.color, moving, pSet.beepOrigin);
                AddPin(posPartitionSet2, pSet.color, moving, pSet.beepOrigin);
                AddConnectorPin(posPartitionSetConnectorPin1, pSet.color, moving);
                AddConnectorPin(posPartitionSetConnectorPin2, pSet.color, moving);
                AddLine(posPartitionSet1, posPartitionSetConnectorPin1, pSet.color, false, moving, pSet.beepsThisRound);
                AddLine(posPartitionSet2, posPartitionSetConnectorPin2, pSet.color, false, moving, pSet.beepsThisRound);
                AddLine(posPartitionSetConnectorPin1, posPartitionSetConnectorPin2, pSet.color, false, moving, pSet.beepsThisRound);
                // 2. Add Lines + Connector Lines
                AddLines_PartitionSetExpanded(state, snap, pSet, posPartitionSet1, posPartitionSet2, moving);
                
            }
            for (int i = 0; i < state.singletonSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = state.singletonSets[i];
                AddLines_SingletonSetExpanded(state, snap, pSet, moving);
            }
            AddLines_ExternalWithoutPartitionSet(state, snap, moving);
            // Reset Temporary Data
            for (int j = 0; j < 6; j++)
            {
                globalDirLineSet1[j] = false;
                globalDirLineSet2[j] = false;
            }
        }
    }

    private void AddLines_PartitionSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, Vector2 posPartitionSet1, Vector2 posPartitionSet2, bool moving)
    {
        foreach (var pin in pSet.pins)
        {
            // Inner Line
            Vector2 posPin = CalculateGlobalPinPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
            AddLine(pin.isHead ? posPartitionSet1 : posPartitionSet2, posPin, pSet.color, false, moving, pSet.beepsThisRound);
            // Outter Line
            if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
            {
                Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                AddLine(posPin, posOutterLineCenter, pSet.color, true, moving, pSet.beepsThisRound);
                if (pin.isHead) globalDirLineSet1[pin.globalDir] = true;
                else globalDirLineSet2[pin.globalDir] = true;
            }
        }
    }

    private void AddLines_SingletonSetContracted(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool moving)
    {
        foreach (var pin in pSet.pins)
        {
            Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
            // Outter Line
            if (state.hasNeighbor1[pin.globalDir])
            {
                Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                AddLine(posPin, posOutterLineCenter, pSet.color, true, moving, pSet.beepsThisRound);
                globalDirLineSet1[pin.globalDir] = true;
            }
        }
    }

    private void AddLines_SingletonSetExpanded(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, ParticlePinGraphicState.PSetData pSet, bool moving)
    {
        foreach (var pin in pSet.pins)
        {
            Vector2 posPin = CalculateGlobalPinPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
            // Outter Line
            if (pin.isHead ? state.hasNeighbor1[pin.globalDir] : state.hasNeighbor2[pin.globalDir])
            {
                Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(pin.isHead ? snap.position1 : snap.position2, pin, state.pinsPerSide);
                AddLine(posPin, posOutterLineCenter, pSet.color, true, moving, pSet.beepsThisRound);
                if (pin.isHead) globalDirLineSet1[pin.globalDir] = true;
                else globalDirLineSet2[pin.globalDir] = true;
            }
            // Beep Origin
            if (pSet.beepOrigin)
            {
                AddSingletonBeep(posPin, pSet.color, moving);
            }
        }
    }

    private void AddLines_ExternalWithoutPartitionSet(ParticlePinGraphicState state, ParticleGraphicsAdapterImpl.PositionSnap snap, bool moving)
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
                    AddLine(posPin, posOutterLineCenter, Color.black, true, moving, false);
                }
            }
            if (state.hasNeighbor2[k] && globalDirLineSet2[k] == false && ((state.neighbor1ToNeighbor2Direction + 3) % 6) != k)
            {
                for (int j = 0; j < state.pinsPerSide; j++)
                {
                    ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(k, j, false);
                    Vector2 posPin = CalculateGlobalPinPosition(snap.position2, pin, state.pinsPerSide);
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position2, pin, state.pinsPerSide);
                    AddLine(posPin, posOutterLineCenter, Color.black, true, moving, false);
                }
            }
        }
    }

    

    public void AddLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, Color color, bool isConnectorLine, bool moving, bool beeping)
    {
        // Normal Circuit
        RendererCircuits_RenderBatch batch = GetBatch_Line(color, isConnectorLine, moving, false);
        batch.AddLine(globalLineStartPos, globalLineEndPos);
        // Beep
        if(beeping)
        {
            batch = GetBatch_Line(Color.white, isConnectorLine, moving, true);
            batch.AddLine(globalLineStartPos, globalLineEndPos);
        }
    }

    public void AddPin(Vector2 pinPos, Color color, bool moving, bool beeping)
    {
        RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, moving, false);
        batch.AddPin(pinPos, false);
        // Beep
        if (beeping)
        {
            batch = GetBatch_Pin(color, moving, true);
            batch.AddPin(pinPos, false);
        }
    }

    public void AddSingletonBeep(Vector2 pinPos, Color color, bool moving)
    {
        // Beep
        RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, moving, true);
        batch.AddPin(pinPos, true);

    }

    public void AddConnectorPin(Vector2 pinPos, Color color, bool moving)
    {
        RendererCircuitPins_RenderBatch batch = GetBatch_Pin(color, moving, false);
        batch.AddConnectorPin(pinPos);
    }

    public RendererCircuits_RenderBatch GetBatch_Line(Color color, bool isConnectorLine, bool moving, bool beeping)
    {
        RendererCircuits_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuits_RenderBatch.PropertyBlockData(color, isConnectorLine, moving, beeping);
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

    public RendererCircuitPins_RenderBatch GetBatch_Pin(Color color, bool moving, bool beeping)
    {
        RendererCircuitPins_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuitPins_RenderBatch.PropertyBlockData(color, moving, beeping);
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



    public void Render()
    {
        bool firstRenderFrame = isRenderingActive == false;
        foreach (var batch in propertiesToRenderBatchMap.Values)
        {
            batch.Render(firstRenderFrame);
        }
        foreach (var batch in propertiesToPinRenderBatchMap.Values)
        {
            batch.Render(firstRenderFrame);
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