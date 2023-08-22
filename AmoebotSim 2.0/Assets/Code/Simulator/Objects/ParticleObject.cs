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
            colorHistory = new ValueHistory<Color>(color, system.CurrentRound);
            occupiedRel = new List<Vector2Int>();
            occupiedRel.Add(Vector2Int.zero);

            graphics = new ObjectGraphicsAdapter(this, system.renderSystem.rendererObj);
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


        /*
         * IReplayHistory
         */

        public void ContinueTracking()
        {
            positionHistory.ContinueTracking();
            position = positionHistory.GetMarkedValue();
            colorHistory.ContinueTracking();
            color = colorHistory.GetMarkedValue();
            graphics.UpdateColor();
        }

        public void CutOffAtMarker()
        {
            positionHistory.CutOffAtMarker();
            colorHistory.CutOffAtMarker();
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
            graphics.UpdateColor();
        }

        public void ShiftTimescale(int amount)
        {
            positionHistory.ShiftTimescale(amount);
        }

        public void StepBack()
        {
            positionHistory.StepBack();
            position = positionHistory.GetMarkedValue();
            colorHistory.StepBack();
            color = colorHistory.GetMarkedValue();
            graphics.UpdateColor();
        }

        public void StepForward()
        {
            positionHistory.StepForward();
            position = positionHistory.GetMarkedValue();
            colorHistory.StepForward();
            color = colorHistory.GetMarkedValue();
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
            this.system = system;
            occupiedRel = new List<Vector2Int>(data.occupiedRel);

            graphics = new ObjectGraphicsAdapter(this, system.renderSystem.rendererObj);
        }

    }

} // namespace AS2.Sim
