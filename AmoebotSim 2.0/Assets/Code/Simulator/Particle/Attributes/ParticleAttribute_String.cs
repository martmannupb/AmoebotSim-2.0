using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// <see cref="ParticleAttribute{T}"/> subclass representing strings.
    /// </summary>
    public class ParticleAttribute_String : ParticleAttributeWithHistory<string>, IParticleAttribute
    {
        public ParticleAttribute_String(Particle particle, string name, string value = "") : base(particle, name)
        {
            history = new ValueHistory<string>(value, particle != null ? particle.system.CurrentRound : 0);
        }

        public override string GetValue()
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

        public override string GetCurrentValue()
        {
            if (particle == null)
                return history.GetMarkedValue();
            if (!particle.isActive)
            {
                throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
            }
            return history.GetMarkedValue();
        }

        public override void SetValue(string value)
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
            return "ParticleAttribute (string) with name " + name + " and value " + ToString_AttributeValue();
        }

        public string ToString_AttributeValue()
        {
            return history.GetMarkedValue();
        }

        public bool UpdateAttributeValue(string value)
        {
            if (value != null)
            {
                history.RecordValueInRound(value, particle != null ? particle.system.CurrentRound : 0);
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot convert " + value + " to string attribute.");
                return false;
            }
        }

        public override bool Equals(ParticleAttribute<string> other)
        {
            return other is not null && GetValue().Equals(other.GetValue());
        }
    }

} // namespace AS2.Sim
