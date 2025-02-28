// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Visuals;

namespace AS2.Sim
{

    /// <summary>
    /// Represents objects in the particle system that the
    /// particles can interact with.
    /// <para>
    /// An object is a structure occupying a connected set of
    /// grid nodes. Particles can connect to objects through
    /// bonds and move the objects using the joint movement
    /// mechanisms. Objects also form bonds to each other.
    /// A particle can trigger a neighboring object to release
    /// all its bonds to other objects.
    /// </para>
    /// </summary>
    public class ParticleObject : IParticleObject, IObjectInfo, IReplayHistory
    {

        private ParticleSystem system;

        /// <summary>
        /// The graphics adapter of this object. Used to represent
        /// the object in the render system and make visualization updates.
        /// </summary>
        public ObjectGraphicsAdapter graphics;

        /// <summary>
        /// The global root position of the object. This position
        /// marks the origin of the local coordinate system and is
        /// always occupied by the object.
        /// </summary>
        private Vector2Int position;
        /// <summary>
        /// The history of root positions.
        /// </summary>
        private ValueHistory<Vector2Int> positionHistory;
        /// <summary>
        /// The list of positions occupied by the object in
        /// local coordinates, i.e., relative to the root position.
        /// </summary>
        private List<Vector2Int> occupiedRel;

        /// <summary>
        /// The current root position of the object.
        /// </summary>
        public Vector2Int Position
        {
            get { return position; }
        }

        /// <summary>
        /// The object's int identifier. Does not have to be unique.
        /// </summary>
        private int identifier = 0;

        public int Identifier
        {
            get { return identifier; }
            set {
                if (system.InInitializationState)
                    identifier = value;
                else
                    Log.Warning("Identifier cannot be changed in Simulation Mode");
            }
        }

        /// <summary>
        /// The current display color of the object.
        /// </summary>
        private Color color = Color.black;
        /// <summary>
        /// The history of colors.
        /// </summary>
        private ValueHistory<Color> colorHistory;
        /// <summary>
        /// The display color of the object.
        /// </summary>
        public Color Color
        {
            get { return color; }
            set {
                color = value;
                colorHistory.RecordValueInRound(color, system.CurrentRound);
                graphics.UpdateColor();
            }
        }

        /// <summary>
        /// The absolute offset from the object's initial location
        /// that was accumulated by joint movements.
        /// </summary>
        public Vector2Int jmOffset;
        /// <summary>
        /// The history of joint movement offsets. Used for
        /// storing animation data.
        /// </summary>
        private ValueHistory<Vector2Int> jmOffsetHistory;

        /// <summary>
        /// Indicates whether this object has already received a
        /// joint movement offset during the movement simulation.
        /// </summary>
        public bool receivedJmOffset = false;

        /// <summary>
        /// Indicates whether this object should release all bonds to
        /// other objects in the current movement simulation. Must be
        /// reset after each round.
        /// </summary>
        public bool releaseBonds = false;

        /// <summary>
        /// The edges on the boundary of the object, used for collision detection.
        /// Every entry <c>(x, y, z)</c> stores a relative node position
        /// <c>(x, y)</c> and a cardinal direction code <c>z</c>.
        /// </summary>
        private HashSet<Vector3Int> boundaryEdges = new HashSet<Vector3Int>();

        public ParticleObject(Vector2Int position, ParticleSystem system, int identifier = 0)
        {
            this.position = position;
            this.system = system;
            this.identifier = identifier;
            positionHistory = new ValueHistory<Vector2Int>(position, system.CurrentRound);
            colorHistory = new ValueHistory<Color>(color, system.CurrentRound);
            jmOffsetHistory = new ValueHistory<Vector2Int>(Vector2Int.zero, system.CurrentRound);
            occupiedRel = new List<Vector2Int>();
            occupiedRel.Add(Vector2Int.zero);

            graphics = new ObjectGraphicsAdapter(this, system.renderSystem.rendererObj);
        }

