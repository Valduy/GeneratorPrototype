using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using Graph;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;
using TriangulatedTopology.Helpers;
using TriangulatedTopology.LevelGraph;

namespace TriangulatedTopology.Props.Algorithms
{
    public class PipesGeneratorAlgorithm : INetAlgorithm
    {
        public static readonly Color PipesColor = Color.FromArgb(255, 217, 0);

        private static Model PipeSupportModel = Model.Load("Content/Models/PipeSupport.fbx");

        private static Material PipeMaterial = new Material
        {
            Color = new Vector3(0.427451f, 0.470588f, 0.541176f),
            Ambient = 0.08f,
            Specular = 0.4f,
            Shininess = 9.84615f,
        };

        public readonly float ZeroLevel;

        public PipesGeneratorAlgorithm(float zeroLevel)
        {
            ZeroLevel = zeroLevel;
        }

        public bool CanProcessRule(Rule rule)
        {
            return rule.Logical.Enumerate().Any(c => c.IsSame(PipesColor));
        }

        public bool[] GetRuleConnections(Rule rule)
        {
            var connections = new bool[4];

            for (int i = 0; i < Cell.NeighboursCount; i++)
            {
                var side = rule[i];
                connections[i] = side[1].IsSame(PipesColor) && side[2].IsSame(PipesColor);
            }

            return connections;
        }

        public void ProcessNet(Engine engine, Net<LogicalNode> net)
        {
            int resolution = 32;            
            float radius = 0.28f;
            float extrusion = 0.5f;

            var nodes = net.ToList();
            var (points, joints) = GetPipePoints(nodes, radius, extrusion);
            var model = MeshGenerator.GenerateTubeFromSpline(points, resolution, radius);
            InstantiateJoints(engine, points, joints);

            var go = engine.CreateGameObject();
            var render = go.Add<MaterialRenderComponent>();
            render.Model = model;
            render.Material = PipeMaterial;
        }

        private (List<SplineVertex> Points, List<SplineVertex> Joints) GetPipePoints(
            List<LogicalNode> nodes, 
            float radius,
            float extrusion)
        {
            float epsilon = 0.01f;
            var points = new List<SplineVertex>();
            var joints = new List<SplineVertex>();
            var segments = new List<List<SplineVertex>>();
            int resolution = 20;

            for (int i = 1; i < nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var next = nodes[i];

                var jointPoints = SplinesGenerationHelper.CreatePointsAroundJoint(
                    prev, next, ZeroLevel, extrusion, radius, resolution);
                segments.Add(jointPoints);                

                var prevNormal = Mathematics.GetNormal(prev.Corners);
                var nextNormal = Mathematics.GetNormal(next.Corners);
                var cosa = Math.Clamp(Vector3.Dot(prevNormal, nextNormal), -1, 1);
                var acos = MathF.Acos(cosa);

                if (acos < MathHelper.PiOver3)
                {
                    joints.Add(jointPoints[jointPoints.Count / 2]);
                }
            }

            points.AddRange(SplinesGenerationHelper.CreateBegin(
                nodes[0], nodes[1], ZeroLevel, extrusion, radius, resolution));

            for (int i = 1; i < segments.Count; i++)
            {
                var prev = segments[i - 1];
                var next = segments[i];

                var inner = CreatePipeInnerPoints(prev[prev.Count - 1], next[0], resolution);
                points.AddRange(prev.Concat(inner));

                if (Mathematics.ApproximatelyEqualEpsilon(inner[0].Forward, inner[inner.Count - 1].Forward, epsilon))
                {
                    joints.Add(inner[inner.Count / 2]);
                }                
            }

            points.AddRange(segments[segments.Count - 1]);
            points.AddRange(SplinesGenerationHelper.CreateEnd(
                nodes[nodes.Count - 2], nodes[nodes.Count - 1], ZeroLevel, extrusion, radius, resolution));
            return (points, joints);
        }

        private static List<SplineVertex> CreatePipeInnerPoints(SplineVertex p1, SplineVertex p2, int resolution)
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
            {
                var first = points[0];
                var rotation = Mathematics.FromToRotation(Vector3.UnitY, first.Forward);
                InstantiatePipeJoint(engine, first.Position, rotation);
            }

            {
                var last = points[points.Count - 1];
                var rotation = Mathematics.FromToRotation(Vector3.UnitY, -last.Forward);
                InstantiatePipeJoint(engine, last.Position, rotation);
            }

            foreach (var joint in joints)
            {
                InstantiateDualPipeJoints(engine, joint.Up, joint.Position, joint.Forward);
            }
        }

        private static void InstantiateDualPipeJoints(Engine engine, Vector3 axis, Vector3 position, Vector3 direction)
        {
            float epsilon = 0.01f;

            if (Mathematics.ApproximatelyEqualEpsilon(MathF.Abs(Vector3.Dot(axis, Vector3.UnitY)), 1.0f, epsilon))
            {
                axis = Vector3.Normalize(Vector3.Cross(axis, direction));
            }

            var forwardRotation = Mathematics.FromToRotation(axis, Vector3.UnitY, direction);
            var backwardRotation = Quaternion.FromAxisAngle(axis, MathF.PI) * forwardRotation;
            InstantiatePipeJoint(engine, position, forwardRotation);
            InstantiatePipeJoint(engine, position, backwardRotation);
        }

        private static GameObject InstantiatePipeJoint(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = PipeSupportModel;
            renderer.Material = PipeMaterial;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }
    }
}
