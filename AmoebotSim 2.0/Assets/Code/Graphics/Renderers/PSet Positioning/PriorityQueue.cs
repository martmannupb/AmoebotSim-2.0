using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    public class PriorityQueue<T>
    {
        List<Tuple<float, T>> items = new List<Tuple<float, T>>();

        public int Count {
            get {
                return items.Count;
            }
        }

        public void Enqueue(float priority, T value)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if(items[i].Item1 > priority)
                {
                    items.Insert(i, new Tuple<float, T>(priority, value));
                    return;
                }
            }
            items.Add(new Tuple<float, T>(priority, value));
        }

        public T Dequeue()
        {
            if (Count == 0) return default;
            T item = items[0].Item2;
            items.RemoveAt(0);
            return item;
        }

        public T Peek()
        {
            if (Count == 0) return default;
            T item = items[0].Item2;
            return item;
        }

        public bool IsEmpty()
        {
            return items.Count == 0;
        }

        public void Clear()
        {
            items.Clear();
        }

        /// <summary>
        /// Returns the internal list which is sorted in ascending order.
        /// </summary>
        /// <returns></returns>
        public List<Tuple<float, T>> GetSortedList()
        {
            return items;
        }
    }
}