using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using OpenTK.Mathematics;
using System.Drawing;

namespace TriangulatedTopology.Helpers
{
    public static class PrimitivesHelper
    {
        public static Model RingModel = Model.Load("Content/Models/Ring.obj");

        public static GameObject InstantiateCube(
            this Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color)
        {
            return engine.InstantiateCube(position, rotation, scale, color.RgbaToVector3());
        }

        public static GameObject InstantiateCube(
            this Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Vector3 color)
        {
            var cube = engine.CreateCube(position, Quaternion.Identity, scale);
            var renderer = cube.Get<MaterialRenderComponent>();
            renderer!.Material.Color = color;
            cube.Rotation = rotation;
            return cube;
        }

        public static GameObject InstantiateSphere(
            this Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color)
        {
            return engine.InstantiateSphere(position, rotation, scale, color.RgbaToVector3());
        }

        public static GameObject InstantiateSphere(
            this Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Vector3 color)
        {
            var cube = engine.CreateSphere(position, rotation, scale);
            var renderer = cube.Get<MaterialRenderComponent>();
            renderer!.Material.Color = color;
            cube.Rotation = rotation;
            return cube;
        }

        public static GameObject InstantiateRing(
            this Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color)
        {
            return engine.InstantiateRing(position, rotation, scale, color.RgbaToVector3());
        }

        public static GameObject InstantiateRing(
            this Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Vector3 color)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer!.Model = RingModel;
            renderer!.Material.Color = color;
            go.Position = position;
            go.Rotation = rotation;
            go.Scale = scale;
            return go;
        }
    }
}
