using System.Collections.Generic;
using AS2.Visuals;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// A stripped down particle class that is only used for
    /// system initialization. The data stored in this class
    /// is used to instantiate the proper particles and the
    /// associated algorithms when simulation mode is entered.
    /// </summary>
    public abstract class InitializationParticle : IParticleState
    {
        /// <summary>
        /// The grid position of the particle's tail.
        /// </summary>
        protected Vector2Int tailPos;
        /// <summary>
        /// The grid position of the particle's head.
        /// </summary>
        protected Vector2Int headPos;

        /// <summary>
        /// The particle's global head direction.
        /// </summary>
        protected Direction expansionDir;

        /// <summary>
        /// The particle's global head direction.
        /// <see cref="Direction.NONE"/> means the particle
        /// is contracted.
        /// </summary>
        public Direction ExpansionDir
        {
            get { return expansionDir; }
            set
            {
                if (system.TryChangeInitParticleExpansion(this, value))
                {
                    if (value == Direction.NONE)
                    {
                        headPos = tailPos;
                    }
                    else
                    {
                        headPos = ParticleSystem_Utils.GetNbrInDir(tailPos, value);
                    }
                    expansionDir = value;
                }
            }
        }

        /// <summary>
        /// The particle's chirality. <c>true</c> means
        /// that it agrees with the global coordinate system.
        /// </summary>
        protected bool chirality;
        /// <summary>
        /// The particle's chirality. <c>true</c> means
        /// that it agrees with the global coordinate system.
        /// </summary>
        public bool Chirality
        {
            get { return chirality; }
            set { chirality = value; }
        }

        /// <summary>
        /// The particle's compass direction. This is the
        /// global direction that the particle believes to
        /// be <see cref="Direction.E"/>.
        /// </summary>
        protected Direction compassDir;
        /// <summary>
        /// The particle's compass direction. This is the
        /// global direction that the particle believes to
        /// be <see cref="Direction.E"/>.
        /// </summary>
        public Direction CompassDir
        {
            get { return compassDir; }
            set
            {
                if (!value.IsCardinal())
                    Log.Warning("Compass direction '" + value + "' is not valid, must be cardinal.");
                else
                    compassDir = value;
            }
        }

        // Attributes representing the parameter values
        /// <summary>
        /// Attributes storing initialization parameters.
        /// </summary>
        protected List<IParticleAttribute> attributes;

        /// <summary>
        /// Reference to the rendering representation of the particle.
        /// </summary>
        public ParticleGraphicsAdapterImpl graphics;
        /// <summary>
        /// The system in which this particle is placed.
        /// </summary>
        protected ParticleSystem system;

        public InitializationParticle(ParticleSystem system, Vector2Int position, bool chirality, Direction compassDir, Direction expansionDir = Direction.NONE)
        {
            tailPos = position;
            this.chirality = chirality;
            this.compassDir = compassDir;
            this.expansionDir = expansionDir;
            if (expansionDir == Direction.NONE)
                headPos = tailPos;
            else
                headPos = ParticleSystem_Utils.GetNbrInDir(tailPos, expansionDir);
            this.system = system;

            // Setup the attributes according to the selected algorithm's init parameters
            attributes = new List<IParticleAttribute>();
            string algo = system.SelectedAlgorithm;
            AlgorithmManager man = AlgorithmManager.Instance;
            if (man.IsAlgorithmKnown(algo))
            {
                System.Reflection.ParameterInfo[] initParams = man.GetInitParameters(algo);
                if (initParams != null)
                {
                    foreach (System.Reflection.ParameterInfo param in initParams)
                    {
                        IParticleAttribute attr = ParticleAttributeFactory.CreateParticleAttribute(null, param.ParameterType, param.Name, param.HasDefaultValue ? param.DefaultValue : null);
                        if (attr != null)
                            attributes.Add(attr);
                    }
                }
            }

            // Add particle to the render system and update the visuals of the particle
            graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
        }

        /*
         * IParticleState implementation
         */

        public int GetCircuitPinsPerSide()
        {
            return 0;
        }

        public Color GetParticleColor()
        {
            return Color.gray;
        }

        public int GlobalHeadDirectionInt()
        {
            return expansionDir.ToInt();
        }

        public Vector2Int Head()
        {
            return headPos;
        }

        public bool IsExpanded()
        {
            return expansionDir != Direction.NONE;
        }

        public bool IsParticleColorSet()
        {
            return true;
        }

        public Vector2Int Tail()
        {
            return tailPos;
        }

        bool IParticleState.Chirality()
        {
            return chirality;
        }

        Direction IParticleState.CompassDir()
        {
            return compassDir;
        }

        public void SetChirality(bool chirality)
        {
            this.chirality = chirality;
        }

        public void SetCompassDir(Direction compassDir)
        {
            if (compassDir.IsCardinal())
                this.compassDir = compassDir;
        }

        public List<IParticleAttribute> GetAttributes()
        {
            return attributes;
        }

        public IParticleAttribute TryGetAttributeByName(string attrName)
        {
            foreach (IParticleAttribute attr in attributes)
            {
                if (attr.ToString_AttributeName().Equals(attrName))
                    return attr;
            }
            return null;
        }

        public void SetAttribute(string attrName, object value)
        {
            foreach (IParticleAttribute attr in attributes)
            {
                if (attr.ToString_AttributeName().Equals(attrName))
                {
                    attr.UpdateAttributeValue(value.ToString());
                    return;
                }
            }
            Log.Warning("Tried to set value of attribute '" + attrName + "' but could not find this attribute.");
        }

        public void SetAttributes(object[] values)
        {
            int n = Mathf.Min(values.Length, attributes.Count);
            for (int i = 0; i < n; i++)
            {
                attributes[i].UpdateAttributeValue(values[i].ToString());
            }
        }

        public object[] GetParameterValues()
        {
            object[] vals = new object[attributes.Count];

            for (int i = 0; i < vals.Length; i++)
            {
                vals[i] = attributes[i].GetObjectValue();
            }

            return vals;
        }

        /// <summary>
        /// Generates a serializable representation of this
        /// particle.
        /// </summary>
        /// <returns>A serializable object representing this particle.</returns>
        public abstract InitParticleSaveData GenerateSaveData();

        public bool IsAnchor()
        {
            return system.IsAnchor(this);
        }

        public bool MakeAnchor()
        {
            return system.SetAnchor(this);
        }

        // Added by Tobias (put it where you like) ____________________________

        public IParticleGraphicsAdapter GetGraphicsAdapter()
        {
            return graphics;
        }
    }

} // namespace AS2.Sim
