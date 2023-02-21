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

    public class AmoebotSimulator : MonoBehaviour
    {

        // Singleton
        public static AmoebotSimulator instance;

        // System Data
        public AS2.Sim.ParticleSystem system;
        public RenderSystem renderSystem;
        // System State
        public bool running = true;

        // UI
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
            AS2.Sim.ParticleObject.DrawObjects();
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

        public void RoundChanged()
        {
            uiHandler.particleUI.SimState_RoundChanged();
            if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
        }

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
        /// Toggles the Play/Pause functionality.
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

        public void PlaySim()
        {
            if (running == false)
            {
                TogglePlayPause();
            }
        }

        public void PauseSim()
        {
            if (running)
            {
                TogglePlayPause();
            }
        }

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
