using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Abstract base class for particle attributes that adds functionality
/// which should be hidden from the algorithm developer, especially the
/// history methods.
/// </summary>
/// <typeparam name="T">The type of values the attribute stores.</typeparam>
public abstract class ParticleAttributeWithHistory<T> : ParticleAttribute<T>, IReplayHistory
{
    /// <summary>
    /// The history of values recorded by this attribute.
    /// </summary>
    protected ValueHistory<T> history;

    public ParticleAttributeWithHistory(Particle p, string name) : base(p, name) { }

    /**
     * Methods implementing IReplayHistory.
     * These are implemented here already to avoid having to
     * implement them in every ParticleAttribute subclass.
     */

    public virtual int GetFirstRecordedRound()
    {
        return history.GetFirstRecordedRound();
    }

    public virtual void SetMarkerToRound(int round)
    {
        history.SetMarkerToRound(round);
    }

    public virtual void StepBack()
    {
        history.StepBack();
    }

    public virtual void StepForward()
    {
        history.StepForward();
    }

    public virtual int GetMarkedRound()
    {
        return history.GetMarkedRound();
    }

    public virtual void ContinueTracking()
    {
        history.ContinueTracking();
    }

    public virtual void CutOffAtMarker()
    {
        history.CutOffAtMarker();
    }

    public virtual void ShiftTimescale(int amount)
    {
        history.ShiftTimescale(amount);
    }

    /**
     * Some methods of IParticleAttribute interface can already be implemented here
     */

    public virtual string ToString_AttributeName()
    {
        return name;
    }

    public virtual System.Type GetAttributeType()
    {
        return System.Type.GetType(nameof(T));
    }


    // <<<TEMPORARY: FOR DEBUGGING HISTORIES>>>
    public void Print()
    {
        Debug.Log(name + ":");
        if (history != null)
        {
            history.Print();
        }
        else
        {
            Debug.LogWarning("History is null.");
        }
    }
}
