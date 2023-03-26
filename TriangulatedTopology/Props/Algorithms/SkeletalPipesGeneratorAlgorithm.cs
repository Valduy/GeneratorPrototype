using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using Graph;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology.Props.Algorithms
{
    public class SkeletalPipesGeneratorAlgorithm : INetAlgorithm
    {
        public static readonly Color PipesColor = Color.FromArgb(255, 217, 0);
        public static readonly Color VentilationColor = Color.FromArgb(255, 60, 246);

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
            float epsilon = 0.01f;
            float extrusionFactor = 0.5f;

            foreach (var node in net.GetNodes())
            {
                if (node.Neighbours.Count > 2 || node.Neighbours.Count <= 0)
                {
                    throw new ArgumentException("Pipe should has 1 or 2 neighbours.");
                }

                var centroid = Mathematics.GetCentroid(node.Item.Corners);
                var normal = Mathematics.GetNormal(node.Item.Corners);
                var pivot = centroid + extrusionFactor * normal;
                float k = 0.96f;

                if (node.Neighbours.Count == 2)
                {
                    var pipe = InstantiatePipe(engine, pivot, Quaternion.Identity);
                    var skeleton = pipe.Get<SkeletalMeshRenderComponent>()!.Model.Skeleton!;

                    GetPipeSideDeformations(
                        node.Item, node.Neighbours[0].Item,
                        pivot, normal, Vector3.UnitZ * k, extrusionFactor,
                        out var topSideDirection,
                        out var topSocketCoerce,
                        out var topSocketRotation);

                    var top = skeleton["Top"];
                    var topHand = skeleton["TopHand"];
                    top.Position = topSideDirection;
                    topHand.Position = topSocketCoerce;
                    topHand.Rotation = topSocketRotation;

                    GetPipeSideDeformations(
                        node.Item, node.Neighbours[1].Item,
                        pivot, normal, -Vector3.UnitZ * k, extrusionFactor,
                        out var bottomSideDirection,
                        out var bottomSocketCoerce,
                        out var bottomSocketRotation);

                    var bottom = skeleton["Bottom"];
                    var bottomHand = skeleton["BottomHand"];
                    bottom.Position = bottomSideDirection;
                    bottomHand.Position = bottomSocketCoerce;
                    bottomHand.Rotation = bottomSocketRotation;
                }
                else
                {
                    var pipe = InstantiatePipe(engine, pivot, Quaternion.Identity);
                    var skeleton = pipe.Get<SkeletalMeshRenderComponent>()!.Model.Skeleton!;

                    GetPipeSideDeformations(node.Item, node.Neighbours[0].Item,
                        pivot, normal, Vector3.UnitZ * k, extrusionFactor,
                        out var topSideDirection,
                        out var topSocketCoerce,
                        out var topSocketRotation);

                    var top = skeleton["Top"];
                    var topHand = skeleton["TopHand"];
                    top.Position = topSideDirection;
                    topHand.Position = topSocketCoerce;
                    topHand.Rotation = topSocketRotation;

                    var shared = Mathematics.GetSharedPoints(node.Item.Corners, node.Neighbours[0].Item.Corners, epsilon);
                    var to = Mathematics.GetCentroid(shared) + extrusionFactor * normal;
                    var toNeighbour = to - pivot;
                    toNeighbour.Normalize();

                    GetPipeEndingDeformations(
                        centroid, pivot, -toNeighbour, -Vector3.UnitZ * k,
                        out var bottomSideDirection,
                        out var bottomSocketCoerce,
                        out var bottomSocketRotation);

                    var bottom = skeleton["Bottom"];
                    var bottomHand = skeleton["BottomHand"];
                    bottom.Position = bottomSideDirection;
                    bottomHand.Position = bottomSocketCoerce;
                    bottomHand.Rotation = bottomSocketRotation;
                }
            }
        }

        private static void GetPipeSideDeformations(
            LogicalNode node,
            LogicalNode neighbour,
            Vector3 pivot,
            Vector3 normal,
            Vector3 socketOffset,
            float extrusionFactor,
            out Vector3 sideDirection,
            out Vector3 socketCoerce,
            out Quaternion socketRotation)
        {
            float epsilon = 0.01f;

            var neighbourNormal = Mathematics.GetNormal(neighbour.Corners);
            var extrusionDirection = Vector3.Lerp(normal, neighbourNormal, 0.5f).Normalized();

            var sharedPoints = Mathematics.GetSharedPoints(node.Corners, neighbour.Corners, epsilon);
            var centroid = Mathematics.GetCentroid(sharedPoints);

            var to = centroid + extrusionFactor * extrusionDirection;
            var forward = centroid + extrusionFactor * normal - pivot;
            sideDirection = to - pivot;

            var edgeAxis = sharedPoints[1] - sharedPoints[0];
            edgeAxis.Normalize();

            var socketDirection = Vector3.Cross(edgeAxis, extrusionDirection);
            socketDirection.Normalize();

            // Rough but ok...
            if (Vector3.Dot(socketDirection, sideDirection) < 0)
            {
                socketDirection = -socketDirection;
            }

            var normalRotation = Mathematics.FromToRotation(forward, socketDirection);
            normal = Vector3.Transform(normal, normalRotation).Normalized();

            socketRotation = Mathematics.FromToRotation(socketOffset, socketDirection);
            var unwinding = GetUnwinding(socketDirection, socketRotation, normal);
            var withoutUnwinding = socketRotation; // for debug            

            var socketTransform = Matrix4.CreateTranslation(socketOffset);
            socketTransform *= Matrix4.CreateFromQuaternion(socketRotation);
            socketCoerce = -socketTransform.ExtractTranslation();
            socketRotation = unwinding * socketRotation;

            //var a0 = to;
            //var b0 = a0 + Vector3.Transform(Vector3.UnitY * 2, withoutUnwinding);

            //var line0 = Engine.Line(a0, b0, Colors.Green);
            //line0.Get<LineRenderComponent>()!.Width = 2;

            //var a1 = to;
            //var b1 = a1 + Vector3.Transform(Vector3.UnitY * 2, socketRotation);

            //var line1 = Engine.Line(a1, b1, Colors.Green);
            //line1.Get<LineRenderComponent>()!.Width = 2;
        }

        private static void GetPipeEndingDeformations(
            Vector3 centroid,
            Vector3 pivot,
            Vector3 normal,
            Vector3 socketOffset,
            out Vector3 sideDirection,
            out Vector3 socketCoerce,
            out Quaternion socketRotation)
        {
            sideDirection = centroid - pivot;
            socketRotation = Mathematics.FromToRotation(socketOffset, sideDirection);
            var unwinding = GetUnwinding(sideDirection, socketRotation, normal);
            var withoutUnwinding = socketRotation; // for debug           

            var socketTransform = Matrix4.CreateTranslation(socketOffset);
            socketTransform *= Matrix4.CreateFromQuaternion(socketRotation);
            socketCoerce = -socketTransform.ExtractTranslation();
            socketRotation = unwinding * socketRotation;

            //var a0 = centroid;
            //var b0 = a0 + Vector3.Transform(yAxis * 2, withoutUnwinding);

            //var line0 = Engine.Line(a0, b0, Colors.Green);
            //line0.Get<LineRenderComponent>()!.Width = 2;

            //var a1 = centroid;
            //var b1 = a1 + Vector3.Transform(yAxis * 2, socketRotation);

            //var line1 = Engine.Line(a1, b1, Colors.Red);
            //line1.Get<LineRenderComponent>()!.Width = 2;
        }

        private static Quaternion GetUnwinding(
            Vector3 sideDirection,
            Quaternion socketRotation,
            Vector3 normal)
        {
            var yAxis = Vector3.UnitY;
            var axis = sideDirection.Normalized();
            var up = Vector3.Transform(yAxis, socketRotation);
            var unwinding = Mathematics.FromToRotation(axis, up, normal);

            var epsilon = 0.01f;
            var unwindedUp = Vector3.Transform(up, unwinding);
            var normalUpAngle = Vector3.Dot(unwindedUp, normal);

            // GetRotation return always positive value.
            // We invert unwinding rotation if we should use negative angle.
            if (!MathHelper.ApproximatelyEqualEpsilon(normalUpAngle, 1.0f, epsilon))
            {
                unwinding.Invert();
            }

            var socketRotationWithUnwinding = unwinding * socketRotation;
            var upWithUnwinding = Vector3.Transform(yAxis, socketRotationWithUnwinding);
            var angle = Vector3.Dot(upWithUnwinding.Normalized(), normal);

            // Fix not correct rotation for 90 degrees between up and normal case.
            if (MathHelper.ApproximatelyEqualEpsilon(angle, -1.0f, epsilon))
            {
                unwinding *= Quaternion.FromAxisAngle(axis, MathF.PI);
            }

            return unwinding;
        }

        private static GameObject InstantiatePipe(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<SkeletalMeshRenderComponent>();
            //renderer.Model = Model.Load("Content/Models/PipeSegment.fbx");
            renderer.Model = Model.Load("Content/Models/CurvePipe.fbx");
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }
    }
}
