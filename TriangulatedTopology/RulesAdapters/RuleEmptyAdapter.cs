using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology.RulesAdapters
{
    public class RuleEmptyAdapter : RuleAdapter
    {
        public RuleEmptyAdapter(int size) 
            : base(size)
        { }

        public override Color Access(Rule rule, int x, int y)
        {
            return rule.Logical[x, y];
        }
    }
}
