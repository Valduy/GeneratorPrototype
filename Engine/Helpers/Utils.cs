namespace GameEngine.Helpers
{
    public static class Utils
    {
        private static Random random = new();

        public static T GetCircular<T>(this IList<T> list, int index) 
            => index >= list.Count ? list[index % list.Count] 
                : index < 0 ? list[index % list.Count + list.Count] : list[index];

        public static T GetRandom<T>(this IList<T> list)
            => list[random.Next(0, list.Count)];
    }
}
