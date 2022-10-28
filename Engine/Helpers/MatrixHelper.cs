using OpenTK.Mathematics;

namespace GameEngine.Helpers
{
    public static class MatrixHelper
    {
        public static IEnumerable<T> Enumerate<T>(this T[,] matrix)
        {
            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    yield return matrix[x, y];
                }
            }
        }

        public static IEnumerable<Vector2i> GetNeighboursCross<T>(this T[,] cells, Vector2i coords)
        {
            if (coords.X < cells.GetLength(0) - 1)
            {
                yield return new Vector2i(coords.X + 1, coords.Y);
            }
            if (coords.Y > 0)
            {
                yield return new Vector2i(coords.X, coords.Y - 1);
            }
            if (coords.X > 0)
            {
                yield return new Vector2i(coords.X - 1, coords.Y);
            }
            if (coords.Y < cells.GetLength(1) - 1)
            {
                yield return new Vector2i(coords.X, coords.Y + 1);
            }
        }

        public static IEnumerable<Vector2i> GetNeighboursSquare<T>(this T[,] cells, Vector2i coords)
        {
            if (coords.Y > 0)
            {
                yield return new Vector2i(coords.X, coords.Y - 1);

                if (coords.X > 0)
                {
                    yield return new Vector2i(coords.X - 1, coords.Y - 1);
                }
                if (coords.X < cells.GetLength(0) - 1)
                {
                    yield return new Vector2i(coords.X + 1, coords.Y - 1);
                }
            }
            if (coords.X > 0)
            {
                yield return new Vector2i(coords.X - 1, coords.Y);
            }
            if (coords.X < cells.GetLength(0) - 1)
            {
                yield return new Vector2i(coords.X + 1, coords.Y);
            }
            if (coords.Y < cells.GetLength(1) - 1)
            {
                yield return new Vector2i(coords.X, coords.Y + 1);

                if (coords.X > 0)
                {
                    yield return new Vector2i(coords.X - 1, coords.Y + 1);
                }
                if (coords.X < cells.GetLength(0) - 1)
                {
                    yield return new Vector2i(coords.X + 1, coords.Y + 1);
                }
            }
        }

        public static T[,] RotateMatrixClockwise<T>(this T[,] oldMatrix)
        {
            T[,] newMatrix = new T[oldMatrix.GetLength(1), oldMatrix.GetLength(0)];
            int newColumn;
            int newRow = 0;

            for (int oldColumn = 0; oldColumn < oldMatrix.GetLength(1); oldColumn++)
            {
                newColumn = 0;

                for (int oldRow = oldMatrix.GetLength(0) - 1; oldRow >= 0; oldRow--)
                {
                    newMatrix[newRow, newColumn] = oldMatrix[oldRow, oldColumn];
                    newColumn++;
                }

                newRow++;
            }

            return newMatrix;
        }

        public static T[,] RotateMatrixCounterClockwise<T>(this T[,] oldMatrix)
        {
            T[,] newMatrix = new T[oldMatrix.GetLength(1), oldMatrix.GetLength(0)];
            int newColumn;
            int newRow = 0;

            for (int oldColumn = oldMatrix.GetLength(1) - 1; oldColumn >= 0; oldColumn--)
            {
                newColumn = 0;

                for (int oldRow = 0; oldRow < oldMatrix.GetLength(0); oldRow++)
                {
                    newMatrix[newRow, newColumn] = oldMatrix[oldRow, oldColumn];
                    newColumn++;
                }

                newRow++;
            }

            return newMatrix;
        }
    }
}
