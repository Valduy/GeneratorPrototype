using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public struct Vertex
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly Vector2 TextureCoords;

        public Vertex(Vector3 position, Vector3 normal, Vector2 textureCoords)
        {
            Position = position;
            Normal = normal;
            TextureCoords = textureCoords;
        }
    }
}
