using OpenTK.Mathematics;
using System.Reflection.Metadata;

namespace GameEngine.Graphics
{
    public class Bone
    {
        private Bone? _parent;
        private List<Bone> _children = new();

        public readonly string Name;
        public readonly int Id;
        public readonly Matrix4 Offset;

        public Bone? Parent
        {
            get => _parent;
            internal set => _parent = value;
        }

        public IReadOnlyList<Bone> Children => _children;
        public Vector3 Position = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Scale = Vector3.One;       

        public Bone(string name, int id, Matrix4 offset) 
        { 
            Name = name; 
            Id = id; 
            Offset = offset; 
        }

        public Matrix4 GetBoneMatrix()
        {
            var model = Matrix4.Identity;
            model *= Matrix4.CreateScale(Scale);
            model *= Matrix4.CreateFromQuaternion(Rotation);
            model *= Matrix4.CreateTranslation(Position);

            if (Parent != null)
            {
                model *= Parent.GetBoneMatrix();
            }

            return model;
        }

        internal void AddChildren(Bone bone)
        {
            _children.Add(bone);
        }
    }
}
