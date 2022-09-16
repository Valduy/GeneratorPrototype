using System.Drawing;

namespace PatternDemo
{
    public class Rule
    {
        //public const int LogicalResolution = 3;
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

        public readonly Color[,] Logical = new Color[LogicalResolution, LogicalResolution];
        public readonly Color[,] Detailed = new Color[DetailedResolution, DetailedResolution];

        public Rule()
        { }

        public Rule(Color[,] logical, Color[,] detailed)
        {
            Logical = logical;
            Detailed = detailed;
        }

        public Color[] this[int index]
        {
            get
            {
                var result = new Color[LogicalResolution];

                switch (index)
                {
                    case 0:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[i, 0];
                        }

                        break;
                    case 1:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[0, i];
                        }

                        break;
                    case 2:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[i, LogicalResolution - 1];
                        }

                        break;
                    case 3:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[LogicalResolution - 1, i];
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException("index");
                }

                return result;
            }
        }
    }
}