        /// <summary>
        /// Adds a new position to the object. The position
        /// does not have to be connected to the other positions
        /// as long as the object is connected when the simulation starts.
        /// <para>
        /// If the object is already registered in the render
        /// system, its mesh will be recalculated.
        /// </para>
        /// </summary>
        /// <param name="pos">The global position that should
        /// be added to the object.</param>
        public void AddPosition(Vector2Int pos)
        {
            AddPositionRel(pos - position);
        }

        /// <summary>
        /// Similar to <see cref="AddPosition(Vector2Int)"/>,
        /// but specifies the new position in local coordinates,
        /// relative to the object's root position.
        /// </summary>
        /// <param name="posRel">The local position that should
        /// be added to the object.</param>
        public void AddPositionRel(Vector2Int posRel)
        {
            // Don't do anything if the position is already occupied
            if (occupiedRel.Contains(posRel))
                return;

            // First check if the position can be registered in the system
            if (graphics.IsRegistered)
            {
                if (!system.AddPositionToObject(this, posRel + position))
                {
                    Log.Error("Unable to add relative position " + posRel + " to object.");
                    return;
                }
            }
            
            occupiedRel.Add(posRel);
            if (graphics.IsRegistered)
                graphics.GenerateMesh();
        }

        /// <summary>
        /// Computes the set of global positions occupied
        /// by the object.
        /// </summary>
        /// <returns>An array containing the global grid
        /// coordinates of all nodes occupied by the object.</returns>
        public Vector2Int[] GetOccupiedPositions()
        {
            Vector2Int[] p = occupiedRel.ToArray();
            for (int i = 0; i < p.Length; i++)
            {
                p[i].x += position.x;
                p[i].y += position.y;
            }
            return p;
        }

        /// <summary>
        /// Returns the set of relative positions occupied
        /// by the object.
        /// </summary>
        /// <returns>An array containing the grid coordinates of
        /// all occupied nodes relative to the object position.</returns>
        public Vector2Int[] GetRelPositions()
        {
            return occupiedRel.ToArray();
        }

        /// <summary>
        /// Moves the entire object by the given offset.
        /// Only affects the object's local data. The object
        /// is not automatically moved in the system.
        /// </summary>
        /// <param name="offset">The offset vector by which
        /// the object should be moved.</param>
        public void MovePosition(Vector2Int offset)
        {
            position += offset;
            positionHistory.RecordValueInRound(position, system.CurrentRound);
        }

        /// <summary>
        /// Moves the entire object to the given position.
        /// This can be used in Init Mode to easily create
        /// multiple copies of a shape in different locations.
        /// </summary>
        /// <param name="position">The new position of the
        /// object's origin.</param>
        public void MoveTo(Vector2Int position)
        {
            this.position = position;
            positionHistory.RecordValueInRound(position, system.CurrentRound);
        }

        /// <summary>
        /// Creates a copy of this object. The copy is not
        /// added to the render system automatically, even
        /// if the original has already been added.
        /// </summary>
        /// <returns>A copy of this object.</returns>
        public ParticleObject Copy()
        {
            ParticleObject copy = new ParticleObject(position, system, identifier);
            copy.occupiedRel.RemoveAt(0);
            copy.occupiedRel.AddRange(occupiedRel);
            copy.Color = color;
            return copy;
        }

        public void SetColor(Color color)
        {
            Color = color;
        }

        /// <summary>
        /// Checks whether the object occupies a
        /// connected set of grid nodes.
        /// </summary>
        /// <returns><c>true</c> if and only if the
        /// object is a connected shape.</returns>
        public bool IsConnected()
        {
            return IsConnected(Vector2Int.zero, false);
        }

        public bool IsConnected(Vector2Int removedPosition)
        {
            // First check if the removed position is even used
            removedPosition -= position;
            bool useRemovedPosition = occupiedRel.Contains(removedPosition);
            if (useRemovedPosition && occupiedRel.Count == 1)
            {
                // The empty set is connected
                return true;
            }

            return IsConnected(removedPosition, useRemovedPosition);
        }

