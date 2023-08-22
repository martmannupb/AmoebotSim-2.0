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

        /// <summary>
        /// Helper struct storing vertex information on a
        /// single hexagon for mesh generation.
        /// </summary>
        private struct VertexInfo
        {
            /// <summary>
            /// Vertex indices of the 6 vertices of the hexagon.
            /// The entry at position i is the index of the
            /// vertex in the mesh's vertex list. Hexagon vertices
            /// are counted in counter-clockwise order, starting
            /// with the ENE vertex.
            /// </summary>
            public int[] vertices;
            /// <summary>
            /// Bools telling which vertices of the hexagon are
            /// outer vertices. Indexing works like for
            /// <see cref="VertexInfo.vertices"/>.
            /// </summary>
            public bool[] outer;

            public VertexInfo(bool active)
            {
                vertices = new int[6];
                outer = new bool[6];
            }
        }

        /// <summary>
        /// The object represented by this adapter.
        /// </summary>
        public ParticleObject obj;
        /// <summary>
        /// The renderer responsible for rendering this object.
        /// </summary>
        public RendererObjects renderer;

        /// <summary>
        /// Whether this object is already registered in a renderer.
        /// </summary>
        private bool isRegistered = false;

        /// <summary>
        /// The mesh used to draw the object. Its origin is at
        /// the object's origin. Is <c>null</c> as long as no
        /// mesh has been generated.
        /// </summary>
        public Mesh mesh;

        /// <summary>
        /// The color in which the object should be rendered.
        /// </summary>
        public Color Color
        {
            get { return obj.Color; }
        }

        /// <summary>
        /// The property block used to render this object.
        /// Is <c>null</c> as long as the object has not been
        /// registered for rendering.
        /// </summary>
        public MaterialPropertyBlockData_Objects propertyBlock;

        public ObjectGraphicsAdapter(ParticleObject obj, RendererObjects renderer)
        {
            this.obj = obj;
            this.renderer = renderer;
        }

        /// <summary>
        /// Adds this object to the render system.
        /// </summary>
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

        /// <summary>
        /// Removes this object from the render system.
        /// </summary>
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

        public void UpdateColor()
        {
            if (isRegistered)
                renderer.UpdateObjectColor(this);
        }

        /// <summary>
        /// Generates the mesh for this object.
        /// </summary>
        /// <param name="debug">Whether the mesh lines should
        /// be displayed for debugging.</param>
        public void GenerateMesh(bool debug = false)
        {
            // Vertex positions relative to a hexagon's center
            // Element at 0 is vertex in ENE direction,
            // increasing counter-clockwise
            // Need outer and inner positions
            // Inner are not part of boundary and are further away from the center
            // Outer are part of boundary and are closer to the center (as
            // specified by const_objectIndentFactor)
            Vector2[] basePosInner = new Vector2[6];
            Vector2[] basePosOuter = new Vector2[6];
            float dInner = AmoebotFunctions.hexRadiusMajor;
            float facOuter = RenderSystem.const_objectIndentFactor;
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

            /// DEBUG
            Color debugLineColor = Color.blue;
            float debugLineDuration = 15f;
            /// DEBUG

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

            // Collect mesh vertices and triangles in these lists
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            // Helper data: We have 6 neighbors, arranged in matrix like this:
            // . . . . .
            // . 2 1 . .
            // . 3 X 0 .
            // . . 4 5 .
            // . . . . .
            // Matrix indices of neighbors
            int[] nbrX = new int[6];    // E at 0, NNE at 1 etc.
            int[] nbrY = new int[6];
            // Indicator which neighbors are occupied
            bool[] occupied = new bool[6];
            
            // Now iterate through the shape and fill in the vertex info
            // We traverse the matrix row by row, from top to bottom
            for (int y = sizeY; y > 0; y--)
            {
                // Initialize neighbor indices for this row
                nbrX[0] = nbrX[5] = 2;
                nbrX[1] = nbrX[4] = 1;
                nbrX[2] = nbrX[3] = 0;
                nbrY[0] = nbrY[3] = y;
                nbrY[1] = nbrY[2] = y + 1;
                nbrY[4] = nbrY[5] = y - 1;
                // Initialize occupied array
                occupied[2] = occupied[3] = false;
                occupied[4] = matrix[nbrX[4], nbrY[4]];
                // Grid Y coordinate of the matrix cell
                int nodeY = y + yMin - 1;

                for (int x = 1; x < sizeX + 1; x++)
                {
                    // Add new occupied neighbors
                    occupied[0] = matrix[nbrX[0], nbrY[0]];
                    occupied[1] = matrix[nbrX[1], nbrY[1]];
                    occupied[5] = matrix[nbrX[5], nbrY[5]];

                    // Only have to process if this entry is not empty
                    if (matrix[x, y])
                    {
                        // This struct holds the vertices of the hexagon
                        VertexInfo newInfo = new VertexInfo(true);

                        // Grid X coordinate of the matrix cell
                        int nodeX = x + xMin - 1;
                        // World position
                        Vector2 pos = AmoebotFunctions.GridToWorldPositionVector2(nodeX, nodeY);

                        // Process vertices
                        // Add new vertices if necessary
                        // 0 is ENE, 1 is N etc.
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
                                        newInfo.vertices[i] = vertexInfoMat[nbrX[i + 1], nbrY[i + 1]].vertices[i + 4];
                                    }
                                    else
                                    {
                                        newInfo.vertices[i] = vertexInfoMat[nbrX[i], nbrY[i]].vertices[i + 2];
                                    }
                                }
                                else
                                {
                                    // Have to create new vertex
                                    Vector2 vertex = pos + basePosInner[i];
                                    newInfo.vertices[i] = verts.Count;
                                    verts.Add(vertex);
                                }
                            }
                            else
                            {
                                // Outer vertex
                                // Create new vertex
                                Vector2 vertex = pos + basePosOuter[i];
                                newInfo.vertices[i] = verts.Count;
                                newInfo.outer[i] = true;
                                verts.Add(vertex);
                            }
                        }

                        vertexInfoMat[x, y] = newInfo;

                        // Add triangles
                        // Start with inner triangles of the hexagon
                        tris.AddRange(new int[] {
                            // Middle
                            newInfo.vertices[0], newInfo.vertices[4], newInfo.vertices[2],
                            // Top
                            newInfo.vertices[0], newInfo.vertices[2], newInfo.vertices[1],
                            // Left
                            newInfo.vertices[2], newInfo.vertices[4], newInfo.vertices[3],
                            // Right
                            newInfo.vertices[0], newInfo.vertices[5], newInfo.vertices[4]
                        });

                        // Now add triangles connecting the hexagons where necessary
                        for (int i = 1; i < 4; i++)
                        {
                            if (!occupied[i])
                                continue;

                            VertexInfo nbrInfo = vertexInfoMat[nbrX[i], nbrY[i]];

                            bool outer1 = newInfo.outer[i - 1];
                            bool outer2 = newInfo.outer[i];

                            /// DEBUG
                            List<int> debugIndices = new List<int>();
                            /// DEBUG

                            if (outer1 && outer2)
                            {
                                // Need 2 triangles
                                tris.AddRange(new int[] {
                                    newInfo.vertices[i - 1], newInfo.vertices[i], nbrInfo.vertices[i + 2],
                                    newInfo.vertices[i - 1], nbrInfo.vertices[i + 2], nbrInfo.vertices[(i + 3) % 6]
                                });

                                /// DEBUG
                                if (debug)
                                    debugIndices.AddRange(new int[] {
                                        newInfo.vertices[i - 1], newInfo.vertices[i],
                                        newInfo.vertices[i], nbrInfo.vertices[i + 2],
                                        nbrInfo.vertices[i + 2], newInfo.vertices[i - 1],

                                        newInfo.vertices[i - 1], nbrInfo.vertices[i + 2],
                                        nbrInfo.vertices[i + 2], nbrInfo.vertices[(i + 3) % 6],
                                        nbrInfo.vertices[(i + 3) % 6], newInfo.vertices[i - 1]
                                    });
                                /// DEBUG
                            }
                            else if (outer1)
                            {
                                // Need 1 triangle, smaller index is outer
                                tris.AddRange(new int[] {
                                    newInfo.vertices[i - 1], newInfo.vertices[i], nbrInfo.vertices[(i + 3) % 6]
                                });

                                /// DEBUG
                                if (debug)
                                    debugIndices.AddRange(new int[] {
                                        newInfo.vertices[i - 1], newInfo.vertices[i],
                                        newInfo.vertices[i], nbrInfo.vertices[(i + 3) % 6],
                                        nbrInfo.vertices[(i + 3) % 6], newInfo.vertices[i - 1]
                                    });
                                /// DEBUG
                            }
                            else if (outer2)
                            {
                                // Need 1 triangle, larger index is outer
                                tris.AddRange(new int[] {
                                    newInfo.vertices[i - 1], newInfo.vertices[i], nbrInfo.vertices[i + 2]
                                });

                                /// DEBUG
                                if (debug)
                                    debugIndices.AddRange(new int[] {
                                        newInfo.vertices[i - 1], newInfo.vertices[i],
                                        newInfo.vertices[i], nbrInfo.vertices[i + 2],
                                        nbrInfo.vertices[i + 2], newInfo.vertices[i - 1]
                                    });
                                /// DEBUG
                            }

                            /// DEBUG
                            if (debug)
                            {
                                Vector3 bPos2 = AmoebotFunctions.GridToWorldPositionVector3(obj.Position);
                                for (int j = 0; j < debugIndices.Count; j += 2)
                                {
                                    Debug.DrawLine(verts[debugIndices[j]] + bPos2, verts[debugIndices[j + 1]] + bPos2, debugLineColor, debugLineDuration);
                                }
                            }
                            /// DEBUG
                        }

                        /// DEBUG

                        if (debug)
                        {
                            Vector3 bPos = AmoebotFunctions.GridToWorldPositionVector3(obj.Position);
                            int[] indices = new int[] {
                                0, 4, 4, 2, 2, 0,
                                2, 1, 1, 0,
                                4, 3, 3, 2,
                                0, 5, 5, 4
                            };
                            for (int i = 0; i < indices.Length; i += 2)
                            {
                                Debug.DrawLine(verts[newInfo.vertices[indices[i]]] + bPos, verts[newInfo.vertices[indices[i + 1]]] + bPos, debugLineColor, debugLineDuration);
                            }
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
            // (Don't need normals or UVs because of the simple unlit shader)
            mesh = new Mesh();
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
        }
    }

} // namespace AS2.Visuals
