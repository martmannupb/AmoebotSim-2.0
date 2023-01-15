using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Specialization of the <see cref="InitializationParticle"/>
    /// class that provides direct access to some of its protected
    /// members. Should only be used by the system because setting
    /// these values directly can lead to inconsistent states.
    /// </summary>
    public class OpenInitParticle : InitializationParticle
    {
        public OpenInitParticle(ParticleSystem system, Vector2Int position, bool chirality, Direction compassDir, Direction expansionDir = Direction.NONE)
            : base(system, position, chirality, compassDir, expansionDir) { }

        /// <summary>
        /// Direct access to the tail position of the particle.
        /// </summary>
        public Vector2Int TailPosDirect
        {
            get { return tailPos; }
            set { tailPos = value; }
        }

        /// <summary>
        /// Direct access to the head position of the particle.
        /// </summary>
        public Vector2Int HeadPosDirect
        {
            get { return headPos; }
            set { headPos = value; }
        }

        /// <summary>
        /// Direct access to the expansion direction of the particle.
        /// </summary>
        public Direction ExpansionDirDirect
        {
            get { return expansionDir; }
            set { expansionDir = value; }
        }

        // Save and Load functionality

        public override InitParticleSaveData GenerateSaveData()
        {
            InitParticleSaveData data = new InitParticleSaveData();

            data.tailPos = tailPos;
            data.expansionDir = expansionDir;
            data.chirality = chirality;
            data.compassDir = compassDir;

            data.boolAttributes = new List<ParticleAttributeSaveData<bool>>();
            data.dirAttributes = new List<ParticleAttributeSaveData<Direction>>();
            data.floatAttributes = new List<ParticleAttributeSaveData<float>>();
            data.intAttributes = new List<ParticleAttributeSaveData<int>>();
            data.enumAttributes = new List<ParticleAttributeEnumSaveData>();
            data.pcAttributes = new List<ParticleAttributePCSaveData>();
            data.stringAttributes = new List<ParticleAttributeSaveData<string>>();

            // Fill in the particle attributes ordered by type
            // Must use reflection here
            for (int i = 0; i < attributes.Count; i++)
            {
                System.Type t = attributes[i].GetAttributeType();
                if (t == typeof(int))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.intAttributes.Add(aData as ParticleAttributeSaveData<int>);
                }
                else if (t == typeof(bool))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.boolAttributes.Add(aData as ParticleAttributeSaveData<bool>);
                }
                else if (t == typeof(Direction))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.dirAttributes.Add(aData as ParticleAttributeSaveData<Direction>);
                }
                else if (t == typeof(float))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.floatAttributes.Add(aData as ParticleAttributeSaveData<float>);
                }
                else if (t == typeof(string))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.stringAttributes.Add(aData as ParticleAttributeSaveData<string>);
                }
                else if (t == typeof(PinConfiguration))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.pcAttributes.Add(aData as ParticleAttributePCSaveData);
                }
                else if (attributes[i].GetType().IsGenericType && attributes[i].GetType().GetGenericTypeDefinition() == typeof(ParticleAttribute_Enum<>))
                {
                    ParticleAttributeSaveDataBase aData = attributes[i].GenerateSaveData();
                    data.enumAttributes.Add(aData as ParticleAttributeEnumSaveData);
                }
            }

            return data;
        }

        public OpenInitParticle(ParticleSystem system, InitParticleSaveData data) : base(system, Vector2Int.zero, true, Direction.E, Direction.NONE)
        {
            tailPos = data.tailPos;
            expansionDir = data.expansionDir;
            headPos = expansionDir != Direction.NONE ? ParticleSystem_Utils.GetNbrInDir(tailPos, expansionDir) : tailPos;
            chirality = data.chirality;
            compassDir = data.compassDir;

            // The attributes have already been created for the algorithm, we now just need to fill them with different values
            // We store the attributes provided by the save state in a dictionary for easy access
            Dictionary<string, ParticleAttributeSaveDataBase> savedAttrs = new Dictionary<string, ParticleAttributeSaveDataBase>();
            foreach (var list in new IEnumerable[] { data.boolAttributes, data.dirAttributes, data.floatAttributes, data.intAttributes,
            data.enumAttributes, data.pcAttributes, data.stringAttributes })
            {
                foreach (ParticleAttributeSaveDataBase a in list)
                {
                    savedAttrs.Add(a.name, a);
                }
            }

            foreach (IParticleAttribute myAttr in attributes)
            {
                string name = myAttr.ToString_AttributeName();
                if (!savedAttrs.ContainsKey(name))
                {
                    Debug.LogError("Attribute " + name + " not stored in save data.");
                    continue;
                }
                // Try filling in the values
                if (!myAttr.RestoreFromSaveData(savedAttrs[name]))
                {
                    Debug.LogError("Unable to restore attribute " + name + " from saved data.");
                }
            }
        }
    }

} // namespace AS2.Sim