        /// <summary>
        /// Helper implementing the actual connectivity check.
        /// The case that the object occupies only one node and
        /// this node is excluded should already have been handled.
        /// </summary>
        /// <param name="removedPosition">The relative position that
        /// should be excluded from the connectivity check. Must be
        /// part of the object.</param>
        /// <param name="useRemovedPosition">Whether the removed
        /// position should be used.</param>
        /// <returns><c>true</c> if and only if the object is a
        /// connected shape.</returns>
        private bool IsConnected(Vector2Int removedPosition, bool useRemovedPosition = false)
        {
            /*
             * Calculate helper data structure:
             * We compute the bounding rect of the object and
             * create an int matrix telling us where the
             * occupied positions of the object are. The entries
             * are the list indices of the nodes.
             * This enables us to find neighbors in constant
             * time rather than O(n).
             */

            // First find the dimensions of the bounding rect
            int xMin = 0;
            int xMax = 0;
            int yMin = 0;
            int yMax = 0;
            foreach (Vector2Int v in occupiedRel)
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
            int sizeX = xMax - xMin + 1;
            int sizeY = yMax - yMin + 1;

            // Create and fill the matrix
            // Stretch the dimensions by one position in each direction
            // Indices range from 0 to sizeN + 1
            // Occupied positions are 1 to sizeN
            int[,] matrix = new int[sizeX + 2, sizeY + 2];
            // Fill with -1
            for (int i = 0; i < sizeX + 2; i++)
                for (int j = 0; j < sizeY + 2; j++)
                    matrix[i, j] = -1;
            for (int i = 0; i < occupiedRel.Count; i++)
            {
                Vector2Int v = occupiedRel[i];
                if (!useRemovedPosition || v != removedPosition)
                    matrix[v.x - xMin + 1, v.y - yMin + 1] = i;
            }

            // Array containing neighbor coordinate offsets
            Vector2Int[] nbrOffsets = new Vector2Int[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(1, -1)
            };

            // Perform a BFS on the occupied positions, using the
            // matrix as a helper
            Queue<int> queue = new Queue<int>();
            bool[] visited = new bool[occupiedRel.Count];
            // Find a starting position
            int numVisited = 1;
            for (int i = 0; i < occupiedRel.Count; i++)
            {
                if (!useRemovedPosition || occupiedRel[i] != removedPosition)
                {
                    visited[i] = true;
                    queue.Enqueue(i);
                    break;
                }
            }

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                Vector2Int pos = occupiedRel[current];
                foreach (Vector2Int offset in nbrOffsets)
                {
                    Vector2Int nbr = pos + offset;
                    int idx = matrix[nbr.x - xMin + 1, nbr.y - yMin + 1];
                    if (idx != -1 && !visited[idx])
                    {
                        visited[idx] = true;
                        numVisited++;
                        queue.Enqueue(idx);
                    }
                }
            }

            if (useRemovedPosition)
                return numVisited == occupiedRel.Count - 1;
            else
                return numVisited == occupiedRel.Count;
        }

