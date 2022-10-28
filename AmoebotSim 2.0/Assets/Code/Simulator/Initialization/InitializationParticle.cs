using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A stripped down particle class that is only used for
/// system initialization. The data stored in this class
/// is used to instantiate the proper particles and the
/// associated algorithms when simulation mode is entered.
/// </summary>
public abstract class InitializationParticle : IParticleState
{
    protected Vector2Int tailPos;
    protected Vector2Int headPos;

    protected Direction expansionDir;
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

    protected bool chirality;
    public bool Chirality
    {
        get { return chirality; }
        set { chirality = value; }
    }

    protected Direction compassDir;
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
    protected List<IParticleAttribute> attributes;

    public List<int> genericParams;
    public ParticleGraphicsAdapterImpl graphics;
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

        genericParams = new List<int>(system.NumGenericParameters);
        for (int i = 0; i < system.NumGenericParameters; i++)
            genericParams.Add(0);
        
        // Add particle to the render system and update the visuals of the particle
        graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
    }

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
    /// Appends a new generic parameter with the given initial value.
    /// <para>
    /// This method should not be called on individual particles.
    /// Instead, add a new parameter to all current particles at
    /// once using the corresponding interface method.
    /// </para>
    /// </summary>
    /// <param name="initialVal">The initial value of the new
    /// generic parameter.</param>
    public void AddGenericParam(int initialVal = 0)
    {
        genericParams.Add(initialVal);
    }

    /// <summary>
    /// Removes the generic parameter with the given index.
    /// <para>
    /// This method should not be called on individual particles.
    /// Instead, remove the parameter from all current particles
    /// at once using the corresponding interface method.
    /// </para>
    /// </summary>
    /// <param name="index">The index of the generic parameter
    /// to be removed.</param>
    public void RemoveGenericParam(int index)
    {
        genericParams.RemoveAt(index);
    }
}
