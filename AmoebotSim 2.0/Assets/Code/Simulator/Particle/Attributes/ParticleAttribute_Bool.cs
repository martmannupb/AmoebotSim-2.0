using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// <see cref="ParticleAttribute{T}"/> subclass representing boolean values.
    /// </summary>
    public class ParticleAttribute_Bool : ParticleAttributeWithHistory<bool>, IParticleAttribute
    {
        public ParticleAttribute_Bool(Particle particle, string name, bool value = false) : base(particle, name)
        {
            history = new ValueHistory<bool>(value, particle != null ? particle.system.CurrentRound : 0);
        }

        public override bool GetValue()
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

        public override bool GetCurrentValue()
        {
            if (particle == null)
                return history.GetMarkedValue();
            if (!particle.isActive)
            {
                throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
            }
            return history.GetMarkedValue();
        }

        public override void SetValue(bool value)
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
            return "ParticleAttribute (bool) with name " + name + " and value " + ToString_AttributeValue();
        }

        public string ToString_AttributeValue()
        {
            return history.GetMarkedValue().ToString();
        }

        public bool UpdateAttributeValue(string value)
        {
            if (bool.TryParse(value, out bool parsedVal))
            {
                history.RecordValueInRound(parsedVal, particle != null ? particle.system.CurrentRound : 0);
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot convert " + value + " to bool attribute.");
                return false;
            }
        }

        public bool SetRandomValue()
        {
            // Find a random bool value
            history.RecordValueInRound(Random.Range(0, 2) == 0, particle != null ? particle.system.CurrentRound : 0);
            return true;
        }

        public override bool Equals(ParticleAttribute<bool> other)
        {
            return other is not null && GetValue() == other.GetValue();
        }
    }

} // namespace AS2.Sim
