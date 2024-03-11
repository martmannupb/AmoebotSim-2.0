using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.ETT;
using AS2.Subroutines.PASC;
using AS2.Subroutines.SingleSourceSP;

namespace AS2.Subroutines.SPFPropagation
{

    /// <summary>
    /// Implements the propagation primitive that propagates
    /// a shortest path forest from one region into an adjacent
    /// region through the portal that separates them. It is possible
    /// to run this procedure on both sides of a portal (or even 4
    /// sides of a marker) if the source region is the portal itself.
    /// However, only the first phase can run simultaneously. For the
    /// second phase, a coloring of the portal graph that colors adjacent
    /// regions differently is required, such that only regions of the
    /// same color run the procedure at the same time.
    /// </summary>

    // Algorithm plan:
    // 1. Cover the region visible by the portal
    //  1.1. Send activation beeps
    //      - Establish circuits along two axes and let the portal amoebots beep on them
    //      - All amoebots in the visible region will receive at least one beep
    //      - Amoebots that receive only one beep already set their parent direction
    //  1.2. Send reply beeps
    //      - Let the amoebots receiving two axis beeps reply on the same circuits
    //      - Also let them beep on a global/regional circuit to determine whether the next phase is necessary
    //  1.3. Use PASC to finish the visible region
    //      - Only run this if there was a beep on the global circuit
    //      - Let all non-source amoebots in the "upper" region beep in the direction of their parent
    //      - Then establish PASC circuits along the shortest path in the upper region
    //      - Establish axis circuits in the "lower" region
    //      - Run PASC and let the portal amoebots forward their distances to the amoebots in the lower region
    //          - (Limit this to the axes where it is necessary)
    //      - The amoebots that received two beeps in the lower region compare the two PASC results and choose the smaller one for their parent direction
    //      - Use a regional circuit for synchronization and terminate when all PASC trees are finished
    // (Allow the calling algorithm to wait here)
    // 2. Cover the non-visible regions
    //  2.1. Establish the regions and start points
    //      - Let amoebots that are not in the visible region beep on a global circuit
    //          - Terminate already if there is no beep
    //      - Let all amoebots in the visible region send a beep to the neighbors that might be in a non-visible region
    //          - The amoebots in the non-visible region can now determine their start points
    //  2.2. Use 1-SPF to solve the non-visible regions
    //      - Run the single-source subroutine on each non-visible region independently
    //      - Use a regional circuit for synchronization
    //      - Terminate as soon as all regions are finished


    // Round plan:

    // 1.1. Activation beeps

    // Round 0:
    //  Send:
    //  - Establish axis circuits in the lower region and let portal amoebots beep
    //  Receive:
    //  - Amoebots in the lower region listen for axis beeps
    //  - If only one beep: Set parent direction
    //  - If no beep: We are in a non-visible region (store this info)
    //  - If two beeps: We are in the visible region but have two options
    //  - Go to round 1

    // 1.2. Send reply beeps

    // Round 1:
    //  Send:
    //  - Establish a regional circuit
    //  - Amoebots in the visible region that received two beeps send reply on the regional circuit
    //  Receive:
    //  - If no beep on the regional circuit: Go to second phase
    //  - Else:
    //      - Find the amoebots that have to participate in the next phase

    // 1.3. Use PASC to finish the visible region

    // Round 2:
    //  Send:
    //  - Establish singleton pin configurations and let all non-source amoebots in the "upper" region and on the portal beep towards their parent
    //  Receive:
    //  - Initialize PASC circuits in the upper region

    // Round 3:
    //  Send:
    //  - Setup PASC circuit and axis circuits and let the portal amoebots connect their secondary partition sets to the axis circuits
    //  - Send PASC beep
    //  Receive:
    //  - Receive PASC and axis beeps
    //  - Update comparison result
    //  - Go to round 4

