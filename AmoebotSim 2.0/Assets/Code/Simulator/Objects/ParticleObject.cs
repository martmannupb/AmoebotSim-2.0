using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    public class ParticleObject : IReplayHistory
    {

        public struct BoundaryVertex
        {
            public Vector2Int node;
            public Direction dir;

            public BoundaryVertex(Vector2Int node, Direction dir)
            {
                this.node = node;
                this.dir = dir;
            }
        }

        private ParticleSystem system;

        private Vector2Int position;
        private ValueHistory<Vector2Int> positionHistory;
        private List<Vector2Int> occupiedRel;

        /// <summary>
        /// The absolute offset from the object's initial location,
        /// accumulated by joint movements.
        /// </summary>
        public Vector2Int jmOffset;

        /// <summary>
        /// Indicates whether this object has already received a
        /// joint movement offset from 
        /// </summary>
        public bool receivedJmOffset = false;

        public ParticleObject(Vector2Int position, ParticleSystem system)
        {
            this.position = position;
            this.system = system;
            positionHistory = new ValueHistory<Vector2Int>(position, system.CurrentRound);
            occupiedRel = new List<Vector2Int>();
            occupiedRel.Add(Vector2Int.zero);
        }

        public void AddPosition(Vector2Int pos)
        {
            pos.x -= position.x;
            pos.y -= position.y;
            AddPositionRel(pos);
        }

        public void AddPositionRel(Vector2Int posRel)
        {
            if (!occupiedRel.Contains(posRel))
                occupiedRel.Add(posRel);
        }

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

        public void CalculateBoundaries()
        {
            // If we have only one part: Simple solution
            if (occupiedRel.Count == 1)
            {
                // TODO
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

            //Log.Debug("Matrix:");
            //string s = "";
            //for (int y = sizeY + 1; y >= 0; y--)
            //{
            //    for (int x = 0; x < sizeX + 2; x++)
            //        s += matrix[x, y] != -1 ? "X" : "O";
            //    s += "\n";
            //}
            //Log.Debug(s);

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
            //Direction initialBoundaryDir = boundaryDir;     // Remember this to know when we are finished
            //Vector2Int current = top;
            List<Vector2Int> outerBoundary = new List<Vector2Int>();
            List<Direction> successorDirs = new List<Direction>();

            ComputeBoundary(top, boundaryDir, outerBoundary, successorDirs, xMin, yMin, matrix, finishedDirections);

            // Determine vertices on the boundary
            List<BoundaryVertex> vertices = ComputeBoundaryVertices(outerBoundary, successorDirs);

            // Draw the object as debug lines
            DrawBoundaryVertices(vertices, Color.black, 20.0f);

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
                            List<Vector2Int> boundaryNodes = new List<Vector2Int>();
                            List<Direction> succDirs = new List<Direction>();
                            ComputeBoundary(node, d, boundaryNodes, succDirs, xMin, yMin, matrix, finishedDirections);
                            List<BoundaryVertex> verts = ComputeBoundaryVertices(boundaryNodes, succDirs);
                            DrawBoundaryVertices(verts, Color.blue, 20.0f);
                        }
                    }

                }
            }
        }

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

        private List<BoundaryVertex> ComputeBoundaryVertices(List<Vector2Int> boundaryNodes, List<Direction> successorDirs)
        {
            List<BoundaryVertex> vertices = new List<BoundaryVertex>();

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
                    vertices.Add(new BoundaryVertex(node, vertexDir));
                    vertexDir = vertexDir.Rotate60(-1);
                }
            }

            return vertices;
        }

        private void DrawBoundaryVertices(List<BoundaryVertex> vertices, Color color, float duration = 30.0f)
        {
            List<Vector3> points = new List<Vector3>();
            float dist = 0.5f;
            foreach (BoundaryVertex bv in vertices)
            {
                Vector3 pos = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(bv.node + position, -2);
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
            data.positionHistory = positionHistory.GenerateSaveData();
            data.occupiedRel = occupiedRel.ToArray();
            return data;
        }

        public static ParticleObject CreateFromSaveData(ParticleSystem system, ParticleObjectSaveData data)
        {
            return new ParticleObject(system, data);
        }

        private ParticleObject(ParticleSystem system, ParticleObjectSaveData data)
        {
            positionHistory = new ValueHistory<Vector2Int>(data.positionHistory);
            position = positionHistory.GetMarkedValue();
            this.system = system;
            occupiedRel = new List<Vector2Int>(data.occupiedRel);
        }

    }

} // namespace AS2.Sim
