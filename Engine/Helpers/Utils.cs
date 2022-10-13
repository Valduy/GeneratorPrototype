namespace GameEngine.Helpers
{
    public static class Utils
    {
        private static Random random = new();

        public static void UseSeed(int seed) 
            => random = new Random(seed);

        public static T GetCircular<T>(this IList<T> list, int index)
            => index >= list.Count ? list[index % list.Count]
                : index < 0 ? list[index % list.Count + list.Count] : list[index];

        public static T GetRandom<T>(this IReadOnlyList<T> list)
            => list[random.Next(0, list.Count)];

        public static void ShiftRight<T>(this IList<T> list)
        {
            var last = list[list.Count - 1];

            for (int j = list.Count - 1; j > 0; j--)
            {
                list[j] = list[j - 1];
            }

            list[0] = last;
        }

        public static IEnumerable<TResult> Zip<TFirst, TSecond, TThird, TResult>(
            this IEnumerable<TFirst> source, IEnumerable<TSecond> second, 
            IEnumerable<TThird> third, 
            Func<TFirst, TSecond, TThird, TResult> selector)
        {
            using IEnumerator<TFirst> iterator1 = source.GetEnumerator();
            using IEnumerator<TSecond> iterator2 = second.GetEnumerator();
            using IEnumerator<TThird> iterator3 = third.GetEnumerator();

            while (iterator1.MoveNext() && iterator2.MoveNext() && iterator3.MoveNext())
            {
                yield return selector(iterator1.Current, iterator2.Current, iterator3.Current);
            }
        }

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
