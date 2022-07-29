using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Material
    {
        public Vector3 Color = new(1.0f);
        public float Ambient = 0.1f;
        public float Shininess = 32.0f;
        public float Specular = 0.5f;
    }
}
