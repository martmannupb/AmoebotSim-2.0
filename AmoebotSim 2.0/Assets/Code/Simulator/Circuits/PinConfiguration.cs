using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinConfiguration
{
    private Particle particle;
    private int pinsPerEdge;
    private int expansionDir;
    private int numPins;

    private Pin[] pins;
    private PartitionSet[] partitionSets;

    public PinConfiguration(Particle particle, int pinsPerEdge, int expansionDir = -1)
    {
        this.particle = particle;
        this.pinsPerEdge = pinsPerEdge;
        this.expansionDir = expansionDir;
        this.numPins = expansionDir == -1 ? 6 * pinsPerEdge : 10 * pinsPerEdge;

        partitionSets = new PartitionSet[numPins];
        pins = new Pin[numPins];

        // Initialize
        if (expansionDir == -1)
        {
            // Initialize partition sets
            // Default is singleton: Each pin is its own partition set (for now)
            for (int i = 0; i < 6 * pinsPerEdge; i++)
            {
                partitionSets[i] = new PartitionSet(this, i, 1);
            }
            // Then initialize pins
            for (int locDir = 0; locDir < 6; locDir++)
            {
                for (int p = 0; p < pinsPerEdge; p++)
                {
                    int id = locDir * pinsPerEdge + p;
                    pins[id] = new Pin(partitionSets[id], id, locDir, true, p);
                    partitionSets[id].AddPin(id);
                }
            }
        } else
        {
            // TODO
            // Need label conversion operations here
        }
    }
}
