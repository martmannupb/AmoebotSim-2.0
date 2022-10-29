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
    private static readonly string Generator_Property = "GenerationMethod";
    private static readonly string Init_Method = "Init";
    
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
        public MethodInfo initMethod;
        public string generationMethod;

        public AlgorithmInfo(string name, Type type, ConstructorInfo ctor, Initialization.Chirality chirality, Initialization.Compass compassDir,
            MethodInfo initMethod, string generationMethod)
        {
            this.name = name;
            this.type = type;
            this.ctor = ctor;
            this.chirality = chirality;
            this.compassDir = compassDir;
            this.initMethod = initMethod;
            this.generationMethod = generationMethod;
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
        string defaultGenerator = (string)baseAlgoType.GetProperty(Generator_Property).GetValue(null);

        algorithms = new Dictionary<string, AlgorithmInfo>();

        foreach (Type algoType in subclasses)
        {
            PropertyInfo nameProp = algoType.GetProperty(Name_Property);
            PropertyInfo chiralityProp = algoType.GetProperty(Chirality_Property);
            PropertyInfo compassProp = algoType.GetProperty(Compass_Property);
            PropertyInfo generatorProp = algoType.GetProperty(Generator_Property);

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
            string generator = defaultGenerator;

            if (chiralityProp != null)
                chirality = (Initialization.Chirality)chiralityProp.GetValue(null);

            if (compassProp != null)
                compass = (Initialization.Compass)compassProp.GetValue(null);

            if (generatorProp != null)
                generator = (string)generatorProp.GetValue(null);

            ConstructorInfo ctor = algoType.GetConstructor(new Type[] { typeof(Particle) });
            if (ctor == null)
            {
                Debug.LogWarning("ParticleAlgorithm with name '" + name + "' does not declare a constructor with Particle parameter, cannot load algorithm");
                continue;
            }

            MethodInfo initMethod = algoType.GetMethod(Init_Method);

            if (algorithms.ContainsKey(name))
            {
                Debug.LogWarning("ParticleAlgorithm with name '" + name + "' already exists, cannot load algorithm");
            }
            else
            {
                algorithms[name] = new AlgorithmInfo(name, algoType, ctor, chirality, compass, initMethod, generator);
            }
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

    public string GetAlgorithmGenerationMethod(string name)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        if (info != null)
            return info.generationMethod;
        else
            throw new System.ArgumentException("Could not find algorithm");
    }

    public ParameterInfo[] GetInitParameters(string name)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        if (info == null || info.initMethod == null)
            return null;

        return info.initMethod.GetParameters();
    }

    public ParticleAlgorithm Instantiate(string name, Particle particle)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        if (info == null)
        {
            throw new System.ArgumentException("Could not find algorithm");
        }

        //ConstructorInfo ctor = info.type.GetConstructor(new Type[] { typeof(Particle), typeof(int[]) });
        return (ParticleAlgorithm)info.ctor.Invoke(new object[] { particle });
    }

    public void Initialize(string name, ParticleAlgorithm algo, object[] parameters)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        if (info == null)
        {
            throw new System.ArgumentException("Could not find algorithm");
        }

        if (info.initMethod != null)
        {
            info.initMethod.Invoke(algo, parameters);
        }
    }

    public List<string> GetAlgorithmNames()
    {
        return algorithms.Keys.ToList();
    }

    public bool IsAlgorithmKnown(string name)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        return info != null;
    }
}
