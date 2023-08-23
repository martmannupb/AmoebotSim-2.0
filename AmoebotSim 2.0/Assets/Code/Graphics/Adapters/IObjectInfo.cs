using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Interface implemented by objects so that they can be
    /// selected and modified through the UI.
    /// </summary>
    public interface IObjectInfo
    {
        public ICollection<Vector2Int> OccupiedPositions();
    }

} // namespace AS2.Visuals
