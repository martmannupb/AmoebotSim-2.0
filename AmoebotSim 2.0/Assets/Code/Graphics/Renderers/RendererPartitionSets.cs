using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererPartitionSets
{

    public void Render(ViewType viewType)
    {
        switch (viewType)
        {
            case ViewType.Hexagonal:
                Render_Hexagonal();
                break;
            case ViewType.Circular:
                Render_Circular();
                break;
            default:
                break;
        }
    }

    private void Render_Hexagonal()
    {
        
    }

    private void Render_Circular()
    {
        
    }

}