using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Utility methods and constants for computations done by the
    /// <see cref="ParticleSystem"/>.
    /// </summary>
    public static class ParticleSystem_Utils
    {
        /*
         * Label conversion matrices
         * 
         * Labels are indices assigned to the edges incident to a
         * particle. They range from 0 to 5 for a contracted
         * particle and from 0 to 9 for an expanded particle, and
         * they increase in local counter-clockwise order around the
         * particle (if the chirality of the particle is reversed,
         * the global direction of the edges increases in clockwise
         * direction).
         * 
         * Labels of contracted particles are equal to the integer
         * representations of the local directions.
         * 
         * For expanded particles, labels 0,1,2 always correspond to
         * directions 0,1,2. For head directions 0,1,2, label 0 belongs
         * to the head, and for head directions 3,4,5, label 0 belongs
         * to the tail.
         * 
         * Label computations are always based on the integer mapping
         * of cardinal directions. Computing labels for secondary
         * directions does not make sense and using integers allows
         * a fast lookup with array indices.
         */

        /// <summary>
        /// Conversion matrix for converting a local direction of an
        /// expanded particle into a label.
        /// <para>
        /// The entry at position <c>[exp,loc]</c> is the head label
        /// of an expanded particle with head direction <c>exp</c>
        /// corresponding to the local direction <c>loc</c>. To get
        /// the tail label for that direction, use <c>(exp + 3) % 6</c>
        /// as the first index.
        /// </para>
        /// <para>
        /// A label value of <c>-1</c> means that the label does not
        /// exist because there is no port in that location.
        /// </para>
        /// </summary>
        private static readonly int[,] expandedLabels = new int[6, 6]
        {
        // Head direction 0,...,5 for head labels or
        // 3,4,5,0,1,2 for tail labels
        { 0, 1, 2, -1, 8, 9 },
        { 0, 1, 2, 3, -1, 9 },
        { 0, 1, 2, 3, 4, -1 },
        { -1, 3, 4, 5, 6, 7 },
        { 8, -1, 4, 5, 6, 7 },
        { 8, 9, -1, 5, 6, 7 }
        };

        /// <summary>
        /// Conversion matrix for converting a label into a direction.
        /// <para>
        /// The entry at position <c>[exp,label]</c> is the direction
        /// of an expanded particle with head direction <c>exp</c>
        /// corresponding to label <c>label</c>.
        /// </para>
        /// <para>
        /// Note that the conversion for head directions <c>3,4,5</c>
        /// is the same as for head directions <c>0,1,2</c>, which is
        /// why the second half of the matrix is redundant.
        /// </para>
        /// </summary>
        private static readonly int[,] labelDirections = new int[,]
        {
        // Head direction 0,...,5
        { 0, 1, 2, 1, 2, 3, 4, 5, 4, 5 },
        { 0, 1, 2, 3, 2, 3, 4, 5, 0, 5 },
        { 0, 1, 2, 3, 4, 3, 4, 5, 0, 1 },
        // NOTE: The second half could be removed, but we would have
        // one additional operation when reading...
        { 0, 1, 2, 1, 2, 3, 4, 5, 4, 5 },
        { 0, 1, 2, 3, 2, 3, 4, 5, 0, 5 },
        { 0, 1, 2, 3, 4, 3, 4, 5, 0, 1 }
        };

        /// <summary>
        /// Bool matrix telling whether a given label is a head or
        /// tail label.
        /// <para>
        /// The entry at position <c>[exp,label]</c> is <c>true</c>
        /// if and only if the label <c>label</c> belongs to the
        /// head of an expanded particle with head direction <c>exp</c>.
        /// </para>
        /// <para>
        /// Note that the entries for head directions <c>3,4,5</c> are
        /// exactly the opposite of the entries for head directions
        /// <c>0,1,2</c>.
        /// </para>
        /// </summary>
        private static readonly bool[,] isHeadLabel = new bool[,]
        {
        // Head direction 0,...,5
        { true, true, true, false, false, false, false, false, true, true },
        { true, true, true, true, false, false, false, false, false, true },
        { true, true, true, true, true, false, false, false, false, false },
        // NOTE: The second half could be removed, but we would have
        // additional operations when reading...
        { false, false, false, true, true, true, true, true, false, false },
        { false, false, false, false, true, true, true, true, true, false },
        { false, false, false, false, false, true, true, true, true, true }
        };

        /// <summary>
        /// Computes the edge label corresponding to the given direction and
        /// expansion state.
        /// <para>
        /// If <paramref name="headDir"/> is <see cref="Direction.NONE"/>,
        /// i.e., the particle is contracted, the label always equals the
        /// given <paramref name="direction"/>'s int representation.
        /// </para>
        /// </summary>
        /// <param name="direction">The direction of the edge marked by
        /// the returned label.</param>
        /// <param name="headDir">The head direction of the particle, if
        /// it is expanded. Leave it at <see cref="Direction.NONE"/> otherwise.</param>
        /// <param name="head">If the particle is expanded, this flag
        /// determines whether the label belongs to the particle's head
        /// or tail.</param>
        /// <returns>The label of the edge specified by the direction
        /// and expansion state of a particle.</returns>
        public static int GetLabelInDir(Direction direction, Direction headDir = Direction.NONE, bool head = true)
        {
            if (headDir == Direction.NONE)
            {
                return direction.ToInt();
            }
            else
            {
                return expandedLabels[head ? headDir.ToInt() : ((headDir.ToInt() + 3) % 6), direction.ToInt()];
            }
        }

        /// <summary>
        /// Computes the direction in which the given edge label points.
        /// </summary>
        /// <param name="label">The label of which to compute the direction.</param>
        /// <param name="headDir">The head direction of the particle, if it is
        /// expanded. Leave it at <see cref="Direction.NONE"/> otherwise.</param>
        /// <returns>The direction of the edge label <paramref name="label"/>
        /// of a particle with head direction <paramref name="headDir"/>.</returns>
        public static Direction GetDirOfLabel(int label, Direction headDir = Direction.NONE)
        {
            if (headDir == Direction.NONE)
            {
                return DirectionHelpers.Cardinal(label);
            }
            else
            {
                return DirectionHelpers.Cardinal(labelDirections[headDir.ToInt(), label]);
            }
        }

        /// <summary>
        /// Checks if the given label belongs to the particle's head.
        /// <para>
        /// If <paramref name="headDir"/> is <see cref="Direction.NONE"/>,
        /// i.e., the particle is contracted, the result will always be <c>true</c>.
        /// </para>
        /// </summary>
        /// <param name="label">The edge label to check.</param>
        /// <param name="headDir">The head direction of the particle, if
        /// it is expanded. Leave it at <see cref="Direction.NONE"/> otherwise.</param>
        /// <returns><c>true</c> if and only if the given <paramref name="label"/>
        /// represents an edge incident to the particle's head.</returns>
        public static bool IsHeadLabel(int label, Direction headDir = Direction.NONE)
        {
            if (headDir == Direction.NONE)
            {
                return true;
            }
            else
            {
                return isHeadLabel[headDir.ToInt(), label];
            }
        }

        /// <summary>
        /// Checks if the given label belongs to the particle's tail.
        /// <para>
        /// If <paramref name="headDir"/> is <see cref="Direction.NONE"/>,
        /// i.e., the particle is contracted, the result will always be <c>true</c>.
        /// </para>
        /// </summary>
        /// <param name="label">The edge label to check.</param>
        /// <param name="headDir">The head direction of the particle, if
        /// it is expanded. Leave it at <see cref="Direction.NONE"/> otherwise.</param>
        /// <returns><c>true</c> if and only if the given <paramref name="label"/>
        /// represents an edge incident to the particle's tail.</returns>
        public static bool IsTailLabel(int label, Direction headDir = Direction.NONE)
        {
            if (headDir == Direction.NONE)
            {
                return true;
            }
            else
            {
                return !isHeadLabel[headDir.ToInt(), label];
            }
        }

        /// <summary>
        /// Computes the unit vector in the grid coordinate system that
        /// points in the given direction. Secondary directions are mapped
        /// to their corresponding cardinal direction.
        /// </summary>
        /// <param name="globalDir">The global direction of the vector.</param>
        /// <returns>A grid vector representing one step in the indicated
        /// global direction. Will be <c>(0,0)</c> if <paramref name="globalDir"/>
        /// is <see cref="Direction.NONE"/>.</returns>
        public static Vector2Int DirectionToVector(Direction globalDir)
        {
            if (globalDir == Direction.NONE) return Vector2Int.zero;
            int dirId = globalDir.ToInt();
            switch (dirId)
            {
                case 0:
                    return new Vector2Int(1, 0);
                case 1:
                    return new Vector2Int(0, 1);
                case 2:
                    return new Vector2Int(-1, 1);
                case 3:
                    return new Vector2Int(-1, 0);
                case 4:
                    return new Vector2Int(0, -1);
                case 5:
                    return new Vector2Int(1, -1);
                default:
                    throw new System.ArgumentOutOfRangeException("globalDir", "Direction must have int representation in {0,1,2,3,4,5}.");
            }
        }

        /// <summary>
        /// Computes the global direction into which the given grid vector is pointing.
        /// </summary>
        /// <param name="vector">The grid vector whose direction should be found.</param>
        /// <returns>The direction into which <paramref name="vector"/> is pointing, if
        /// it is a multiple of the corresponding unit vector, otherwise
        /// <see cref="Direction.NONE"/>.</returns>
        public static Direction VectorToDirection(Vector2Int vector)
        {
            int x = vector.x;
            int y = vector.y;
            if (x > 0)
            {
                if (y == 0)
                    return Direction.E;
                else if (y == -x)
                    return Direction.SSE;
            }
            else if (x < 0)
            {
                if (y == 0)
                    return Direction.W;
                else if (y == -x)
                    return Direction.NNW;
            }
            else // x == 0
            {
                if (y > 0)
                    return Direction.NNE;
                else if (y < 0)
                    return Direction.SSW;
            }
            return Direction.NONE;
        }

        /// <summary>
        /// Computes the neighbor of a grid node position in the given direction and distance.
        /// </summary>
        /// <param name="pos">The original grid node position.</param>
        /// <param name="globalDir">The global direction in which the neighbor lies.
        /// Secondary directions are mapped back to their cardinal directions.</param>
        /// <param name="distance">The number of steps to reach the neighbor from the original position.</param>
        /// <returns>The node that lies <paramref name="distance"/> steps in direction <paramref name="globalDir"/>
        /// from node <paramref name="pos"/>.</returns>
        public static Vector2Int GetNbrInDir(Vector2Int pos, Direction globalDir, int distance = 1)
        {
            return pos + distance * DirectionToVector(globalDir);
        }

        /// <summary>
        /// Turns the given local direction into the corresponding global direction
        /// based on the given compass orientation and chirality.
        /// </summary>
        /// <param name="locDir">The local direction.</param>
        /// <param name="compassDir">The global offset of the compass direction (independent of chirality).
        /// This is the global direction that is interpreted as <see cref="Direction.E"/> by the
        /// local direction.</param>
        /// <param name="chirality">The direction in which rotation is applied for the
        /// local direction. <c>true</c> means counter-clockwise is positive rotation and
        /// <c>false</c> means clockwise.</param>
        /// <returns>The global direction corresponding to <paramref name="locDir"/> offset by <paramref name="compassDir"/>.</returns>
        public static Direction LocalToGlobalDir(Direction locDir, Direction compassDir, bool chirality)
        {
            return locDir.AddTo(compassDir, !chirality);
            // Equivalent to this:
            // return chirality ? (compassDir + locDir) % 6 : (compassDir - locDir + 6) % 6;
        }

        /// <summary>
        /// Turns the given global direction into the corresponding local direction
        /// based on the given compass orientation and chirality.
        /// </summary>
        /// <param name="globalDir">The global direction.</param>
        /// <param name="compassDir">The global offset of the compass direction (independent of chirality).
        /// This is the global direction that is interpreted as <see cref="Direction.E"/> by the
        /// local direction.</param>
        /// <param name="chirality">The direction in which rotation is applied for the
        /// local direction. <c>true</c> means counter-clockwise is positive rotation and
        /// <c>false</c> means clockwise.</param>
        /// <returns>The local direction corresponding to <paramref name="globalDir"/> offset by <paramref name="compassDir"/>.</returns>
        public static Direction GlobalToLocalDir(Direction globalDir, Direction compassDir, bool chirality)
        {
            return globalDir.Subtract(compassDir, !chirality);
            // Equivalent to this:
            // return chirality ? (globalDir - compassDir + 6) % 6 : (compassDir - globalDir + 6) % 6;
        }

        /// <summary>
        /// Translates the given local label into the corresponding global label.
        /// </summary>
        /// <param name="localLabel">The local label to be translated.</param>
        /// <param name="localHeadDirection">The local head direction of the particle.</param>
        /// <param name="compassDir">The global compass direction of the particle.</param>
        /// <param name="chirality">The chirality of the particle.</param>
        /// <returns>The global label that identifies the same port as the given
        /// local label.</returns>
        public static int LocalToGlobalLabel(int localLabel, Direction localHeadDirection, Direction compassDir, bool chirality)
        {
            Direction localDir = GetDirOfLabel(localLabel, localHeadDirection);
            bool head = IsHeadLabel(localLabel, localHeadDirection);
            Direction globalDir = LocalToGlobalDir(localDir, compassDir, chirality);
            Direction globalHeadDir = LocalToGlobalDir(localHeadDirection, compassDir, chirality);
            return GetLabelInDir(globalDir, globalHeadDir, head);
        }

        /// <summary>
        /// Translates the given global label into the corresponding local label.
        /// </summary>
        /// <param name="globalLabel">The global label to be translated.</param>
        /// <param name="globalHeadDirection">The global head direction of the particle.</param>
        /// <param name="compassDir">The global compass direction of the particle.</param>
        /// <param name="chirality">The chirality of the particle.</param>
        /// <returns>The local label that identifies the same port as the given
        /// global label.</returns>
        public static int GlobalToLocalLabel(int globalLabel, Direction globalHeadDirection, Direction compassDir, bool chirality)
        {
            Direction globalDir = GetDirOfLabel(globalLabel, globalHeadDirection);
            bool head = IsHeadLabel(globalLabel, globalHeadDirection);
            Direction localDir = GlobalToLocalDir(globalDir, compassDir, chirality);
            Direction localHeadDirection = GlobalToLocalDir(globalHeadDirection, compassDir, chirality);
            return GetLabelInDir(localDir, localHeadDirection, head);
        }

        /// <summary>
        /// Determines the grid node neighboring the given particle in the
        /// indicated direction.
        /// </summary>
        /// <param name="p">The Particle whose neighbor node to find.</param>
        /// <param name="locDir">The local direction of the Particle <paramref name="p"/>
        /// indicating in which direction to look.</param>
        /// <param name="fromHead">If <c>true</c>, use the Particle's head as reference,
        /// otherwise use the Particle's tail.</param>
        /// <returns>The grid node in direction <paramref name="locDir"/> relative to
        /// Particle <paramref name="p"/>'s head or tail, depending on <paramref name="fromHead"/>.</returns>
        public static Vector2Int GetNeighborPosition(Particle p, Direction locDir, bool fromHead)
        {
            return GetNbrInDir(fromHead ? p.Head() : p.Tail(), LocalToGlobalDir(locDir, p.comDir, p.chirality));
        }

        public static int GridDistance(Vector2Int p1, Vector2Int p2)
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
    }

} // namespace AS2.Sim
