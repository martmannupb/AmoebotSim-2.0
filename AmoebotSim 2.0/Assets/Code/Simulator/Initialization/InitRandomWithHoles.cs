using System.Collections.Generic;
using UnityEngine;

public class InitRandomWithHoles : InitializationMethod
{
    public InitRandomWithHoles(ParticleSystem system) : base(system)
    {

    }

    public static new string Name { get { return "Random With Holes"; } }

    public void Generate(int numParticles, float holeProb, Initialization.Chirality chirality, Initialization.Compass compassDir)
    {
        if (numParticles < 1)
            return;

        int n = 1;
        // Always start by adding a particle at position (0, 0)
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int node = new Vector2Int(0, 0);
        AddParticle(node, Direction.NONE, chirality, compassDir);

        for (int d = 0; d < 6; d++)
            candidates.Add(ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d)));

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();       // Occupied by particles
        HashSet<Vector2Int> excluded = new HashSet<Vector2Int>();       // Reserved for holes
        occupied.Add(node);

        int numExcludedChosen = 0;

        while (n < numParticles)
        {
            // Find next position
            Vector2Int newPos = Vector2Int.zero;
            bool choseExcluded = false;
            if (candidates.Count > 0)
            {
                int randIdx = Random.Range(0, candidates.Count);
                newPos = candidates[randIdx];
                candidates.RemoveAt(randIdx);
            }
            else
            {
                // Choose random excluded position
                int randIdx = Random.Range(0, excluded.Count);
                int i = 0;
                foreach (Vector2Int v in excluded)
                {
                    if (i == randIdx)
                    {
                        newPos = v;
                        break;
                    }
                    i++;
                }
                numExcludedChosen++;
                excluded.Remove(newPos);
                choseExcluded = true;
            }

            // Either use newPos to insert particle or to insert hole
            if (choseExcluded || Random.Range(0.0f, 1.0f) >= holeProb)
            {
                for (int d = 0; d < 6; d++)
                {
                    Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                    if (!occupied.Contains(nbr) && !excluded.Contains(nbr) && !candidates.Contains(nbr))
                        candidates.Add(nbr);
                }

                AddParticle(newPos, Direction.NONE, chirality, compassDir);

                occupied.Add(newPos);
                n++;
            }
            else
            {
                excluded.Add(newPos);
            }
        }
        Log.Debug("Created system with " + n + " particles, had to choose " + numExcludedChosen + " excluded positions");
    }
}
