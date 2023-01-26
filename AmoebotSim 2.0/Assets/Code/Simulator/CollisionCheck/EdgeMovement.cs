using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    public struct EdgeMovement
    {
        public Vector2Int start1;   // 1 means before movement
        public Vector2Int end1;
        public Vector2Int start2;   // 2 means after movement
        public Vector2Int end2;

        private EdgeMovement(Vector2Int s1, Vector2Int e1, Vector2Int s2, Vector2Int e2)
        {
            start1 = s1;
            end1 = e1;
            start2 = s2;
            end2 = e2;
        }

        public void Reset(Vector2Int s1, Vector2Int e1, Vector2Int s2, Vector2Int e2)
        {
            start1 = s1;
            end1 = e1;
            start2 = s2;
            end2 = e2;
        }

        public void Reset()
        {
            start1 = Vector2Int.zero;
            end1 = Vector2Int.zero;
            start2 = Vector2Int.zero;
            end2 = Vector2Int.zero;
        }

        public Vector2Int StartTranslation()
        {
            return start2 - start1;
        }

        public Vector2Int EndTranslation()
        {
            return end2 - end1;
        }

        public override string ToString()
        {
            return start1 + "-" + end1 + "  -->  " + start2 + "-" + end2;
        }

        // Pooling

        private static Stack<EdgeMovement> pool = new Stack<EdgeMovement>();

        public static EdgeMovement Create(Vector2Int s1, Vector2Int e1, Vector2Int s2, Vector2Int e2)
        {
            if (pool.Count > 0)
            {
                EdgeMovement em = pool.Pop();
                em.Reset(s1, e1, s2, e2);
                return em;
            }
            else
            {
                return new EdgeMovement(s1, e1, s2, e2);
            }
        }

        public static void Release(EdgeMovement em)
        {
            pool.Push(em);
        }
    }

} // namespace AS2.Sim
