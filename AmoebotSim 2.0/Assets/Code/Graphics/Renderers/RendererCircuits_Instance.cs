using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCircuits_Instance
{

    public Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererCircuits_RenderBatch.PropertyBlockData, RendererCircuits_RenderBatch>();

    // Data

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
                Vector2 posPartitionSet = CalculateGlobalPartitionSetPinPosition(snap.position1, i, amountPartitionSets, RenderSystem.global_particleScale * 0.8f, 0f);
                foreach (var pin in pSet.pins)
                {
                    // Inner Line
                    Vector2 posPin = CalculateGlobalPinPosition(snap.position1, pin, state.pinsPerSide);
                    AddLine(posPartitionSet, posPin, pSet.color, moving);
                    // Outter Line
                    Vector2 posOutterLineCenter = CalculateGlobalOutterPinLineCenterPosition(snap.position1, pin, state.pinsPerSide);
                    //AddLine(posPin, posOutterLineCenter, pSet.color, moving);
                    AddLine(posPin, posOutterLineCenter, Color.black, moving);
                    //if(pin.globalDir == 3)
                    //{
                    //    Debug.Log("pinCenter: "+ AmoebotFunctions.CalculateAmoebotCenterPositionVector2(snap.position1)+", pinPos: " + posPin + ", posOutterLineCenter: " + posOutterLineCenter);
                    //}
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

    public void Render()
    {
        foreach (var batch in propertiesToRenderBatchMap.Values)
        {
            batch.Render();
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
    }







    

    private Vector2 CalculateGlobalPinPosition(Vector2Int gridPosParticle, ParticlePinGraphicState.PinDef pinDef, int pinsPerSide)
    {
        Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
        Vector2 relPinPos = AmoebotFunctions.CalculateRelativePinPosition(pinDef, pinsPerSide, RenderSystem.global_particleScale);
        return posParticle + relPinPos;
    }

    private Vector2 CalculateGlobalPartitionSetPinPosition(Vector2Int gridPosParticle, int partitionSetID, int amountOfPartitionSetsAtNode, float placementLineLength, float rotationDegrees)
    {
        Vector2 posParticle = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(gridPosParticle);
        return posParticle + CalculateRelativePartitionSetPinPosition(partitionSetID, amountOfPartitionSetsAtNode, placementLineLength, rotationDegrees);
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

    private Vector2 CalculateRelativePartitionSetPinPosition(int partitionSetID, int amountOfPartitionSetsAtNode, float placementLineLength, float rotationDegrees)
    {
        if (amountOfPartitionSetsAtNode == 1) return Vector2.zero;
        else
        {
            float height = (placementLineLength / 2f) - (amountOfPartitionSetsAtNode - 1) * (placementLineLength / (amountOfPartitionSetsAtNode - 1));
            Vector2 position = new Vector2(0f, height);
            position = Quaternion.Euler(0f, 0f, rotationDegrees) * position;
            return position;
        }
    }



}