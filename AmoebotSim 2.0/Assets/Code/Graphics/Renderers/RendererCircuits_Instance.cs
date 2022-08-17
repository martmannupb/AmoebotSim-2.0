using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuits_Instance
{

    public Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch>();
    public Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch> propertiesToPinRenderBatchMap = new Dictionary<RendererCircuitPins_RenderBatch.PropertyBlockData, RendererCircuitPins_RenderBatch>();

    // Data

    //private bool[] globalDirLineSet = new bool[] { false, false, false, false, false, false };

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
                Vector2 posPartitionSet = CalculateGlobalPartitionSetPinPosition(snap.position1, i, amountPartitionSets, 0f);
                // 1. Add Pin
                AddPin(posPartitionSet, pSet.color, moving);
                // 2. Add Lines
                //for (int j = 0; j < 6; j++) globalDirLineSet[j] = false;
                foreach (var pin in pSet.pins)
                {
                    // Inner Line
                    Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                    AddLine(posPartitionSet, posPin, pSet.color, moving);
                    // Outter Line
                    //if (state.hasNeighbor1[pin.globalDir])
                    //{
                    //    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    //    //AddLine(posPin, posOutterLineCenter, pSet.color, moving); // this does not look good enough
                    //    AddLine(posPin, posOutterLineCenter, Color.black, moving);
                    //    globalDirLineSet[pin.globalDir] = true;
                    //}
                }
            }
            // Add Connection Pins
            for (int i = 0; i < 6; i++)
            {
                if(state.hasNeighbor1[i])
                {
                    for (int j = 0; j < state.pinsPerSide; j++)
                    {
                        ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(i, j, true);
                        Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                        Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                        AddLine(posPin, posOutterLineCenter, Color.black, moving);
                    }
                }
            }
        }
        else
        {
            // Expanded
            for (int i = 0; i < state.partitionSets.Count; i++)
            {
                ParticlePinGraphicState.PSetData pSet = state.partitionSets[i];
                Vector2 posPartitionSet1 = CalculateGlobalPartitionSetPinPosition(snap.position1, i, amountPartitionSets, 60f * snap.globalExpansionDir);
                Vector2 posPartitionSet2 = CalculateGlobalPartitionSetPinPosition(snap.position2, i, amountPartitionSets, 60f * snap.globalExpansionDir);
                Vector2 posPartitionSetConnectorPin1 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position1, i, amountPartitionSets, 60f * snap.globalExpansionDir);
                Vector2 posPartitionSetConnectorPin2 = CalculateGlobalExpandedPartitionSetCenterNodePosition(snap.position2, i, amountPartitionSets, 60f * snap.globalExpansionDir);
                // 1. Add Pins + Connectors + Internal Lines
                AddPin(posPartitionSet1, pSet.color, moving);
                AddPin(posPartitionSet2, pSet.color, moving);
                AddConnectorPin(posPartitionSetConnectorPin1, pSet.color, moving);
                AddConnectorPin(posPartitionSetConnectorPin2, pSet.color, moving);
                AddLine(posPartitionSet1, posPartitionSetConnectorPin1, pSet.color, moving);
                AddLine(posPartitionSet2, posPartitionSetConnectorPin2, pSet.color, moving);
                AddLine(posPartitionSetConnectorPin1, posPartitionSetConnectorPin2, pSet.color, moving);

                // 2. Add Lines
                //for (int j = 0; j < 6; j++) globalDirLineSet[j] = false;
                foreach (var pin in pSet.pins)
                {
                    // Inner Line
                    Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                    AddLine(pin.isHead ? posPartitionSet1 : posPartitionSet2, posPin, pSet.color, moving);
                    // Outter Line
                    //if (state.hasNeighbor1[pin.globalDir])
                    //{
                    //    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    //    //AddLine(posPin, posOutterLineCenter, pSet.color, moving); // this does not look good enough
                    //    AddLine(posPin, posOutterLineCenter, Color.black, moving);
                    //    globalDirLineSet[pin.globalDir] = true;
                    //}
                }
            }
            // Add Connection Pins
            for (int i = 0; i < 6; i++)
            {
                if (state.hasNeighbor1[i] && state.neighbor1ToNeighbor2Direction != i)
                {
                    for (int j = 0; j < state.pinsPerSide; j++)
                    {
                        ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(i, j, true);
                        Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                        Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                        AddLine(posPin, posOutterLineCenter, Color.black, moving);
                    }
                }
                if (state.hasNeighbor2[i] && state.neighbor1ToNeighbor2Direction != ((i + 3) % 6))
                {
                    for (int j = 0; j < state.pinsPerSide; j++)
                    {
                        ParticlePinGraphicState.PinDef pin = new ParticlePinGraphicState.PinDef(i, j, true);
                        Vector2 posPin = CalculateGlobalPinPosition(snap.position2, pin, state.pinsPerSide);
                        Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position2, pin, state.pinsPerSide);
                        AddLine(posPin, posOutterLineCenter, Color.black, moving);
                    }
                }
            }
        }
    }

    public void AddLine(Vector2 globalLineStartPos, Vector2 globalLineEndPos, Color color, bool moving)
    {
        RendererCircuits_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuits_RenderBatch.PropertyBlockData(color, moving);
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
        batch.AddLine(globalLineStartPos, globalLineEndPos);
    }

    public void AddPin(Vector2 pinPos, Color color, bool moving)
    {
        RendererCircuitPins_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuitPins_RenderBatch.PropertyBlockData(color, moving);
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
        batch.AddPin(pinPos);
    }

    public void AddConnectorPin(Vector2 pinPos, Color color, bool moving)
    {
        RendererCircuitPins_RenderBatch.PropertyBlockData propertyBlockData = new RendererCircuitPins_RenderBatch.PropertyBlockData(color, moving);
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
        batch.AddConnectorPin(pinPos);
    }

    public void Render()
    {
        foreach (var batch in propertiesToRenderBatchMap.Values)
        {
            batch.Render();
        }
        foreach (var batch in propertiesToPinRenderBatchMap.Values)
        {
            batch.Render();
        }
    }

    public void ApplyUpdates(float animationStartTime, float animationDuration)
    {
        foreach (var batch in propertiesToRenderBatchMap.Values)
        {
            batch.ApplyUpdates(animationStartTime, animationDuration);
        }
        foreach (var batch in propertiesToPinRenderBatchMap.Values)
        {
            batch.ApplyUpdates(animationStartTime, animationDuration);
        }
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
    }







    

    private Vector2 CalculateGlobalPinPosition(Vector2Int gridPosParticle, ParticlePinGraphicState.PinDef pinDef, int pinsPerSide)
    {
        Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
        Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(pinDef, pinsPerSide, RenderSystem.global_particleScale);
        return posParticle + relPinPos;
    }

    private Vector2 CalculateGlobalPartitionSetPinPosition(Vector2Int gridPosParticle, int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees)
    {
        Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
        return posParticle + CalculateRelativePartitionSetPinPosition(partitionSetID, amountOfPartitionSetsAtNode, rotationDegrees);
    }

    private Vector2 CalculateGlobalExpandedPartitionSetCenterNodePosition(Vector2Int gridPosParticle, int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees)
    {
        Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
        return posParticle + CalculateRelativeExpandedPartitionSetCenterNodePosition(partitionSetID, amountOfPartitionSetsAtNode, rotationDegrees);
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

    private Vector2 CalculateRelativePartitionSetPinPosition(int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees)
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
            float height = (lineLength / 2f) - partitionSetID * (lineLength / (amountOfPartitionSetsAtNode - 1));
            Vector2 position = new Vector2(0f, height);
            position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
            return position;
        }
    }

    private Vector2 CalculateRelativeExpandedPartitionSetCenterNodePosition(int partitionSetID, int amountOfPartitionSetsAtNode, float rotationDegrees)
    {
        float relXPos = RenderSystem.global_particleScale * 0.7f;

        if (amountOfPartitionSetsAtNode == 1) return Quaternion.Euler(0f, 0f, rotationDegrees) * new Vector2(relXPos, 0f);
        else
        {
            float lineLength;
            switch (amountOfPartitionSetsAtNode)
            {
                case 2:
                    lineLength = RenderSystem.global_particleScale * 0.5f;
                    break;
                case 3:
                    lineLength = RenderSystem.global_particleScale * 0.5f;
                    break;
                default:
                    lineLength = RenderSystem.global_particleScale * 0.5f;
                    break;
            }
            float height = (lineLength / 2f) - partitionSetID * (lineLength / (amountOfPartitionSetsAtNode - 1));
            Vector2 position = new Vector2(relXPos, height);
            position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
            return position;
        }
    }
}