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
    public class VentilationGeneratorAlgorithm : INetAlgorithm
    {
        public static readonly Color VentilationColor = Color.FromArgb(255, 60, 246);

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
            float side = 0.5f;
            float extrusionFactor = 0.6f;

            var nodes = net.ToList();
            var points = GetPoints(nodes, side, extrusionFactor);
            var model = MeshGenerator.GenerateTubeFromSpline(points, side);

            var go = engine.CreateGameObject();
            var render = go.Add<MaterialRenderComponent>();
            render.Model = model;
        }

        private static List<SplineVertex> GetPoints(List<LogicalNode> nodes, float radius, float extrusionFactor)
        {
            var points = new List<SplineVertex>();
            var segments = new List<List<SplineVertex>>();
            int resolution = 3;

            for (int i = 1; i < nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var next = nodes[i];

                var jointPoints = CreatePointsAroundJoint(prev, next, extrusionFactor, radius, resolution);
                segments.Add(jointPoints);
            }

            points.AddRange(CreateBegin(nodes[0], nodes[1], extrusionFactor, radius, resolution));

            for (int i = 1; i < segments.Count; i++)
            {
                var prev = segments[i - 1];
                var next = segments[i];

                var inner = CreateInnerPoints(prev[prev.Count - 1], next[0], resolution);
                points.AddRange(prev.Concat(inner));
            }

            points.AddRange(segments[segments.Count - 1]);
            points.AddRange(CreateEnd(nodes[nodes.Count - 2], nodes[nodes.Count - 1], extrusionFactor, radius, resolution));
            return points;
        }

        private static List<SplineVertex> CreatePointsAroundJoint(
            LogicalNode prev,
            LogicalNode next,
            float extrusionFactor,
            float radius,
            int resolution)
        {
            float epsilon = 0.01f;
            var points = new List<SplineVertex>();

            var prevNormal = Mathematics.GetNormal(prev.Corners);
            var nextNormal = Mathematics.GetNormal(next.Corners);
            var blendedNormal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, 0.5f));

            var prevPivot = Mathematics.GetCentroid(prev.Corners);
            var nextPivot = Mathematics.GetCentroid(next.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(prev.Corners, next.Corners, epsilon);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var prevDirection = Vector3.Normalize(joint - prevPivot);
            var nextDirection = Vector3.Normalize(nextPivot - joint);
            var blendedDirection = Vector3.Normalize(Vector3.Lerp(prevDirection, nextDirection, 0.5f));

            var prevP1 = prevPivot + extrusionFactor * prevNormal;
            var prevP2 = joint + extrusionFactor * prevNormal;
            var e1 = prevP2 - prevP1;

            var nextP1 = nextPivot + extrusionFactor * nextNormal;
            var nextP2 = joint + extrusionFactor * nextNormal;
            var e2 = nextP2 - nextP1;

            if (Mathematics.TryGetIntersactionPoint(prevP1, e1, nextP1, e2, epsilon, out var p))
            {
                var cosa = Math.Clamp(Vector3.Dot(prevDirection, nextDirection), -1.0f, 1.0f);
                var acos = MathF.Acos(cosa);
                var b = MathF.PI - acos;
                var offset = MathF.Abs(radius / MathF.Tan(b / 2));

                var prevP = p - offset * prevDirection;
                var nextP = p + offset * nextDirection;

                if (!Mathematics.TryGetIntersactionPoint(prevP, prevNormal, nextP, nextNormal, epsilon, out var rotationPivot))
                {
                    throw new ArgumentException("Something damn happened.");
                }

                var sign = MathF.Sign(Vector3.Dot(p - rotationPivot, blendedNormal));

                for (int i = 0; i <= resolution; i++)
                {
                    var t = (float)i / resolution;
                    var normal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, t));
                    var position = rotationPivot + sign * radius * normal;
                    var direction = Vector3.Normalize(Vector3.Lerp(prevDirection, nextDirection, t));
                    points.Add(new SplineVertex(position, normal, direction));
                }
            }
            else
            {
                points.Add(new SplineVertex(joint + extrusionFactor * blendedNormal, blendedNormal, blendedDirection));
            }

            return points;
        }

        private static List<SplineVertex> CreateBegin(
            LogicalNode begin,
            LogicalNode next,
            float extrusionFactor,
            float radius,
            int resolution)
        {
            float epsilon = 0.01f;

            var points = new List<SplineVertex>();

            var beginNormal = Mathematics.GetNormal(begin.Corners);

            var pivot = Mathematics.GetCentroid(begin.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(begin.Corners, next.Corners, epsilon);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var fromPivotDirection = beginNormal;
            var toJointDirection = Vector3.Normalize(joint - pivot);

            var p = pivot + extrusionFactor * beginNormal;
            var rotationPivot = p - radius * fromPivotDirection + radius * toJointDirection;

            points.Add(new SplineVertex(pivot, -toJointDirection, fromPivotDirection));

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                var normal = Vector3.Normalize(Vector3.Lerp(-toJointDirection, beginNormal, t));
                var direction = Vector3.Normalize(Vector3.Lerp(fromPivotDirection, toJointDirection, t));
                var position = rotationPivot + radius * normal;
                points.Add(new SplineVertex(position, normal, direction));
            }

            return points;
        }

        private static List<SplineVertex> CreateEnd(
            LogicalNode prev,
            LogicalNode end,
            float extrusionFactor,
            float radius,
            int resolution)
        {
            float epsilon = 0.01f;

            var points = new List<SplineVertex>();

            var endNormal = Mathematics.GetNormal(end.Corners);

            var pivot = Mathematics.GetCentroid(end.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(prev.Corners, end.Corners, epsilon);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var fromJointDirection = Vector3.Normalize(pivot - joint);
            var toPivotDirection = -endNormal;

            var p = pivot + extrusionFactor * endNormal;
            var rotationPivot = p + radius * toPivotDirection - radius * fromJointDirection;

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                var normal = Vector3.Normalize(Vector3.Lerp(endNormal, fromJointDirection, t));
                var direction = Vector3.Normalize(Vector3.Lerp(fromJointDirection, toPivotDirection, t));
                var position = rotationPivot + radius * normal;
                points.Add(new SplineVertex(position, normal, direction));
            }

            points.Add(new SplineVertex(pivot, fromJointDirection, toPivotDirection));
            return points;
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
    }
}
