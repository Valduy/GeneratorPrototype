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

        public static bool operator== (Vertex lhs, Vertex rhs)
        {
            return lhs.Position == rhs.Position
                && lhs.TextureCoords == rhs.TextureCoords
                && lhs.Normal == rhs.Normal;
        }

        public static bool operator!= (Vertex lhs, Vertex rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Normal.GetHashCode() ^ TextureCoords.GetHashCode();
        }

        public override bool Equals([NotNullWhen(true)] object? other)
        {            
            if (other is not Vertex vertex)
            {
                return false;
            }

            return Equals(vertex);
        }

        private bool Equals(Vertex other)
        {
            return Position.Equals(other.Position) 
                && Normal.Equals(other.Normal)
                && TextureCoords.Equals(other.TextureCoords);
        }
    }
}
