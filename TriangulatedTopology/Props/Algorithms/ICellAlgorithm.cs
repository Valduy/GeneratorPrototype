using GameEngine.Core;

namespace TriangulatedTopology.Props.Algorithms
{
    public interface ICellAlgorithm
    {
        public bool ProcessCell(Engine engine, LogicalNode node);
    }
}
