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
    /// <see cref="ParticleAttribute{T}"/> subclass representing integer values.
    /// </summary>
    public class ParticleAttribute_Int : ParticleAttributeWithHistory<int>, IParticleAttribute
    {
        public ParticleAttribute_Int(Particle particle, string name, int value = 0) : base(particle, name)
        {
            history = new ValueHistory<int>(value, particle != null ? particle.system.CurrentRound : 0);
        }

        public override int GetValue()
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

        public override int GetCurrentValue()
        {
            if (particle == null)
                return history.GetMarkedValue();
            if (!particle.isActive)
            {
                throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
            }
            return history.GetMarkedValue();
        }

        public override void SetValue(int value)
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
            return "ParticleAttribute (int) with name " + name + " and value " + ToString_AttributeValue();
        }

        public string ToString_AttributeValue()
        {
            return history.GetMarkedValue().ToString();
        }

        public bool UpdateAttributeValue(string value)
        {
            if (int.TryParse(value, out int parsedVal))
            {
                history.RecordValueInRound(parsedVal, particle != null ? particle.system.CurrentRound : 0);
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot convert " + value + " to int attribute.");
                return false;
            }
        }

        public override bool Equals(ParticleAttribute<int> other)
        {
            return other is not null && GetValue() == other.GetValue();
        }
    }

} // namespace AS2.Sim