    // Round 4:
    //  Send:
    //  - Setup regional circuit and beep if we became passive
    //  Receive:
    //  - If no beep was received:
    //      - The PASC phase is finished
    //      - Use the comparison result to set the parent direction
    //      - Go to round 5
    //  - Else:
    //      - Go back to round 3

    // 2. Cover the non-visible regions

    // 2.1. Establish the regions and start points

    // Round 5:
    //  Send:
    //  - Setup a regional circuit
    //  - Let amoebots outside the visible region beep on the regional circuit
    //  - Also let amoebots in the visible region beep in all directions
    //  Receive:
    //  - If there is no beep on the regional circuit: Terminate
    //  - Else:
    //      - Let non-visible amoebots identify their visible neighbors
    //      - Also identify the start points for the 1-SPF subroutine and initialize it

    // 2.2. Use 1-SPF to solve the non-visible regions

    // Round 6:
    //  Send:
    //  - Setup 1-SPF circuit and beep
    //  Receive:
    //  - Receive 1-SPF beep
    //  - Go to round 7

    // Round 7:
    //  Send:
    //  - Setup regional circuit and beep if our 1-SPF is not finished yet
    //  Receive:
    //  - Receive beep on regional circuit
    //  - If no beep:
    //      - Set parent direction and terminate
    //  - Else:
    //      - Go back to round 6



    public class SubSPFPropagation : Subroutine
    {
        enum ComparisonResult
        {
            EQUAL = 0,
            GREATER = 1,
            LESS = 2
        }

        //     30         29      28         27        26                   25                    24              23       22             21            20                    1918            1715           1412           119          8    3        2 0
        // x   x          x       x          x         x                    x                     x               x        x              x             x                      xx             xxx            xxx            xxx          xxxxxx        xxx
        //     Finished   Color   Two axes   Visible   Region below other   Region below source   Source region   Source   Other portal   Main portal   Source region portal   Instance idx   Parent dir 2   Parent dir 1   Portal dir   Ignore dirs   Round
        ParticleAttribute<int> state1;

        //                                 76           5    0
        // xxxx xxxx xxxx xxxx xxxx xxxx   xx           xxxxxx
        //                                 Comparison   Child directions
        ParticleAttribute<int> state2;

        BinAttributeInt round;                          // Round counter (0-7)
        BinAttributeBitField ignoreDirections;          // In which directions we should ignore neighbors
        BinAttributeDirection portalDir;                // The main portal direction (always points "right", the propagation always heads "down")
        BinAttributeDirection parentDir1;               // Our original parent direction (in the source region)
        BinAttributeDirection parentDir2;               // Our new parent direction (both regions)
        BinAttributeInt instanceIndex;                  // The index of this instance (used to obtain unique partition set IDs, 0-3)
        BinAttributeBool sourceRegionIsPortal;          // Whether the source region is just the portal
        BinAttributeBool onMainPortal;                  // Whether we are on the portal separating the two regions
        BinAttributeBool onOtherPortal;                 // Whether we are on a portal ending one of the two regions
        BinAttributeBool isSource;                      // Whether we are a source
        BinAttributeBool inSourceRegion;                // Whether we are in the source region
        BinAttributeBool regionBelowSourcePortal;       // Whether the target region is "below" the source region's boundary portal
                                                        // So the source region's boundary portal knows how to restrict its pins
                                                        // This is only necessary if the source region is not the main portal itself
        BinAttributeBool regionBelowOtherPortal;        // Whether the target region is "below" the target region's boundary portal
                                                        // Same as above for the target region, but this is always applicable
        BinAttributeBool inVisibleRegion;               // Whether we are in the visible region of the portal
        BinAttributeBool onTwoAxes;                     // Whether we received beeps from two source portal amoebots
        BinAttributeBool controlColor;                  // Whether we should control the amoebot color
        BinAttributeBool finished;                      // Whether we are finished

        BinAttributeBitField childDirections;           // Directions in which we have children
        BinAttributeEnum<ComparisonResult> comparison;  // Result of the PASC comparison

        SubPASC2 pasc;
        Sub1SPF singleSourceSPF;

