using GameEngine.Core;
using Graph;
using TextureUtils;

namespace TriangulatedTopology.Props.Algorithms
{
    public interface INetAlgorithm
    {
        public bool CanProcessRule(Rule rule);
        public bool[] GetRuleConnections(Rule rule);
        public void ProcessNet(Engine engine, Net<LogicalNode> net);
    }
}
