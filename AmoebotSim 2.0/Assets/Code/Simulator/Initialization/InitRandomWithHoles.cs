
public class InitRandomWithHoles : InitializationMethod
{
    public InitRandomWithHoles(ParticleSystem system) : base(system)
    {

    }

    public static new string Name { get { return "Random With Holes"; } }

    public void Generate(int numParticles = 50, float holeProb = 0.3f, Initialization.Chirality chirality = Initialization.Chirality.Random, Initialization.Compass compassDir = Initialization.Compass.Random)
    {
        GenerateRandomWithHoles(numParticles, holeProb, chirality, compassDir);
    }
}
