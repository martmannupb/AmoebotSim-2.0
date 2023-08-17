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
    /// mechanisms. Objects do not form bonds to each other
    /// since there would be no way of releasing the bonds.
    /// </para>
    /// </summary>
    public class ParticleObject : IParticleObject, IReplayHistory
    {

        /// <summary>
        /// Represents a vertex used to draw the border of
        /// an object or hexagon.
        /// </summary>
        public struct ObjectBorderVertex
        {
            /// <summary>
            /// The grid node to which the vertex belongs.
            /// </summary>
            public Vector2Int node;
            /// <summary>
            /// The direction in which the vertex lies relative
            /// to the node's center.
            /// </summary>
            public Direction dir;

            public ObjectBorderVertex(Vector2Int node, Direction dir)
            {
                this.node = node;
                this.dir = dir;
            }
        }

        private static List<ParticleObject> allObjects = new List<ParticleObject>();

        private ParticleSystem system;

        public ObjectGraphicsAdapter graphics;

        /// <summary>
        /// The global root position of the object. This position
        /// marks the origin of the local coordinate system and is
        /// always occupied by a node.
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

        private List<ObjectBorderVertex> tmpOuterBoundaryVerts = new List<ObjectBorderVertex>();
        private List<List<ObjectBorderVertex>> tmpInnerBoundaryVerts = new List<List<ObjectBorderVertex>>();

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
            set { identifier = value; }
        }

        /// <summary>
        /// The display color of the object.
        /// </summary>
        private Color color = Color.black;
        /// <summary>
        /// The display color of the object.
        /// </summary>
        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        /// <summary>
        /// The absolute offset from the object's initial location,
        /// accumulated by joint movements.
        /// </summary>
        public Vector2Int jmOffset;

        /// <summary>
        /// Indicates whether this object has already received a
        /// joint movement offset during the movement simulation.
        /// </summary>
        public bool receivedJmOffset = false;

        public ParticleObject(Vector2Int position, ParticleSystem system, int identifier = 0)
        {
            this.position = position;
            this.system = system;
            this.identifier = identifier;
            positionHistory = new ValueHistory<Vector2Int>(position, system.CurrentRound);
            occupiedRel = new List<Vector2Int>();
            occupiedRel.Add(Vector2Int.zero);

            graphics = new ObjectGraphicsAdapter(this, system.renderSystem.rendererObj);

            allObjects.Add(this);
        }

        public void Free()
        {
            allObjects.Remove(this);
        }

        public static void DrawObjects()
        {
            foreach (ParticleObject o in allObjects)
                o.Draw();
        }

        /// <summary>
        /// Adds a new position to the object. Does not
        /// have to be connected to the other positions
        /// as long as the object is connected when it is
        /// inserted into the system.
        /// </summary>
        /// <param name="pos">The global position that should
        /// be added to the object.</param>
        public void AddPosition(Vector2Int pos)
        {
            pos.x -= position.x;
            pos.y -= position.y;
            AddPositionRel(pos);
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
            if (!occupiedRel.Contains(posRel))
                occupiedRel.Add(posRel);
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
        /// </summary>
        /// <param name="offset">The offset vector by which
        /// the object should be moved.</param>
        public void MovePosition(Vector2Int offset)
        {
            position += offset;
            positionHistory.RecordValueInRound(position, system.CurrentRound);
        }

        /// <summary>
        /// Calculates all boundaries of the object so that the object
        /// can be rendered nicely. This should be called once when the
        /// simulation starts and the object is complete.
        /// </summary>
        public void CalculateBoundaries()
        {
            float tStart = Time.realtimeSinceStartup;

            // If we have only one part: Simple solution
            if (occupiedRel.Count == 1)
            {
                // Boundary consists only of the 6 vertices of our origin node
                Vector2Int node = occupiedRel[0];

                tmpOuterBoundaryVerts.Clear();
                tmpInnerBoundaryVerts.Clear();

                foreach (Direction d in DirectionHelpers.Iterate60(Direction.ENE, 6, true))
                    tmpOuterBoundaryVerts.Add(new ObjectBorderVertex(node, d));
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
            List<Vector2Int> outerBoundary = new List<Vector2Int>();
            List<Direction> successorDirs = new List<Direction>();

            ComputeBoundary(top, boundaryDir, outerBoundary, successorDirs, xMin, yMin, matrix, finishedDirections);

            // Determine vertices on the boundary
            List<ObjectBorderVertex> vertices = ComputeBoundaryVertices(outerBoundary, successorDirs);

            tmpOuterBoundaryVerts = vertices;

            // Draw the object as debug lines
            //DrawBoundaryVertices(vertices, Color.black, 20.0f);

            // Now do the same for the inner boundaries
            tmpInnerBoundaryVerts.Clear();
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
                            List<Vector2Int> boundaryNodes = new List<Vector2Int>();
                            List<Direction> succDirs = new List<Direction>();
                            ComputeBoundary(node, d, boundaryNodes, succDirs, xMin, yMin, matrix, finishedDirections);
                            List<ObjectBorderVertex> verts = ComputeBoundaryVertices(boundaryNodes, succDirs);
                            tmpInnerBoundaryVerts.Add(verts);
                            //DrawBoundaryVertices(verts, Color.blue, 20.0f);
                        }
                    }

                }
            }

            Log.Debug("Calculated boundaries in " + (Time.realtimeSinceStartup - tStart) + "s");
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
        /// <param name="boundaryNodes">The list of local grid
        /// coordinates into which the boundary nodes should be written.
        /// The list will start with the given start node and follow the
        /// boundary in clockwise or counter-clockwise order, depending
        /// on whether it is the outer or an inner boundary.</param>
        /// <param name="successorDirs">The list of directions into which
        /// the successor directions should be written. The element at
        /// position <c>i</c> will contain the direction that points from
        /// the boundary node at position <c>i</c> to its successor.</param>
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
            List<Vector2Int> boundaryNodes, List<Direction> successorDirs,
            int xMin, int yMin,
            int[,] matrix, bool[,] finishedDirections)
        {
            // Rotate initial boundary direction to the left until a neighbor is found
            // => ensures that all empty neighbors of the boundary are checked
            for (int i = 0; i < 6; i++)
            {
                Direction d = initialBoundaryDir.Rotate60(1);
                Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(start, d);
                if (matrix[nbr.x - xMin + 1, nbr.y -yMin + 1] != -1)
                {
                    break;
                }
                initialBoundaryDir = d;
            }

            Direction boundaryDir = initialBoundaryDir;
            Vector2Int current = start;
            int currentIdx = matrix[start.x - xMin + 1, start.y - yMin + 1];
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
                        successorDirs.Add(nbrDir);
                        boundaryNodes.Add(current);
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

            // Add direction from last to first node
            successorDirs.Add(ParticleSystem_Utils.VectorToDirection(boundaryNodes[0] - boundaryNodes[boundaryNodes.Count - 1]));
        }

        /// <summary>
        /// Turns the given list of boundary nodes and successor
        /// directions into a list of vertices that can be used
        /// for rendering the object.
        /// </summary>
        /// <param name="boundaryNodes">The list of local coordinates
        /// of all nodes belonging to the boundary.</param>
        /// <param name="successorDirs">The list of successor directions
        /// belonging to the given boundary.</param>
        /// <returns>A list of border vertices representing the given
        /// boundary.</returns>
        private List<ObjectBorderVertex> ComputeBoundaryVertices(List<Vector2Int> boundaryNodes, List<Direction> successorDirs)
        {
            List<ObjectBorderVertex> vertices = new List<ObjectBorderVertex>();

            for (int i = 0; i < boundaryNodes.Count; i++)
            {
                Vector2Int node = boundaryNodes[i];
                Direction incoming = successorDirs[(successorDirs.Count + i - 1) % successorDirs.Count];
                Direction outgoing = successorDirs[i];
                int numTurns = incoming.DistanceTo(outgoing, true) / 2; // Number of clockwise 60 degree rotations
                if (numTurns > 3)
                    numTurns = -1;

                // Direction of first vertex is "right" of predecessor
                Direction vertexDir = incoming.Opposite().Rotate30(-1);
                for (int j = 0; j < numTurns + 3; j++)
                {
                    vertices.Add(new ObjectBorderVertex(node, vertexDir));
                    vertexDir = vertexDir.Rotate60(-1);
                }
            }

            return vertices;
        }

        /// <summary>
        /// Draws the given boundary using the simple
        /// debug line function.
        /// </summary>
        /// <param name="vertices">The vertices of the boundary.</param>
        /// <param name="color">The color in which the boundary should be drawn.</param>
        /// <param name="duration">The duration for which the lines should be displayed.</param>
        private void DrawBoundaryVertices(List<ObjectBorderVertex> vertices, Color color, float duration = 30.0f)
        {
            List<Vector3> points = new List<Vector3>();
            float dist = 0.5f;
            foreach (ObjectBorderVertex bv in vertices)
            {
                Vector3 pos = AmoebotFunctions.GridToWorldPositionVector3(bv.node + position, -2);
                float angle = (bv.dir.ToInt() * 60 + 30) * Mathf.Deg2Rad;
                pos.x += Mathf.Cos(angle) * dist;
                pos.y += Mathf.Sin(angle) * dist;
                points.Add(pos);
            }
            for (int i = 0; i < points.Count; i++)
            {
                Debug.DrawLine(points[i], points[(i + 1) % points.Count], color, duration);
            }
        }

        public void Draw()
        {
            DrawBoundaryVertices(tmpOuterBoundaryVerts, Color.black, 0.1f);
            for (int i = 0; i < tmpInnerBoundaryVerts.Count; i++)
                DrawBoundaryVertices(tmpInnerBoundaryVerts[i], Color.blue, 0.1f);
        }


        /*
         * IReplayHistory
         */

        public void ContinueTracking()
        {
            positionHistory.ContinueTracking();
            position = positionHistory.GetMarkedValue();
        }

        public void CutOffAtMarker()
        {
            positionHistory.CutOffAtMarker();
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
        }

        public void ShiftTimescale(int amount)
        {
            positionHistory.ShiftTimescale(amount);
        }

        public void StepBack()
        {
            positionHistory.StepBack();
            position = positionHistory.GetMarkedValue();
        }

        public void StepForward()
        {
            positionHistory.StepForward();
            position = positionHistory.GetMarkedValue();
        }

        /*
         * Save/Load
         */

        public ParticleObjectSaveData GenerateSaveData()
        {
            ParticleObjectSaveData data = new ParticleObjectSaveData();
            data.identifier = identifier;
            data.positionHistory = positionHistory.GenerateSaveData();
            data.occupiedRel = occupiedRel.ToArray();
            data.color = color;
            return data;
        }

        public static ParticleObject CreateFromSaveData(ParticleSystem system, ParticleObjectSaveData data)
        {
            ParticleObject o = new ParticleObject(system, data);
            o.CalculateBoundaries();
            o.graphics.AddObject();
            return o;
        }

        private ParticleObject(ParticleSystem system, ParticleObjectSaveData data)
        {
            identifier = data.identifier;
            positionHistory = new ValueHistory<Vector2Int>(data.positionHistory);
            position = positionHistory.GetMarkedValue();
            this.system = system;
            occupiedRel = new List<Vector2Int>(data.occupiedRel);
            color = data.color;

            graphics = new ObjectGraphicsAdapter(this, system.renderSystem.rendererObj);
        }

    }

} // namespace AS2.Sim
