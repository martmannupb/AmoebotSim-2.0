using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class ParticleUIHandler : MonoBehaviour
{

    // Singleton
    public static ParticleUIHandler instance;

    // References
    private UIHandler uiHandler;

    // Data
    public GameObject go_particlePanel;
    public GameObject go_attributeParent;

    private void Start()
    {
        // Singleton
        instance = this;

        // Hide Panel
        ExitParticlePanel();
    }

    public void ShowParticlePanel(Particle p)
    {
        go_particlePanel.SetActive(true);
    }

    public void ExitParticlePanel()
    {
        go_particlePanel.SetActive(false);
    }

}
