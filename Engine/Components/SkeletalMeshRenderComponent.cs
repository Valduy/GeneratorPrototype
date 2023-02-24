using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace GameEngine.Components
{
    public class SkeletalMeshRenderComponent : MeshRenderComponent
    {
        [StructLayout(LayoutKind.Explicit, Size = 64)]
        private struct SkeletalVertex
        {
            [FieldOffset(0)]
            public Vector3 Position;
            [FieldOffset(12)]
            public Vector3 Normal;
            [FieldOffset(24)]
            public Vector2 TextureCoords;
            [FieldOffset(32)]
            public Vector4 Weights;
            [FieldOffset(48)]
            public Vector4i BonesIds;
        }

        private const string VertexShaderPath = "Shaders/Skeletal.vert";
        private const string FragmentShaderPath = "Shaders/Material.frag";

        public Texture Texture { get; set; } = Texture.Default;
        public Material Material { get; set; } = new();

        public SkeletalMeshRenderComponent()
            : base(VertexShaderPath, FragmentShaderPath)
        { }

        protected override MeshBuffers DescribeLayout(Mesh mesh)
        {
            SkeletalVertex[] vertices = GetVertices(mesh);
            int[] indices = mesh.Indices.ToArray();
            var meshBuffers = new MeshBuffers();
            int stride = Marshal.SizeOf<SkeletalVertex>();

            meshBuffers.VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(meshBuffers.VertexArrayObject);

            meshBuffers.VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, meshBuffers.VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * stride, vertices, BufferUsageHint.StaticDraw);           

            meshBuffers.VertexElementObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, meshBuffers.VertexElementObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            var positionLocation = Shader.GetAttribLocation("vertexPosition");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, stride, 0);

            var normalLocation = Shader.GetAttribLocation("vertexNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, stride, 12);

            var textureLocation = Shader.GetAttribLocation("vertexTextureCoord");
            GL.EnableVertexAttribArray(textureLocation);
            GL.VertexAttribPointer(textureLocation, 2, VertexAttribPointerType.Float, false, stride, 24);

            var weightsLocation = Shader.GetAttribLocation("weights");
            GL.EnableVertexAttribArray(weightsLocation);
            GL.VertexAttribPointer(weightsLocation, 4, VertexAttribPointerType.Float, false, stride, 32);

            var bonesLocation = Shader.GetAttribLocation("bonesIds");
            GL.EnableVertexAttribArray(bonesLocation);
            GL.VertexAttribPointer(bonesLocation, 4, VertexAttribPointerType.Int, false, stride, 48);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            return meshBuffers;
        }

        protected override void SetupShader()
        {
            Shader.SetMatrix4("transform.model", GameObject!.GetModelMatrix());
            Shader.SetMatrix4("transform.view", GameObject!.Engine.Camera.GetViewMatrix());
            Shader.SetMatrix4("transform.projection", GameObject!.Engine.Camera.GetProjectionMatrix());

            var bonesMatrices = new Matrix4[Model.Skeleton!.Count];

            foreach (var bone in Model.Skeleton!)
            {
                bonesMatrices[bone.Id] = bone.GetBoneMatrix();
            }

            Shader.SetMatrices4("bonesMatrices[0]", bonesMatrices);

            Texture.Use(TextureUnit.Texture0);
            Shader.SetVector3("viewPosition", GameObject!.Engine.Camera.Position);

            Shader.SetVector3("material.color", Material.Color);
            Shader.SetFloat("material.ambient", Material.Ambient);
            Shader.SetFloat("material.shininess", Material.Shininess);
            Shader.SetFloat("material.specular", Material.Specular);

            Shader.SetVector3("light.position", GameObject!.Engine.Light.Position);
            Shader.SetVector3("light.color", GameObject!.Engine.Light.Color);
        }

        private SkeletalVertex[] GetVertices(Mesh mesh)
        {
            var vertices = new SkeletalVertex[mesh.Vertices.Count];

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                vertices[i] = new SkeletalVertex()
                {
                    Position =mesh.Vertices[i].Position,
                    Normal = mesh.Vertices[i].Normal,
                    TextureCoords = mesh.Vertices[i].TextureCoords,
                    Weights = ToVector4(mesh.Weights[i].Weights),
                    BonesIds = ToVector4i(mesh.Weights[i].Bones)                    
                };
            }

            return vertices;
        }

        private static Vector4i ToVector4i(IReadOnlyList<int> collection)
        {
            return new Vector4i(collection[0], collection[1], collection[2], collection[3]);
        }

        private static Vector4 ToVector4(IReadOnlyList<float> collection)
        {
            return new Vector4(collection[0], collection[1], collection[2], collection[3]);
        }
    }
}
