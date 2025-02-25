// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Singleton class that uses reflection to manage all initialization
    /// methods, providing an interface to access their names and parameters
    /// and to execute them to initialize the system.
    /// </summary>
    public class InitializationMethodManager
    {
        // The name of the method to be implemented
        private static readonly string GenerationMethodName = "Generate";

        // Singleton
        private static InitializationMethodManager instance = new InitializationMethodManager();
        /// <summary>
        /// The singleton instance of this class.
        /// </summary>
        public static InitializationMethodManager Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Represents a single generation method and contains all
        /// information needed to initialize a system with it.
        /// </summary>
        private class AlgorithmInfo
        {
            /// <summary>
            /// The full name of the generation method.
            /// </summary>
            public string name;
            /// <summary>
            /// The type of the generator class.
            /// </summary>
            public Type type;
            /// <summary>
            /// Reference to the method's constructor.
            /// This must be the constructor that gets a single
            /// parameter of type <see cref="Sim.ParticleSystem"/>.
            /// </summary>
            public ConstructorInfo ctor;
            /// <summary>
            /// Reference to the generation method itself.
            /// </summary>
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
                // Find out the algorithm's name (simply use the full type name)
                string name = algoType.FullName;

                // Find the right constructor
                ConstructorInfo ci = algoType.GetConstructor(new Type[] { typeof(Sim.ParticleSystem) });
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
                        generateMethod = mi;
                    }
                }

                if (generateMethod == null)
                {
                    Debug.LogWarning("Initialization method with name '" + name + "' does not implement a " + GenerationMethodName + " method.");
                }
                else
                {
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

        /// <summary>
        /// Tries to get the generation method with the given name.
        /// </summary>
        /// <param name="name">The string ID of the generation method.</param>
        /// <returns>The record of the generation method with name
        /// <paramref name="name"/> if it exists, otherwise <c>null</c>.</returns>
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

        /// <summary>
        /// Gets a list of all known generation method names.
        /// </summary>
        /// <returns>A list containing the names of all generation methods.</returns>
        public List<string> GetAlgorithmNames()
        {
            return algorithms.Keys.ToList();
        }

        /// <summary>
        /// Gets the parameters of the generation method.
        /// </summary>
        /// <param name="algorithmName">The string ID of the generation method.</param>
        /// <returns>An array containing the parameters of the
        /// generation method, if the algorithm exists and
        /// it implements this method, otherwise <c>null</c>.</returns>
        public ParameterInfo[] GetAlgorithmParameters(string algorithmName)
        {
            AlgorithmInfo info = FindAlgorithm(algorithmName);
            if (info == null)
                return null;

            return info.generateMethod.GetParameters();
        }

        /// <summary>
        /// Calls the specified generation method to initialize the
        /// given system with no parameters.
        /// </summary>
        /// <param name="system">The system to initialize.</param>
        /// <param name="algorithmName">The string ID of the generation method.</param>
        /// <returns><c>true</c> if and only if the generation method was
        /// executed successfully.</returns>
        public bool GenerateSystem(AS2.Sim.ParticleSystem system, string algorithmName)
        {
            return GenerateSystem(system, algorithmName, new object[0]);
        }

        /// <summary>
        /// Calls the specified generation method to initialize the
        /// given system with the given parameters.
        /// </summary>
        /// <param name="system">The system to initialize.</param>
        /// <param name="algorithmName">The string ID of the generation method.</param>
        /// <param name="parameters">The parameters to be forwarded to
        /// the generation method.</param>
        /// <returns><c>true</c> if and only if the generation method was
        /// executed successfully.</returns>
        public bool GenerateSystem(AS2.Sim.ParticleSystem system, string algorithmName, object[] parameters)
        {
            AlgorithmInfo info = FindAlgorithm(algorithmName);
            if (info == null)
            {
                return false;
            }

            try
            {
                // Fill in missing parameters with default values
                ParameterInfo[] parameterList = info.generateMethod.GetParameters();
                if (parameters.Length < parameterList.Length)
                {
                    object[] newParams = new object[parameterList.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        newParams[i] = parameters[i];
                    for (int i = parameters.Length; i < parameterList.Length; i++)
                    {
                        if (!parameterList[i].HasDefaultValue)
                        {
                            Log.Warning("Parameter '" + parameterList[i].Name + "' of generation algorithm '" + algorithmName + "' does not have a default value.");
                        }
                        newParams[i] = parameterList[i].DefaultValue;
                    }
                    parameters = newParams;
                }

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

        /// <summary>
        /// Checks if the given generation method name is known.
        /// </summary>
        /// <param name="name">The string ID to check.</param>
        /// <returns><c>true</c> if and only if a generation method
        /// with the given name is registered.</returns>
        public bool IsAlgorithmKnown(string name)
        {
            AlgorithmInfo info = FindAlgorithm(name);
            return info != null;
        }
    }

} // namespace AS2