        /// <summary>
        /// Calculates all boundaries of the object so that the object
        /// can be used in collision checks. This should be called once
        /// when the simulation starts and the object is complete.
        /// </summary>
        /// <param name="debug">Whether debug lines showing the detected
        /// edges should be drawn.</param>
        public void CalculateBoundaries(bool debug = false)
        {
            // If we have only one part: No boundary edges
            if (occupiedRel.Count == 1)
            {
                return;
            }

            /*
             * Calculate helper data structures:
             * We compute the bounding rect of the object and
             * create an int matrix telling us where the
             * occupied positions of the object are. -1 entries
             * are empty and other entries give the index of the
             * node occupying this position.
             * This enables us to check whether a position is
             * occupied in constant time rather than O(n).
             */

            // First find the dimensions of the bounding rect
            int xMin = 0;
            int xMax = 0;
            int yMin = 0;
            int yMax = 0;
            foreach (Vector2Int v in occupiedRel)
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
            int sizeX = xMax - xMin + 1;
            int sizeY = yMax - yMin + 1;

            // Create and fill the matrix
            // Stretch the dimensions by one position in each direction
            // Indices range from 0 to sizeN + 1
            // Occupied positions are 1 to sizeN
            int[,] matrix = new int[sizeX + 2, sizeY + 2];
            for (int i = 0; i < sizeX + 2; i++)
                for (int j = 0; j < sizeY + 2; j++)
                    matrix[i, j] = -1;
            for (int i = 0; i < occupiedRel.Count; i++)
            {
                Vector2Int v = occupiedRel[i];
                matrix[v.x - xMin + 1, v.y - yMin + 1] = i;
            }

            /*
             * We also create a bool matrix telling us whether
             * each direction of a node has been considered in
             * a boundary detection pass. This allows us to
             * avoid duplicate boundaries.
             */
            bool[,] finishedDirections = new bool[occupiedRel.Count, 6];

            /*
             * Start with outer boundary
             */

            // Find the left- and topmost position (must be on the outer boundary)
            Vector2Int top = occupiedRel[0];
            bool foundTop = false;
            for (int y = sizeY; y > 0; y--)
            {
                for (int x = 1; x < sizeX + 1; x++)
                {
                    if (matrix[x, y] != -1)
                    {
                        top = new Vector2Int(x + xMin - 1, y + yMin - 1);
                        foundTop = true;
                        break;
                    }
                }
                if (foundTop)
                    break;
            }

            // top has no neighbors in W, NNW and NNE direction
            // Now walk around the outer boundary in clockwise direction
            Direction boundaryDir = Direction.NNE;  // boundaryDir is like a normal pointing away from the shape
            boundaryEdges.Clear();

            ComputeBoundary(top, boundaryDir, boundaryEdges, xMin, yMin, matrix, finishedDirections);

            // Now do the same for the inner boundaries
            for (int i = 0; i < occupiedRel.Count; i++)
            {
                Vector2Int node = occupiedRel[i];
                for (int j = 0; j < 6; j++)
                {
                    if (!finishedDirections[i, j])
                    {
                        Direction d = DirectionHelpers.Cardinal(j);
                        Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(node, d);
                        if (matrix[nbr.x - xMin + 1, nbr.y - yMin + 1] == -1)
                        {
                            // Compute new boundary here
                            ComputeBoundary(node, d, boundaryEdges, xMin, yMin, matrix, finishedDirections);
                        }
                    }

                }
            }

            // Draw the boundary for debugging
            if (debug)
            {
                Debug.Log("Computed " + boundaryEdges.Count + " boundary edges");
                foreach (Vector3Int v in boundaryEdges)
                {
                    Vector2Int start = new Vector2Int(v.x, v.y) + position;
                    Vector2Int end = ParticleSystem_Utils.GetNbrInDir(start, DirectionHelpers.Cardinal(v.z));
                    Debug.DrawLine(AmoebotFunctions.GridToWorldPositionVector3(start, -5f), AmoebotFunctions.GridToWorldPositionVector3(end, -5f), Color.blue, 15f);
                }
            }
        }

