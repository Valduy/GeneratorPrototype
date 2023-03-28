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

namespace TriangulatedTopology.Props.Algorithms
{
    public class WiresGeneratorAlgorithm : INetAlgorithm
    {
        private const float Trashold = 45.0f;

        public static readonly Color WireColor = Color.FromArgb(255, 0, 24);

        public static Model WireSupportModel = Model.Load("Content/Models/WireSupport.fbx");
        public static Model SourceModel = Model.Load("Content/Models/Source.fbx");
        public static Model MonitorModel = Model.Load("Content/Models/Monitor.fbx");

        public static Texture MonitorTexture = Texture.LoadFromFile("Content/Textures/Monitor.png");

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
            var models = GenerateWiresModels(engine, net);

            foreach (var model in models)
            {
                var go = engine.CreateGameObject();
                var render = go.Add<MaterialRenderComponent>();
                render.Material.Color = new Vector3(0.1f, 0.7f, 0.4f);
                render.Model = model;
            }
        }

        private static List<Model> GenerateWiresModels(Engine engine, Net<LogicalNode> net)
        {
            int resolution = 32;
            float radius = 0.1f;

            var models = new List<Model>();
            var nodes = net.ToList();
            var pointsLines = GetWiresPointsLines(nodes, 3);

            foreach (var line in pointsLines)
            {
                var spline = GetSpline(line);

                if (spline.Count == 0)
                {
                    continue;
                }

                var model = MeshGenerator.GenerateTubeFromSpline(spline, resolution, radius);
                models.Add(model);
            }

            var middle = pointsLines[pointsLines.Count / 2];
            PlaceSupports(engine, middle);

            var first = middle[0];
            var last = middle[middle.Count - 1];

            // Monitors
            if (MathHelper.ApproximatelyEqualEpsilon(Vector3.Dot(first.Up, Vector3.UnitY), 0.0f, 0.01f))
            {
                InstantiateMonitor(engine, first.Position + first.Up * 0.2f, Mathematics.FromToRotation(Vector3.UnitZ, first.Up));
            }
            else
            {
                InstantiateSource(engine, first.Position, Mathematics.FromToRotation(Vector3.UnitZ, first.Forward));
            }

            if (MathHelper.ApproximatelyEqualEpsilon(Vector3.Dot(last.Up, Vector3.UnitY), 0.0f, 0.01f))
            {
                InstantiateMonitor(engine, last.Position + last.Up * 0.2f, Mathematics.FromToRotation(Vector3.UnitZ, last.Up));
            }
            else
            {
                InstantiateSource(engine, last.Position, Mathematics.FromToRotation(Vector3.UnitZ, -last.Forward));
            }

            return models;
        }

        private static List<List<SplineVertex>> GetWiresPointsLines(List<LogicalNode> nodes, int count)
        {
            float extrusionFactor = 0.1f;
            float offset = 0.2f;
            var pointsLines = new List<List<SplineVertex>>();

            for (int i = 0; i < count; i++)
            {
                pointsLines.Add(new List<SplineVertex>());
            }

            AddFirstSplineVertex(pointsLines, nodes[0], nodes[1], extrusionFactor, offset, count);
            AddSharedSplineVertex(pointsLines, nodes[0], nodes[1], extrusionFactor, offset, count);

            for (int i = 1; i < nodes.Count - 1; i++)
            {
                var prev = nodes[i - 1];
                var temp = nodes[i];
                var next = nodes[i + 1];

                var normal = Mathematics.GetNormal(temp.Corners);
                var cosa = Vector3.Dot(Vector3.UnitY, normal);
                var acos = MathF.Acos(cosa);
                var angle = MathHelper.RadiansToDegrees(acos);

                if (angle > Trashold)
                {
                    AddSplineVertexInsideNode(pointsLines, prev, temp, next, extrusionFactor, offset, count);
                }                
               
                AddSharedSplineVertex(pointsLines, temp, next, extrusionFactor, offset, count);
            }

            AddLastSplineVertex(pointsLines, nodes[nodes.Count - 2], nodes[nodes.Count - 1], extrusionFactor, offset, count);
            return pointsLines;
        }

