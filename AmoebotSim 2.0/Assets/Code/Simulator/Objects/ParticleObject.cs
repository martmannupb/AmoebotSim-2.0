using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    public class ParticleObject : IReplayHistory
    {
        private ParticleSystem system;

        private Vector2Int position;
        private ValueHistory<Vector2Int> positionHistory;
        private List<Vector2Int> occupiedRel;

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
             * Calculate helper data structure:
             * We compute the bounding rect of the object and
             * create a bool matrix telling us where the
             * occupied positions of the object are.
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
            bool[,] matrix = new bool[sizeX + 2, sizeY + 2];
            foreach (Vector2Int v in occupiedRel)
            {
                matrix[v.x - xMin + 1, v.y - yMin + 1] = true;
            }

            Log.Debug("Matrix:");
            string s = "";
            for (int y = sizeY + 1; y >= 0; y--)
            {
                for (int x = 0; x < sizeX + 2; x++)
                    s += matrix[x, y] ? "X" : "O";
                s += "\n";
            }
            Log.Debug(s);

            /*
             * Start with outer boundary
             */

            List<Vector2Int> outerBoundary = new List<Vector2Int>();

            // Find the left- and topmost position (must be on the outer boundary)
            Vector2Int top = occupiedRel[0];
            bool foundTop = false;
            for (int y = sizeY; y > 0; y--)
            {
                for (int x = 1; x < sizeX + 1; x++)
                {
                    if (matrix[x, y])
                    {
                        top = new Vector2Int(x + xMin - 1, y + yMin - 1);
                        foundTop = true;
                        break;
                    }
                }
                if (foundTop)
                    break;
            }
            Log.Debug("Top: " + top);

            // top has no neighbors in W, NNW and NNE direction
            // Now walk around the outer boundary in clockwise direction
            Direction boundaryDir = Direction.NNE;  // boundaryDir is like a normal pointing away from the shape
            Direction initialBoundaryDir = boundaryDir;     // Remember this to know when we are finished
            Vector2Int current = top;
            outerBoundary.Add(top);
            bool finished = false;
            while (!finished)
            {
                // Search for the next part of the boundary by rotating the
                // boundary direction until a neighbor is found
                Direction nbrDir = boundaryDir;
                for (int i = 0; i < 6; i++)
                {
                    nbrDir = nbrDir.Rotate60(-1);
                    // We are finished if we have reached the starting position again
                    if (current == outerBoundary[0] && nbrDir == initialBoundaryDir)
                    {
                        finished = true;
                        break;
                    }

                    Vector2Int nbrPos = ParticleSystem_Utils.GetNbrInDir(current, nbrDir);
                    if (matrix[nbrPos.x - xMin + 1, nbrPos.y - yMin + 1])
                    {
                        current = nbrPos;
                        outerBoundary.Add(current);
                        boundaryDir = nbrDir.Rotate60(2);
                        break;
                    }
                }
            }

            Log.Debug("Outer boundary:");
            s = "";
            foreach (Vector2Int v in outerBoundary)
                s += v + "\n";
            Log.Debug(s);

            // Draw the object as debug lines
            List<Vector3> points = new List<Vector3>();
            foreach (Vector2Int v in outerBoundary)
            {
                Vector2Int p = v + position;
                Vector3 point = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(p);
                points.Add(point);
            }
            for (int i = 0; i < points.Count; i++)
            {
                Debug.DrawLine(points[i], points[(i + 1) % points.Count], Color.black, 10.0f);
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
