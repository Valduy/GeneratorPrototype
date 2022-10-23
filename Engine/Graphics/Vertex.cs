using OpenTK.Mathematics;
using System.Diagnostics.CodeAnalysis;

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

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Normal.GetHashCode() ^ TextureCoords.GetHashCode();
        }

        public override bool Equals([NotNullWhen(true)] object? other)
        {
            if (!(other is Vertex))
            {
                return false;
            }

            return Equals((Vertex)other);
        }

        public bool Equals(Vertex other)
        {
            return Position.Equals(other.Position) 
                && Normal.Equals(other.Normal)
                && TextureCoords.Equals(other.TextureCoords);
        }

    }
}
