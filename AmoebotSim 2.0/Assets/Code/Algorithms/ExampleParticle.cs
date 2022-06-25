using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State { IDLE, ROOT, LEADER }

public class ExampleParticle : Particle
{
    public ParticleAttribute_Int myInt;
    public ParticleAttribute_Enum<State> myEnum;

    public ExampleParticle(ParticleSystem system, int x = 0, int y = 0) : base(system, x, y)
    {
        myInt = new ParticleAttribute_Int(this, "Display name of myInt", 0);
        myEnum = new ParticleAttribute_Enum<State>(this, "Display name of myEnum", State.IDLE);
    }

    public override void Activate()
    {
        Debug.Log("myInt: " + myInt.ToString());
        Debug.Log("myEnum: " + myEnum.ToString());
        myInt.UpdateParameterValue("42");
        myEnum.UpdateParameterValue("LEADER");
        Debug.Log("myInt after: " + myInt.ToString());
        Debug.Log("myEnum after: " + myEnum.ToString());
        //Debug.Log("myInt before: " + myInt.ToString());
        //myInt = 42;
        //Debug.Log("myInt after assignment: " + myInt.ToString());
        //myInt++;
        //Debug.Log("myInt after increment: " + myInt.ToString());
        //myInt--;
        //Debug.Log("myInt after decrement: " + myInt.ToString());
    }
}
