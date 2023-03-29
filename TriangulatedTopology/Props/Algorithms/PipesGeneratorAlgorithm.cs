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

            //float epsilon = 0.01f;

            //foreach (var joint in joints)
            //{
            //    var prevNormal = Mathematics.GetNormal(joint.Prev.Corners);
            //    var nextNormal = Mathematics.GetNormal(joint.Next.Corners);

            //    if (Mathematics.ApproximatelyEqualEpsilon(Vector3.Dot(prevNormal, nextNormal), 1.0f, epsilon))
            //    {
            //        InstantiateDualPipeJoints(engine, joint.Vertex.Up, joint.Vertex.Position, joint.Vertex.Forward);
            //    }
            //}

            //for (int i = 1; i < points.Count; i++)
            //{
            //    var prev = points[i - 1];
            //    var next = points[i];

            //    var line = engine.Line(prev.Position, next.Position, Colors.Green);
            //    line.Get<LineRenderComponent>()!.Width = 10;
            //}

            //for (int i = 0; i < points.Count; i++)
            //{
            //    var temp = points[i];

            //    var line = engine.Line(temp.Position, temp.Position + temp.Up * 3, Colors.Red);
            //    line.Get<LineRenderComponent>()!.Width = 1;
            //}

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

                var jointPoints = CreatePipePointsAroundJoint(prev, next, extrusion, radius, resolution);
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

            points.AddRange(CreatePipeBegin(nodes[0], nodes[1], extrusion, radius, resolution));

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
            points.AddRange(CreatePipeEnd(nodes[nodes.Count - 2], nodes[nodes.Count - 1], extrusion, radius, resolution));
            return (points, joints);
        }

        private List<SplineVertex> CreatePipePointsAroundJoint(
            LogicalNode prev,
            LogicalNode next,
            float extrusion,
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

            var prevP1 = prevPivot + (ZeroLevel + extrusion) * prevNormal;
            var prevP2 = joint + (ZeroLevel + extrusion) * prevNormal;
            var e1 = prevP2 - prevP1;

            var nextP1 = nextPivot + (ZeroLevel + extrusion) * nextNormal;
            var nextP2 = joint + (ZeroLevel + extrusion) * nextNormal;
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
                points.Add(new SplineVertex(joint + (ZeroLevel + extrusion) * blendedNormal, blendedNormal, blendedDirection));
            }

            return points;
        }

        private List<SplineVertex> CreatePipeBegin(
            LogicalNode begin,
            LogicalNode next,
            float extrusion,
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

            var p = pivot + (ZeroLevel + extrusion) * beginNormal;
            var rotationPivot = p - radius * fromPivotDirection + radius * toJointDirection;

            points.Add(new SplineVertex(pivot + ZeroLevel * beginNormal, -toJointDirection, fromPivotDirection));

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

        private List<SplineVertex> CreatePipeEnd(
            LogicalNode prev,
            LogicalNode end,
            float extrusion,
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

            var p = pivot + (ZeroLevel + extrusion) * endNormal;
            var rotationPivot = p + radius * toPivotDirection - radius * fromJointDirection;

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                var normal = Vector3.Normalize(Vector3.Lerp(endNormal, fromJointDirection, t));
                var direction = Vector3.Normalize(Vector3.Lerp(fromJointDirection, toPivotDirection, t));
                var position = rotationPivot + radius * normal;
                points.Add(new SplineVertex(position, normal, direction));
            }

            points.Add(new SplineVertex(pivot + ZeroLevel * endNormal, fromJointDirection, toPivotDirection));
            return points;
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

            //float minimumDistance = 0.2f;
            //float desiredDistance = 2.0f;
            //float epsilon = 0.01f;

            //var (lines, curves) = ExtractLinesAndCurves(points);

            //for (int i = 1; i < lines.Count - 1; i++)
            ////for (int i = 0; i < lines.Count; i++)
            //{
            //    var line = lines[i];
            //    var from = 0;
            //    var to = line.Count - 1;
            //    bool canInstantiateOnlySingleJoint = false;

            //    if (Vector3.Distance(line[from].Position, line[to].Position) < minimumDistance)
            //    {
            //        continue;
            //    }

            //    //InstantiateSphere(engine, line[0].Position, Quaternion.Identity, new Vector3(0.4f), Color.Navy);
            //    //InstantiateSphere(engine, line[line.Count - 1].Position, Quaternion.Identity, new Vector3(0.4f), Color.Red);

            //    while (Vector3.Distance(line[0].Position, line[from].Position) < minimumDistance)
            //    {
            //        from += 1;

            //        if (from >= line.Count)
            //        {
            //            canInstantiateOnlySingleJoint = true;
            //            break;
            //        }
            //    }

            //    if (canInstantiateOnlySingleJoint || Vector3.Distance(line[from].Position, line[line.Count - 1].Position) < minimumDistance)
            //    {
            //        var first = line[0];
            //        var last = line[line.Count - 1];
            //        var position = (first.Position + last.Position) / 2;
            //        var axis = Vector3.Normalize(Vector3.Lerp(first.Up, last.Up, 0.5f));
            //        var direction = Vector3.Normalize(Vector3.Lerp(first.Forward, last.Forward, 0.5f));
            //        InstantiateDualPipeJoints(engine, axis, position, direction);
            //        continue;
            //    }

            //    while (Vector3.Distance(line[to].Position, line[line.Count - 1].Position) < minimumDistance)
            //    {
            //        to -= 1;

            //        if (to == from)
            //        {
            //            canInstantiateOnlySingleJoint = true;
            //            break;
            //        }
            //    }

            //    if (canInstantiateOnlySingleJoint || Vector3.Distance(line[to].Position, line[0].Position) < minimumDistance)
            //    {
            //        var first = line[0];
            //        var last = line[line.Count - 1];
            //        var position = (first.Position + last.Position) / 2;
            //        var axis = Vector3.Normalize(Vector3.Lerp(first.Up, last.Up, 0.5f));
            //        var direction = Vector3.Normalize(Vector3.Lerp(first.Forward, last.Forward, 0.5f));
            //        InstantiateDualPipeJoints(engine, axis, position, direction);
            //        continue;
            //    }

            //    //InstantiateSphere(engine, line[from].Position, Quaternion.Identity, new Vector3(0.4f), Color.Purple);
            //    //InstantiateSphere(engine, line[to].Position, Quaternion.Identity, new Vector3(0.4f), Color.Green);

            //    var innerDistance = Vector3.Distance(line[from].Position, line[to].Position);

            //    if (innerDistance < minimumDistance)
            //    {
            //        var first = line[from];
            //        var last = line[to];
            //        var position = (first.Position + last.Position) / 2;
            //        var axis = Vector3.Normalize(Vector3.Lerp(first.Up, last.Up, 0.5f));
            //        var direction = Vector3.Normalize(Vector3.Lerp(first.Forward, last.Forward, 0.5f));
            //        InstantiateDualPipeJoints(engine, axis, position, direction);
            //        continue;
            //    }

            //    InstantiateDualPipeJoints(engine, line[from].Up, line[from].Position, line[from].Forward);
            //    InstantiateDualPipeJoints(engine, line[to].Up, line[to].Position, line[to].Forward);
            //    SplitPipe(engine, line, from, to, desiredDistance);
            //}
        }

        private static (List<List<SplineVertex>> Lines, List<List<SplineVertex>> Curves) ExtractLinesAndCurves(List<SplineVertex> points)
        {
            float epsilon = 0.0001f;
            var lines = new List<List<SplineVertex>>();
            var curves = new List<List<SplineVertex>>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var prev = points[i];
                var next = points[i + 1];
                var cosa = Vector3.Dot(prev.Forward, next.Forward);

                if (Mathematics.ApproximatelyEqualEpsilon(cosa, 1, epsilon))
                {
                    var line = new List<SplineVertex>();

                    for (var j = i; j < points.Count - 1; j++)
                    {
                        prev = points[j];
                        next = points[j + 1];
                        cosa = Vector3.Dot(prev.Forward, next.Forward);

                        if (!Mathematics.ApproximatelyEqualEpsilon(cosa, 1, epsilon))
                        {
                            break;
                        }

                        line.Add(prev);
                    }

                    i += line.Count;
                    line.Add(points[i]);
                    lines.Add(line);
                }
                else
                {
                    var curve = new List<SplineVertex>();

                    for (var j = i; j < points.Count - 1; j++)
                    {
                        prev = points[j];
                        next = points[j + 1];
                        cosa = Vector3.Dot(prev.Forward, next.Forward);

                        if (Mathematics.ApproximatelyEqualEpsilon(cosa, 1, epsilon))
                        {
                            break;
                        }

                        curve.Add(prev);
                    }

                    i += curve.Count;
                    curve.Add(points[i]);
                    curves.Add(curve);
                }
            }

            {
                var cosa = Vector3.Dot(points[points.Count - 2].Forward, points[points.Count - 1].Forward);

                if (Mathematics.ApproximatelyEqualEpsilon(cosa, 1, epsilon))
                {
                    lines[lines.Count - 1].Add(points[points.Count - 1]);
                }
                else
                {
                    curves[curves.Count - 1].Add(points[points.Count - 1]);
                }
            }

            return (lines, curves);
        }

        private static void SplitPipe(Engine engine, List<SplineVertex> line, int from, int to, float distance)
        {
            if (Vector3.Distance(line[from].Position, line[to].Position) > distance)
            {
                int pivot = (from + to) / 2;
                InstantiateDualPipeJoints(engine, line[pivot].Up, line[pivot].Position, line[pivot].Forward);

                SplitPipe(engine, line, from, pivot, distance);
                SplitPipe(engine, line, pivot, to, distance);
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
