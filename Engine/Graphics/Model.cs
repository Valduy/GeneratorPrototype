using Assimp;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Model
    {      
        public static readonly Model Empty = new(Enumerable.Empty<Mesh>());
        public static readonly Model Cube = Load("Content/Cube.obj");
        public static readonly Model Pyramid = Load("Content/Pyramid.obj");
        public static readonly Model Sphere = Load("Content/Sphere.obj");

        public readonly IReadOnlyList<Mesh> Meshes;
        public readonly Skeleton? Skeleton;

        public Model(Mesh meshes)
        {
            Skeleton = null;
            Meshes = new List<Mesh> { meshes };            
        }

        public Model(Skeleton? skeleton, Mesh meshes)
        {
            Skeleton = skeleton;
            Meshes = new List<Mesh> { meshes };            
        }

        public Model(IEnumerable<Mesh> meshes)
        {
            Skeleton = null;
            Meshes = meshes.ToList();
        }

        public Model(Skeleton? skeleton, IEnumerable<Mesh> meshes)
        {
            Skeleton = skeleton;
            Meshes = meshes.ToList();
        }

        public static Model Triangle(float side)
        {
            var leg = side / 2;
            
            var vertices = new List<Vertex>
            {
                new(new Vector3(-leg,  -leg, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.0f, 0.0f)),
                new(new Vector3( leg,  -leg, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(1.0f, 0.0f)),
                new(new Vector3( 0.0f, leg,  0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.5f, 1.0f)),
            };

            var indices = new List<int> {0, 1, 2};
            
            var mesh = new Mesh(vertices, indices);
            return new Model(new List<Mesh> {mesh});
        }

        public static Model Square(float side)
        {
            var half = side / 2;

            var vertices = new List<Vertex>()
            {
                new(new Vector3(-half, -half, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.0f, 0.0f)),
                new(new Vector3( half, -half, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(1.0f, 0.0f)),
                new(new Vector3( half,  half, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(1.0f, 1.0f)),
                new(new Vector3(-half,  half, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.0f, 1.0f)),
            };

            var indices = new List<int>
            {
                0, 1, 2,
                0, 2, 3,
            };

            var mesh = new Mesh(vertices, indices);
            return new Model(new List<Mesh> { mesh });
        }

        public static Model Rectangle(float width, float height)
        {
            var halfWidth = width / 2;
            var halfHeight = height / 2;

            var vertices = new List<Vertex>()
            {
                new(new Vector3(-halfWidth, -halfHeight, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.0f, 0.0f)),
                new(new Vector3( halfWidth, -halfHeight, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(1.0f, 0.0f)),
                new(new Vector3( halfWidth,  halfHeight, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(1.0f, 1.0f)),
                new(new Vector3(-halfWidth,  halfHeight, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.0f, 1.0f)),
            };

            var indices = new List<int>
            {
                0, 1, 2,
                0, 2, 3,
            };

            var mesh = new Mesh(vertices, indices);
            return new Model(new List<Mesh> { mesh });
        }

        public static Model FromPoly(IEnumerable<Vector2> points)
        {
            var pointsList = points.ToList();
            var vertices = pointsList.Select(p =>
                new Vertex(new Vector3(p.X, p.Y, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector2(0.0f, 0.0f)));
            var indices = Mathematics.Mathematics.Triangulate(pointsList);

            var mesh = new Mesh(vertices, indices);
            return new Model(new List<Mesh> { mesh });
        }

        public static Model Load(
            string path, 
            PostProcessSteps flags = PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder)
        {
            var aiImporter = new AssimpContext();
            var aiScene = aiImporter.ImportFile(path, flags);
            return ProcessNode(aiScene, aiScene.RootNode);
        }

        private static Model ProcessNode(Assimp.Scene aiScene, Assimp.Node aiRoot)
        {
            var meshes = new List<Mesh>();
            var namesToBones = new Dictionary<string, Bone>();
            ProcessNode(aiScene, aiRoot, meshes, namesToBones);

            if (namesToBones.Count > 1)
            {
                var skeleton = BuildSkeleton(aiScene, namesToBones);
                return new Model(skeleton, meshes);
            }
            
            return new Model(null, meshes);
        }

        private static void ProcessNode(
            Assimp.Scene aiScene, 
            Assimp.Node aiNode, 
            List<Mesh> meshes,
            Dictionary<string, Bone> namesToBones)
        {          
            for (int i = 0; i < aiNode.MeshCount; i++)
            {
                var mesh = aiScene.Meshes[aiNode.MeshIndices[i]];
                meshes.Add(ProcessMesh(mesh, namesToBones));
            }

            for (int i = 0; i < aiNode.ChildCount; i++)
            {
                ProcessNode(aiScene, aiNode.Children[i], meshes, namesToBones);
            }
        }

        private static Mesh ProcessMesh(
            Assimp.Mesh aiMesh,
            Dictionary<string, Bone> namesToBones)
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

            if (aiMesh.HasBones)
            {
                var weights = ProcessBones(aiMesh, vertices, namesToBones);
                return new Mesh(vertices, indices, weights);
            }

            return new Mesh(vertices, indices);
        }

        private static VertexWeights[] ProcessBones(
            Assimp.Mesh aiMesh, 
            List<Vertex> vertices, 
            Dictionary<string, Bone> namesToBones)
        {
            var verticesWeights = new VertexWeights[vertices.Count];
            var weights = new List<float>[vertices.Count];
            var indices = new List<int>[vertices.Count];
            int boneCounter = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                weights[i] = new List<float>();
                indices[i] = new List<int>();
            }

            for (int boneIndex = 0; boneIndex < aiMesh.Bones.Count; boneIndex++)
            {
                var aiBone = aiMesh.Bones[boneIndex];
                var boneId = -1;

                if (!namesToBones.TryGetValue(aiBone.Name, out var bone))
                {
                    var offset = ToMatrix4(aiBone.OffsetMatrix);
                    bone = new Bone(aiBone.Name, boneCounter, offset);
                    namesToBones[bone.Name] = bone;
                    boneId = bone.Id;
                    boneCounter += 1;
                }
                else
                {
                    boneId = bone.Id;
                }

                var aiWeights = aiMesh.Bones[boneIndex].VertexWeights;

                foreach (var aiWeight in aiWeights)
                {
                    weights[aiWeight.VertexID].Add(aiWeight.Weight);
                    indices[aiWeight.VertexID].Add(boneId);
                }
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                verticesWeights[i] = new VertexWeights(weights[i], indices[i]);
            }

            return verticesWeights;
        }

        private static Skeleton BuildSkeleton(
            Assimp.Scene aiScene,
            Dictionary<string, Bone> namesToBones)
        {
            // Not shure about offset
            //var root = new Bone("Root", namesToBones.Count, Matrix4.Identity);
            var notConnectedBones = new HashSet<Bone>(namesToBones.Values);
            int boneCounter = namesToBones.Count;

            while (notConnectedBones.Any())
            {
                var node = notConnectedBones.First();                
                var aiNode = FindNodeWithName(aiScene.RootNode, node.Name)!;
                aiNode = aiNode.Parent;

                while (aiNode.Name != "RootNode")
                {                
                    if (!namesToBones.TryGetValue(aiNode.Name, out var parent))
                    {
                        parent = new Bone(aiNode.Name, boneCounter, Matrix4.Identity);
                        namesToBones[aiNode.Name] = parent;
                        boneCounter += 1;
                    }

                    node.Parent = parent;
                    node.Parent.AddChildren(node);
                    notConnectedBones.Remove(node);
                    node = parent;
                    aiNode = aiNode.Parent;
                }

                notConnectedBones.Remove(node);
            }

            return new Skeleton(namesToBones.Values.First(b => b.Parent == null));
        }

        private static Assimp.Node? FindNodeWithName(Assimp.Node aiNode, string name)
        {
            if (aiNode.Name == name)
            {
                return aiNode;
            }

            for(int i = 0; i < aiNode.ChildCount; i++)
            {
                var result = FindNodeWithName(aiNode.Children[i], name);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Vector3 ToVector3(Vector3D vector)
            => new(vector.X, vector.Y, vector.Z);

        private static Matrix4 ToMatrix4(Matrix4x4 matrix) => new(
                matrix.A1, matrix.A2, matrix.A3, matrix.A4,
                matrix.B1, matrix.B2, matrix.B3, matrix.B4,
                matrix.C1, matrix.C2, matrix.C3, matrix.C4,
                matrix.D1, matrix.D2, matrix.D3, matrix.D4);
    }
}
