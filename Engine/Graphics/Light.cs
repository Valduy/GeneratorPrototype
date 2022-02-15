using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Light
    {
        public Vector3 Position = new(0.0f);
        public Vector3 Ambient = new(0.2f);
        public Vector3 Diffuse = new(0.5f);
        public Vector3 Specular = new(1.0f);
    }
}
