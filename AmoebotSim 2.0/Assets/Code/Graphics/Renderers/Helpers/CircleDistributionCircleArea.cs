using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{
    /// <summary>
    /// Helper class for distributing points evenly inside a circle.
    /// Uses a modified version of Lloyd's algorithm. The random
    /// number generator used by the algorithm is reset on every
    /// call so that the results are deterministic.
    /// </summary>
    public static class CircleDistributionCircleArea
    {

        // RNG
        private const int rng_seed = 42;
        private static RandomNumberGenerator rng = new RandomNumberGenerator(rng_seed);
        // Lists
        private static List<Vector2> points = new List<Vector2>();
        private static List<Vector2> newPoints = new List<Vector2>();
        private static List<Vector2> shortDistancePoints = new List<Vector2>();

        /// <summary>
        /// Spreads the given points in a circle such that they have a minimum
        /// distance to each other while staying close to their original position
        /// and inside the circle.
        /// Uses a modified version of Lloyd's algorithm.
        /// </summary>
        /// <param name="inputOutputCoordList">A list of point coordinates relative
        /// to the circle's center. The final result will be stored in this list as well.</param>
        /// <param name="minDistanceBetweenPoints">The minimum distance between
        /// any two points that should be achieved.</param>
        /// <param name="minMovementPerIteration">The minimum distance a point should be
        /// moved per iteration if it is too close to other points.</param>
        /// <param name="maxMovementPerIteration">The maximum distance a point should be
        /// moved per iteration if it is too close to other points.</param>
        /// <param name="maxCircleRadius">The radius of the circle.</param>
        /// <returns><c>true</c> if and only if the algorithm has converged, i.e., no
        /// more points have moved in the last iteration. If the algorithm has not
        /// converged, <c>false</c> is returned and the unfinished list of points
        /// is written into <paramref name="inputOutputCoordList"/>.</returns>
        public static bool DistributePointsInCircle(List<Vector2> inputOutputCoordList, float minDistanceBetweenPoints, float minMovementPerIteration = 0.05f, float maxMovementPerIteration = 0.1f, float maxCircleRadius = 0.45f)
        {
            // Null check
            if (inputOutputCoordList == null || inputOutputCoordList.Count == 0) return true;
            if(inputOutputCoordList.Count == 1)
            {
                // Clamp to circle bounds
                if (inputOutputCoordList[0].magnitude > maxCircleRadius)
                    inputOutputCoordList[0] = inputOutputCoordList[0].normalized * maxCircleRadius;
                return true;
            }

            // Prepare lists
            points.Clear();
            newPoints.Clear();
            foreach (var p in inputOutputCoordList)
            {
                // Clamp to circle bounds
                Vector2 vector = p;
                if (vector.magnitude > maxCircleRadius)
                    vector = vector.normalized * maxCircleRadius;
                points.Add(vector);
            }
            inputOutputCoordList.Clear();

            // Prepare random number generator
            rng.Reset();

            // Define settings
            int maxIterations = 100;
            if (points.Count > 1 && minDistanceBetweenPoints * 1.1f >= (maxCircleRadius * 2f) / (points.Count - 1))
                minDistanceBetweenPoints = (maxCircleRadius * 2f / 1.1f) / (points.Count - 1); // adjust if min distance is set too large

            // Perform edited version of the Lloyd relaxation algorithm
            for (int i = 0; i < maxIterations; i++)
            {
                // Covergence bool
                bool converged = true;

                // For each point, calculate the min distance to other points
                for (int j = 0; j < points.Count; j++)
                {
                    Vector2 cur = points[j];
                    shortDistancePoints.Clear();
                    for (int k = 0; k < points.Count; k++)
                    {
                        if (j != k)
                        {
                            Vector2 next = points[k];
                            float distance = Vector2.Distance(cur, next);
                            if (distance < 0.01f) // yes, we cannot use == here bec. of precision errors
                            {
                                // 2 points have the same / similar values, repeat loop
                                if (i != 0)
                                {
                                    Log.Warning("CircleDistribution: Somehow adjustments are made after the first interation. This should not happen.");
                                }
                                Tuple<double, double> degreeAndRadius = Library.DegreeConstants.CartesianToPolar(cur);
                                bool useCenterVector = maxCircleRadius - degreeAndRadius.Item2 <= maxCircleRadius * 0.1f;
                                Vector2 offsetVector = Library.DegreeConstants.PolarToCartesian(useCenterVector ? ((degreeAndRadius.Item1 + 180f) % 360f) : rng.Range(0f, 360f), rng.Range(0.01f, 0.05f));
                                points[j] += offsetVector;
                                newPoints.Clear();
                                j = -1;
                                k = points.Count;
                                continue;
                            }
                            else
                            {
                                // Update distances
                                if(distance < minDistanceBetweenPoints)
                                {
                                    shortDistancePoints.Add(next - cur);
                                }
                            }
                        }
                    }
                    if (j >= 0)
                    {
                        // Calc rel center, move into that direction
                        Vector2 newPos = cur;
                        Vector2 offsetDirection = Vector2.zero;
                        Vector2 nearestOffset = new Vector2(float.MaxValue, float.MaxValue);
                        float nearestOffsetMagnitude = float.MaxValue;
                        if (shortDistancePoints.Count > 0)
                        {
                            // We need another iteration
                            converged = false;
                            // Calculate offset direction
                            foreach (var item in shortDistancePoints)
                            {
                                // Check if nearest point
                                float magnitude = item.magnitude;
                                if(magnitude < nearestOffsetMagnitude)
                                {
                                    nearestOffset = item;
                                    nearestOffsetMagnitude = magnitude;
                                }
                                // Update offset direction
                                float scaleFactor = (minDistanceBetweenPoints - item.magnitude);
                                Vector2 offsetAddition = scaleFactor * new Vector2(-item.x, -item.y).normalized;
                                offsetDirection += offsetAddition;
                            }
                            // Include offset
                            if (offsetDirection != Vector2.zero)
                                offsetDirection = offsetDirection.normalized;
                            else
                                offsetDirection = new Vector2(-nearestOffset.x, -nearestOffset.y).normalized;
                            newPos += offsetDirection * Mathf.Lerp(maxMovementPerIteration, minMovementPerIteration, nearestOffsetMagnitude / minDistanceBetweenPoints) ;
                            // Clamp to circle bounds
                            if (newPos.magnitude > maxCircleRadius)
                                newPos = newPos.normalized * maxCircleRadius;
                        }
                        newPoints.Add(newPos);

                        // Check if the distances break the convergence
                        if (j == -1) converged = true; // loop repeat
                    }
                }

                // Convergence
                if (converged)
                {
                    inputOutputCoordList.AddRange(points);
                    return true;
                }

                // Prepare lists for next iteration
                points.Clear();
                List<Vector2> temp = points;
                points = newPoints;
                newPoints = temp;
            }

            inputOutputCoordList.AddRange(points);
            return false;
        }
    }
}
