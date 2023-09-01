using System;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// <see cref="ParticleAttribute{T}"/> subclass representing direction values.
    /// </summary>
    public class ParticleAttribute_Direction : ParticleAttributeWithHistory<Direction>, IParticleAttribute
    {
        public ParticleAttribute_Direction(Particle particle, string name, Direction value = Direction.NONE) : base(particle, name)
        {
            history = new ValueHistory<Direction>(value, particle != null ? particle.system.CurrentRound : 0);
        }

        public override Direction GetValue()
        {
            if (particle == null)
                return history.GetMarkedValue();
            if (particle.system.InMovePhase || !hasIntermediateVal)
            {
                return history.GetValueInRound(particle.system.PreviousRound);
            }
            else
            {
                return intermediateVal;
            }
        }

        public override Direction GetCurrentValue()
        {
            if (particle == null)
                return history.GetMarkedValue();
            if (!particle.isActive)
            {
                throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
            }
            return history.GetMarkedValue();
        }

        public override void SetValue(Direction value)
        {
            if (particle == null)
                history.RecordValueInRound(value, 0);
            if (!particle.isActive)
            {
                throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
            }
            if (particle.system.InMovePhase)
            {
                hasIntermediateVal = true;
                intermediateVal = value;
            }
            history.RecordValueInRound(value, particle.system.CurrentRound);
        }

        public override string ToString()
        {
            return "ParticleAttribute (direction) with name " + name + " and value " + ToString_AttributeValue();
        }

        public string ToString_AttributeValue()
        {
            return history.GetMarkedValue().ToString();
        }

        public bool UpdateAttributeValue(string value)
        {
            if (Enum.TryParse(typeof(Direction), value, out object parsedVal))
            {
                history.RecordValueInRound((Direction)parsedVal, particle != null ? particle.system.CurrentRound : 0);
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot convert " + value + " to direction attribute.");
                return false;
            }
        }

        public bool SetRandomValue()
        {
            // Set random direction value that is not NONE
            history.RecordValueInRound((Direction)UnityEngine.Random.Range(0, 12), particle != null ? particle.system.CurrentRound : 0);
            return true;
        }

        public override bool Equals(ParticleAttribute<Direction> other)
        {
            return other is not null && GetValue() == other.GetValue();
        }
    }

} // namespace AS2.Sim