        /// <summary>
        /// Helper method that collects the boundary nodes and
        /// direction vectors for the boundary to which the given
        /// node belongs.
        /// </summary>
        /// <param name="start">The starting node whose boundary
        /// should be computed.</param>
        /// <param name="initialBoundaryDir">A cardinal direction that
        /// points from <paramref name="start"/> towards the empty
        /// space the boundary is adjacent to.</param>
        /// <param name="edges">The set into which the detected edges
        /// should be written. The format for each edge is <c>(x, y, z)</c>,
        /// where <c>(x, y)</c> is the relative position of a boundary node
        /// and <c>z</c> is the cardinal direction int of the edge.</param>
        /// <param name="xMin">The minimum x coordinate of the object's
        /// bounding rectangle, used for indexing the object matrix.</param>
        /// <param name="yMin">The minimum y coordinate of the object's
        /// bounding rectangle, used for indexing the object matrix.</param>
        /// <param name="matrix">The object matrix that maps relative
        /// coordinates in the bounding rectangle to node indices.</param>
        /// <param name="finishedDirections">A matrix indicating which
        /// directions of which node have been considered for boundary
        /// detection already. Will be updated with all nodes and
        /// directions belonging to the new boundary.</param>
        private void ComputeBoundary(Vector2Int start, Direction initialBoundaryDir,
            HashSet<Vector3Int> edges,
            int xMin, int yMin,
            int[,] matrix, bool[,] finishedDirections)
        {
            // Rotate initial boundary direction to the left until a neighbor is found
            // => ensures that all empty neighbors of the boundary are checked
            for (int i = 0; i < 6; i++)
            {
                Direction d = initialBoundaryDir.Rotate60(1);
                Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(start, d);
                if (matrix[nbr.x - xMin + 1, nbr.y - yMin + 1] != -1)
                {
                    break;
                }
                initialBoundaryDir = d;
            }

            Direction boundaryDir = initialBoundaryDir;
            Vector2Int current = start;
            Vector2Int last = current;
            int currentIdx = matrix[start.x - xMin + 1, start.y - yMin + 1];
            int dirInt;
            int iterations = 0;
            int maxIterations = 100000;
            while (iterations < maxIterations)
            {
                // Search for the next part of the boundary by rotating the
                // boundary direction until a neighbor is found
                Direction nbrDir = boundaryDir;
                for (int i = 0; i < 6; i++)
                {
                    // The current nbrDir is empty
                    finishedDirections[currentIdx, nbrDir.ToInt()] = true;

                    nbrDir = nbrDir.Rotate60(-1);
                    Vector2Int nbrPos = ParticleSystem_Utils.GetNbrInDir(current, nbrDir);
                    int matrixNbrEntry = matrix[nbrPos.x - xMin + 1, nbrPos.y - yMin + 1];
                    if (matrixNbrEntry != -1)
                    {
                        dirInt = nbrDir.ToInt();
                        if (dirInt < 3)
                            edges.Add(new Vector3Int(current.x, current.y, dirInt));
                        else
                            edges.Add(new Vector3Int(nbrPos.x, nbrPos.y, (dirInt + 3) % 6));
                        last = current;
                        current = nbrPos;
                        currentIdx = matrixNbrEntry;
                        boundaryDir = nbrDir.Rotate60(2);
                        break;
                    }
                }
                // We are finished if we have reached the starting position again
                if (current == start && boundaryDir == initialBoundaryDir)
                {
                    break;
                }
                iterations++;
            }

            if (iterations >= maxIterations)
            {
                Log.Error("Error while computing object boundary: Exceeded max iterations (" + maxIterations + ")");
                return;
            }

            // Add edge from last to first node
            Direction dir = ParticleSystem_Utils.VectorToDirection(start - last);
            dirInt = dir.ToInt();
            if (dirInt < 3)
                edges.Add(new Vector3Int(last.x, last.y, dirInt));
            else
                edges.Add(new Vector3Int(start.x, start.y, (dirInt + 3) % 6));
        }

