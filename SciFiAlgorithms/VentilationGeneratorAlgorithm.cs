using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using Graph;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;
using UVWfc.Helpers;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Props.Algorithms;

namespace SciFiAlgorithms
{
    public class VentilationGeneratorAlgorithm : INetAlgorithm
    {
        private static Model VentilationJoint = Model.Load("Content/Models/VentilationJoint.fbx");

        public static readonly Color VentilationColor = Color.FromArgb(255, 60, 246);
        private static Material VentilationMaterial = new Material
        {
            Color = new Vector3(0.50754f, 0.50754f, 0.50754f),
            Ambient = 0.19225f,
            Specular = 0.508273f,
            Shininess = 51.2f,
        };

        public readonly float ZeroLevel;

        public VentilationGeneratorAlgorithm(float zeroLevel)
        {
            ZeroLevel = zeroLevel;
        }

        public bool CanProcessRule(Rule rule)
        {
            return rule.Logical.Enumerate().Any(c => c.IsSame(VentilationColor));
        }

        public bool[] GetRuleConnections(Rule rule)
        {
            var connections = new bool[4];

            for (int i = 0; i < Cell.NeighboursCount; i++)
            {
                var side = rule[i];
                connections[i] = side[1].IsSame(VentilationColor) && side[2].IsSame(VentilationColor);
            }

            return connections;
        }

        public void ProcessNet(Engine engine, Net<LogicalNode> net)
        {
            float side = 0.55f;
            float extrusion = 0.6f;

            var nodes = net.ToList();
            var (points, joints) = GetPoints(nodes, side, extrusion);
            InstantiateJoints(engine, points, joints);

            var go = engine.CreateGameObject();
            var render = go.Add<MaterialRenderComponent>();
            render.Model = MeshGenerator.GenerateTubeFromSpline(points, side);
            render.Material = VentilationMaterial;
        }

        private (List<SplineVertex> Points, List<SplineVertex> Joints) GetPoints(
            List<LogicalNode> nodes,
            float radius,
            float extrusion)
        {
            var points = new List<SplineVertex>();
            var joints = new List<SplineVertex>();
            var segments = new List<List<SplineVertex>>();
            int resolution = 3;

            for (int i = 1; i < nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var next = nodes[i];

                var jointPoints = SplinesGenerationHelper.CreatePointsAroundJoint(
                    prev, next, ZeroLevel, extrusion, radius, resolution);

                segments.Add(jointPoints);
                joints.Add(jointPoints[jointPoints.Count / 2]);
            }

            points.AddRange(SplinesGenerationHelper.CreateBegin(
                nodes[0], nodes[1], ZeroLevel, extrusion, radius, resolution));

            for (int i = 1; i < segments.Count; i++)
            {
                var prev = segments[i - 1];
                var next = segments[i];

                var inner = CreateInnerPoints(prev[prev.Count - 1], next[0], resolution);
                points.AddRange(prev.Concat(inner));
            }

            points.AddRange(segments[segments.Count - 1]);
            points.AddRange(SplinesGenerationHelper.CreateEnd(
                nodes[nodes.Count - 2], nodes[nodes.Count - 1], ZeroLevel, extrusion, radius, resolution));
            return (points, joints);
        }

        private static List<SplineVertex> CreateInnerPoints(SplineVertex p1, SplineVertex p2, int resolution)
        {
            float epsilon = 0.01f;
            var points = new List<SplineVertex>();
            var t1 = 2 * p1.Forward;
            var t2 = 2 * p2.Forward;

            if (Mathematics.TryGetIntersactionPoint(p1.Position, p1.Forward, p2.Position, p2.Forward, epsilon, out var p))
            {
                t1 = 2 * (p - p1.Position);
                t2 = 2 * (p2.Position - p);
            }

            for (int i = 1; i < resolution; i++)
            {
                float t = (float)i / resolution;
                var position = Curves.Hermite(p1.Position, p2.Position, t1, t2, t);
                var normal = Vector3.Normalize(Vector3.Lerp(p1.Up, p2.Up, t));
                var direction = Vector3.Normalize(Vector3.Lerp(p1.Forward, p2.Forward, t));
                points.Add(new SplineVertex(position, normal, direction));
            }

            return points;
        }

        private static void InstantiateJoints(Engine engine, List<SplineVertex> points, List<SplineVertex> joints)
        {
            var first = points[0];
            var last = points[points.Count - 1];

            InstantiateVentilationEnding(engine, first.Position, first.Forward, -first.Up);
            InstantiateVentilationEnding(engine, last.Position, -last.Forward, -last.Up);

            foreach (var joint in joints)
            {
                var position = joint.Position;
                var normal = joint.Up;
                var direction = joint.Forward;

                InstantiateVentilationJoint(engine, position, normal, direction);
                InstantiateVentilationJoint(engine, position, normal, -direction);
            }
        }

        private static void InstantiateVentilationEnding(
            Engine engine,
            Vector3 position,
            Vector3 normal,
            Vector3 direction)
        {
            float epsilon = 0.01f;
            var right = Vector3.Cross(normal, direction);

            var rotation = Mathematics.ApproximatelyEqualEpsilon(Vector3.UnitY, -normal, epsilon)
                ? Quaternion.FromAxisAngle(right, MathHelper.Pi)
                : Mathematics.FromToRotation(Vector3.UnitY, normal);

            var xAxis = Vector3.Transform(Vector3.UnitX, rotation);
            var zAxis = Vector3.Transform(Vector3.UnitZ, rotation);
            var crossWithXAxis = Vector3.Cross(xAxis, direction);
            var crossWithZAxis = Vector3.Cross(zAxis, direction);

            if (crossWithXAxis.Length > crossWithZAxis.Length)
            {
                var rotationAxis = crossWithXAxis.Normalized();
                rotation = Mathematics.FromToRotation(rotationAxis, xAxis, direction) * rotation;
            }
            else
            {
                var rotationAxis = crossWithZAxis.Normalized();
                rotation = Mathematics.FromToRotation(rotationAxis, zAxis, direction) * rotation;
            }

            InstantiateVentilationJoint(engine, position, rotation);
        }

        private static void InstantiateVentilationJoint(
            Engine engine,
            Vector3 position,
            Vector3 normal,
            Vector3 direction)
        {
            float epsilon = 0.01f;

            var right = Vector3.Cross(normal, direction);
            var rotation = Mathematics.ApproximatelyEqualEpsilon(Vector3.UnitY, -direction, epsilon)
                ? Quaternion.FromAxisAngle(right, MathHelper.Pi)
                : Mathematics.FromToRotation(Vector3.UnitY, direction);

            var xAxis = Vector3.Transform(Vector3.UnitX, rotation);
            var zAxis = Vector3.Transform(Vector3.UnitZ, rotation);
            var crossWithXAxis = Vector3.Cross(xAxis, normal);
            var crossWithZAxis = Vector3.Cross(zAxis, normal);

            if (crossWithXAxis.Length > crossWithZAxis.Length)
            {
                var rotationAxis = crossWithXAxis.Normalized();
                rotation = Mathematics.FromToRotation(rotationAxis, xAxis, normal) * rotation;
            }
            else
            {
                var rotationAxis = crossWithZAxis.Normalized();
                rotation = Mathematics.FromToRotation(rotationAxis, zAxis, normal) * rotation;
            }

            InstantiateVentilationJoint(engine, position, rotation);
        }

        private static GameObject InstantiateVentilationJoint(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = VentilationJoint;
            renderer.Material = VentilationMaterial;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }
    }
}
