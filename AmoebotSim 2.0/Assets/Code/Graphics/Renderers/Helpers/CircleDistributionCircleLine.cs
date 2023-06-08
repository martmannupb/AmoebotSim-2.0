using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Helper class for distributing points evenly on a circle.
    /// Uses a modified version of Lloyd's algorithm. The random
    /// number generator used by the algorithm is reset on every
    /// call so that the results are deterministic.
    /// </summary>
    public static class CircleDistributionCircleLine
    {

        // RNG
        private const int rng_seed = 42;
        private static RandomNumberGenerator rng = new RandomNumberGenerator(rng_seed);
        // Lists
        private static List<float> points = new List<float>();
        private static List<float> newPoints = new List<float>();

        /// <summary>
        /// Spreads the given points on a circle such that they have a minimum
        /// distance to each other while staying close to their original position.
        /// Uses a modified version of Lloyd's algorithm.
        /// </summary>
        /// <param name="inputOutputDegreeList">A list of angles describing the points.
        /// The final result will be stored in this list as well.</param>
        /// <param name="minDistanceBetweenPoints">The minimum angular distance between
        /// any two points that should be achieved.</param>
        /// <param name="interationMovementTowardsCenterPercentage">Factor controlling
        /// how far a point should move away from its closest neighbor in one interaction.
        /// Smaller values are generally more stable but may require more iterations.</param>
        /// <param name="maxMovementPerInteraction">The maximum angular distance a point can
        /// move in one interaction.</param>
        /// <returns><c>true</c> if and only if the algorithm has converged, i.e., no
        /// more points have moved in the last iteration. If the algorithm has not
        /// converged, <c>false</c> is returned and the unfinished list of points
        /// is written into <paramref name="inputOutputDegreeList"/>.</returns>
        public static bool DistributePointsOnCircle(List<float> inputOutputDegreeList, float minDistanceBetweenPoints, float interationMovementTowardsCenterPercentage = 0.1f, float maxMovementPerInteraction = 360f)
        {
            // Null check
            if (inputOutputDegreeList == null || inputOutputDegreeList.Count <= 1) return true;

            // Prepare lists
            points.Clear();
            newPoints.Clear();
            points.AddRange(inputOutputDegreeList);
            inputOutputDegreeList.Clear();

            // Prepare random number generator
            rng.Reset();

            // Define settings
            int maxIterations = 100;
            if (minDistanceBetweenPoints * 1.1f >= 360f / points.Count)
                minDistanceBetweenPoints = (360f / 1.1f) / points.Count; // adjust if min distance is set too large

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

        /// <summary>
        /// Computes the negative counter-clockwise angle from
        /// <paramref name="point2"/> to <paramref name="point1"/>.
        /// </summary>
        /// <param name="point1">The angle of the first point.</param>
        /// <param name="point2">The angle of the second point.</param>
        /// <returns>The counter-clockwise angle from <paramref name="point2"/>
        /// to <paramref name="point1"/> with a negative sign.</returns>
        private static float RelativeDistanceBetweenPoints(float point1, float point2)
        {
            point1 = NormalizeDegree0To360(point1) + 360f;
            point2 = NormalizeDegree0To360(point2);
            return (point2 - point1) % 360f;
        }

        /// <summary>
        /// Computes the clockwise distance from <paramref name="point1"/>
        /// to <paramref name="point2"/>.
        /// </summary>
        /// <param name="point1">The angle of the first point.</param>
        /// <param name="point2">The angle of the second point.</param>
        /// <returns>The clockwise angle from <paramref name="point1"/>
        /// to <paramref name="point2"/>. Is always in the range between
        /// 0 and 360.</returns>
        private static float RelativeDistanceBetweenPoints_Clockwise(float point1, float point2)
        {
            return -RelativeDistanceBetweenPoints(point1, point2);
        }

        /// <summary>
        /// Computes the counter-clockwise distance from <paramref name="point1"/>
        /// to <paramref name="point2"/>.
        /// </summary>
        /// <param name="point1">The angle of the first point.</param>
        /// <param name="point2">The angle of the second point.</param>
        /// <returns>The counter-clockwise angle from <paramref name="point1"/>
        /// to <paramref name="point2"/>. Is always in the range between
        /// 0 and 360.</returns>
        private static float RelativeDistanceBetweenPoints_CounterClockwise(float point1, float point2)
        {
            return 360 + RelativeDistanceBetweenPoints(point1, point2);
        }

        /// <summary>
        /// Shifts the given angle to the range between
        /// 0 and 360 degrees.
        /// </summary>
        /// <param name="degree">The angle to be normalized in degrees.</param>
        /// <returns>The angle described by <paramref name="degree"/>,
        /// shifted to the range from 0 to 360.</returns>
        private static float NormalizeDegree0To360(float degree)
        {
            if (degree < 0)
                return degree += ((((int)-degree) / 360) + 1) * 360f;
            else
                return degree % 360f;
        }
    }
}
