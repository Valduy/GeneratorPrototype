namespace GameEngine.Graphics
{
    public class Model
    {
        public readonly IReadOnlyList<Mesh> Meshes;

        public Model(IEnumerable<Mesh> meshes)
        {
            Meshes = meshes.ToList();
        }
    }
}
