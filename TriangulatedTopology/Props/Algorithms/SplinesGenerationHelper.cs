using GameEngine.Mathematics;
using OpenTK.Mathematics;

namespace TriangulatedTopology.Props.Algorithms
{
    public static class SplinesGenerationHelper
    {
        public static List<SplineVertex> CreatePointsAroundJoint(
            LogicalNode prev,
            LogicalNode next,
            float zeroLevel,
            float extrusion,
            float radius,
            int resolution)
        {
            float epsilon = 0.1f;
            float finalExtrusion = zeroLevel + extrusion;

            var points = new List<SplineVertex>();

            var prevNormal = Mathematics.GetNormal(prev.Corners);
            var nextNormal = Mathematics.GetNormal(next.Corners);

            var prevPivot = Mathematics.GetCentroid(prev.Corners);
            var nextPivot = Mathematics.GetCentroid(next.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(prev.Corners, next.Corners, epsilon);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var prevDirection = Vector3.Normalize(joint - prevPivot);
            var nextDirection = Vector3.Normalize(nextPivot - joint);

            var prevP1 = prevPivot + finalExtrusion * prevNormal;
            var prevP2 = joint + finalExtrusion * prevNormal;
            var e1 = prevP2 - prevP1;

            var nextP1 = nextPivot + finalExtrusion * nextNormal;
            var nextP2 = joint + finalExtrusion * nextNormal;
            var e2 = nextP2 - nextP1;

            if (Mathematics.TryGetIntersactionPoint(prevP1, e1, nextP1, e2, epsilon, out var intersactionPoint))
            {
                var cosa = Math.Clamp(Vector3.Dot(prevDirection, nextDirection), -1.0f, 1.0f);
                var acos = MathF.Acos(cosa);
                var b = MathF.PI - acos;
                var offset = MathF.Abs(radius / MathF.Tan(b / 2));

                var prevP = intersactionPoint - offset * prevDirection;
                var nextP = intersactionPoint + offset * nextDirection;

                var toRotationPivot = Vector3.Normalize((nextDirection - prevDirection) / 2);
                var rotationPivot = intersactionPoint + radius / MathF.Sin(b / 2) * toRotationPivot;
                var toPrevPosition = Vector3.Normalize(prevP - rotationPivot);
                var toNextPosition = Vector3.Normalize(nextP - rotationPivot);

                for (int i = 0; i <= resolution; i++)
                {
                    var t = (float)i / resolution;
                    var toPosition = Vector3.Normalize(Vector3.Lerp(toPrevPosition, toNextPosition, t));
                    var normal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, t));
                    var position = rotationPivot + radius * toPosition;
                    var direction = Vector3.Normalize(Vector3.Lerp(prevDirection, nextDirection, t));
                    points.Add(new SplineVertex(position, normal, direction));
                }
            }
            else
            {
                var normal = Vector3.Normalize((prevNormal + nextNormal) / 2);
                var position = joint + finalExtrusion * normal;
                var direction = Vector3.Normalize((prevDirection + nextDirection) / 2);
                points.Add(new SplineVertex(position, normal, direction));
            }

            return points;
        }

