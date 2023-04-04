using GameEngine.Core;

namespace UVWfc.Props.Algorithms
{
    public interface ICellAlgorithm
    {
        public bool ProcessCell(Engine engine, LogicalNode node);
    }
}
