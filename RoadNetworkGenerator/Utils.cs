namespace RoadNetworkGenerator
{
    public static class Utils
    {
        public static (T? a, T? b, T? c) ToTriple<T>(this IEnumerable<T> sucessors) 
            => (sucessors.FirstOrDefault(), sucessors.Skip(1).FirstOrDefault(), sucessors.Skip(2).FirstOrDefault());
    }
}
