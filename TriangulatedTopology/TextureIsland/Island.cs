using GameEngine.Graphics;
using GameEngine.Helpers;
using MeshTopology;
using OpenTK.Mathematics;

namespace TriangulatedTopology.TextureIsland
{
    // Island scheme:
    //   1   <----   0
    //   +-----0-----+
    //   | 0;0 | 1;0 |
    // | +-----+-----+ Ʌ
    // | 1 0;1 | 1;1 3 |
    // V +-----+-----+ |
    //   | 0;2 | 1;2 |
    //   +-----2-----+
    //   2   ---->   3
    public class Island
    {
        public const int SidesCount = 4;

        private readonly List<Vertex> _corners = new List<Vertex>();

        public readonly Cell[,] Grid;

        public readonly Side[] Sides;

        public IReadOnlyList<Vertex> Corners => _corners;

        public Island(Cell[,] grid, Side[] sides)
        {
            if (sides.Length != SidesCount)
            {
                throw new ArgumentException($"{nameof(sides)}.{nameof(sides.Length)} != {SidesCount}.");
            }

            for (int i = 0; i < sides.Length; i++)
            {
                var side = sides[i];

                if (!IsSequential(side))
                {
                    throw new ArgumentException($"Side {i} is not sequential");
                }

                var textureCoords = side.SelectMany(e => EnumerateTextureCoords(e)).ToList();

                if (!IsXAxisAligned(textureCoords) &&
                    !IsYAxisAligned(textureCoords))
                {
                    throw new ArgumentException($"UV shape of island is not rectangular.");
                }
            }

            if (!IsLoop(sides))
            {
                throw new ArgumentException("Island is not loop.");
            }

            Grid = grid;
            Sides = sides;

            for (int i = 0; i < SidesCount; i++)
            {
                _corners.Add(Sides[i][0].A);
            }
        }

        public bool IsContainsSegment(Vector3 a, Vector3 b)
        {
            foreach (var side in Sides)
            {
                foreach (var edge in side)
                {
                    if (edge.A.Position == a && edge.B.Position == b ||
                        edge.A.Position == b && edge.B.Position == a)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetSegmentAndSide(Vector3 a, Vector3 b, out Edge? correspondingEdge, out Side? containingSide)
        {
            foreach (var side in Sides)
            {
                foreach (var edge in side)
                {
                    if (edge.A.Position == a && edge.B.Position == b ||
                        edge.A.Position == b && edge.B.Position == a)
                    {
                        correspondingEdge = edge;
                        containingSide = side;
                        return true;
                    }
                }
            }

            correspondingEdge = null;
            containingSide = null;
            return false;
        }

        public List<Cell> GetCorrespodingCells(Vector3 a, Vector3 b, int size, int step)
        {
            if (!TryGetSegmentAndSide(a, b, out var edge, out var side))
            {
                throw new ArgumentException($"Island does not contains segment ({a}; {b})");
            }

            var cells = new List<Cell>();
            var view = GetCellsView(Sides.IndexOf(side));

            var distanceToA = (edge!.A.TextureCoords - side!.A.TextureCoords).Length * size;
            var distanceToB = (edge!.B.TextureCoords - side!.A.TextureCoords).Length * size;
            var from = (int)MathHelper.Ceiling(distanceToA / step);
            var to = (int)MathHelper.Ceiling(distanceToB / step);

            for (int i = from; i < to; i++)
            {
                cells.Add(view[i]);
            }

            // We should reverse cells to provide expected order (from a to b)
            if (edge.A.Position != a)
            {
                cells.Reverse();
            }

            return cells;
        }

        private MatrixView<Cell> GetCellsView(int index)
        {
            switch (index)
            {
                case 0:
                    {
                        var from = new Vector2i(Grid.GetLength(0) - 1, 0);
                        var to = new Vector2i(0, 0);
                        return new MatrixView<Cell>(Grid, from, to);
                    }
                case 1:
                    {
                        var from = new Vector2i(0, 0);
                        var to = new Vector2i(0, Grid.GetLength(1) - 1);
                        return new MatrixView<Cell>(Grid, from, to);
                    }
                case 2:
                    {
                        var from = new Vector2i(0, Grid.GetLength(1) - 1);
                        var to = new Vector2i(Grid.GetLength(0) - 1, Grid.GetLength(1) - 1);
                        return new MatrixView<Cell>(Grid, from, to);
                    }
                case 3:
                    {
                        var from = new Vector2i(Grid.GetLength(0) - 1, Grid.GetLength(1) - 1);
                        var to = new Vector2i(Grid.GetLength(0) - 1, 0);
                        return new MatrixView<Cell>(Grid, from, to);
                    }
                default:
                    throw new IndexOutOfRangeException(nameof(index));
            }
        }

        private IEnumerable<Vector2> EnumerateTextureCoords(Edge edge)
        {
            yield return edge.A.TextureCoords;
            yield return edge.B.TextureCoords;
        }

        private bool IsSequential(Side side)
        {
            for (int i = 0; i < side.Count - 1; i++)
            {
                var prev = side[i];
                var next = side[i + 1];

                if (prev.B != next.A)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsLoop(IReadOnlyList<Side> sides)
        {
            for (int i = 0; i < sides.Count; i++)
            {
                var prev = sides.GetCircular(i);
                var next = sides.GetCircular(i + 1);

                if (prev.Last().B != next.First().A)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsXAxisAligned(IEnumerable<Vector2> points)
        {
            return IsAxisAligned(points, Vector2.UnitX);
        }

        private bool IsYAxisAligned(IEnumerable<Vector2> points)
        {
            return IsAxisAligned(points, Vector2.UnitY);
        }

        private bool IsAxisAligned(IEnumerable<Vector2> points, Vector2 axis)
        {
            return points.Select(p => p * axis).AreAllSame();
        }
    }
}
