using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

public class AlgorithmManager
{
    // Property names
    private static readonly string Name_Property = "Name";
    private static readonly string Chirality_Property = "Chirality";
    private static readonly string Compass_Property = "Compass";
    
    // Singleton
    private static AlgorithmManager instance = new AlgorithmManager();
    public static AlgorithmManager Instance
    {
        get { return instance; }
    }

    private class AlgorithmInfo
    {
        public string name;
        public Type type;
        public ConstructorInfo ctor;
        public Initialization.Chirality chirality;
        public Initialization.Compass compassDir;

        public AlgorithmInfo(string name, Type type, ConstructorInfo ctor, Initialization.Chirality chirality, Initialization.Compass compassDir)
        {
            this.name = name;
            this.type = type;
            this.ctor = ctor;
            this.chirality = chirality;
            this.compassDir = compassDir;
        }
    }

    private Dictionary<string, AlgorithmInfo> algorithms;

    public AlgorithmManager()
    {
        // Find all ParticleAlgorithm subtypes
        Type baseAlgoType = typeof(ParticleAlgorithm);
        Assembly baseAlgoAssembly = baseAlgoType.Assembly;
        IEnumerable<Type> subclasses = baseAlgoAssembly.GetTypes().Where(t => t.IsSubclassOf(baseAlgoType));

        Initialization.Chirality defaultChirality = (Initialization.Chirality)baseAlgoType.GetProperty(Chirality_Property).GetValue(null);
        Initialization.Compass defaultCompass = (Initialization.Compass)baseAlgoType.GetProperty(Compass_Property).GetValue(null);

        algorithms = new Dictionary<string, AlgorithmInfo>();

        foreach (Type algoType in subclasses)
        {
            PropertyInfo nameProp = algoType.GetProperty(Name_Property);
            PropertyInfo chiralityProp = algoType.GetProperty(Chirality_Property);
            PropertyInfo compassProp = algoType.GetProperty(Compass_Property);

            string name;
            if (nameProp == null)
            {
                name = algoType.FullName;
            }
            else
            {
                name = (string)nameProp.GetValue(null);
            }

            Initialization.Chirality chirality = defaultChirality;
            Initialization.Compass compass = defaultCompass;

            if (chiralityProp != null)
                chirality = (Initialization.Chirality)chiralityProp.GetValue(null);

            if (compassProp != null)
                compass = (Initialization.Compass)compassProp.GetValue(null);

            ConstructorInfo ctor = algoType.GetConstructor(new Type[] { typeof(Particle), typeof(int[]) });
            if (ctor == null)
            {
                Debug.LogWarning("ParticleAlgorithm with name '" + name + "' does not declare a constructor with parameters (Particle, int[]), cannot load algorithm");
                continue;
            }

            if (algorithms.ContainsKey(name))
            {
                Debug.LogWarning("ParticleAlgorithm with name '" + name + "' already exists, cannot load algorithm");
            }
            else
            {
                algorithms[name] = new AlgorithmInfo(name, algoType, ctor, chirality, compass);
            }
        }

        Debug.Log("Algorithms:");
        foreach (KeyValuePair<string, AlgorithmInfo> kv in algorithms)
        {
            Debug.Log("Name: " + kv.Key + "\nType: " + kv.Value.type + "\nChirality: " + kv.Value.chirality + "\nCompass: " + kv.Value.compassDir);
        }
    }

    private AlgorithmInfo FindAlgorithm(string name)
    {
        if (algorithms.TryGetValue(name, out AlgorithmInfo info))
        {
            return info;
        }
        else
        {
            Debug.LogError("Error: No algorithm with name '" + name + "' known");
            return null;
        }
    }

    public Initialization.Chirality GetAlgorithmChirality(string name)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        if (info != null)
            return info.chirality;
        else
            throw new System.ArgumentException("Could not find algorithm");
    }

    public Initialization.Compass GetAlgorithmCompass(string name)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        if (info != null)
            return info.compassDir;
        else
            throw new System.ArgumentException("Could not find algorithm");
    }

    public ParticleAlgorithm Instantiate(string name, Particle particle, int[] genericParams)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        if (info == null)
        {
            throw new System.ArgumentException("Could not find algorithm");
        }

        //ConstructorInfo ctor = info.type.GetConstructor(new Type[] { typeof(Particle), typeof(int[]) });
        return (ParticleAlgorithm)info.ctor.Invoke(new object[] { particle, genericParams });
    }

    public List<string> GetAlgorithmNames()
    {
        return algorithms.Keys.ToList();
    }
}
