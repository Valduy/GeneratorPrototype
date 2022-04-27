using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Material
    {
        public Vector3 Ambient = new(0.25f);
        public Vector3 Diffuse = new(0.4f);
        public Vector3 Specular = new(0.0f);
        public float Shininess = 10.0f;
    }
}
