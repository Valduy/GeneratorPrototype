namespace RoadNetworkGenerator
{
    public static class Utils
    {
        public static (T? a, T? b, T? c) ToTriple<T>(this IEnumerable<T> sucessors)
        {
            (T? a, T? b, T? c) result;

            result.a = sucessors.FirstOrDefault();
            sucessors.Skip(1);

            result.b = sucessors.FirstOrDefault();
            sucessors.Skip(1);

            result.c = sucessors.FirstOrDefault();
            sucessors.Skip(1);

            return result;
        }
    }
}
