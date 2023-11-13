using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace AS2
{

    /// <summary>
    /// Singleton class that uses reflection to manage all particle
    /// algorithms, providing an interface to access their names,
    /// generation methods and parameters.
    /// </summary>
    public class AlgorithmManager
    {
        /// <summary>
        /// A list of algorithm names that should be excluded from
        /// the system. These algorithms will be skipped during
        /// algorithm discovery and will not appear in the algorithm
        /// selection dropdown menu.
        /// </summary>
        private readonly List<string> excludedAlgorithms = new List<string>(new string[] {
            // Example: AS2.Algos.ExpandedCircuitTest.ExpandedCircuitTestParticle.Name
        });

        // Property names
        private static readonly string Name_Property = "Name";
        private static readonly string Generator_Property = "GenerationMethod";
        private static readonly string Init_Method = "Init";

        // Singleton
        private static AlgorithmManager instance = new AlgorithmManager();
        /// <summary>
        /// The singleton instance of this class.
        /// </summary>
        public static AlgorithmManager Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Represents a single particle algorithm and contains all
        /// information needed to create particles for it.
        /// </summary>
        private class AlgorithmInfo
        {
            /// <summary>
            /// The string ID of the algorithm.
            /// </summary>
            public string name;
            /// <summary>
            /// The type of the algorithm class.
            /// </summary>
            public Type type;
            /// <summary>
            /// Reference to the algorithm's constructor.
            /// This must be the constructor that gets a single
            /// parameter of type <see cref="Particle"/>.
            /// </summary>
            public ConstructorInfo ctor;
            /// <summary>
            /// Reference to the algorithm's <c>Init</c> method.
            /// May be <c>null</c> if the algorithm does not implement
            /// this method.
            /// </summary>
            public MethodInfo initMethod;
            /// <summary>
            /// The full name of the algorithm's generator class.
            /// This is a class that inherits from
            /// <see cref="InitializationMethod"/> and that is used to
            /// initialize the particle system. Has a default value if
            /// the algorithm does not specify a method.
            /// </summary>
            public string generationMethod;
            /// <summary>
            /// The display names, tooltips and method references of the
            /// status info methods defined by the algorithm.
            /// </summary>
            public Tuple<string, string, MethodInfo>[] statusInfoMethods;

            public AlgorithmInfo(string name, Type type, ConstructorInfo ctor, MethodInfo initMethod, string generationMethod, Tuple<string, string, MethodInfo>[] statusInfoMethods)
            {
                this.name = name;
                this.type = type;
                this.ctor = ctor;
                this.initMethod = initMethod;
                this.generationMethod = generationMethod;
                this.statusInfoMethods = statusInfoMethods;
            }
        }

        private Dictionary<string, AlgorithmInfo> algorithms;

        private AlgorithmManager()
        {
            // Find all ParticleAlgorithm subtypes
            Type baseAlgoType = typeof(ParticleAlgorithm);
            Assembly baseAlgoAssembly = baseAlgoType.Assembly;
            IEnumerable<Type> subclasses = baseAlgoAssembly.GetTypes().Where(t => t.IsSubclassOf(baseAlgoType));

            string defaultGenerator = (string)baseAlgoType.GetProperty(Generator_Property).GetValue(null);

            algorithms = new Dictionary<string, AlgorithmInfo>();

            foreach (Type algoType in subclasses)
            {
                PropertyInfo nameProp = algoType.GetProperty(Name_Property);
                PropertyInfo generatorProp = algoType.GetProperty(Generator_Property);

                // Get the name
                string name;
                if (nameProp == null)
                {
                    name = algoType.FullName;
                }
                else
                {
                    name = (string)nameProp.GetValue(null);
                }

                // Check if excluded
                if (excludedAlgorithms.Contains(name))
                    continue;

                // Check for duplicates
                if (algorithms.ContainsKey(name))
                {
                    Debug.LogWarning("ParticleAlgorithm with name '" + name + "' already exists, cannot load algorithm");
                    continue;
                }

                // Get the generator
                string generator = defaultGenerator;
                if (generatorProp != null)
                    generator = (string)generatorProp.GetValue(null);

                // Get the constructor
                // It must have a single parameter of type Particle
                ConstructorInfo ctor = algoType.GetConstructor(new Type[] { typeof(Particle) });
                if (ctor == null)
                {
                    Debug.LogWarning("ParticleAlgorithm with name '" + name + "' does not declare a constructor with Particle parameter, cannot load algorithm");
                    continue;
                }

                // Get the Init method
                MethodInfo initMethod = algoType.GetMethod(Init_Method);

                // Get the status info methods
                List<Tuple<string, string, MethodInfo>> statusInfoMethods = new List<Tuple<string, string, MethodInfo>>();
                foreach (MethodInfo mi in algoType.GetMethods())
                {
                    // Must be static
                    if (!mi.IsStatic)
                        continue;

                    // Must have a StatusInfo attribute
                    StatusInfoAttribute attr = mi.GetCustomAttribute<StatusInfoAttribute>();
                    if (attr is null)
                        continue;

                    // Must have a ParticleSystem and a Particle parameter
                    ParameterInfo[] parameters = mi.GetParameters();
                    if (parameters.Length != 2 ||
                        !parameters[0].ParameterType.Equals(typeof(AS2.Sim.ParticleSystem)) ||
                        !parameters[1].ParameterType.Equals(typeof(Particle)))
                        continue;

                    statusInfoMethods.Add(new Tuple<string, string, MethodInfo>(attr.name, attr.tooltip, mi));
                }

                // Add record for the new algorithm
                algorithms[name] = new AlgorithmInfo(name, algoType, ctor, initMethod, generator, statusInfoMethods.ToArray());
            }
        }

        /// <summary>
        /// Tries to get the algorithm with the given name.
        /// </summary>
        /// <param name="name">The string ID of the desired algorithm.</param>
        /// <returns>The record of the algorithm with name <paramref name="name"/>
        /// if it exists, otherwise <c>null</c>.</returns>
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
        /// Gets the name of the given algorithm's generation method.
        /// </summary>
        /// <param name="name">The string ID of the algorithm.</param>
        /// <returns>The name of the algorithm's generation method,
        /// if a record for this algorithm exists.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the algorithm is not known.
        /// </exception>
        public string GetAlgorithmGenerationMethod(string name)
        {
            AlgorithmInfo info = FindAlgorithm(name);
            if (info != null)
                return info.generationMethod;
            else
                throw new System.ArgumentException("Could not find algorithm");
        }

        /// <summary>
        /// Gets the parameters of the algorithm's <c>Init</c> method.
        /// </summary>
        /// <param name="name">The string ID of the algorithm.</param>
        /// <returns>An array containing the parameters of the
        /// algorithm's <c>Init</c> method, if the algorithm exists and
        /// it implements this method, otherwise <c>null</c>.</returns>
        public ParameterInfo[] GetInitParameters(string name)
        {
            AlgorithmInfo info = FindAlgorithm(name);
            if (info == null || info.initMethod == null)
                return null;

            return info.initMethod.GetParameters();
        }

        /// <summary>
        /// Creates a new instance of the given algorithm for the
        /// given particle.
        /// </summary>
        /// <param name="name">The string ID of the algorithm.</param>
        /// <param name="particle">The particle that shall be
        /// controlled by the algorithm.</param>
        /// <returns>A new instance of the specified algorithm.</returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the algorithm is not known.
        /// </exception>
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

        /// <summary>
        /// Initializes the given algorithm with the given parameters
        /// by calling its <c>Init</c> method.
        /// </summary>
        /// <param name="name">The string ID of the algorithm type.</param>
        /// <param name="algo">The algorithm instance to initialize.</param>
        /// <param name="parameters">The parameters to be passed to the
        /// <c>Init</c> method.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the algorithm is not known.
        /// </exception>
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

        /// <summary>
        /// Gets a list of all known algorithm names.
        /// </summary>
        /// <returns>A list containing the names of all algorithms.</returns>
        public List<string> GetAlgorithmNames()
        {
            return algorithms.Keys.ToList();
        }

        /// <summary>
        /// Checks if the given algorithm name is known.
        /// </summary>
        /// <param name="name">The string ID to check.</param>
        /// <returns><c>true</c> if and only if a particle algorithm
        /// with the given name is registered.</returns>
        public bool IsAlgorithmKnown(string name)
        {
            AlgorithmInfo info = FindAlgorithm(name);
            return info != null;
        }

        /// <summary>
        /// Gets the status info methods defined for the given algorithm.
        /// </summary>
        /// <param name="name">The string ID of the algorithm.</param>
        /// <returns>An array of tuples <c>(name, tooltip, methodInfo)</c>,
        /// where <c>name</c> is the display name of the status method,
        /// <c>tooltip</c> is the tooltip text to be displayed for the
        /// button and <c>methodInfo</c> is the method object obtained by
        /// reflection. The method must be called with two parameters,
        /// the first of type <see cref="Sim.ParticleSystem"/> and the
        /// second of type <see cref="Particle"/>.</returns>
        public Tuple<string, string, MethodInfo>[] GetStatusInfoMethods(string name)
        {
            AlgorithmInfo info = FindAlgorithm(name);
            if (info == null)
            {
                throw new System.ArgumentException("Could not find algorithm");
            }

            return info.statusInfoMethods;
        }
    }

} // namespace AS2