        public static List<List<SplineVertex>> CreatePointsAroundJoint(
            LogicalNode prev,
            LogicalNode next,
            float zeroLevel,
            float extrusion,
            float radius,
            float distanceBetweenLines,
            int resolution,
            int linesCount)
        {
            int half = linesCount / 2;
            float epsilon = 0.1f;
            float finalExtrusion = zeroLevel + extrusion;

            var pointsLines = new List<List<SplineVertex>>();
            
            for (int line = 0; line < linesCount; line++)
            {
                pointsLines.Add(new List<SplineVertex>());
            }

            var prevNormal = Mathematics.GetNormal(prev.Corners);
            var nextNormal = Mathematics.GetNormal(next.Corners);
            
            var prevPivot = Mathematics.GetCentroid(prev.Corners);
            var nextPivot = Mathematics.GetCentroid(next.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(prev.Corners, next.Corners, 0.01f);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var prevDirection = Vector3.Normalize(joint - prevPivot);
            var nextDirection = Vector3.Normalize(nextPivot - joint);
            
            var prevRight = Vector3.Normalize(Vector3.Cross(prevDirection, prevNormal));
            var nextRight = Vector3.Normalize(Vector3.Cross(nextDirection, nextNormal));

            var prevP1 = prevPivot + finalExtrusion * prevNormal;
            var prevP2 = joint + finalExtrusion * prevNormal;
            var e1 = prevP2 - prevP1;

            var nextP1 = nextPivot + finalExtrusion * nextNormal;
            var nextP2 = joint + finalExtrusion * nextNormal;
            var e2 = nextP2 - nextP1;

            if (Mathematics.TryGetIntersactionPoint(prevP1, e1, nextP1, e2, epsilon, out var intersactionPoint))
            {
                var cosa = Math.Clamp(Vector3.Dot(prevDirection, nextDirection), -1.0f, 1.0f);
                var acos = MathF.Acos(cosa);
                var b = MathF.PI - acos;
                var offset = MathF.Abs(radius / MathF.Tan(b / 2));

                var right = Vector3.Normalize(prevRight + nextRight);

                for (int line = 0; line < linesCount; line++)
                {
                    int factor = line - half;
                    var points = pointsLines[line];
                    var point = intersactionPoint + factor * distanceBetweenLines * right;

                    var prevP = point - offset * prevDirection;
                    var nextP = point + offset * nextDirection;

                    var toRotationPivot = Vector3.Normalize(Vector3.Lerp(-prevDirection, nextDirection, 0.5f));
                    var rotationPivot = point + radius / MathF.Sin(b / 2) * toRotationPivot;
                    var toPrevPosition = Vector3.Normalize(prevP - rotationPivot);
                    var toNextPosition = Vector3.Normalize(nextP - rotationPivot);

                    for (int i = 0; i <= resolution; i++)
                    {
                        var t = (float)i / resolution;
                        var toPosition = Vector3.Normalize(Vector3.Lerp(toPrevPosition, toNextPosition, t));
                        var normal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, t));
                        var position = rotationPivot + radius * toPosition;
                        var direction = Vector3.Normalize(Vector3.Lerp(prevDirection, nextDirection, t));
                        points.Add(new SplineVertex(position, normal, direction));
                    }
                }
            }
            else
            {
                for (int line = 0; line < linesCount; line++)
                {
                    int factor = line - half;
                    var right = Vector3.Normalize((prevRight + nextRight) / 2);
                    var normal = Vector3.Normalize((prevNormal + nextNormal) / 2);
                    var position = joint + finalExtrusion * normal + factor * distanceBetweenLines * right;
                    var direction = Vector3.Normalize((prevDirection + nextDirection) / 2);
                    pointsLines[line].Add(new SplineVertex(position, normal, direction));
                }
            }

