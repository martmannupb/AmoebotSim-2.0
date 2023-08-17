using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Visuals
{

    /// <summary>
    /// Representation of a <see cref="AS2.Sim.ParticleObject"/> in the render system.
    /// Stores all graphics-specific data of the object.
    /// </summary>
    public class ObjectGraphicsAdapter
    {

        private struct VertexInfo
        {
            public int[] vertices;
            public bool[] outer;

            public VertexInfo(bool active)
            {
                vertices = new int[6];
                outer = new bool[6];
            }
        }

        public ParticleObject obj;
        public RendererObjects renderer;

        private bool isRegistered = false;

        public Mesh mesh;

        public ObjectGraphicsAdapter(ParticleObject obj, RendererObjects renderer)
        {
            this.obj = obj;
            this.renderer = renderer;
        }

        public void AddObject()
        {
            if (isRegistered)
            {
                Log.Error("Object graphics adapter already registered.");
                return;
            }

            isRegistered = true;
            GenerateMesh();
            renderer.AddObject(this);
        }

        public void RemoveObject()
        {
            if (!isRegistered)
            {
                Log.Error("Cannot remove object graphics adapter that is not registered.");
                return;
            }

            isRegistered = false;
            renderer.RemoveObject(this);
        }

        public void GenerateMesh()
        {
            // Vertex positions relative to a hexagon's center
            // Element at 0 is vertex in ENE direction,
            // increasing counter-clockwise
            // Need outer and inner positions
            // Inner are not part of boundary and are further away from the center
            // Outer are part of boundary and are closer to the center
            Vector2[] basePosInner = new Vector2[6];
            Vector2[] basePosOuter = new Vector2[6];
            float dInner = AmoebotFunctions.hexRadiusMajor;
            float facOuter = 0.9f;
            float baseX = Mathf.Cos(Mathf.Deg2Rad * 30f) * dInner;
            float baseY = Mathf.Sin(Mathf.Deg2Rad * 30f) * dInner;
            basePosInner[0] = new Vector2(baseX, baseY);
            basePosInner[1] = new Vector2(0, dInner);
            basePosInner[2] = new Vector2(-baseX, baseY);
            basePosInner[3] = new Vector2(-baseX, -baseY);
            basePosInner[4] = new Vector2(0, -dInner);
            basePosInner[5] = new Vector2(baseX, -baseY);
            for (int i = 0; i < 6; i++)
                basePosOuter[i] = basePosInner[i] * facOuter;

            /*
             * Calculate helper data structures:
             * We compute the bounding rect of the object and
             * create a bool matrix telling us where the
             * occupied positions of the object are. false entries
             * are empty and true entries are occupied.
             * This enables us to check whether a position is
             * occupied in constant time rather than O(n).
             */

            Vector2Int[] occupiedRel = obj.GetRelPositions();

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
            for (int i = 0; i < occupiedRel.Length; i++)
            {
                Vector2Int v = occupiedRel[i];
                matrix[v.x - xMin + 1, v.y - yMin + 1] = true;
            }

            // Create a second matrix of the same format storing
            // vertex information
            VertexInfo[,] vertexInfoMat = new VertexInfo[sizeX + 2, sizeY + 2];

            // Collect vertices and triangles in these lists
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            // Helper data: Have 6 neighbors, arranged in matrix like this:
            // . . . . .
            // . 2 1 . .
            // . 3 X 0 .
            // . . 4 5 .
            // . . . . .
            // Helper data: Matrix indices of neighbors
            int[] nbrX = new int[6];    // E at 0, NNE at 1 etc.
            int[] nbrY = new int[6];
            // Helper data: Indicator which neighbors are occupied
            bool[] occupied = new bool[6];
            
            // Now iterate through the shape and fill in the vertex info
            for (int y = sizeY; y > 0; y--)
            {
                // Initialize neighbor indices
                nbrX[0] = nbrX[5] = 2;
                nbrX[1] = nbrX[4] = 1;
                nbrX[2] = nbrX[3] = 0;
                nbrY[0] = nbrY[3] = y;
                nbrY[1] = nbrY[2] = y + 1;
                nbrY[4] = nbrY[5] = y - 1;
                // Initialize occupied array
                occupied[2] = occupied[3] = false;
                occupied[4] = matrix[nbrX[4], nbrY[4]];
                // Node coordinate
                int nodeY = y + yMin - 1;

                for (int x = 1; x < sizeX + 1; x++)
                {
                    // Add new occupied neighbors
                    occupied[0] = matrix[nbrX[0], nbrY[0]];
                    occupied[1] = matrix[nbrX[1], nbrY[1]];
                    occupied[5] = matrix[nbrX[5], nbrY[5]];

                    if (matrix[x, y])
                    {
                        VertexInfo newMat = new VertexInfo(true);

                        // Node coordinate
                        int nodeX = x + xMin - 1;

                        // Node position
                        Vector2 pos = AmoebotFunctions.GridToWorldPositionVector2(nodeX, nodeY);

                        // Process vertices
                        // Add new vertices if necessary
                        for (int i = 0; i < 6; i++)
                        {
                            if (occupied[i] && occupied[(i+1) % 6])
                            {
                                // Inner vertex
                                // Use vertex from neighbor or create new one
                                if (i < 4)
                                {
                                    // Can reuse vertex of neighbor
                                    if (i < 2)
                                    {
                                        newMat.vertices[i] = vertexInfoMat[nbrX[i + 1], nbrY[i + 1]].vertices[i + 4];
                                    }
                                    else
                                    {
                                        newMat.vertices[i] = vertexInfoMat[nbrX[i], nbrY[i]].vertices[i + 2];
                                    }
                                }
                                else
                                {
                                    // Have to create new vertex
                                    Vector2 vertex = pos + basePosInner[i];
                                    newMat.vertices[i] = verts.Count;
                                    verts.Add(vertex);
                                }
                            }
                            else
                            {
                                // Outer vertex
                                // Create new vertex
                                Vector2 vertex = pos + basePosOuter[i];
                                newMat.vertices[i] = verts.Count;
                                newMat.outer[i] = true;
                                verts.Add(vertex);
                            }
                        }

                        vertexInfoMat[x, y] = newMat;

                        // Add triangles
                        // Start with inner triangles of the hexagon
                        tris.AddRange(new int[] {
                            // Middle
                            newMat.vertices[0], newMat.vertices[4], newMat.vertices[2],
                            // Top
                            newMat.vertices[0], newMat.vertices[2], newMat.vertices[1],
                            // Left
                            newMat.vertices[2], newMat.vertices[4], newMat.vertices[3],
                            // Right
                            newMat.vertices[0], newMat.vertices[5], newMat.vertices[4]
                        });

                        // Now triangles connecting the hexagons where necessary
                        for (int i = 1; i < 4; i++)
                        {
                            if (!occupied[i])
                                continue;

                            VertexInfo nbrInfo = vertexInfoMat[nbrX[i], nbrY[i]];

                            bool outer1 = newMat.outer[i - 1];
                            bool outer2 = newMat.outer[i];

                            /// DEBUG
                            List<int> debugIndices = new List<int>();
                            /// DEBUG

                            if (outer1 && outer2)
                            {
                                // Need 2 triangles
                                tris.AddRange(new int[] {
                                    newMat.vertices[i - 1], newMat.vertices[i], nbrInfo.vertices[i + 2],
                                    newMat.vertices[i - 1], nbrInfo.vertices[i + 2], nbrInfo.vertices[(i + 3) % 6]
                                });

                                /// DEBUG
                                debugIndices.AddRange(new int[] {
                                    newMat.vertices[i - 1], newMat.vertices[i],
                                    newMat.vertices[i], nbrInfo.vertices[i + 2],
                                    nbrInfo.vertices[i + 2], newMat.vertices[i - 1],

                                    newMat.vertices[i - 1], nbrInfo.vertices[i + 2],
                                    nbrInfo.vertices[i + 2], nbrInfo.vertices[(i + 3) % 6],
                                    nbrInfo.vertices[(i + 3) % 6], newMat.vertices[i - 1]
                                });
                                /// DEBUG
                            }
                            else if (outer1)
                            {
                                // Need 1 triangle, smaller index is outer
                                tris.AddRange(new int[] {
                                    newMat.vertices[i - 1], newMat.vertices[i], nbrInfo.vertices[(i + 3) % 6]
                                });

                                /// DEBUG
                                debugIndices.AddRange(new int[] {
                                    newMat.vertices[i - 1], newMat.vertices[i],
                                    newMat.vertices[i], nbrInfo.vertices[(i + 3) % 6],
                                    nbrInfo.vertices[(i + 3) % 6], newMat.vertices[i - 1]
                                });
                                /// DEBUG
                            }
                            else if (outer2)
                            {
                                // Need 1 triangle, larger index is outer
                                tris.AddRange(new int[] {
                                    newMat.vertices[i - 1], newMat.vertices[i], nbrInfo.vertices[i + 2]
                                });

                                /// DEBUG
                                debugIndices.AddRange(new int[] {
                                    newMat.vertices[i - 1], newMat.vertices[i],
                                    newMat.vertices[i], nbrInfo.vertices[i + 2],
                                    nbrInfo.vertices[i + 2], newMat.vertices[i - 1]
                                });
                                /// DEBUG
                            }

                            /// DEBUG
                            Vector3 bPos2 = AmoebotFunctions.GridToWorldPositionVector3(obj.Position);
                            for (int j = 0; j < debugIndices.Count; j += 2)
                            {
                                Debug.DrawLine(verts[debugIndices[j]] + bPos2, verts[debugIndices[j + 1]] + bPos2, Color.green, 15f);
                            }
                            /// DEBUG
                        }

                        /// DEBUG
                        Vector3 bPos = AmoebotFunctions.GridToWorldPositionVector3(obj.Position);
                        int[] indices = new int[] {
                            0, 4, 4, 2, 2, 0,
                            2, 1, 1, 0,
                            4, 3, 3, 2,
                            0, 5, 5, 4
                        };
                        for (int i = 0; i < indices.Length; i += 2)
                        {
                            Debug.DrawLine(verts[newMat.vertices[indices[i]]] + bPos, verts[newMat.vertices[indices[i + 1]]] + bPos, Color.green, 15f);
                        }
                        /// DEBUG

                    }

                    // Compute indices for next neighbors
                    for (int i = 0; i < 6; i++)
                        nbrX[i]++;
                    // Shift occupied array
                    occupied[2] = occupied[1];
                    occupied[4] = occupied[5];
                    occupied[3] = matrix[x, y];
                }
            }

            // Finally create the mesh
            mesh = new Mesh();
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
        }
    }

} // namespace AS2.Visuals
