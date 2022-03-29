using GameEngine.Graphics;
using ObjLoader.Loader.Loaders;

namespace PipesDemo.Utils
{
    public class DirectoryStreamProvider : IMaterialStreamProvider
    {
        public string Directory { get; }

        public DirectoryStreamProvider(string directory)
        {
            Directory = directory;
        }

        public Stream Open(string materialFilePath)
        {
            return File.Open($"{Directory}/{materialFilePath}", FileMode.Open, FileAccess.Read); ;
        }
    }

    public static class ObjLoader
    {
        public static Mesh Load(string directory, string fileName)
        {
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create(new DirectoryStreamProvider(directory));
            using var fileStream = File.OpenRead($"{directory}/{fileName}");
            var result = objLoader.Load(fileStream);

            return new Mesh(EnumerateLoadResult(result).ToArray());
        }

        private static IEnumerable<float> EnumerateLoadResult(LoadResult result)
        {
            foreach (var face in result.Groups[0].Faces)
            {
                for (int i = 0; i < face.Count; i++)
                {
                    var faceVertex = face[i];
                    var vertex = result.Vertices[faceVertex.VertexIndex - 1];
                    var normal = result.Normals[faceVertex.NormalIndex - 1];

                    yield return vertex.X;
                    yield return vertex.Y;
                    yield return vertex.Z;
                    yield return normal.X;
                    yield return normal.Y;
                    yield return normal.Z;
                }
            }
        }
    }
}
