using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinConfiguration
{
    public Particle particle;
    public int pinsPerEdge;
    public int expansionDir;
    public int numPins;

    public Pin[] pins;
    public PartitionSet[] partitionSets;

    public PinConfiguration(Particle particle, int pinsPerEdge, int expansionDir = -1)
    {
        this.particle = particle;
        this.pinsPerEdge = pinsPerEdge;
        this.expansionDir = expansionDir;

        numPins = expansionDir == -1 ? (6 * pinsPerEdge) : (10 * pinsPerEdge);

        partitionSets = new PartitionSet[numPins];
        pins = new Pin[numPins];

        // Initialize partition sets and pins
        // Default is singleton: Each pin is its own partition set (for now)
        if (expansionDir == -1)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                for (int idx = 0; idx < pinsPerEdge; idx++)
                {
                    int id = direction * pinsPerEdge + idx;
                    PartitionSet ps = new PartitionSet(this, id, numPins);
                    Pin pin = new Pin(ps, id, direction, true, idx);
                    ps.AddPin(id);
                    partitionSets[id] = ps;
                    pins[id] = pin;
                }
            }
        }
        else
        {
            for (int label = 0; label < 10; label++)
            {
                int direction = ParticleSystem_Utils.GetDirOfLabel(label, expansionDir);
                bool isHead = ParticleSystem_Utils.IsHeadLabel(label, expansionDir);
                for (int idx = 0; idx < pinsPerEdge; idx++)
                {
                    int id = label * pinsPerEdge + idx;
                    PartitionSet ps = new PartitionSet(this, id, numPins);
                    Pin pin = new Pin(ps, id, direction, isHead, idx);
                    ps.AddPin(id);
                    partitionSets[id] = ps;
                    pins[id] = pin;
                }
            }
        }
    }
}
