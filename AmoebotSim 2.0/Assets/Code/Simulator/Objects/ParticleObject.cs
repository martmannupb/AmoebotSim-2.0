using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    public class ParticleObject : IReplayHistory
    {
        private Vector2Int position;
        private ValueHistory<Vector2Int> positionHistory;
        private List<Vector2Int> occupiedRel;

        private ParticleSystem system;

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
