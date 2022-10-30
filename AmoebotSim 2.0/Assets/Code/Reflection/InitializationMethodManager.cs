using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class InitializationMethodManager
{
    // The name of the method to be implemented
    private static readonly string GenerationMethodName = "Generate";

    // Singleton
    private static InitializationMethodManager instance = new InitializationMethodManager();
    public static InitializationMethodManager Instance
    {
        get { return instance; }
    }

    private class AlgorithmInfo
    {
        public string name;
        public Type type;
        public ConstructorInfo ctor;
        public MethodInfo generateMethod;

        public AlgorithmInfo(string name, Type type, ConstructorInfo ctor, MethodInfo generateMethod)
        {
            this.name = name;
            this.type = type;
            this.ctor = ctor;
            this.generateMethod = generateMethod;
        }
    }

    private Dictionary<string, AlgorithmInfo> algorithms;

    public InitializationMethodManager()
    {
        // Find all InitializationMethod subtypes
        Type baseAlgoType = typeof(InitializationMethod);
        Assembly baseAlgoAssembly = baseAlgoType.Assembly;
        IEnumerable<Type> subclasses = baseAlgoAssembly.GetTypes().Where(t => t.IsSubclassOf(baseAlgoType));

        algorithms = new Dictionary<string, AlgorithmInfo>();

        foreach (Type algoType in subclasses)
        {
            Debug.Log("Found initialization method with name " + algoType.FullName);

            // Find out the algorithm's name (simply use the full type name)
            string name = algoType.FullName;

            // Find the right constructor
            ConstructorInfo ci = algoType.GetConstructor(new Type[] { typeof(ParticleSystem) });
            if (ci == null)
            {
                Log.Warning("Initialization method with name '" + name + "' does not implement a constructor with a single ParticleSystem parameter.");
                continue;
            }

            // Find the generation method and its signature
            MethodInfo[] methods = algoType.GetMethods();
            MethodInfo generateMethod = null;
            foreach (MethodInfo mi in methods)
            {
                if (mi.Name.Equals(GenerationMethodName))
                {
                    ParameterInfo[] parameters = mi.GetParameters();
                    string s = "Params:\n";
                    foreach (ParameterInfo pi in parameters)
                    {
                        s += pi.Name + ", " + pi.ParameterType + "\n";
                    }
                    Debug.Log(s);
                    generateMethod = mi;
                }
            }

            if (generateMethod == null)
            {
                Debug.LogWarning("Initialization method with name '" + name + "' does not implement a " + GenerationMethodName + " method.");
            }
            else
            {
                Debug.Log("Found generation method!");
                if (algorithms.ContainsKey(name))
                {
                    Debug.LogWarning("InitializationMethod with name '" + name + "' already exists, cannot load this method");
                }
                else
                {
                    algorithms[name] = new AlgorithmInfo(name, algoType, ci, generateMethod);
                }
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

    public List<string> GetAlgorithmNames()
    {
        return algorithms.Keys.ToList();
    }

    public ParameterInfo[] GetAlgorithmParameters(string algorithmName)
    {
        AlgorithmInfo info = FindAlgorithm(algorithmName);
        if (info == null)
            return null;

        return info.generateMethod.GetParameters();
    }

    public bool GenerateSystem(ParticleSystem system, string algorithmName, object[] parameters)
    {
        AlgorithmInfo info = FindAlgorithm(algorithmName);
        if (info == null)
        {
            return false;
        }

        try
        {
            InitializationMethod methodObj = (InitializationMethod)info.ctor.Invoke(new object[] { system });
            info.generateMethod.Invoke(methodObj, parameters);
        }
        catch (Exception e)
        {
            Log.Error("Could not use initialization method '" + info.name + "': " + e);
            return false;
        }

        return true;
    }

    public bool IsAlgorithmKnown(string name)
    {
        AlgorithmInfo info = FindAlgorithm(name);
        return info != null;
    }
}
