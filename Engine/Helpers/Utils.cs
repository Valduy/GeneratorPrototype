namespace GameEngine.Helpers
{
    public static class Utils
    {
        private static Random random = new();

        public static T GetCircular<T>(this IList<T> list, int index)
            => index >= list.Count ? list[index % list.Count]
                : index < 0 ? list[index % list.Count + list.Count] : list[index];

        public static T GetRandom<T>(this IReadOnlyList<T> list)
            => list[random.Next(0, list.Count)];

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(item))
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
