namespace GameEngine.Helpers
{
    public static class Utils
    {
        public static T GetCircular<T>(this IList<T> list, int index) 
            => index >= list.Count ? list[index % list.Count] 
                : index < 0 ? list[index % list.Count + list.Count] : list[index];
    }
}
