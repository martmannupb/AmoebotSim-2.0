using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{
    class CircleDistribution
    {

        // RNG
        private const int rng_seed = 42;
        private static RandomNumberGenerator rng = new RandomNumberGenerator(rng_seed);
        // Lists
        private static List<float> points = new List<float>();
        private static List<float> newPoints = new List<float>();

        public static bool DistributePointsOnCircle(List<float> inputOutputDegreeList, float minDistanceBetweenPoints, float interationMovementTowardsCenterPercentage = 0.1f, float maxMovementPerInteraction = 360f)
        {
            // Null check
            if (inputOutputDegreeList == null || inputOutputDegreeList.Count == 0) return true;

            // Prepare lists
            points.Clear();
            newPoints.Clear();
            points.AddRange(inputOutputDegreeList);
            inputOutputDegreeList.Clear();

            // Prepare random number generator
            rng.Reset();

            // Define settings
            int maxIterations = 100;
            if (minDistanceBetweenPoints * 1.1f >= 360f / inputOutputDegreeList.Count) minDistanceBetweenPoints = (360f / 1.1f) / inputOutputDegreeList.Count; // adjust if min distance is set too large

            // Perform edited version of the Lloyd relaxation algorithm
            for (int i = 0; i < maxIterations; i++)
            {
                // Covergence bool
                bool converged = true;

                // For each point, calculate the min distance to other points
                for (int j = 0; j < points.Count; j++)
                {
                    float cur = points[j];
                    float minDist_clockwise = 360f;
                    float minDist_counterclockwise = 360f;
                    float minDist = 360f;
                    for (int k = 0; k < points.Count; k++)
                    {
                        if(j != k)
                        {
                            float next = points[k];
                            if(Mathf.Abs(cur - next) < 0.2f) // yes, we cannot use == here bec. of precision errors
                            {
                                // 2 points have the same / similar values, repeat loop
                                if(i != 0)
                                {
                                    Log.Error("CircleDistribution: Somehow adjustments are made after the first interation. This should not happen.");
                                }
                                points[j] += rng.Range(1f, 2f);
                                newPoints.Clear();
                                j = -1;
                                k = points.Count;
                                continue;
                            }
                            else
                            {
                                // Update distances
                                minDist_clockwise = Mathf.Min(RelativeDistanceBetweenPoints_Clockwise(cur, next), minDist_clockwise);
                                minDist_counterclockwise = Mathf.Min(RelativeDistanceBetweenPoints_CounterClockwise(cur, next), minDist_counterclockwise);
                                minDist = Mathf.Min(minDist_clockwise, minDist_counterclockwise);
                            }
                        }
                    }
                    if(j >= 0)
                    {
                        // Calc rel center, move into that direction
                        float newPos = cur;
                        if (minDist < minDistanceBetweenPoints)
                        {
                            float relCenter = (minDist_counterclockwise + (-minDist_clockwise)) / 2f;
                            newPos = ((cur + Mathf.Min(interationMovementTowardsCenterPercentage * relCenter, maxMovementPerInteraction)) + 360f) % 360f;
                        }
                        newPoints.Add(newPos);

                        // Check if the distances break the convergence
                        if (j == -1) converged = true; // loop repeat
                        else if (minDist_clockwise < minDistanceBetweenPoints || minDist_counterclockwise < minDistanceBetweenPoints) converged = false;
                    }
                }

                // Convergence
                if (converged)
                {
                    inputOutputDegreeList.AddRange(points);
                    return true;
                }
                
                // Prepare lists for next iteration
                points.Clear();
                List<float> temp = points;
                points = newPoints;
                newPoints = temp;
            }

            inputOutputDegreeList.AddRange(points);
            return false;
        }

        public static float DistanceBetweenPoints(float point1, float point2)
        {
            // Function to calculate distance between two points on a circle
            // taking into account that the circle wraps around at 360 degrees
            float dist = Math.Abs(point1 - point2);
            if (dist > 180)
            {
                dist = 360 - dist;
            }
            return dist;
        }

        public static float RelativeDistanceBetweenPoints(float point1, float point2)
        {
            point1 = NormalizeDegree0To360(point1) + 360f;
            point2 = NormalizeDegree0To360(point2);
            float distCounterclockwise = (point2 - point1) % 360f;
            float distClockwise = 360f - distCounterclockwise;
            if (distCounterclockwise <= distClockwise) return distCounterclockwise;
            else return -distClockwise;
        }

        public static float RelativeDistanceBetweenPoints_Clockwise(float point1, float point2)
        {
            float relDist = RelativeDistanceBetweenPoints(point1, point2);
            if (relDist < 0) return -relDist;
            else return 360f - relDist;
        }

        public static float RelativeDistanceBetweenPoints_CounterClockwise(float point1, float point2)
        {
            float relDist = RelativeDistanceBetweenPoints(point1, point2);
            if (relDist < 0) return 360f + relDist;
            else return relDist;
        }

        private static float NormalizeDegree0To360(float degree)
        {
            if (degree < 0) return degree += ((((int)-degree) / 360) + 1) * 360f;
            else return degree % 360f;
        }
    }
}