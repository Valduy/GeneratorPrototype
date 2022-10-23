namespace GameEngine.Helpers
{
    public static class CollectionsHelper
    {
        private static Random random = new();

        public static void UseSeed(int seed) 
            => random = new Random(seed);

        //public static T GetCircular<T>(this IList<T> list, int index)
        //    => index >= list.Count ? list[index % list.Count]
        //        : index < 0 ? list[index % list.Count + list.Count] : list[index];

        public static T GetCircular<T>(this IReadOnlyList<T> list, int index)
            => index >= list.Count ? list[index % list.Count]
                : index < 0 ? list[index % list.Count + list.Count] : list[index];

        public static T GetRandom<T>(this IReadOnlyList<T> list)
            => list[random.Next(0, list.Count)];

        public static void ShiftRight<T>(this IList<T> list)
        {
            var last = list[list.Count - 1];

            for (int i = list.Count - 1; i > 0; i--)
            {
                list[i] = list[i - 1];
            }

            list[0] = last;
        }

        public static void ShiftLeft<T>(this IList<T> list)
        {
            var first = list[0];

            for (int i = 0; i < list.Count - 2; i++)
            {
                list[i] = list[i + 1];
            }

            list[list.Count - 1] = first;
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

        public static bool AreAllSame<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                var toCompare = default(T);

                if (enumerator.MoveNext())
                {
                    toCompare = enumerator.Current;
                }

                while (enumerator.MoveNext())
                {
                    if (toCompare != null && !toCompare.Equals(enumerator.Current))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null && item == null)
                {
                    return i;
                }

                if (list[i] != null && item != null)
                {
                    if (list[i]!.Equals(item))
                    {
                        return i;
                    }
                }
            }

            throw new ArgumentOutOfRangeException();
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, Func<T, bool> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i])) 
                { 
                    return i; 
                }
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
