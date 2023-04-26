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
using UVWfc.Helpers;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Props.Algorithms;
using Material = GameEngine.Graphics.Material;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace SciFiAlgorithms
{
    public class WiresGeneratorAlgorithm : INetAlgorithm
    {
        private const float Trashold = 45.0f;

        private static readonly Color WireColor = Color.FromArgb(255, 0, 24);

        private static readonly Material WireMaterial = new Material
        {
            Color = new Vector3(0.4f, 0.7f, 0.4f),
            Ambient = 0.05f,
            Specular = 0.2f,
            Shininess = 10.0f,
        };

        private static readonly Material SourceMaterial = new Material
        {
            Color = new Vector3(0.93f, 0.93f, 0.93f),
            Ambient = 0.0f,
            Specular = 0.7f,
            Shininess = 32.0f,
        };

        private static Model WireSupportModel = Model.Load("Content/Models/WireSupport.fbx");
        private static Model SourceModel = Model.Load("Content/Models/Source.fbx");
        private static Model MonitorModel = Model.Load("Content/Models/Monitor.fbx");

        private static Texture MonitorTexture = Texture.LoadFromFile("Content/Textures/Monitor.png");

        public readonly float ZeroLevel;

        public WiresGeneratorAlgorithm(float zeroLevel)
        {
            ZeroLevel = zeroLevel;
        }

        public bool CanProcessRule(Rule rule)
        {
            return rule.Logical.Enumerate().Any(c => c.IsSame(WireColor));
        }

        public bool[] GetRuleConnections(Rule rule)
        {
            var connections = new bool[4];

            for (int i = 0; i < Cell.NeighboursCount; i++)
            {
                var side = rule[i];
                connections[i] = side[1].IsSame(WireColor) && side[2].IsSame(WireColor);
            }

            return connections;
        }

        public void ProcessNet(Engine engine, Net<LogicalNode> net)
        {
            float extrusion = 0.09f;
            float radius = extrusion;
            float distanceBetweenLines = 2 * radius;
            int resolution = 32;
            int linesCount = 3;

            var nodes = net.ToList();
            var (pointsLines, joints) = GetPoints(nodes, extrusion, radius, distanceBetweenLines, linesCount);

            foreach (var line in pointsLines)
            {
                var model = MeshGenerator.GenerateTubeFromSpline(line, resolution, radius);

                var go = engine.CreateGameObject();
                var render = go.Add<MaterialRenderComponent>();
                render.Model = model;
                render.Material = WireMaterial;
            }

            var middleLine = pointsLines[pointsLines.Count / 2];
            var first = middleLine[0];
            var last = middleLine[middleLine.Count - 1];

            PlaceSupports(engine, joints);
            InstantiateWireEnding(engine, first.Position, first.Forward, -first.Up, radius);
            InstantiateWireEnding(engine, last.Position, -last.Forward, -last.Up, radius);
        }

        private (List<List<SplineVertex>> Points, List<SplineVertex> Joints) GetPoints(
            List<LogicalNode> nodes,
            float extrusion,
            float radius,
            float distanceBetweenLines,
            int linesCount)
        {
            var joints = new List<SplineVertex>();
            var pointsLines = new List<List<SplineVertex>>();
            var segmentsLines = new List<List<List<SplineVertex>>>();
            int resolution = 10;
            int half = linesCount / 2;

            for (int line = 0; line < linesCount; line++)
            {
                pointsLines.Add(new List<SplineVertex>());
                segmentsLines.Add(new List<List<SplineVertex>>());
            }

            for (int i = 1; i < nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var next = nodes[i];

                var jointPoints = SplinesGenerationHelper.CreatePointsAroundJoint(
                    prev, next, ZeroLevel, extrusion, radius, distanceBetweenLines, resolution, linesCount);

                for (int line = 0; line < linesCount; line++)
                {
                    segmentsLines[line].Add(jointPoints[line]);
                }

                var middlePoints = jointPoints[jointPoints.Count / 2];
                var prevNormal = Mathematics.GetNormal(prev.Corners);
                var nextNormal = Mathematics.GetNormal(next.Corners);
                var cosa = Math.Clamp(Vector3.Dot(prevNormal, nextNormal), -1, 1);
                var acos = MathF.Acos(cosa);

                if (acos < MathHelper.PiOver3)
                {
                    joints.Add(middlePoints[middlePoints.Count / 2]);
                }
            }

            var beginPoints = SplinesGenerationHelper.CreateBegin(
                nodes[0], 
                nodes[1], 
                ZeroLevel, 
                extrusion, 
                radius, 
                distanceBetweenLines, 
                resolution, 
                linesCount);

            for (int line = 0; line < linesCount; line++)
            {
                pointsLines[line].AddRange(beginPoints[line]);
            }

            for (int line = 0; line < linesCount; line++)
            {
                var segments = segmentsLines[line];
                var points = pointsLines[line];

                for (int i = 1; i < segments.Count; i++)
                {
                    var prev = segments[i - 1];
                    var next = segments[i];

                    var inner = CreateInnerPoints(prev[prev.Count - 1], next[0], resolution);
                    points.AddRange(prev.Concat(inner));

                    if (line == half)
                    {
                        var prevForward = inner[0].Forward;
                        var nextForward = inner[1].Forward;
                        var cosa = Math.Clamp(Vector3.Dot(prevForward, nextForward), -1, 1);
                        var acos = MathF.Acos(cosa);

                        if (acos < MathHelper.PiOver3)
                        {
                            joints.Add(inner[inner.Count / 2]);
                        }
                    }
                }

                points.AddRange(segments[segments.Count - 1]);
            }

            var endPoints = SplinesGenerationHelper.CreateEnd(
                nodes[nodes.Count - 2], 
                nodes[nodes.Count - 1], 
                ZeroLevel, 
                extrusion, 
                radius, 
                distanceBetweenLines, 
                resolution, 
                linesCount);

            for (int line = 0; line < linesCount; line++)
            {
                pointsLines[line].AddRange(endPoints[line]);
            }

            return (pointsLines, joints);
        }

        private static List<SplineVertex> CreateInnerPoints(SplineVertex p1, SplineVertex p2, int resolution)
        {
            float epsilon = 0.1f;
            var points = new List<SplineVertex>();
            var t1 = 2.0f * p1.Forward;
            var t2 = 2.0f * p2.Forward;

            if (Mathematics.TryGetIntersactionPoint(p1.Position, p1.Forward, p2.Position, p2.Forward, epsilon, out var p))
            {
                t1 = 2.0f * (p - p1.Position);
                t2 = 2.0f * (p2.Position - p);
            }

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                var position = Curves.Hermite(p1.Position, p2.Position, t1, t2, t);
                var normal = Vector3.Normalize(Vector3.Lerp(p1.Up, p2.Up, t));
                var direction = Vector3.Normalize(Vector3.Lerp(p1.Forward, p2.Forward, t));
                points.Add(new SplineVertex(position, normal, direction));
            }

            return points;
        }

        private static void PlaceSupports(Engine engine, List<SplineVertex> points)
        {
            float epsilon = 0.01f;

            for (int i = 0; i < points.Count; i += 1)
            {
                if (IsSplineVertexLieOnFloor(points[i]))
                {
                    continue;
                }

                var rotation = Mathematics.FromToRotation(Vector3.UnitY, points[i].Up);
                var forward = Vector3.Transform(Vector3.UnitZ, rotation);

                if (!Mathematics.ApproximatelyEqualEpsilon(forward, -points[i].Forward, epsilon))
                {
                    rotation = Mathematics.FromToRotation(forward, points[i].Forward) * rotation;
                }

                InstantiateWireSupport(engine, points[i].Position, rotation);
            }
        }

        private static bool IsSplineVertexLieOnFloor(SplineVertex vertex)
        {
            var cosa = Vector3.Dot(Vector3.UnitY, vertex.Up);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);
            return angle < Trashold;
        }

        private static GameObject InstantiateWireSupport(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = WireSupportModel;
            renderer.Material.Color = Colors.Gray;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }

        private static void InstantiateWireEnding(
            Engine engine,
            Vector3 position,
            Vector3 normal,
            Vector3 direction,
            float radius)
        {
            var cosa = Vector3.Dot(Vector3.UnitY, normal);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);

            if (angle >= Trashold)
            {
                InstantiateMonitor(engine, position, normal, direction, radius);
            }
            else
            {
                InstantiateWireSource(engine, position, normal, direction, radius);
            }
        }

        private static void InstantiateMonitor(
            Engine engine,
            Vector3 position,
            Vector3 normal,
            Vector3 direction,
            float radius)
        {
            float epsilon = 0.01f;
            float monitorAdjustment = 0.1f;
            var right = Vector3.Cross(normal, direction);

            var rotation = Mathematics.ApproximatelyEqualEpsilon(Vector3.UnitZ, -normal, epsilon)
                ? Quaternion.FromAxisAngle(right, MathHelper.Pi)
                : Mathematics.FromToRotation(Vector3.UnitZ, normal);

            var up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rotation));
            var yAxis = Vector3.Normalize(Vector3.UnitY - Vector3.Dot(Vector3.UnitY, normal) * normal);

            rotation = Mathematics.ApproximatelyEqualEpsilon(up, -yAxis, epsilon)
                ? Quaternion.FromAxisAngle(normal, MathHelper.Pi) * rotation
                : Mathematics.FromToRotation(up, yAxis) * rotation;

            position += (2 * radius + monitorAdjustment) * normal; ;
            InstantiateMonitor(engine, position, rotation);
        }

        private static void InstantiateWireSource(
            Engine engine,
            Vector3 position,
            Vector3 normal,
            Vector3 direction,
            float radius)
        {
            float epsilon = 0.01f;
            var right = Vector3.Cross(normal, direction);

            var rotation = Mathematics.ApproximatelyEqualEpsilon(Vector3.UnitZ, -direction, epsilon)
                ? Quaternion.FromAxisAngle(right, MathHelper.Pi)
                : Mathematics.FromToRotation(Vector3.UnitZ, direction);

            var yAxis = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rotation));
            var rotationAxis = Vector3.Normalize(Vector3.Cross(yAxis, normal));

            rotation = Mathematics.ApproximatelyEqualEpsilon(yAxis, -normal, epsilon)
                ? Quaternion.FromAxisAngle(direction, MathHelper.Pi) * rotation
                : Mathematics.FromToRotation(rotationAxis, yAxis, normal) * rotation;

            position += radius * normal;
            InstantiateSource(engine, position, rotation);
        }

        private static GameObject InstantiateSource(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = SourceModel;
            renderer.Material = SourceMaterial;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }

        private static GameObject InstantiateMonitor(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = MonitorModel;
            renderer.Texture = MonitorTexture;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }
    }
}
