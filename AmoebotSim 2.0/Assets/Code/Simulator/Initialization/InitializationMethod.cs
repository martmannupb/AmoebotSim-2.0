using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Base class for algorithms that initialize a particle system.
    /// Inherit from this class and implement a method called
    /// <c>Generate</c> to create an initialization algorithm.
    /// <para>
    /// An initialization algorithm creates a system of
    /// <see cref="Sim.InitializationParticle"/>s, which is used as a
    /// template to initialize the system of <see cref="Sim.Particle"/>s
    /// in which the simulation is carried out.
    /// </para>
    /// </summary>
    public abstract class InitializationMethod
    {
        /// <summary>
        /// The system in which particles are created.
        /// </summary>
        private AS2.Sim.ParticleSystem system;

        public InitializationMethod(AS2.Sim.ParticleSystem system)
        {
            this.system = system;
        }

        /// <summary>
        /// Adds a new particle with the given parameters to the system.
        /// </summary>
        /// <param name="position">The initial tail position of the particle.</param>
        /// <param name="headDir">The initial global head direction of the particle.
        /// <see cref="Direction.NONE"/> means that the particle is contracted.</param>
        /// <param name="chirality">The chirality of the particle.</param>
        /// <param name="compassDir">The compass direction of the particle.</param>
        /// <returns>The new initialization particle.</returns>
        public InitializationParticle AddParticle(Vector2Int position, Direction headDir = Direction.NONE,
            Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise,
            Initialization.Compass compassDir = Initialization.Compass.E)
        {
            bool chir = true;
            if (chirality == Initialization.Chirality.Clockwise)
                chir = false;
            else if (chirality == Initialization.Chirality.Random)
                chir = Random.Range(0, 2) == 0;

            Direction comDir = compassDir == Initialization.Compass.Random ?
                DirectionHelpers.Cardinal(Random.Range(0, 6)) :
                DirectionHelpers.Cardinal((int)compassDir);

            return system.AddInitParticle(position, chir, comDir, headDir);
        }

        /// <summary>
        /// Tries to get the <see cref="AS2.Sim.InitializationParticle"/> at the given position.
        /// </summary>
        /// <param name="position">The grid position at which to look for the particle.</param>
        /// <param name="particle">The particle at the given position, if it exists,
        /// otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if and only if a particle was found at the given position.</returns>
        public bool TryGetParticleAt(Vector2Int position, out InitializationParticle particle)
        {
            return system.TryGetInitParticleAt(position, out particle);
        }

        /// <summary>
        /// Returns an array of all currently placed particles. This is useful
        /// for cases where particle parameters have to be set after the
        /// particles were placed and where chirality and compass directions
        /// were chosen randomly.
        /// </summary>
        /// <returns>An array containing all current particles.</returns>
        public InitializationParticle[] GetParticles()
        {
            return system.GetInitParticles();
        }

        /// <summary>
        /// Adds contracted particles in the shape of a parallelogram.
        /// The particles are added row by row, where
        /// <paramref name="length"/> determines how many particles are
        /// in a row and <paramref name="height"/> determines how many
        /// rows there are. Rows are offset to form an acute angle
        /// at the start location if <paramref name="acuteAngle"/> is
        /// <c>true</c>, i.e., the offset direction is the main direction
        /// rotated by 60° in counter-clockwise direction. Otherwise, the
        /// offset direction will be rotated by 120° and the angle becomes
        /// obtuse.
        /// </summary>
        /// <param name="startPos">The position at which the first particle
        /// will be placed.</param>
        /// <param name="mainDir">The direction in which rows grow.</param>
        /// <param name="length">The number of particles in each row.</param>
        /// <param name="acuteAngle">If <c>true</c>, the parallelogram's
        /// angle at the first particle is acute (60°), otherwise, it is
        /// obtuse (120°).</param>
        /// <param name="height">The number of rows.</param>
        /// <param name="chirality">The chirality setting for all particles.</param>
        /// <param name="compass">The compass setting for all particles.</param>
        public void PlaceParallelogram(Vector2Int startPos, Direction mainDir, int length, bool acuteAngle = true, int height = 1,
            Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compass = Initialization.Compass.E)
        {
            if (length < 1 || height < 1)
                return;

            if (!mainDir.IsCardinal())
            {
                Log.Warning("Cannot place parallelogram with main direction " + mainDir + ", must be a cardinal direction");
                return;
            }

            Direction secDir = acuteAngle ? mainDir.Rotate60(1) : mainDir.Rotate60(2);

            for (int h = 0; h < height; h++)
            {
                Vector2Int pos = startPos;

                for (int l = 0; l < length; l++)
                {
                    AddParticle(pos, Direction.NONE, chirality, compass);
                    pos = ParticleSystem_Utils.GetNbrInDir(pos, mainDir);
                }
                startPos = ParticleSystem_Utils.GetNbrInDir(startPos, secDir);
            }
        }

        public ParticleObject CreateObject(Vector2Int pos, int identifier = 0)
        {
            return new ParticleObject(pos, system, identifier);
        }

        public void AddObjectToSystem(ParticleObject o)
        {
            system.AddObject(o);
        }

        /// <summary>
        /// Generates a system with a fixed number of particles using a
        /// randomized breadth-first-search algorithm that leaves out some
        /// positions to insert holes. It is guaranteed that the desired
        /// number of particles is placed because if all available locations
        /// are marked as holes, one of them is chosen at random to be
        /// replaced with a particle and keep the algorithm going.
        /// All particles are contracted.
        /// </summary>
        /// <param name="numParticles">The number of particles to place.</param>
        /// <param name="holeProb">The probability of a grid position becoming
        /// a hole when it is first encountered. Note that the final system might
        /// still have holes if this is set to <c>0</c>.</param>
        /// <param name="chirality">The chirality setting for all particles.</param>
        /// <param name="compassDir">The compass setting for all particles.</param>
        public void GenerateRandomWithHoles(int numParticles = 50, float holeProb = 0.3f,
            Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compassDir = Initialization.Compass.E)
        {
            if (numParticles < 1)
                return;

            List<Vector2Int> positions = GenerateRandomConnectedPositions(Vector2Int.zero, numParticles, holeProb);
            foreach (Vector2Int pos in positions)
            {
                AddParticle(pos, Direction.NONE, chirality, compassDir);
            }

            return;
        }

        public List<Vector2Int> GenerateRandomConnectedPositions(Vector2Int startPos, int numPositions, float holeProb = 0.3f,
            bool fillHoles = false, System.Func<Vector2Int, bool> excludeFunc = null, bool allowExcludedHoles = true)
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            if (numPositions < 1)
                return positions;

            int n = 1;
            // Always start by adding the start position
            List<Vector2Int> candidates = new List<Vector2Int>();
            Vector2Int node = startPos;
            positions.Add(node);

            for (int d = 0; d < 6; d++)
                candidates.Add(ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d)));

            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();       // Occupied by particles
            HashSet<Vector2Int> holes = new HashSet<Vector2Int>();          // Reserved for holes
            HashSet<Vector2Int> excluded = new HashSet<Vector2Int>();       // Excluded from available positions by exclude function
            occupied.Add(node);

            // Mechanism for checking whether the placement is blocked
            int numIterationsBeforeCheck = numPositions / 10 + 1;           // Number of iterations without placement until block is checked
            int numIterationsWithoutChange = 0;
            bool somethingChanged;

            while (n < numPositions)
            {
                somethingChanged = false;
                Vector2Int newPos = Vector2Int.zero;
                bool foundNewPos = false;
                bool choseHole = false;

                // If nothing has changed for too long: Check if any position is eligible at all
                if (numIterationsWithoutChange >= numIterationsBeforeCheck)
                {
                    // First check candidates
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        Vector2Int pos = candidates[i];
                        if (!IsPositionHoleOpening(pos, occupied, excludeFunc, allowExcludedHoles))
                        {
                            newPos = pos;
                            foundNewPos = true;
                            candidates.RemoveAt(i);
                            break;
                        }
                    }

                    // Then check hole positions if necessary
                    if (!foundNewPos)
                    {
                        foreach (Vector2Int pos in holes)
                        {
                            if (!IsPositionHoleOpening(pos, occupied, excludeFunc, allowExcludedHoles))
                            {
                                newPos = pos;
                                foundNewPos = true;
                                choseHole = true;
                            }
                            if (foundNewPos)
                                break;
                        }
                        if (foundNewPos)
                            holes.Remove(newPos);
                    }

                    // If we still have not found a suitable position, abort
                    if (!foundNewPos)
                    {
                        Log.Warning("Could not place " + numPositions + " positions due to placement restrictions. Only placed " + positions.Count);
                        return positions;
                    }
                }

                // Find next position
                if (candidates.Count > 0)
                {
                    int randIdx = Random.Range(0, candidates.Count);
                    newPos = candidates[randIdx];
                    candidates.RemoveAt(randIdx);
                    foundNewPos = true;
                }
                else if (holes.Count > 0)
                {
                    // Choose random hole position
                    int randIdx = Random.Range(0, holes.Count);
                    int i = 0;
                    foreach (Vector2Int v in holes)
                    {
                        if (i == randIdx)
                        {
                            newPos = v;
                            break;
                        }
                        i++;
                    }
                    holes.Remove(newPos);
                    choseHole = true;
                    foundNewPos = true;
                }
                else
                {
                    // No holes or candidates to choose from, jump to next check
                    foundNewPos = false;
                    numIterationsWithoutChange = numIterationsBeforeCheck;
                }

                if (foundNewPos)
                {
                    // Either use newPos to insert position or to insert hole
                    if (choseHole || Random.Range(0.0f, 1.0f) >= holeProb)
                    {
                        // Check whether this position is permissible if we do not allow holes
                        if (fillHoles)
                        {
                            if (IsPositionHoleOpening(newPos, occupied, excludeFunc, allowExcludedHoles))
                            {
                                // Position is not allowed, return it to where it was before
                                if (choseHole)
                                    holes.Add(newPos);
                                else
                                    candidates.Add(newPos);
                                continue;
                            }
                        }

                        // Add available neighbors to candidates
                        for (int d = 0; d < 6; d++)
                        {
                            Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                            if (!occupied.Contains(nbr) && !holes.Contains(nbr) && !candidates.Contains(nbr) && !excluded.Contains(nbr))
                            {
                                // First check if the position should be excluded
                                if (excludeFunc != null && excludeFunc(nbr))
                                    excluded.Add(nbr);
                                else
                                    candidates.Add(nbr);
                            }
                        }

                        positions.Add(newPos);
                        occupied.Add(newPos);
                        n++;
                        somethingChanged = true;
                    }
                    else
                    {
                        holes.Add(newPos);
                        somethingChanged = true;
                    }
                }

                if (!somethingChanged)
                    numIterationsWithoutChange++;
                else
                    numIterationsWithoutChange = 0;
            }

            return positions;
        }

        private bool IsPositionHoleOpening(Vector2Int pos, HashSet<Vector2Int> occupied,
            System.Func<Vector2Int, bool> excludeFunc, bool allowExcludedHoles)
        {
            // Go around the position and count the number of switches between
            // occupied and unoccupied neighbors
            // If the number of switches is greater than 2, the position is a tunnel opening
            // and occupying it would create a hole
            // (The number of switches is always even, 0 and 2 switches are allowed,
            // 4 or more are not allowed)
            // If a neighbor position is excluded and excluded holes are allowed, the position
            // inherits the state of its predecessor ()
            Vector2Int[] nbrs = new Vector2Int[6];
            bool[] nbrOccupied = new bool[6];
            for (int d = 0; d < 6; d++)
            {
                Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(pos, DirectionHelpers.Cardinal(d));
                nbrs[d] = nbr;
                nbrOccupied[d] = occupied.Contains(nbr);
            }
            // Find excluded holes if applicable
            bool[] isExcluded = new bool[6];
            int numExcluded = 0;
            if (allowExcludedHoles && excludeFunc != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    isExcluded[i] = excludeFunc(nbrs[i]);
                    if (isExcluded[i])
                        numExcluded++;
                }
            }

            // Now count the number of switches
            int numSwitches = 0;
            for (int i = 0; i < 6; i++)
            {
                if (nbrOccupied[i] != nbrOccupied[(i + 1) % 6])
                    numSwitches++;
            }

            // If we have a hole situation: Check whether closing the hole is
            // allowed due to excluded positions
            if (numSwitches > 2 && allowExcludedHoles && excludeFunc != null)
            {
                // First compute the bounding rect of the occupied shape
                int xMin = pos.x;
                int xMax = pos.x;
                int yMin = pos.y;
                int yMax = pos.y;
                foreach (Vector2Int v in occupied)
                {
                    if (v.x < xMin)
                        xMin = v.x;
                    else if (v.x > xMax)
                        xMax = v.x;
                    if (v.y < yMin)
                        yMin = v.y;
                    else if (v.y > yMax)
                        yMax = v.y;
                }
                xMin -= 2;
                xMax += 2;
                yMin -= 2;
                yMax += 2;
                Vector2Int topLeft = new Vector2Int(xMin, yMax);
                Vector2Int topRight = new Vector2Int(xMax, yMax);
                Vector2Int botLeft = new Vector2Int(xMin, yMin);
                Vector2Int botRight = new Vector2Int(xMax, yMin);

                // Find all openings that do not contain an excluded position and
                // perform a search in each of them: If the search reaches an
                // excluded position or a position outside of the occupied shape,
                // the hole may be closed

                // First find the start indices of the openings and determine whether or not they
                // contain excluded positions
                int startIdx = -1;
                for (int i = 0; i < 6; i++)
                {
                    if (!nbrOccupied[i] && nbrOccupied[(i + 5) % 6])
                    {
                        startIdx = i;
                        break;
                    }
                }
                int holeIndex = -1;
                int[] startIndices = new int[3];
                bool[] hasExcluded = new bool[3];
                for (int i = 0; i < 6; i++)
                {
                    int idx = (startIdx + i) % 6;
                    if (!nbrOccupied[idx] && nbrOccupied[(idx + 5) % 6])
                    {
                        // A hole starts here
                        holeIndex++;
                        startIndices[holeIndex] = idx;
                    }
                    // This works because the first idx already starts a hole region
                    if (isExcluded[idx])
                        hasExcluded[holeIndex] = true;
                }

                // Now perform a search for each of the hole openings
                for (int i = 0; i <= holeIndex; i++)
                {
                    if (!hasExcluded[i])
                    {
                        // Get the start node and find out which of the corner nodes is the closest
                        Vector2Int startNode = nbrs[startIndices[i]];
                        int distTL = Distance(startNode, topLeft);
                        int distTR = Distance(startNode, topRight);
                        int distBL = Distance(startNode, botLeft);
                        int distBR = Distance(startNode, botRight);
                        int minDist = Mathf.Min(distTL, distTR, distBL, distBR);
                        Vector2Int goalNode;
                        if (distTL == minDist)
                            goalNode = topLeft;
                        else if (distTR == minDist)
                            goalNode = topRight;
                        else if (distBL == minDist)
                            goalNode = botLeft;
                        else
                            goalNode = botRight;

                        if (!HoleValidSearch(startNode, goalNode, pos, occupied, excludeFunc))
                        {
                            return true;
                        }
                    }
                }
                // If all openings are fine, the hole may be closed
                return false;
            }

            return numSwitches > 2;
        }

        private int Distance(Vector2Int p1, Vector2Int p2)
        {
            Vector2Int to = p2 - p1;
            // If the signs of the two distance components are equal,
            // we have to cover both of them
            // If they have opposite signs, we can cover the smaller
            // distance while moving toward the bigger one
            if (to.x * to.y >= 0)
                return Mathf.Abs(to.x + to.y);
            else
                return Mathf.Max(Mathf.Abs(to.x), Mathf.Abs(to.y));
        }

        private bool HoleValidSearch(Vector2Int startNode, Vector2Int goalNode, Vector2Int openingPos,
            HashSet<Vector2Int> occupied, System.Func<Vector2Int, bool> excludeFunc)
        {
            // Start BFS at start node, searching for goal node or excluded position
            // If no goal or excluded position is found, return false
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            queue.Enqueue(startNode);
            visited.Add(startNode);
            visited.Add(openingPos);

            while (queue.Count > 0)
            {
                Vector2Int node = queue.Dequeue();
                // Goal is reached if we are at the same x or y level as the goal node
                // (goal is corner of the bounding rect) or the node is excluded
                if (node.x == goalNode.x || node.y == goalNode.y || excludeFunc(node))
                    return true;

                for (int i = 0; i < 6; i++)
                {
                    Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(i));
                    if (!visited.Contains(nbr) && !occupied.Contains(nbr))
                    {
                        visited.Add(nbr);
                        queue.Enqueue(nbr);
                    }
                }
            }

            return false;
        }
    }

} // namespace AS2
