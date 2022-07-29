using Assimp;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Model
    {
        public static readonly Model Empty = new(Enumerable.Empty<Mesh>());
        public static readonly Model Cube = Load("Content/Cube.obj");
        public static readonly Model Pyramid = Load("Content/Pyramid.obj");

        public readonly IReadOnlyList<Mesh> Meshes;

        public Model(IEnumerable<Mesh> meshes)
        {
            Meshes = meshes.ToList();
        }

        public static Model Load(string path)
        {
            var aiImporter = new AssimpContext();
            Scene aiScene = aiImporter.ImportFile(path,
                PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);

            return ProcessNode(aiScene, aiScene.RootNode);
        }

        private static Model ProcessNode(Scene aiScene, Node aiRoot)
        {
            var meshes = new List<Mesh>();
            ProcessNode(aiScene, aiRoot, meshes);
            return new Model(meshes);
        }

        private static void ProcessNode(Scene aiScene, Node aiNode, List<Mesh> meshes)
        {
            for (int i = 0; i < aiNode.MeshCount; i++)
            {
                var mesh = aiScene.Meshes[aiNode.MeshIndices[i]];
                meshes.Add(ProcessMesh(mesh));
            }

            for (int i = 0; i < aiNode.ChildCount; i++)
            {
                ProcessNode(aiScene, aiNode.Children[i], meshes);
            }
        }

        private static Mesh ProcessMesh(Assimp.Mesh aiMesh)
        {
            var vertices = new List<Vertex>();
            var indices = new List<int>();

            for (int i = 0; i < aiMesh.VertexCount; i++)
            {
                var position = ToVector3(aiMesh.Vertices[i]);
                var normal = ToVector3(aiMesh.Normals[i]);
                var textureCoords = aiMesh.HasTextureCoords(0)
                    ? ToVector3(aiMesh.TextureCoordinateChannels[0][i]).Xy
                    : Vector2.Zero;

                var vertex = new Vertex(position, normal, textureCoords);
                vertices.Add(vertex);
            }

            for (int i = 0; i < aiMesh.FaceCount; i++)
            {
                var aiFace = aiMesh.Faces[i];

                for (int j = 0; j < aiFace.IndexCount; j++)
                {
                    indices.Add(aiFace.Indices[j]);
                }
            }

            return new Mesh(vertices, indices);
        }

        private static Vector3 ToVector3(Vector3D vector)
            => new(vector.X, vector.Y, vector.Z);
    }
}
