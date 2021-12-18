namespace GameEngine.Helpers
{
    public static class Utils
    {
        public static T GetCircular<T>(this IList<T> list, int index)
        {
            if (index >= list.Count)
            {
                return list[index % list.Count];
            }
            if (index < 0)
            {
                return list[index % list.Count + list.Count];
            }

            return list[index];
        }
    }
}