        private static void AddFirstSplineVertex(
            List<List<SplineVertex>> pointsLines,
            LogicalNode temp,
            LogicalNode next,
            float extrusionFactor,
            float offset,
            int count)
        {
            float epsilon = 0.01f;
            int half = count / 2;
            var normal = Mathematics.GetNormal(temp.Corners);
            var shared = Mathematics.GetSharedPoints(temp.Corners, next.Corners, epsilon);
            var centroid = Mathematics.GetCentroid(shared);
            var pivot = Mathematics.GetCentroid(temp.Corners);
            var direction = Vector3.Normalize(centroid - pivot);
            var right = Vector3.Cross(direction, normal);
            var position = pivot + extrusionFactor * normal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, normal, direction));
            }
        }

        private static void AddLastSplineVertex(
            List<List<SplineVertex>> pointsLines,
            LogicalNode prev,
            LogicalNode temp,
            float extrusionFactor,
            float offset,
            int count)
        {
            float epsilon = 0.01f;
            int half = count / 2;
            var normal = Mathematics.GetNormal(temp.Corners);

            var shared = Mathematics.GetSharedPoints(prev.Corners, temp.Corners, epsilon);
            var centroid = Mathematics.GetCentroid(shared);
            var pivot = Mathematics.GetCentroid(temp.Corners);
            var direction = Vector3.Normalize(pivot - centroid);
            var right = Vector3.Cross(direction, normal);
            var position = pivot + extrusionFactor * normal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, normal, direction));
            }
        }

        private static void AddSplineVertexInsideNode(
            List<List<SplineVertex>> pointsLines,
            LogicalNode prev,
            LogicalNode temp,
            LogicalNode next,
            float extrusionFactor,
            float offset,
            int count)
        {
            float epsilon = 0.01f;
            int half = count / 2;
            var normal = Mathematics.GetNormal(temp.Corners);

            var prevSharedPoints = Mathematics.GetSharedPoints(prev.Corners, temp.Corners, epsilon);
            var nextSharedPoints = Mathematics.GetSharedPoints(temp.Corners, next.Corners, epsilon);

            var pivot = Mathematics.GetCentroid(temp.Corners);
            var prevJoint = Mathematics.GetCentroid(prevSharedPoints);
            var nextJoint = Mathematics.GetCentroid(nextSharedPoints);

            var toPivotDirection = Vector3.Normalize(pivot - prevJoint);
            var fromPivotDirection = Vector3.Normalize(nextJoint - pivot);
            var blendedDirection = Vector3.Normalize(Vector3.Lerp(toPivotDirection, fromPivotDirection, 0.5f));

            var right = Vector3.Cross(blendedDirection, normal);
            var position = pivot + extrusionFactor * normal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, normal, blendedDirection));
            }
        }

        private static void AddSharedSplineVertex(
            List<List<SplineVertex>> pointsLines,
            LogicalNode prev,
            LogicalNode next,
            float extrusionFactor,
            float offset,
            int count)
        {
            float epsilon = 0.01f;
            int half = count / 2;
            var prevNormal = Mathematics.GetNormal(prev.Corners);
            var nextNormal = Mathematics.GetNormal(next.Corners);
            var blendedNormal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, 0.5f));

            var prevPivot = Mathematics.GetCentroid(prev.Corners);
            var nextPivot = Mathematics.GetCentroid(next.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(prev.Corners, next.Corners, epsilon);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var toJointDirection = Vector3.Normalize(joint - prevPivot);
            var fromJointDirection = Vector3.Normalize(nextPivot - joint);
            var blendedDirection = Vector3.Normalize(Vector3.Lerp(toJointDirection, fromJointDirection, 0.5f));

            var right = Vector3.Cross(blendedDirection, blendedNormal);
            var position = joint + extrusionFactor * blendedNormal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, blendedNormal, blendedDirection));
            }
        }

        private static List<SplineVertex> GetSpline(List<SplineVertex> points)
        {
            var spline = new List<SplineVertex>();
            spline.AddRange(GenerateInnerVertices(points[0], points[1]));

            for (int i = 0; i < points.Count - 3; i++)
            {
                var prev = points[i + 1];
                var next = points[i + 2];

                if (IsSplineVertexLieOnFloor(prev) ||
                    IsSplineVertexLieOnFloor(next))
                {
                    spline.AddRange(GenerateInnerVertices(prev, next));
                }
                else
                {
                    spline.AddRange(GenerateInnerVertices(points[i], points[i + 1], points[i + 2], points[i + 3]));
                }               
            }

            spline.AddRange(GenerateInnerVertices(points[points.Count - 2], points[points.Count - 1]));
            return spline;
        }

        private static List<SplineVertex> GenerateInnerVertices(SplineVertex a, SplineVertex b)
        {
            int resolution = 20;
            var inner = new List<SplineVertex>();

            for (int j = 0; j < resolution; j++)
            {
                float tempPercent = (float)j / resolution;
                float nextPercent = (float)(j + 1) / resolution;
                float coerce = 1.0f;

                var position = Curves.Hermite(
                    a.Position,
                    b.Position,
                    coerce * a.Forward,
                    coerce * b.Forward,
                    tempPercent);

                var nextPosition = Curves.Hermite(
                    a.Position,
                    b.Position,
                    coerce * a.Forward,
                    coerce * b.Forward,
                    nextPercent);

                var normal = Vector3.Lerp(a.Up, b.Up, tempPercent);
                var direction = Vector3.Normalize(nextPosition - position);
                inner.Add(new SplineVertex(position, normal, direction));
            }

            return inner;
        }

        private static List<SplineVertex> GenerateInnerVertices(
            SplineVertex p0,
            SplineVertex p1,
            SplineVertex p2,
            SplineVertex p3)
        {
            int resolution = 20;
            float alpha = 0.5f;
            var inner = new List<SplineVertex>();

            for (int j = 0; j < resolution; j++)
            {
                float tempPercent = (float)j / resolution;
                float nextPercent = (float)(j + 1) / resolution;                

                var position = Curves.CatmullRom(
                    p0.Position,
                    p1.Position,
                    p2.Position,
                    p3.Position,
                    tempPercent,
                    alpha);

                var nextPosition = Curves.CatmullRom(
                    p0.Position,
                    p1.Position,
                    p2.Position,
                    p3.Position,
                    nextPercent,
                    alpha);

                var normal = Vector3.Lerp(p1.Up, p2.Up, tempPercent);
                var direction = Vector3.Normalize(nextPosition - position);
                inner.Add(new SplineVertex(position, normal, direction));
            }

            return inner;
        }

        private static void PlaceSupports(Engine engine, List<SplineVertex> points)
        {
            for (int i = 1; i < points.Count - 1; i += 1)
            {
                if (IsSplineVertexLieOnFloor(points[i]))
                {
                    continue;
                }

                var rotation = Mathematics.FromToRotation(Vector3.UnitY, points[i].Up);
                var forward = Vector3.Transform(Vector3.UnitZ, rotation);
                rotation = Mathematics.FromToRotation(forward, points[i].Forward) * rotation;
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

        private static GameObject InstantiateSource(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = SourceModel;
            renderer.Material.Color = Colors.Gray;
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
