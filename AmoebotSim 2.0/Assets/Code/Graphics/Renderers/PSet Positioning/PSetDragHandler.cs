using System.Collections;
using System.Collections.Generic;
using AS2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AS2.Visuals
{

    /// <summary>
    /// Handles the dragging of partition sets using the partition set drag tool.
    /// </summary>
    public class PSetDragHandler
    {

        // References
        private AmoebotSimulator sim;

        // State
        private bool drag_active = false;
        private Vector2 drag_originNodeWorldPos;
        private Vector2Int drag_originGridPos;
        private RendererCircuits_Instance.ParticleCircuitData drag_circuitData;
        private RendererCircuits_Instance.ParticleCircuitData.PSetInnerPinRef drag_innerPin;

        public PSetDragHandler(AmoebotSimulator sim)
        {
            this.sim = sim;
        }

        public void DragEvent_Ongoing(Vector2 originWorldPos, Vector2 curWorldPos)
        {
            // 1. Start Dragging
            if(drag_active == false)
            {
                // No drag active
                // Check if new drag can be started
                if (sim.uiHandler.initializationUI.IsOpen() == false && sim.running == false)
                {
                    // Sim paused in simulation mode
                    // Check if partition set is at the given position
                    drag_originNodeWorldPos = AmoebotFunctions.NearestHexFieldWorldPositionFromWorldPosition(originWorldPos);
                    drag_originGridPos = AmoebotFunctions.GetGridPositionFromWorldPosition(drag_originNodeWorldPos);
                    IParticleState particle;
                    sim.system.TryGetParticleAt(drag_originGridPos, out particle);
                    if(particle != null)
                    {
                        // There is a particle at the given position
                        // Check if there is also is a partition set

                        drag_circuitData = sim.renderSystem.rendererP.circuitAndBondRenderer.GetCurrentInstance().GetParticleCircuitData((ParticleGraphicsAdapterImpl)particle.GetGraphicsAdapter());
                        drag_innerPin = drag_circuitData.GetInnerPSetOrConnectorPinAtPosition(originWorldPos);
                        if(drag_innerPin.pinType != RendererCircuits_Instance.ParticleCircuitData.PSetInnerPinRef.PinType.None)
                        {
                            // We are dragging an inner pin
                            // Start drag
                            drag_active = true;
                        }
                    }

                }
            }
            // 2. PSet Position Updating
            if(drag_active)
            {
                // Drag active
                // Check if partition set position can be updated
                if (sim.uiHandler.initializationUI.IsOpen() == false && sim.running == false)
                {
                    // Sim paused in simulation mode
                    Vector2Int curGridPos = AmoebotFunctions.GetGridPositionFromWorldPosition(AmoebotFunctions.NearestHexFieldWorldPositionFromWorldPosition(curWorldPos));
                    if(curGridPos == drag_originGridPos || (drag_circuitData.snap.isExpanded && (curGridPos == drag_circuitData.snap.position1 || curGridPos == drag_circuitData.snap.position2)))
                    {
                        // Current position is at the same node(s) as the particle
                        // Update position
                        drag_circuitData.UpdatePSetOrConnectorPinPosition(drag_innerPin, curWorldPos);
                    }
                }
                else
                {
                    // Either the init mode has been opened or the sim is running again
                    // Stop drag event
                    drag_active = false;
                }
            }
        }

        public void DragEvent_Finished(Vector2 originWorldPos, Vector2 finalWorldPos)
        {
            DragEvent_Ongoing(originWorldPos, finalWorldPos);
            drag_active = false;
        }

        public void AbortDrag()
        {
            drag_active = false;
        }

        /// <summary>
        /// Call this each frame with the current mouse world position and field if the partition set move tool is active.
        /// </summary>
        /// <param name="curMousePosition"></param>
        public void Update(Vector2 curMouseWorldPosition, Vector2Int curMouseWorldField)
        {

        }

    }

}