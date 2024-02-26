using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;

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
    //  - Establish axis circuits and a regional circuit
    //  - Amoebots in the visible region that received two beeps send reply on axis and regional circuit
    //      - Even split the axis circuits to minimize the number of amoebots participating in the next phase
    //  Receive:
    //  - If no beep on the regional circuit: Terminate
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
    //  - Let amoebots outside the visible region beep on the global circuit
    //  - Also let amoebots in the visible region beep in the direction of neighbors that might not be visible
    //  Receive:
    //  - If there is no beep on the global circuit: Terminate
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
        //     30         29      28 27            26         25        24            23              22       21          20                    1918            1715           1412           119          8    3        2 0
        // x   x          x       x  x             x          x         x             x               x        x           x                      xx             xxx            xxx            xxx          xxxxxx        xxx
        //     Finished   Color   PASC axis 1, 2   Two axes   Visible   Region down   Source region   Source   On portal   Source region portal   Instance idx   Parent dir 2   Parent dir 1   Portal dir   Ignore dirs   Round
        ParticleAttribute<int> state;

        BinAttributeInt round;                          // Round counter (0-7)
        BinAttributeBitField ignoreDirections;          // In which directions we should ignore neighbors
        BinAttributeDirection portalDir;                // The main portal direction
        BinAttributeDirection parentDir1;               // Our original parent direction (in the source region)
        BinAttributeDirection parentDir2;               // Our new parent direction (both regions)
        BinAttributeInt instanceIndex;                  // The index of this instance (used to obtain unique partition set IDs, 0-3)
        BinAttributeBool sourceRegionIsPortal;          // Whether the source region is just the portal
        BinAttributeBool onPortal;                      // Whether we are on the portal separating the two regions
        BinAttributeBool isSource;                      // Whether we are a source
        BinAttributeBool inSourceRegion;                // Whether we are in the source region
        BinAttributeBool regionPointsDown;              // Whether the target region points "down" from the portal
        BinAttributeBool inVisibleRegion;               // Whether we are in the visible region of the portal
        BinAttributeBool onTwoAxes;                     // Whether we received beeps from two source portal amoebots
        BinAttributeBitField onPascAxis;                // Whether we are on an axis that should forward PASC beeps (two possible axes)
        BinAttributeBool controlColor;                  // Whether we should control the amoebot color
        BinAttributeBool finished;                      // Whether we are finished

        public SubSPFPropagation(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Prop] State"), 0);

            round = new BinAttributeInt(state, 0, 3);
            ignoreDirections = new BinAttributeBitField(state, 3, 6);
            portalDir = new BinAttributeDirection(state, 9);
            parentDir1 = new BinAttributeDirection(state, 12);
            parentDir2 = new BinAttributeDirection(state, 15);
            instanceIndex = new BinAttributeInt(state, 18, 2);
            sourceRegionIsPortal = new BinAttributeBool(state, 20);
            onPortal = new BinAttributeBool(state, 21);
            isSource = new BinAttributeBool(state, 22);
            inSourceRegion = new BinAttributeBool(state, 23);
            regionPointsDown = new BinAttributeBool(state, 24);
            inVisibleRegion = new BinAttributeBool(state, 25);
            onTwoAxes = new BinAttributeBool(state, 26);
            onPascAxis = new BinAttributeBitField(state, 27, 2);
            controlColor = new BinAttributeBool(state, 29);
            finished = new BinAttributeBool(state, 30);
        }

        public void Init(Direction portalDir, int instanceIndex, bool onPortal, bool isSource, bool regionPointsDown, bool sourceRegionIsPortal,
            bool controlColor = false, List<Direction> ignoreDirections = null, bool inSourceRegion = false, Direction parentDir1 = Direction.NONE)
        {
            state.SetValue(0);
            this.portalDir.SetValue(portalDir);
            this.instanceIndex.SetValue(instanceIndex);
            this.onPortal.SetValue(onPortal);
            this.isSource.SetValue(isSource);
            this.regionPointsDown.SetValue(regionPointsDown);
            this.sourceRegionIsPortal.SetValue(sourceRegionIsPortal);
            this.controlColor.SetValue(controlColor);
            if (!(ignoreDirections is null))
            {
                foreach (Direction d in ignoreDirections)
                    this.ignoreDirections.SetValue(d.ToInt(), true);
            }
            this.inSourceRegion.SetValue(inSourceRegion);
            this.parentDir1.SetValue(parentDir1);
        }

        public void ActivateReceive()
        {
            int r = round.GetCurrentValue();
            switch (r)
            {

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
                        if (!inSourceRegion.GetCurrentValue() && !onPortal.GetCurrentValue())
                            SetupAxisCircuit(pc);
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
                        if (onPortal.GetCurrentValue())
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
            }
            SetColor();
        }

        private void SetColor()
        {
            // TODO
        }

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
        /// Helper computing the "left up" direction in the
        /// target region pointing towards the portal.
        /// </summary>
        /// <returns>The "left" direction that points towards the portal
        /// from the target region.</returns>
        private Direction DirUp1()
        {
            if (regionPointsDown.GetCurrentValue())
                return portalDir.GetCurrentValue().Rotate60(2);
            else
                return portalDir.GetCurrentValue().Rotate60(-1);
        }

        /// <summary>
        /// Helper computing the "right up" direction in the
        /// target region pointing towards the portal.
        /// </summary>
        /// <returns>The "right" direction that points towards the portal
        /// from the target region.</returns>
        private Direction DirUp2()
        {
            if (regionPointsDown.GetCurrentValue())
                return portalDir.GetCurrentValue().Rotate60(1);
            else
                return portalDir.GetCurrentValue().Rotate60(-2);
        }
    }

} // namespace AS2.Subroutines.SPFPropagation