        public SubSPFPropagation(Particle p, SubPASC2 pasc = null, SubETT ett = null) : base(p)
        {
            state1 = algo.CreateAttributeInt(FindValidAttributeName("[Prop] State 1"), 0);
            state2 = algo.CreateAttributeInt(FindValidAttributeName("[Prop] State 2"), 0);

            round = new BinAttributeInt(state1, 0, 3);
            ignoreDirections = new BinAttributeBitField(state1, 3, 6);
            portalDir = new BinAttributeDirection(state1, 9);
            parentDir1 = new BinAttributeDirection(state1, 12);
            parentDir2 = new BinAttributeDirection(state1, 15);
            instanceIndex = new BinAttributeInt(state1, 18, 2);
            sourceRegionIsPortal = new BinAttributeBool(state1, 20);
            onMainPortal = new BinAttributeBool(state1, 21);
            onOtherPortal = new BinAttributeBool(state1, 22);
            isSource = new BinAttributeBool(state1, 23);
            inSourceRegion = new BinAttributeBool(state1, 24);
            regionBelowSourcePortal = new BinAttributeBool(state1, 25);
            regionBelowOtherPortal = new BinAttributeBool(state1, 26);
            inVisibleRegion = new BinAttributeBool(state1, 27);
            onTwoAxes = new BinAttributeBool(state1, 28);
            controlColor = new BinAttributeBool(state1, 29);
            finished = new BinAttributeBool(state1, 30);

            childDirections = new BinAttributeBitField(state2, 0, 6);
            comparison = new BinAttributeEnum<ComparisonResult>(state2, 6, 2);

            if (pasc is null)
                this.pasc = new SubPASC2(p);
            else
                this.pasc = pasc;
            singleSourceSPF = new Sub1SPF(p, ett);
        }

        public void Init(Direction portalDir, int instanceIndex, bool onMainPortal, bool onOtherPortal, bool isSource, bool regionBelowSourcePortal, bool regionBelowOtherPortal, bool sourceRegionIsPortal,
            bool controlColor = false, List<Direction> ignoreDirections = null, bool inSourceRegion = false, Direction parentDir1 = Direction.NONE)
        {
            state1.SetValue(0);
            state2.SetValue(0);
            this.portalDir.SetValue(portalDir);
            this.instanceIndex.SetValue(instanceIndex);
            this.onMainPortal.SetValue(onMainPortal);
            this.onOtherPortal.SetValue(onOtherPortal);
            this.isSource.SetValue(isSource);
            this.regionBelowSourcePortal.SetValue(regionBelowSourcePortal);
            this.regionBelowOtherPortal.SetValue(regionBelowOtherPortal);
            this.sourceRegionIsPortal.SetValue(sourceRegionIsPortal);
            this.controlColor.SetValue(controlColor);
            if (!(ignoreDirections is null))
            {
                foreach (Direction d in ignoreDirections)
                    this.ignoreDirections.SetValue(d.ToInt(), true);
            }
            // Also add non-existent neighbors to ignored directions
            for (int i = 0; i < 6; i++)
            {
                if (!this.ignoreDirections.GetCurrentValue(i) && !algo.HasNeighborAt(DirectionHelpers.Cardinal(i)))
                    this.ignoreDirections.SetValue(i, true);
            }
            this.inSourceRegion.SetValue(inSourceRegion);
            this.parentDir1.SetValue(parentDir1);
        }

