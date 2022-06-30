using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State { IDLE, ROOT, LEADER }

public class ExampleParticle : ParticleAlgorithm
{
    public ParticleAttribute_Int myInt;
    public ParticleAttribute_Enum<State> myEnum;

    public ExampleParticle(Particle p) : base(p)
    {
        myInt = new ParticleAttribute_Int(this, "Display name of myInt", 0);
        myEnum = new ParticleAttribute_Enum<State>(this, "Display name of myEnum", State.IDLE);
    }

    public override void Activate()
    {
        //Debug.Log("myInt: " + myInt.ToString());
        //Debug.Log("myEnum: " + myEnum.ToString());
        //myInt.UpdateParameterValue("42");
        //myEnum.UpdateParameterValue("LEADER");
        //Debug.Log("myInt after: " + myInt.ToString());
        //Debug.Log("myEnum after: " + myEnum.ToString());

        if (IsExpanded())
        {
            ContractHead();
        }
        else
        {
            // Expand in random free direction
            List<int> freeDirs = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                if (!HasNeighborAt(i))
                {
                    freeDirs.Add(i);
                }
            }
            if (freeDirs.Count > 0)
            {
                Expand(freeDirs[Random.Range(0, freeDirs.Count)]);
            }
        }
    }
}
