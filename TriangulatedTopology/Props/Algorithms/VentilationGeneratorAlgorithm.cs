using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using Graph;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;
using TriangulatedTopology.Helpers;

namespace TriangulatedTopology.Props.Algorithms
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
            float epsilon = 0.01f;
            float side = 0.55f;
            float extrusion = 0.6f;

            var nodes = net.ToList();
            var (points, joints) = GetPoints(nodes, side, extrusion);
            InstantiateJoints(engine, points, joints);

            foreach(var joint in joints)
            {
                var position = joint.Position;
                var direction = joint.Forward;

                var forwardRotation = Quaternion.Identity;
                var backwardRotation = Quaternion.Identity;

                forwardRotation = Mathematics.FromToRotation(Vector3.UnitY, direction);
                forwardRotation *= Mathematics.FromToRotation(Vector3.UnitX, joint.Up);

                if (Mathematics.ApproximatelyEqualEpsilon(MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)), 1.0f, epsilon))
                {
                    var axis = Vector3.Transform(Vector3.UnitX, forwardRotation);
                    backwardRotation = Quaternion.FromAxisAngle(axis, MathHelper.Pi) * forwardRotation;              
                }
                else
                {
                    backwardRotation = Mathematics.FromToRotation(Vector3.UnitY, -direction);
                    backwardRotation *= Mathematics.FromToRotation(Vector3.UnitX, joint.Up);                    
                }

                InstantiateVentilationJoint(engine, position, forwardRotation);
                InstantiateVentilationJoint(engine, position, backwardRotation);
            }

            var model = MeshGenerator.GenerateTubeFromSpline(points, side);

            var go = engine.CreateGameObject();
            var render = go.Add<MaterialRenderComponent>();
            render.Model = model;
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
            float epsilon = 0.01f;

            {
                var first = points[0];
                var rotation = Mathematics.FromToRotation(Vector3.UnitY, first.Forward);
                var axis = Vector3.Transform(Vector3.UnitX, rotation);
                rotation = Mathematics.FromToRotation(axis, first.Up) * rotation;
                InstantiateVentilationJoint(engine, first.Position, rotation);
            }

            {
                var last = points[points.Count - 1];
                var rotation = Mathematics.FromToRotation(Vector3.UnitY, -last.Forward);
                var axis = Vector3.Transform(Vector3.UnitX, rotation);
                rotation = Mathematics.FromToRotation(axis, last.Up) * rotation;
                InstantiateVentilationJoint(engine, last.Position, rotation);
            }

            foreach (var joint in joints)
            {
                var position = joint.Position;
                var direction = joint.Forward;

                var forwardRotation = Quaternion.Identity;
                var backwardRotation = Quaternion.Identity;

                forwardRotation = Mathematics.FromToRotation(Vector3.UnitY, direction);
                forwardRotation *= Mathematics.FromToRotation(Vector3.UnitX, joint.Up);

                if (Mathematics.ApproximatelyEqualEpsilon(MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)), 1.0f, epsilon))
                {
                    var axis = Vector3.Transform(Vector3.UnitX, forwardRotation);
                    backwardRotation = Quaternion.FromAxisAngle(axis, MathHelper.Pi) * forwardRotation;
                }
                else
                {
                    backwardRotation = Mathematics.FromToRotation(Vector3.UnitY, -direction);
                    backwardRotation *= Mathematics.FromToRotation(Vector3.UnitX, joint.Up);
                }

                InstantiateVentilationJoint(engine, position, forwardRotation);
                InstantiateVentilationJoint(engine, position, backwardRotation);
            }
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
