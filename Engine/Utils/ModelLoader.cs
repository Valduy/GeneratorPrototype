using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using Mesh = GameEngine.Graphics.Mesh;

namespace GameEngine.Utils
{
    public static class ModelLoader
    {
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
                var position = aiMesh.Vertices[i].ToVector3();
                var normal = aiMesh.Normals[i].ToVector3();
                var textureCoords = aiMesh.HasTextureCoords(0)
                    ? aiMesh.TextureCoordinateChannels[0][i].ToVector3().Xy
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

        private static Vector3 ToVector3(this Vector3D vector) 
            => new(vector.X, vector.Y, vector.Z);
    }
}
