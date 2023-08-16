using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using AS2.Visuals;
using AS2.UI;
using UnityEngine;
using System;
using TMPro;

namespace AS2
{

    /// <summary>
    /// The main class of the entire simulator.
    /// Manages the particle system and the render system and
    /// provides the high-level interface for controlling the simulation.
    /// This class triggers the main render call in each frame and the
    /// round simulation in each fixed update.
    /// </summary>
    public class AmoebotSimulator : MonoBehaviour
    {

        /// <summary>
        /// The singleton instance of the class.
        /// </summary>
        public static AmoebotSimulator instance;

        // System Data
        /// <summary>
        /// The particle system handling the simulation.
        /// </summary>
        public AS2.Sim.ParticleSystem system;
        /// <summary>
        /// The render system handling the visualization.
        /// </summary>
        public RenderSystem renderSystem;

        // System State
        /// <summary>
        /// Whether the simulation is currently running.
        /// </summary>
        public bool running = true;

        // UI
        /// <summary>
        /// Reference to the UI handler root object.
        /// </summary>
        public UIHandler uiHandler;

        public AmoebotSimulator()
        {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            // Init Renderer + Particle System
            renderSystem = new RenderSystem(this, FindObjectOfType<InputController>());
            system = new AS2.Sim.ParticleSystem(this, renderSystem);

            // Register UI
            if (uiHandler != null) uiHandler.RegisterSim(this);

            // Open Init Mode (when initialized)
            StartCoroutine(OpenInitModeCoroutine());
        }

        // Update is called once per frame
        void Update()
        {
            renderSystem.Render();
        }

        // FixedUpdate is called once per Time.fixedDeltaTime interval
        void FixedUpdate()
        {
            if (running)
            {
                PlayStep();
            }
        }

        /// <summary>
        /// Triggers the simulation of a single round or a step forward
        /// in the simulation history, depending on where the marker is.
        /// Called on each FixedUpdate while the simulation is running.
        /// </summary>
        public void PlayStep()
        {
            if (system.IsInLatestRound())
                system.SimulateRound();
            else
                system.StepForward();
            RoundChanged();
        }

        /// <summary>
        /// Triggers UI updates after the displayed round has changed in
        /// Simulation Mode. Should be called every time the round changes.
        /// </summary>
        public void RoundChanged()
        {
            uiHandler.particleUI.SimState_RoundChanged();
            if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
        }

        /// <summary>
        /// Updates the simulation speed to the given value.
        /// </summary>
        /// <param name="roundTime">The time between two round simulations.</param>
        public void SetSimSpeed(float roundTime)
        {
            if (roundTime == 0)
            {
                // Max Speed
                roundTime = 0.01f;
            }
            Time.fixedDeltaTime = roundTime;
            renderSystem.SetRoundTime(roundTime);
        }

        /// <summary>
        /// Toggles the play state of the simulation between
        /// Playing and Paused.
        /// </summary>
        public void TogglePlayPause()
        {
            if (uiHandler.initializationUI.IsOpen() == false)
            {
                if (running == false)
                {
                    EventDatabase.event_sim_startedStopped?.Invoke(true);
                }
                else
                {
                    //system.Print();
                    EventDatabase.event_sim_startedStopped?.Invoke(false);
                }
                running = !running;
                if (uiHandler != null) uiHandler.NotifyPlayPause(running);
            }
        }

        /// <summary>
        /// Starts playing the simulation.
        /// </summary>
        public void PlaySim()
        {
            if (running == false)
            {
                TogglePlayPause();
            }
        }

        /// <summary>
        /// Pauses the simulation.
        /// </summary>
        public void PauseSim()
        {
            if (running)
            {
                TogglePlayPause();
            }
        }

        /// <summary>
        /// Helper coroutine that opens the Init Mode UI after
        /// waiting for all required components to be initialized.
        /// </summary>
        /// <returns></returns>
        private IEnumerator OpenInitModeCoroutine()
        {
            yield return new WaitForEndOfFrame();
            if (uiHandler != null && uiHandler.initializationUI != null && uiHandler.initializationUI.IsInitialized())
            {
                uiHandler.initializationUI.Open();
            }
            else
            {
                StartCoroutine(OpenInitModeCoroutine());
            }
        }

    }

} // namespace AS2
