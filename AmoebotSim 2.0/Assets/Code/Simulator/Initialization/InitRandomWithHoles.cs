
namespace AS2
{

    public class InitRandomWithHoles : InitializationMethod
    {
        public InitRandomWithHoles(AS2.Sim.ParticleSystem system) : base(system)
        {

        }

        public void Generate(int numParticles = 50, float holeProb = 0.3f, Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compassDir = Initialization.Compass.E)
        {
            GenerateRandomWithHoles(numParticles, holeProb, chirality, compassDir);
        }
    }

} // namespace AS2
