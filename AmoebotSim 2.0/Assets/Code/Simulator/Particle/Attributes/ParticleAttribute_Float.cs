// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// <see cref="ParticleAttribute{T}"/> subclass representing float values.
    /// </summary>
    public class ParticleAttribute_Float : ParticleAttributeWithHistory<float>, IParticleAttribute
    {
        public ParticleAttribute_Float(Particle particle, string name, float value = 0f) : base(particle, name)
        {
            history = new ValueHistory<float>(value, particle != null ? particle.system.CurrentRound : 0);
        }

        public override float GetValue()
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

        public override float GetCurrentValue()
        {
            if (particle == null)
                return history.GetMarkedValue();
            if (!particle.isActive)
            {
                throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
            }
            return history.GetMarkedValue();
        }

        public override void SetValue(float value)
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
            return "ParticleAttribute (float) with name " + name + " and value " + ToString_AttributeValue();
        }

        public string ToString_AttributeValue()
        {
            return history.GetMarkedValue().ToString();
        }

        public bool UpdateAttributeValue(string value)
        {
            float parsedVal;
            if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsedVal)
                || float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out parsedVal))
            {
                history.RecordValueInRound(parsedVal, particle != null ? particle.system.CurrentRound : 0);
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot convert " + value + " to float attribute.");
                return false;
            }
        }

        public override bool Equals(ParticleAttribute<float> other)
        {
            return other is not null && GetValue() == other.GetValue();
        }
    }

} // namespace AS2.Sim
