namespace TextureUtils
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
    }
}
