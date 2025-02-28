// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


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

        private class DistanceSortedVectorList
        {
            private List<Vector2Int> list;
            private Vector2Int center;

            public DistanceSortedVectorList(Vector2Int center)
            {
                list = new List<Vector2Int>();
                this.center = center;
            }

            public Vector2Int this[int index]
            {
                get { return list[index]; }
            }

            public void Clear()
            {
                list.Clear();
            }

            public int Count
            {
                get { return list.Count; }
            }

            public void Remove(Vector2Int v)
            {
                list.Remove(v);
            }

            public void RemoveAt(int index)
            {
                list.RemoveAt(index);
            }

            public bool Contains(Vector2Int v)
            {
                return list.Contains(v);
            }

            public void Add(Vector2Int v)
            {
                // Empty list: Just add the new vector
                if (list.Count == 0)
                {
                    list.Add(v);
                    return;
                }

                // Check if new vector can be added at the front or the back
                int distNew = ParticleSystem_Utils.GridDistance(v, center);
                int distFirst = ParticleSystem_Utils.GridDistance(list[0], center);
                int distLast = ParticleSystem_Utils.GridDistance(list[list.Count - 1], center);
                if (distNew < distFirst)
                {
                    list.Insert(0, v);
                    return;
                }
                else if (distNew >= distLast)
                {
                    list.Add(v);
                    return;
                }

                // New vector cannot be added at front or back, use binary search to find correct location
                int left = 0;
                int right = list.Count - 1;
                int middle;
                int distMiddle;
                while (left < right - 1)
                {
                    middle = (left + right) / 2;
                    distMiddle = ParticleSystem_Utils.GridDistance(list[middle], center);
                    if (distNew < distMiddle)
                        right = middle;
                    else
                        left = middle;
                }
                list.Insert(right, v);
            }
        }

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
        /// Removes the <see cref="AS2.Sim.InitializationParticle"/> at the given position.
        /// </summary>
        /// <param name="position">The grid position from which a particle should
        /// be removed. If an expanded particle occupies this position, its other
        /// occupied position will be free as well.</param>
        public void RemoveParticleAt(Vector2Int position)
        {
            if (system.TryGetInitParticleAt(position, out InitializationParticle ip))
            {
                system.RemoveParticle(ip);
            }
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
        /// rotated by 60� in counter-clockwise direction. Otherwise, the
        /// offset direction will be rotated by 120� and the angle becomes
        /// obtuse.
        /// </summary>
        /// <param name="startPos">The position at which the first particle
        /// will be placed.</param>
        /// <param name="mainDir">The direction in which rows grow.</param>
        /// <param name="length">The number of particles in each row.</param>
        /// <param name="acuteAngle">If <c>true</c>, the parallelogram's
        /// angle at the first particle is acute (60�), otherwise, it is
        /// obtuse (120�).</param>
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

        /// <summary>
        /// Creates a new object occupying the given position.
        /// When the object is finished, submit it to the system
        /// by calling <see cref="AddObjectToSystem(ParticleObject)"/>.
        /// </summary>
        /// <param name="pos">The first grid position occupied
        /// by the new object.</param>
        /// <param name="identifier">The identifier of the new object.
        /// Does not have to be unique.</param>
        /// <returns>A new object with the given <paramref name="identifier"/>
        /// occupying the given position <paramref name="pos"/>.</returns>
        public ParticleObject CreateObject(Vector2Int pos, int identifier = 0)
        {
            return new ParticleObject(pos, system, identifier);
        }

        /// <summary>
        /// Adds a copy of the given object <paramref name="o"/> to
        /// the system. Note that you cannot make any changes to an
        /// object once it has been added to the system.
        /// </summary>
        /// <param name="o">The object to be added.</param>
        public void AddObjectToSystem(ParticleObject o)
        {
            system.AddObject(o.Copy());
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

        /// <summary>
        /// Randomly generates a set of connected grid positions
        /// according to the given constraints.
        /// <para>
        /// The algorithm performs a breadth-first search starting at the
        /// given <paramref name="startPos"/>. In each iteration, one of
        /// the open neighbors is chosen randomly to place a position or
        /// mark a position as a hole. If there are no more open neighbor
        /// positions, one of the marked hole positions is chosen instead.
        /// Before placing a new position, it is checked whether doing this
        /// would close an inner boundary in case this is not allowed.
        /// </para>
        /// </summary>
        /// <param name="startPos">The start position of the shape. This
        /// position is always part of the resulting set, even if it should
        /// be excluded.</param>
        /// <param name="numPositions">The desired number of grid positions
        /// in the generated set.</param>
        /// <param name="holeProb">The probability of a random candidate position
        /// selected to be kept unoccupied.</param>
        /// <param name="fillHoles">If <c>true</c>, the system of positions
        /// is not allowed to have inner boundaries.</param>
        /// <param name="excludeFunc">A function that marks grid positions
        /// as excluded such that no excluded position is contained in
        /// the resulting set. Note that it is possible that the resulting set
        /// is smaller than desired if there is not enough room for all positions.</param>
        /// <param name="allowExcludedHoles">If <paramref name="fillHoles"/>
        /// is <c>true</c>, this controls whether inner boundaries that
        /// surround empty regions containing excluded positions are allowed.</param>
        /// <param name="prioritizeInner">If <c>true</c>, new positions are not
        /// selected uniformly at random from the set of candidate locations, but
        /// using an exponential distribution that prioritizes positions close to
        /// the <paramref name="startPos"/>.</param>
        /// <param name="lambda">The lambda parameter of the exponential distribution
        /// if <paramref name="prioritizeInner"/> is <c>true</c>. Larger values
        /// lead to a stronger bias towards positions close to <paramref name="startPos"/>.
        /// Very small values can lead to a bias towards positions further away.</param>
        /// <returns>A list of grid positions forming a connected system according
        /// to the specified constraints.</returns>
        public List<Vector2Int> GenerateRandomConnectedPositions(Vector2Int startPos, int numPositions, float holeProb = 0.3f,
            bool fillHoles = false, System.Func<Vector2Int, bool> excludeFunc = null, bool allowExcludedHoles = true,
            bool prioritizeInner = false, float lambda = 0.1f)
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            if (numPositions < 1)
                return positions;

            int n = 1;
            // Always start by adding the start position
            DistanceSortedVectorList candidates = new DistanceSortedVectorList(startPos);
            Vector2Int node = startPos;
            positions.Add(node);

            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();       // Occupied by particles
            HashSet<Vector2Int> holes = new HashSet<Vector2Int>();          // Reserved for holes
            HashSet<Vector2Int> excluded = new HashSet<Vector2Int>();       // Excluded from available positions by exclude function
            occupied.Add(node);

            // Collect the start position's neighbors
            for (int d = 0; d < 6; d++)
            {
                Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d));
                if (excludeFunc == null || !excludeFunc(nbr))
                    candidates.Add(nbr);
                else
                    excluded.Add(nbr);
            }

            // Mechanism for checking whether the placement is blocked
            int numIterationsBeforeCheck = numPositions / 10 + 1;           // Number of iterations without placement until block is checked
            int numIterationsWithoutChange = 0;
            bool somethingChanged;

            while (n < numPositions)
            {
                somethingChanged = false;
                Vector2Int newPos = Vector2Int.zero;
                bool bruteForcedPos = false;
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
                                break;
                            }
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
                    bruteForcedPos = true;
                }

                // Decide whether to place position or hole
                bool placePosition = foundNewPos && choseHole
                    || !foundNewPos && candidates.Count == 0 || Random.Range(0.0f, 1.0f) >= holeProb;

                if (placePosition)
                {
                    // Select a random position if none has been selected yet
                    if (!foundNewPos)
                    {
                        if (candidates.Count > 0)
                        {
                            int randIdx;
                            if (prioritizeInner)
                            {
                                randIdx = Mathf.RoundToInt(-Mathf.Log(Random.Range(0.00001f, 1f)) / lambda);
                                randIdx = Mathf.Clamp(randIdx, 0, candidates.Count - 1);
                            }
                            else
                                randIdx = Random.Range(0, candidates.Count);
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
                            // No holes or candidates to choose from, jump to next blockade check
                            numIterationsWithoutChange = numIterationsBeforeCheck;
                            continue;
                        }
                    }

                    // Check if the position is allowed if it has not been brute forced anyway
                    bool permissible = true;
                    if (!bruteForcedPos && fillHoles)
                    {
                        if (IsPositionHoleOpening(newPos, occupied, excludeFunc, allowExcludedHoles))
                        {
                            permissible = false;
                            // Position is not allowed, return it to where it was before
                            if (choseHole)
                                holes.Add(newPos);
                            else
                                candidates.Add(newPos);
                        }
                    }

                    if (permissible)
                    {
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
                }
                else
                {
                    // Want to place a hole, simply select random candidate position
                    // if none has been chosen yet
                    if (!foundNewPos)
                    {
                        int idx = Random.Range(0, candidates.Count);
                        newPos = candidates[idx];
                        candidates.RemoveAt(idx);
                    }
                    holes.Add(newPos);
                    somethingChanged = true;
                }

                if (!somethingChanged)
                    numIterationsWithoutChange++;
                else
                    numIterationsWithoutChange = 0;
            }

            return positions;
        }

        /// <summary>
        /// Random placement helper that checks whether placing a position
        /// would close an inner boundary that should not be closed.
        /// </summary>
        /// <param name="pos">The new position to be placed.</param>
        /// <param name="occupied">The set of already occupied positions.</param>
        /// <param name="excludeFunc">A function marking some grid nodes as excluded.</param>
        /// <param name="allowExcludedHoles">Determines whether inner boundaries around
        /// empty regions that contain excluded positions are allowed.</param>
        /// <returns><c>true</c> if and only if placing <paramref name="pos"/> would
        /// close an inner boundary that should not be closed.</returns>
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
                        int distTL = ParticleSystem_Utils.GridDistance(startNode, topLeft);
                        int distTR = ParticleSystem_Utils.GridDistance(startNode, topRight);
                        int distBL = ParticleSystem_Utils.GridDistance(startNode, botLeft);
                        int distBR = ParticleSystem_Utils.GridDistance(startNode, botRight);
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

        /// <summary>
        /// Random placement helper that checks whether a hole that
        /// would be closed by placing a new position is allowed or not.
        /// Uses a BFS to search for a goal node that is a corner of the
        /// occupied system's bounding box or for any excluded position.
        /// </summary>
        /// <param name="startNode">The node from which the search
        /// should be started.</param>
        /// <param name="goalNode">The closest corner of the occupied
        /// system's bounding rectangle. If the search ever visits this
        /// position's x or y coordinate, the hole is considered to be
        /// open to the outer boundary.</param>
        /// <param name="openingPos">The grid node where the new position
        /// should be placed, used to limit the search to this side of
        /// the hole.</param>
        /// <param name="occupied">The set of occupied positions.</param>
        /// <param name="excludeFunc">The function that marks positions
        /// as excluded. Must not be <c>null</c>.</param>
        /// <returns><c>true</c> if and only if the search reaches the
        /// bounding rectangle of the occupied system or an excluded node.</returns>
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