            return pointsLines;
        }

        public static List<SplineVertex> CreateBegin(
            LogicalNode begin,
            LogicalNode next,
            float zeroLevel,
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

            var p = pivot + (zeroLevel + extrusion) * beginNormal;
            var rotationPivot = p - radius * fromPivotDirection + radius * toJointDirection;

            points.Add(new SplineVertex(pivot + zeroLevel * beginNormal, -toJointDirection, fromPivotDirection));

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

        public static List<List<SplineVertex>> CreateBegin(
            LogicalNode begin,
            LogicalNode next,
            float zeroLevel,
            float extrusion,
            float radius,
            float distanceBetweenLines,
            int resolution,
            int linesCount)
        {
            float epsilon = 0.01f;
            int half = linesCount / 2;

            var pointsLines = new List<List<SplineVertex>>();

            for (int line = 0; line < linesCount; line++)
            {
                pointsLines.Add(new List<SplineVertex>());
            }

            var beginNormal = Mathematics.GetNormal(begin.Corners);

            var pivot = Mathematics.GetCentroid(begin.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(begin.Corners, next.Corners, epsilon);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var fromPivotDirection = beginNormal;
            var toJointDirection = Vector3.Normalize(joint - pivot);

            var p = pivot + (zeroLevel + extrusion) * beginNormal;
            var rotationPivot = p - radius * fromPivotDirection + radius * toJointDirection;

            var right = Vector3.Normalize(Vector3.Cross(toJointDirection, beginNormal));

            for (int line = 0; line < linesCount; line++)
            {
                int factor = line - half;
                var points = pointsLines[line];
                points.Add(new SplineVertex(pivot + zeroLevel * beginNormal + factor * distanceBetweenLines * right, -toJointDirection, fromPivotDirection));

                for (int i = 0; i <= resolution; i++)
                {
                    float t = (float)i / resolution;
                    var normal = Vector3.Normalize(Vector3.Lerp(-toJointDirection, beginNormal, t));
                    var direction = Vector3.Normalize(Vector3.Lerp(fromPivotDirection, toJointDirection, t));
                    var position = rotationPivot + radius * normal + factor * distanceBetweenLines * right;
                    points.Add(new SplineVertex(position, normal, direction));
                }
            }

            return pointsLines;
        }

        public static List<SplineVertex> CreateEnd(
            LogicalNode prev,
            LogicalNode end,
            float zeroLevel,
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

            var p = pivot + (zeroLevel + extrusion) * endNormal;
            var rotationPivot = p + radius * toPivotDirection - radius * fromJointDirection;

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                var normal = Vector3.Normalize(Vector3.Lerp(endNormal, fromJointDirection, t));
                var direction = Vector3.Normalize(Vector3.Lerp(fromJointDirection, toPivotDirection, t));
                var position = rotationPivot + radius * normal;
                points.Add(new SplineVertex(position, normal, direction));
            }

            points.Add(new SplineVertex(pivot + zeroLevel * endNormal, fromJointDirection, toPivotDirection));
            return points;
        }

        public static List<List<SplineVertex>> CreateEnd(
            LogicalNode prev,
            LogicalNode end,
            float zeroLevel,
            float extrusion,
            float radius,
            float distanceBetweenLines,
            int resolution,
            int linesCount)
        {
            float epsilon = 0.01f;
            int half = linesCount / 2;

            var pointsLines = new List<List<SplineVertex>>();

            for (int line = 0; line < linesCount; line++)
            {
                pointsLines.Add(new List<SplineVertex>());
            }

            var endNormal = Mathematics.GetNormal(end.Corners);

            var pivot = Mathematics.GetCentroid(end.Corners);

            var sharedPoints = Mathematics.GetSharedPoints(prev.Corners, end.Corners, epsilon);
            var joint = Mathematics.GetCentroid(sharedPoints);

            var fromJointDirection = Vector3.Normalize(pivot - joint);
            var toPivotDirection = -endNormal;

            var p = pivot + (zeroLevel + extrusion) * endNormal;
            var rotationPivot = p + radius * toPivotDirection - radius * fromJointDirection;

            var right = Vector3.Normalize(Vector3.Cross(fromJointDirection, endNormal));

            for (int line = 0; line < linesCount; line++)
            {
                int factor = line - half;
                var points = pointsLines[line];

                for (int i = 0; i <= resolution; i++)
                {
                    float t = (float)i / resolution;
                    var normal = Vector3.Normalize(Vector3.Lerp(endNormal, fromJointDirection, t));
                    var direction = Vector3.Normalize(Vector3.Lerp(fromJointDirection, toPivotDirection, t));
                    var position = rotationPivot + radius * normal + factor * distanceBetweenLines * right;
                    points.Add(new SplineVertex(position, normal, direction));
                }

                points.Add(new SplineVertex(pivot + zeroLevel * endNormal + factor * distanceBetweenLines * right, fromJointDirection, toPivotDirection));
            }

            return pointsLines;
        }
    }
}