        /// <summary>
        /// Computes all edge movements belonging to this object.
        /// The edges are computed from the boundaries of the
        /// object and their movement is determined by the
        /// object's current joint movement offset.
        /// <para>
        /// This only works after the boundary edges of the
        /// object have been calculated.
        /// </para>
        /// </summary>
        /// <returns>An array containing all edge movements
        /// of this object for the current round.</returns>
        public EdgeMovement[] ComputeEdgeMovements()
        {
            EdgeMovement[] movements = new EdgeMovement[boundaryEdges.Count];

            int i = 0;
            foreach (Vector3Int v in boundaryEdges)
            {
                // Have to subtract offset here because our position has already been updated
                Vector2Int start1 = new Vector2Int(v.x, v.y) + position - jmOffset;
                Vector2Int end1 = AmoebotFunctions.GetNeighborPosition(start1, v.z);
                movements[i] = EdgeMovement.Create(start1, end1, start1 + jmOffset, end1 + jmOffset);
                i++;
            }

            return movements;
        }

        /// <summary>
        /// Stores the current joint movement offset in the object's history
        /// and resets the offset and corresponding flag for the next round.
        /// </summary>
        public void StoreAndResetMovementInfo()
        {
            receivedJmOffset = false;
            jmOffsetHistory.RecordValueInRound(jmOffset, system.CurrentRound);
            RenderMovement();
            jmOffset = Vector2Int.zero;
        }

        /// <summary>
        /// Submits the current joint movement offset to the render system
        /// to display a movement animation. This should be called after
        /// each round and whenever the simulation state changes.
        /// </summary>
        /// <param name="withAnimation">Whether an animation should
        /// be played.</param>
        public void RenderMovement(bool withAnimation = true)
        {
            if (withAnimation)
                graphics.jmOffset = jmOffsetHistory.GetMarkedValue();
            else
                graphics.jmOffset = Vector2Int.zero;
        }


        /*
         * IObjectInfo
         */

        public ICollection<Vector2Int> OccupiedPositions()
        {
            return GetOccupiedPositions();
        }

        public bool IsNeighborPosition(Vector2Int pos)
        {
            bool isNbr = false;
            foreach (Vector2Int p in GetOccupiedPositions())
            {
                if (p == pos)
                    return false;
                if (AmoebotFunctions.AreNodesNeighbors(pos, p))
                    isNbr = true;
            }
            return isNbr;
        }

        /// <summary>
        /// Removes the given local grid position from the object,
        /// if the object remains non-empty.
        /// <para>
        /// Note that the object's origin position will change if
        /// the node at the origin is removed.
        /// </para>
        /// </summary>
        /// <param name="pos">The occupied grid position to remove from
        /// the object, relative to the object's origin.</param>
        /// <returns><c>true</c> if and only if the position was
        /// successfully removed from the object.</returns>
        public bool RemovePositionRel(Vector2Int pos)
        {
            return RemovePosition(pos + position);
        }

        public bool RemovePosition(Vector2Int pos)
        {
            // Convert to relative position
            pos -= position;
            if (!occupiedRel.Contains(pos))
                return false;

            bool isOrigin = pos == occupiedRel[0];
            // Don't allow removing the last position
            if (isOrigin && occupiedRel.Count == 1)
            {
                Log.Error("Cannot remove last position from object.");
                return false;
            }

            // Check if the position can be deregistered from the system
            if (graphics.IsRegistered)
            {
                if (!system.RemovePositionFromObject(this, pos + position))
                {
                    Log.Error("Unable to remove relative position " + pos + " from object.");
                    return false;
                }
            }

            // Change origin if necessary
            if (isOrigin)
            {
                Vector2Int offset = occupiedRel[1] - occupiedRel[0];
                position += offset;
                for (int i = 1; i < occupiedRel.Count; i++)
                    occupiedRel[i] -= offset;
                occupiedRel.RemoveAt(0);
                positionHistory.RecordValueInRound(position, system.CurrentRound);
            }
            // Otherwise just remove the position
            else
            {
                occupiedRel.Remove(pos);
            }

            if (graphics.IsRegistered)
                graphics.GenerateMesh();

            return true;
        }

        public bool MoveToPosition(Vector2Int newPos)
        {
            if (newPos == position)
                return true;

            // First move the object in the system
            bool moved = system.MoveObject(this, newPos - position);
            if (moved)
                MoveTo(newPos);
            return moved;
        }

