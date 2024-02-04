using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.PASC;

namespace AS2.Subroutines.ConvexShapeContainment
{

    /// <summary>
    /// Wrapper for all convex shape containment subroutines.
    /// Handles all types of convex shapes and finds all valid
    /// placements for the given rotation and side lengths.
    /// </summary>
    public class SubConvexShapeContainment : Subroutine
    {

        // State:
        //     30         29      28         27        26       25      24       23      22        21      2018    1715    1412     119      876     543        210      
        // x   x          x       x          x         x        x       x        x       x         x       xxx     xxx     xxx      xxx      xxx     xxx        xxx
        //     Send Hex   Color   Finished   Success   MSB d2   MSB a   Bit d2   Bit a   Hex 1/2   Valid   Succ.   Pred.   Dir h2   Dir h1   Dir w   Rotation   Shape
        ParticleAttribute<int> state;

        BinAttributeEnum<ShapeType> shapeType;      // The type of the considered shape
        BinAttributeInt rotation;                   // The rotation of the considered shape
        BinAttributeDirection directionW;           // Main direction of the shape check
        BinAttributeDirection directionH1;          // Secondary direction of the shape check
        BinAttributeDirection directionH2;          // Secondary direction of a hexagon's second part
        BinAttributeDirection directionPred;        // Counter predecessor direction
        BinAttributeDirection directionSucc;        // Counter successor direction
        BinAttributeBool valid;                     // Whether this is a valid placement
        BinAttributeBool hexHalfDone;               // Whether we have already tested the first part of a hexagon
        BinAttributeBool bitA;                      // The bits and MSBs of the trapezoid half of a hexagon
        BinAttributeBool bitD2;
        BinAttributeBool msbA;
        BinAttributeBool msbD2;
        BinAttributeBool success;                   // Whether the containment check was successful
        BinAttributeBool finished;                  // Whether the containment check has finished
        BinAttributeBool color;                     // Whether the subroutine should control the color
        BinAttributeBool sendHex;                   // Whether we have to send / receive the hexagon intersection beep

        SubPASC2 sharedPasc;
        SubParallelogram parallelogram;
        SubMergingAlgo mergeAlgo;

        public SubConvexShapeContainment(Particle p, SubParallelogram parallelogramInstance = null, SubMergingAlgo mergingAlgoInstance = null) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[CSC] State"), 0);

            shapeType = new BinAttributeEnum<ShapeType>(state, 0, 3);
            rotation = new BinAttributeInt(state, 3, 3);
            directionW = new BinAttributeDirection(state, 6);
            directionH1 = new BinAttributeDirection(state, 9);
            directionH2 = new BinAttributeDirection(state, 12);
            directionPred = new BinAttributeDirection(state, 15);
            directionSucc = new BinAttributeDirection(state, 18);
            valid = new BinAttributeBool(state, 21);
            hexHalfDone = new BinAttributeBool(state, 22);
            bitA = new BinAttributeBool(state, 23);
            bitD2 = new BinAttributeBool(state, 24);
            msbA = new BinAttributeBool(state, 25);
            msbD2 = new BinAttributeBool(state, 26);
            success = new BinAttributeBool(state, 27);
            finished = new BinAttributeBool(state, 28);
            color = new BinAttributeBool(state, 29);
            sendHex = new BinAttributeBool(state, 30);

            if (parallelogramInstance is null && mergingAlgoInstance is null)
                sharedPasc = new SubPASC2(p);

            if (parallelogramInstance is null)
                parallelogram = new SubParallelogram(p, sharedPasc);
            else
                parallelogram = parallelogramInstance;
            if (mergingAlgoInstance is null)
                mergeAlgo = new SubMergingAlgo(p, sharedPasc, null);
            else
                mergeAlgo = mergingAlgoInstance;
        }

        public void Init(ShapeType shapeType, Direction dirW, Direction dirH, int rotation, bool controlColor = false,
            bool startHexWithPentagon = false, Direction dirH2 = Direction.NONE,
            Direction counterPred = Direction.NONE, Direction counterSucc = Direction.NONE,
            bool bitA = false, bool msbA = false, bool bitD = false, bool msbD = false,
            bool bitC = false, bool msbC = false, bool bitA2 = false, bool msbA2 = false, bool bitA3 = false, bool msbA3 = false,
            bool bitD2 = false, bool msbD2 = false)
        {
            state.SetValue(0);

            this.shapeType.SetValue(shapeType);
            this.rotation.SetValue(rotation);
            this.color.SetValue(controlColor);
            this.directionW.SetValue(dirW);
            this.directionH1.SetValue(dirH);
            this.directionH2.SetValue(dirH2);
            this.directionPred.SetValue(counterPred);
            this.directionSucc.SetValue(counterSucc);
            this.bitA.SetValue(bitA);
            this.msbA.SetValue(msbA);
            this.bitD2.SetValue(startHexWithPentagon ? bitD2 : bitC);
            this.msbD2.SetValue(startHexWithPentagon ? msbD2 : msbC);

            if (shapeType == ShapeType.TRIANGLE)
            {
                mergeAlgo.Init(shapeType, dirW, dirH, rotation, controlColor, counterPred, counterSucc, bitA, msbA);
            }
            else if (shapeType == ShapeType.PARALLELOGRAM)
            {
                parallelogram.Init(dirW, dirH, rotation, controlColor, counterPred, counterSucc, bitA, bitD, msbA, msbD);
            }
            else if (shapeType == ShapeType.TRAPEZOID)
            {
                mergeAlgo.Init(shapeType, dirW, dirH, rotation, controlColor, counterPred, counterSucc, bitA, msbA, bitD, msbD);
            }
            else if (shapeType == ShapeType.PENTAGON || shapeType == ShapeType.HEXAGON && startHexWithPentagon)
            {
                mergeAlgo.Init(ShapeType.PENTAGON, dirW, dirH, rotation, controlColor, counterPred, counterSucc, bitA, msbA, bitD, msbD, bitC, msbC, bitA2, msbA2, bitA3, msbA3);
            }
            else if (shapeType == ShapeType.HEXAGON && !startHexWithPentagon)
            {
                mergeAlgo.Init(ShapeType.TRAPEZOID, dirW, dirH, rotation, controlColor, counterPred, counterSucc, bitA, msbA, bitD, msbD);
            }
        }

