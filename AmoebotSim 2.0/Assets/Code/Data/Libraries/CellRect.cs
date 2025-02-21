// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AS2
{

    public struct CellRect
    {

        public int minX;
        public int minY;
        public int maxX;
        public int maxY;

        public int Width
        {
            get
            {
                return maxX - minX + 1;
            }
        }
        public int Height
        {
            get
            {
                return maxY - minY + 1;
            }
        }
        public int Area
        {
            get
            {
                return Width * Height;
            }
        }

        public CellRect(int minX, int minY, int width, int height)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = minX + width - 1;
            this.maxY = minY + height - 1;
        }

        /// <summary>
        /// Returns if the rect is valid.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return Width > 0 && Height > 0;
        }

        /// <summary>
        /// Returns if the given point is in the CellRect.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Contains(int x, int y)
        {
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }








        // Operation -----------------------

        /// <summary>
        /// Returns the cut set of the two rects (which is another simple rect). If there is no cut, an invalid rect with Width=0 and Height=0 is returned.
        /// Can be used for an iterator that does not return values.
        /// </summary>
        /// <param name="otherRect"></param>
        /// <returns></returns>
        public CellRect CellRect_CutSet(CellRect otherRect)
        {
            int minValueX = Mathf.Max(minX, otherRect.minX);
            int minValueY = Mathf.Max(minY, otherRect.minY);
            int maxValueX = Mathf.Min(maxX, otherRect.maxX);
            int maxValueY = Mathf.Min(maxY, otherRect.maxY);
            CellRect cutSetRect = new CellRect(minValueX, minValueY, maxValueX - minValueX + 1, maxValueY - minValueY + 1);
            if (minValueX > maxValueX || minValueY > maxValueY)
            {
                // Get invalid rect (iterator would not return values)
                cutSetRect = new CellRect(0, 0, 0, 0);
            }
            return cutSetRect;
        }

        /// <summary>
        /// Returns a list of rects that contain each field from the complement of "this \ otherRect" exactly once.
        /// </summary>
        /// <param name="rect2"></param>
        /// <returns></returns>
        public List<CellRect> CellRect_Complement(CellRect otherRect)
        {
            List<CellRect> list = new List<CellRect>();
            // Check if the cut is empty
            if (maxX < otherRect.minX || minX > otherRect.maxX || maxY < otherRect.minY || minY > otherRect.maxY)
            {
                // Cut is empty
                // Add this element to list
                list.Add(this);
            }
            else
            {
                // Cut is not empty
                if (minX < otherRect.minX)
                {
                    // Left side
                    int minValueX = minX;
                    int minValueY = minY;
                    int maxValueX = Mathf.Min(maxX, otherRect.minX - 1);
                    int maxValueY = maxY;
                    CellRect leftRect = new CellRect(minValueX, minValueY, maxValueX - minValueX + 1, maxValueY - minValueY + 1);
                    list.Add(leftRect);
                }
                if (maxX > otherRect.maxX)
                {
                    // Right side
                    int minValueX = Mathf.Max(minX, otherRect.maxX + 1);
                    int minValueY = minY;
                    int maxValueX = maxX;
                    int maxValueY = maxY;
                    CellRect rightRect = new CellRect(minValueX, minValueY, maxValueX - minValueX + 1, maxValueY - minValueY + 1);
                    list.Add(rightRect);
                }
                // Top and bottom side
                if (minX <= otherRect.maxX && maxX >= otherRect.minX)
                {
                    if (maxY > otherRect.maxY)
                    {
                        // Top side
                        int minValueX = Mathf.Max(minX, otherRect.minX);
                        int minValueY = Mathf.Max(minY, otherRect.maxY + 1);
                        int maxValueX = Mathf.Min(maxX, otherRect.maxX);
                        int maxValueY = maxY;
                        CellRect topRect = new CellRect(minValueX, minValueY, maxValueX - minValueX + 1, maxValueY - minValueY + 1);
                        list.Add(topRect);
                    }
                    if (minY < otherRect.minY)
                    {
                        // Bottom side
                        int minValueX = Mathf.Max(minX, otherRect.minX);
                        int minValueY = minY;
                        int maxValueX = Mathf.Min(maxX, otherRect.maxX);
                        int maxValueY = Mathf.Min(maxY, otherRect.minY - 1);
                        CellRect bottomRect = new CellRect(minValueX, minValueY, maxValueX - minValueX + 1, maxValueY - minValueY + 1);
                        list.Add(bottomRect);
                    }
                }
            }
            return list;
        }

        public static bool operator ==(CellRect left, CellRect right)
        {
            return left.minX == right.minX && left.minY == right.minY && left.maxX == right.maxX && left.maxY == right.maxY;
        }

        public static bool operator !=(CellRect lhs, CellRect rhs) => !(lhs == rhs);

        public override bool Equals(object obj)
        {
            return obj is CellRect rect && this == (CellRect)obj;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(minX, minY, maxX, maxY);
        }






        // ToString

        public override string ToString()
        {
            return "CellRect: Min(" + minX + ", " + minY + "), Max(" + maxX + ", " + maxY + ")";
        }





        // Iterator ---------------------------------

        public CellRect_Iterator GetIterator()
        {
            return new CellRect_Iterator(this);
        }

        public struct CellRect_Iterator
        {
            private int minX;
            private int maxX;
            private int maxY;
            private int x;
            private int y;

            public CellRect_Iterator(CellRect cellRect)
            {
                this.minX = cellRect.minX;
                this.maxX = cellRect.maxX;
                this.maxY = cellRect.maxY;
                this.x = cellRect.minX;
                this.y = cellRect.minY;
            }

            public Vector2Int Current()
            {
                return new Vector2Int(x, y);
            }

            public void Next()
            {
                x++;
                if (x > maxX)
                {
                    x = minX;
                    y++;
                }
            }

            public bool Finished()
            {
                return y > maxY || x > maxX;
            }
        }
    }

} // namespace AS2