        public void ActivateReceive()
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        // Lower region amoebots listen for the axis beeps
                        if (!inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            Direction dirUp1 = DirUp1();
                            Direction dirUp2 = DirUp2();
                            bool beep1 = !ignoreDirections.GetCurrentValue(dirUp1.ToInt()) && pc.GetPinAt(dirUp1, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep();
                            bool beep2 = !ignoreDirections.GetCurrentValue(dirUp2.ToInt()) && pc.GetPinAt(dirUp2, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep();

                            // We are in the visible region if we received at least one beep
                            if (beep1 || beep2)
                            {
                                inVisibleRegion.SetValue(true);
                                // If we received both beeps, we have to remember this
                                if (beep1 && beep2)
                                    onTwoAxes.SetValue(true);
                                else
                                {
                                    // Otherwise, set parent direction based on which beep we got
                                    if (beep1)
                                        parentDir2.SetValue(dirUp1);
                                    else
                                        parentDir2.SetValue(dirUp2);
                                }
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 1:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Go to second phase immediately if there was no beep on the regional circuit
                        if (!pc.ReceivedBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8))
                        {
                            round.SetValue(5);
                        }
                        else
                        {
                            // Move on to the PASC procedure
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 2:
                    {
                        // Amoebots in the source region and on the portal receive parent beep and setup PASC
                        bool portal = onMainPortal.GetCurrentValue();
                        bool source = isSource.GetCurrentValue();
                        if (inSourceRegion.GetCurrentValue() || portal)
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            Direction pDir = portalDir.GetCurrentValue();
                            bool down = regionBelowSourcePortal.GetCurrentValue();

                            // Find directions from where we received beeps
                            List<Direction> childDirs = new List<Direction>();
                            Direction parent = parentDir1.GetCurrentValue();
                            for (int d = 0; d < 6; d++)
                            {
                                Direction dir = DirectionHelpers.Cardinal(d);
                                if (ignoreDirections.GetCurrentValue(d) || dir == parent)
                                    continue;

                                int pin = algo.PinsPerEdge - 1;
                                // Portal amoebots have to use other pins
                                if (portal && sourceRegionIsPortal.GetCurrentValue() || onOtherPortal.GetCurrentValue())
                                {
                                    if (dir == pDir && down || dir == pDir.Opposite() && !down)
                                        pin = 0;
                                }
                                if (pc.GetPinAt(dir, pin).PartitionSet.ReceivedBeep())
                                {
                                    childDirections.SetValue(d, true);
                                    childDirs.Add(dir);
                                }
                            }

                            // If we are not a source, add the parent direction
                            List<Direction> parentDirs = null;
                            if (!source)
                            {
                                if (!ignoreDirections.GetCurrentValue(parent.ToInt()))
                                    parentDirs = new List<Direction>() { parent };
                            }

                            // Initialize PASC accordingly
                            int idx = instanceIndex.GetCurrentValue();
                            pasc.Init(parentDirs, childDirs, 0, 1, 2 * idx, 2 * idx + 1, source);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 3:
                    {
                        // Receive PASC beep
                        if (inSourceRegion.GetCurrentValue() || onMainPortal.GetCurrentValue())
                        {
                            pasc.ActivateReceive();
                        }
                        // Receive axis beeps and update comparison result
                        if (onTwoAxes.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            Direction dirUp1 = DirUp1();
                            Direction dirUp2 = DirUp2();

                            bool beep1 = !ignoreDirections.GetCurrentValue(dirUp1.ToInt()) && pc.GetPinAt(dirUp1, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep();
                            bool beep2 = !ignoreDirections.GetCurrentValue(dirUp2.ToInt()) && pc.GetPinAt(dirUp2, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep();

                            if (beep1 && !beep2)
                                comparison.SetValue(ComparisonResult.GREATER);
                            else if (!beep1 && beep2)
                                comparison.SetValue(ComparisonResult.LESS);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 4:
                    {
                        // Receive beep on regional circuit
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8))
                        {
                            // No beep: Finished with PASC
                            // Use comparison result to set the new parent direction
                            if (onTwoAxes.GetCurrentValue())
                            {
                                if (comparison.GetCurrentValue() != ComparisonResult.GREATER)
                                    parentDir2.SetValue(DirUp1());
                                else
                                    parentDir2.SetValue(DirUp2());
                            }
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Repeat
                            round.SetValue(r - 1);
                        }
                    }
                    break;
                case 5:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Terminate if there was no beep on the regional circuit
                        if (!pc.ReceivedBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8))
                        {
                            finished.SetValue(true);
                            break;
                        }
                        // Let non-visible amoebots identify their visible neighbors and initialize the 1-SPF subroutine
                        if (!inVisibleRegion.GetCurrentValue() && !inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue())
                        {
                            bool[] visibleNbrs = new bool[6];
                            bool[] nonVisibleNbrs = new bool[6];
                            bool hasVisibleNbr = false;
                            for (int i = 0; i < 6; i++)
                            {
                                if (!ignoreDirections.GetCurrentValue(i))
                                {
                                    if (pc.GetPinAt(DirectionHelpers.Cardinal(i), algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                    {
                                        visibleNbrs[i] = true;
                                        hasVisibleNbr = true;
                                    }
                                    else
                                        nonVisibleNbrs[i] = true;
                                }
                            }

                            // Find out whether we are the source of this non-visible region
                            bool source = false;
                            if (hasVisibleNbr)
                            {
                                // Only the 4 directions closer to the main portal are of interest
                                Direction up1 = DirUp1();
                                Direction up2 = DirUp2();
                                Direction side1 = up1.Rotate60(1);
                                Direction side2 = up2.Rotate60(-1);
                                int up1i = up1.ToInt();
                                int up2i = up2.ToInt();
                                int side1i = side1.ToInt();
                                int side2i = side2.ToInt();
                                // If one of the two "up" neighbors is visible and the other one is not part of the invisible region,
                                // we are the source and can choose the visible neighbor as parent
                                if (visibleNbrs[up1i] || visibleNbrs[up2i])
                                {
                                    if (visibleNbrs[up1i] && !nonVisibleNbrs[up2i])
                                    {
                                        source = true;
                                        parentDir2.SetValue(up1);
                                    }
                                    else if (visibleNbrs[up2i] && !nonVisibleNbrs[up1i])
                                    {
                                        source = true;
                                        parentDir2.SetValue(up2);
                                    }
                                }
                                // Else: If one of the two "side" neighbors is visible and the corresponding "top" neighbor on that side
                                // is not part of the same invisible region, we are the source and choose that visible neighbor
                                else if (visibleNbrs[side1i] || visibleNbrs[side2i])
                                {
                                    if (visibleNbrs[side1i] && !nonVisibleNbrs[up1i])
                                    {
                                        source = true;
                                        parentDir2.SetValue(side1);
                                    }
                                    else if (visibleNbrs[side2i] && !nonVisibleNbrs[up2i])
                                    {
                                        source = true;
                                        parentDir2.SetValue(side2);
                                    }
                                }
                            }
                            // Now collect the directions we have to ignore
                            List<Direction> ignoreDirs = new List<Direction>();
                            for (int i = 0; i < 6; i++)
                            {
                                if (ignoreDirections.GetCurrentValue(i) || visibleNbrs[i])
                                    ignoreDirs.Add(DirectionHelpers.Cardinal(i));
                            }

                            // Finally, initialize the subroutine
                            singleSourceSPF.Init(source, !source, controlColor.GetCurrentValue(), ignoreDirs);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 6:
                    {
                        if (!inVisibleRegion.GetCurrentValue() && !inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue())
                            singleSourceSPF.ActivateReceive();
                        round.SetValue(r + 1);
                    }
                    break;
                case 7:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // No beep on regional circuit: Subroutine has finished
                        if (!pc.ReceivedBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8))
                        {
                            // Set parent direction and terminate
                            if (!inVisibleRegion.GetCurrentValue() && !inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue() && parentDir2.GetCurrentValue() == Direction.NONE)
                                parentDir2.SetValue(singleSourceSPF.Parent());
                            finished.SetValue(true);
                        }
                        // Else: Continue running
                        else
                        {
                            round.SetValue(r - 1);
                        }
                    }
                    break;
            }
            SetColor();
        }

        public void SetupPC(PinConfiguration pc)
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        // Establish axis circuits in the lower region
                        if (!inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue())
                            SetupAxisCircuit(pc);
                    }
                    break;
                case 1:
                case 4:
                case 5:
                case 7:
                    {
                        // Setup a regional circuit (using the two center pins)
                        SetupRegionalCircuit(pc);
                    }
                    break;
                case 2:
                    {
                        // Do nothing (require singleton PC as input!)
                    }
                    break;
                case 3:
                    {
                        if (inSourceRegion.GetCurrentValue() || onMainPortal.GetCurrentValue())
                        {
                            // Amoebots on a portal may have to invert some directions
                            List<Direction> invertDirs = new List<Direction>();
                            bool mainPortal = onMainPortal.GetCurrentValue();
                            bool otherPortal = onOtherPortal.GetCurrentValue();
                            if (mainPortal && sourceRegionIsPortal.GetCurrentValue() || otherPortal)
                            {
                                Direction pDir = portalDir.GetCurrentValue();
                                bool down = regionBelowSourcePortal.GetCurrentValue();
                                bool source = isSource.GetCurrentValue();
                                foreach (Direction dir in new Direction[] { pDir, pDir.Opposite() })
                                {
                                    if (!source && dir == parentDir1.GetCurrentValue())
                                    {
                                        if (dir == pDir && down || dir == pDir.Opposite() && !down)
                                            invertDirs.Add(dir);
                                    }
                                    else if (childDirections.GetCurrentValue(dir.ToInt()))
                                    {
                                        if (dir == pDir && !down || dir == pDir.Opposite() && down)
                                            invertDirs.Add(dir);
                                    }

                                }
                            }

                            pasc.SetupPC(pc, invertDirs);

                            // Portal amoebots have to connect their secondary partition sets downwards
                            if (mainPortal || otherPortal)
                            {
                                Direction dirDown1 = DirUp1().Opposite();
                                Direction dirDown2 = DirUp2().Opposite();
                                int idx = 2 * instanceIndex.GetCurrentValue() + 1;
                                if (!ignoreDirections.GetCurrentValue(dirDown1.ToInt()))
                                    pc.GetPartitionSet(idx).AddPin(pc.GetPinAt(dirDown1, 0).Id);
                                if (!ignoreDirections.GetCurrentValue(dirDown2.ToInt()))
                                    pc.GetPartitionSet(idx).AddPin(pc.GetPinAt(dirDown2, 0).Id);
                            }
                        }
                        // Setup axis circuits to forward the PASC beeps
                        if (onTwoAxes.GetCurrentValue())
                        {
                            SetupAxisCircuit(pc);
                        }
                    }
                    break;
                case 6:
                    {
                        if (!inVisibleRegion.GetCurrentValue() && !inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue())
                            singleSourceSPF.SetupPC(pc);
                    }
                    break;
            }
            SetColor();
        }

        public void ActivateSend()
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        // Portal amoebots send beep on the axis circuits
                        if (onMainPortal.GetCurrentValue())
                        {
                            Direction dirDown1 = DirUp1().Opposite();
                            Direction dirDown2 = DirUp2().Opposite();
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            if (!ignoreDirections.GetCurrentValue(dirDown1.ToInt()))
                                pc.GetPinAt(dirDown1, 0).PartitionSet.SendBeep();
                            if (!ignoreDirections.GetCurrentValue(dirDown2.ToInt()))
                                pc.GetPinAt(dirDown2, 0).PartitionSet.SendBeep();
                        }
                    }
                    break;
                case 1:
                    {
                        // Amoebots that received two beeps now reply on the regional circuit
                        if (onTwoAxes.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8);
                        }
                    }
                    break;
                case 2:
                    {
                        // All non-source amoebots in the source region and the main portal beep towards their parent
                        bool portal = onMainPortal.GetCurrentValue();
                        if ((inSourceRegion.GetCurrentValue() || portal) && !isSource.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            Direction d = parentDir1.GetCurrentValue();
                            if (ignoreDirections.GetCurrentValue(d.ToInt()))
                                break;  // This should never be the case

                            int pin = 0;
                            // Portal amoebots have to use other pins
                            if (portal && sourceRegionIsPortal.GetCurrentValue() || onOtherPortal.GetCurrentValue())
                            {
                                Direction pDir = portalDir.GetCurrentValue();
                                bool down = regionBelowSourcePortal.GetCurrentValue();
                                if (d == pDir && !down || d == pDir.Opposite() && down)
                                    pin = algo.PinsPerEdge - 1;
                            }
                            pc.GetPinAt(d, pin).PartitionSet.SendBeep();
                        }
                    }
                    break;
                case 3:
                    {
                        // Send PASC beep
                        if (inSourceRegion.GetCurrentValue() || onMainPortal.GetCurrentValue())
                        {
                            pasc.ActivateSend();
                        }
                    }
                    break;
                case 4:
                    {
                        // PASC participants beep if they became passive
                        if ((inSourceRegion.GetCurrentValue() || onMainPortal.GetCurrentValue()) && pasc.BecamePassive())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8);
                        }
                    }
                    break;
                case 5:
                    {
                        // Amoebots outside the visible region beep on the regional circuit
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        if (!inVisibleRegion.GetCurrentValue() && !inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue())
                        {
                            pc.SendBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8);
                        }
                        // Amoebots in the visible region beep in all directions with neighbors
                        else if (inVisibleRegion.GetCurrentValue())
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                if (!ignoreDirections.GetCurrentValue(i))
                                    pc.GetPinAt(DirectionHelpers.Cardinal(i), 0).PartitionSet.SendBeep();
                            }
                        }
                    }
                    break;
                case 6:
                    {
                        if (!inVisibleRegion.GetCurrentValue() && !inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue())
                            singleSourceSPF.ActivateSend();
                    }
                    break;
                case 7:
                    {
                        // Beep on regional circuit if 1-SPF is not finished yet
                        if (!inVisibleRegion.GetCurrentValue() && !inSourceRegion.GetCurrentValue() && !onMainPortal.GetCurrentValue() && !singleSourceSPF.IsFinished())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(instanceIndex.GetCurrentValue() + 8);
                        }
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Checks whether the first phase of the propagation routine
        /// (handling the visible region) is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if we are finished with the
        /// first phase of the propagation routine.</returns>
        public bool IsFirstPhaseFinished()
        {
            return round.GetCurrentValue() > 4;
        }

        /// <summary>
        /// Checks whether the propagation routine is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if we are finished with
        /// both phases of the propagation routine.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Gets the parent direction produced by the propagation procedure.
        /// </summary>
        /// <returns>The direction of this amoebot's parent after
        /// the propagation.</returns>
        public Direction Parent()
        {
            return IsFinished() ? parentDir2.GetCurrentValue() : Direction.NONE;
        }

        private void SetColor()
        {
            // Let subroutine control the color in the second phase
            if (!controlColor.GetCurrentValue() || round.GetCurrentValue() >= 5)
                return;

            // Don't set color for portals and sources because they might differ between regions
            //if (isSource.GetCurrentValue())
            //    algo.SetMainColor(ColorData.Particle_Red);
            //else if (onPortal.GetCurrentValue())
            //    algo.SetMainColor(ColorData.Particle_Orange);
            if (isSource.GetCurrentValue() || onMainPortal.GetCurrentValue())
                return;

            if (onTwoAxes.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Green);
            //else
            //    algo.SetMainColor(Color.gray);
            // TODO
        }

        /// <summary>
        /// Sets up two axis circuits for the axes that do
        /// not contain the portal direction. Will not connect
        /// pins in ignored directions. The partition set IDs are
        /// 2i and 2i + 1, where i is our instance index.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="split1">Whether the connection on the "left" axis
        /// circuit should be split.</param>
        /// <param name="split2">Whether the connection on the "right" axis
        /// circuit should be split.</param>
        private void SetupAxisCircuit(PinConfiguration pc, bool split1 = false, bool split2 = false)
        {
            if (split1 && split2)
                return;
            
            Direction dirUp1 = DirUp1();
            Direction dirUp2 = DirUp2();
            Direction dirDown1 = dirUp1.Opposite();
            Direction dirDown2 = dirUp2.Opposite();

            int idx = instanceIndex.GetCurrentValue();
            if (!split1 && !ignoreDirections.GetCurrentValue(dirUp1.ToInt()) && !ignoreDirections.GetCurrentValue(dirDown1.ToInt()))
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(dirUp1, algo.PinsPerEdge - 1).Id, pc.GetPinAt(dirDown1, 0).Id }, 2 * idx);
                pc.SetPartitionSetPosition(2 * idx, new Vector2((dirUp1.ToInt() + 1.5f) * 60, 0.5f));
            }
            if (!split2 && !ignoreDirections.GetCurrentValue(dirUp2.ToInt()) && !ignoreDirections.GetCurrentValue(dirDown2.ToInt()))
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(dirUp2, algo.PinsPerEdge - 1).Id, pc.GetPinAt(dirDown2, 0).Id }, 2 * idx + 1);
                pc.SetPartitionSetPosition(2 * idx + 1, new Vector2((dirUp2.ToInt() + 1.5f) * 60, 0.5f));
            }
        }

        /// <summary>
        /// Sets up a circuit that connects the entire region. The partition set
        /// ID is the instance index + 8. This might not be setup correctly if
        /// the amoebot has no neighbors in this region. The circuit uses the
        /// middle 2 pins for amoebots outside a portal and the middle pin closer
        /// to the region for amoebots on a bounding portal.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void SetupRegionalCircuit(PinConfiguration pc)
        {
            List<int> pins = new List<int>();
            Direction pDir = portalDir.GetCurrentValue();
            // Have to be careful if we are on a portal that bounds a region
            bool sourceRegion = inSourceRegion.GetCurrentValue();
            bool restrictPins = onOtherPortal.GetCurrentValue() || sourceRegionIsPortal.GetCurrentValue() && onMainPortal.GetCurrentValue();
            bool restrictUpwards = restrictPins && (sourceRegion && regionBelowSourcePortal.GetCurrentValue() || !sourceRegion && regionBelowOtherPortal.GetCurrentValue());
            for (int d = 0; d < 6; d++)
            {
                if (ignoreDirections.GetCurrentValue(d))
                    continue;
                Direction dir = DirectionHelpers.Cardinal(d);
                if (restrictPins && dir == pDir)
                {
                    // Restrict the "right" pin
                    pins.Add(pc.GetPinAt(dir, restrictUpwards ? 1 : algo.PinsPerEdge - 2).Id);
                }
                else if (restrictPins && dir == pDir.Opposite())
                {
                    // Restrict the "left" pin
                    pins.Add(pc.GetPinAt(dir, restrictUpwards ? algo.PinsPerEdge - 2 : 1).Id);
                }
                else
                {
                    // Use both pins
                    pins.Add(pc.GetPinAt(dir, 1).Id);
                    pins.Add(pc.GetPinAt(dir, algo.PinsPerEdge - 2).Id);
                }
            }
            if (pins.Count > 0)
                pc.MakePartitionSet(pins.ToArray(), instanceIndex.GetCurrentValue() + 8);
        }

        /// <summary>
        /// Helper computing the "left up" direction relative to the main portal.
        /// </summary>
        /// <returns>The "left" direction that points towards the main
        /// portal from the target region.</returns>
        private Direction DirUp1()
        {
            return portalDir.GetCurrentValue().Rotate60(2);
        }

        /// <summary>
        /// Helper computing the "right up" direction relative to the main portal.
        /// </summary>
        /// <returns>The "right" direction that points towards the main
        /// portal from the target region.</returns>
        private Direction DirUp2()
        {
            return portalDir.GetCurrentValue().Rotate60(1);
        }
    }

} // namespace AS2.Subroutines.SPFPropagation