        public void ActivateReceive()
        {
            if (finished.GetCurrentValue())
                return;

            // Special case: Hexagon intersection beep
            if (sendHex.GetCurrentValue())
            {
                PinConfiguration pc = algo.GetCurrentPinConfiguration();
                success.SetValue(pc.ReceivedBeepOnPartitionSet(0));
                if (color.GetCurrentValue() && !success.GetCurrentValue())
                {
                    algo.SetMainColor(ColorData.Particle_Red);
                }
                sendHex.SetValue(false);
                finished.SetValue(true);
                return;
            }

            if (shapeType.GetCurrentValue() == ShapeType.PARALLELOGRAM)
            {
                parallelogram.ActivateReceive();
                if (parallelogram.IsFinished())
                {
                    finished.SetValue(true);
                    success.SetValue(parallelogram.Success());
                    valid.SetValue(parallelogram.IsRepresentative());
                }
            }
            else
            {
                mergeAlgo.ActivateReceive();
                if (mergeAlgo.IsFinished())
                {
                    if (shapeType.GetCurrentValue() == ShapeType.HEXAGON)
                    {
                        if (!mergeAlgo.Success())
                        {
                            // No solution: Terminate with failure
                            success.SetValue(false);
                            finished.SetValue(true);
                            return;
                        }
                        // Solution exists
                        if (hexHalfDone.GetCurrentValue())
                        {
                            // Have finished both halves
                            // Send beep from intersection of the two solution sets
                            valid.SetValue(valid.GetCurrentValue() && mergeAlgo.IsRepresentative());
                            if (color.GetCurrentValue())
                            {
                                if (valid.GetCurrentValue())
                                    algo.SetMainColor(ColorData.Particle_Green);
                                else
                                    algo.SetMainColor(ColorData.Particle_Black);
                            }
                            sendHex.SetValue(true);
                        }
                        else
                        {
                            // Finished first half, now continue with trapezoid
                            valid.SetValue(mergeAlgo.IsRepresentative());
                            hexHalfDone.SetValue(true);
                            int rot = rotation.GetValue();
                            mergeAlgo.Init(ShapeType.TRAPEZOID, directionW.GetValue(), directionH2.GetValue(), rot, color.GetValue(), directionPred.GetValue(), directionSucc.GetValue(), bitA.GetValue(), msbA.GetValue(), bitD2.GetValue(), msbD2.GetValue());
                        }
                    }
                    else
                    {
                        finished.SetValue(true);
                        success.SetValue(mergeAlgo.Success());
                        valid.SetValue(mergeAlgo.IsRepresentative());
                    }
                }
            }
        }

        public void SetupPC(PinConfiguration pc)
        {
            if (finished.GetCurrentValue())
                return;
            
            // Special case: Hexagon intersection beep
            if (sendHex.GetCurrentValue())
            {
                pc.SetToGlobal(0);
                return;
            }

            if (shapeType.GetCurrentValue() == ShapeType.PARALLELOGRAM)
                parallelogram.SetupPC(pc);
            else
                mergeAlgo.SetupPC(pc);
        }

        public void ActivateSend()
        {
            if (finished.GetCurrentValue())
                return;
            
            // Special case: Hexagon intersection beep
            if (sendHex.GetCurrentValue())
            {
                if (valid.GetCurrentValue())
                {
                    PinConfiguration pc = algo.GetPlannedPinConfiguration();
                    pc.SendBeepOnPartitionSet(0);
                }
                return;
            }

            if (shapeType.GetCurrentValue() == ShapeType.PARALLELOGRAM)
                parallelogram.ActivateSend();
            else
                mergeAlgo.ActivateSend();
        }

        /// <summary>
        /// Checks whether the procedure is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if all valid placements
        /// were found or ruled out.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether the procedure finished successfully, i.e.,
        /// there is a valid placement in the system.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// is finished and there is at least one valid placement.</returns>
        public bool Success()
        {
            return IsFinished() && success.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this amoebot is a representative of a
        /// valid placement after the procedure has finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// is finished and this amoebot was determined as a
        /// valid placement.</returns>
        public bool IsRepresentative()
        {
            return IsFinished() && valid.GetCurrentValue();
        }
    }

} // namespace AS2.Subroutines.ConvexShapeContainment
