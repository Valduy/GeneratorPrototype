namespace TriangulatedTopology.Wfc
{
    public static class CellHepler
    {
        private static HashSet<Cell> _defenitions = new();

        public static bool IsDefined(this Cell cell)
            => _defenitions.Contains(cell);

        public static void Define(this Cell cell)
            => _defenitions.Add(cell);

        public static void Undefine(this Cell cell)
            => _defenitions.Remove(cell);
    }
}