        public void RemoveFromSystem()
        {
            system.RemoveObject(this);
            graphics.RemoveObject();
        }

        public bool IsAnchor()
        {
            return system.IsAnchor(this);
        }

        public void MakeAnchor()
        {
            system.SetAnchor(this);
        }

        public int Size
        {
            get { return occupiedRel.Count; }
        }

        public void ReleaseBonds()
        {
            if (system.InMovePhase)
                releaseBonds = true;
        }

        /*
         * IReplayHistory
         */

        public void ContinueTracking()
        {
            positionHistory.ContinueTracking();
            position = positionHistory.GetMarkedValue();
            colorHistory.ContinueTracking();
            color = colorHistory.GetMarkedValue();
            jmOffsetHistory.ContinueTracking();
            graphics.UpdateColor();
        }

        public void CutOffAtMarker()
        {
            positionHistory.CutOffAtMarker();
            colorHistory.CutOffAtMarker();
            jmOffsetHistory.CutOffAtMarker();
        }

        public int GetFirstRecordedRound()
        {
            return positionHistory.GetFirstRecordedRound();
        }

        public int GetMarkedRound()
        {
            return positionHistory.GetMarkedRound();
        }

        public bool IsTracking()
        {
            return positionHistory.IsTracking();
        }

        public void SetMarkerToRound(int round)
        {
            positionHistory.SetMarkerToRound(round);
            position = positionHistory.GetMarkedValue();
            colorHistory.SetMarkerToRound(round);
            color = colorHistory.GetMarkedValue();
            jmOffsetHistory.SetMarkerToRound(round);
            graphics.UpdateColor();
        }

        public void ShiftTimescale(int amount)
        {
            positionHistory.ShiftTimescale(amount);
            colorHistory.ShiftTimescale(amount);
            jmOffsetHistory.ShiftTimescale(amount);
        }

        public void StepBack()
        {
            positionHistory.StepBack();
            position = positionHistory.GetMarkedValue();
            colorHistory.StepBack();
            color = colorHistory.GetMarkedValue();
            jmOffsetHistory.StepBack();
            graphics.UpdateColor();
        }

        public void StepForward()
        {
            positionHistory.StepForward();
            position = positionHistory.GetMarkedValue();
            colorHistory.StepForward();
            color = colorHistory.GetMarkedValue();
            jmOffsetHistory.StepForward();
            graphics.UpdateColor();
        }

        /*
         * Save/Load
         */

        public ParticleObjectSaveData GenerateSaveData()
        {
            ParticleObjectSaveData data = new ParticleObjectSaveData();
            data.identifier = identifier;
            data.positionHistory = positionHistory.GenerateSaveData();
            data.colorHistory = colorHistory.GenerateSaveData();
            data.jmOffsetHistory = jmOffsetHistory.GenerateSaveData();
            data.occupiedRel = occupiedRel.ToArray();
            return data;
        }

        public static ParticleObject CreateFromSaveData(ParticleSystem system, ParticleObjectSaveData data)
        {
            ParticleObject o = new ParticleObject(system, data);
            o.graphics.AddObject();
            return o;
        }

        private ParticleObject(ParticleSystem system, ParticleObjectSaveData data)
        {
            identifier = data.identifier;
            positionHistory = new ValueHistory<Vector2Int>(data.positionHistory);
            position = positionHistory.GetMarkedValue();
            colorHistory = new ValueHistory<Color>(data.colorHistory);
            color = colorHistory.GetMarkedValue();
            jmOffsetHistory = new ValueHistory<Vector2Int>(data.jmOffsetHistory);
            this.system = system;
            occupiedRel = new List<Vector2Int>(data.occupiedRel);

            graphics = new ObjectGraphicsAdapter(this, system.renderSystem.rendererObj);
        }

    }

} // namespace AS2.Sim
