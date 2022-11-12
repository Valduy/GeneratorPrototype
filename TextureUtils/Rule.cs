using System.Drawing;

namespace TextureUtils
{
    public class Rule
    {
        private int _logicalResolution;

        public readonly Color[,] Logical;
        public readonly Color[,] Detailed;

        public Rule(Color[,] logical, Color[,] detailed)
        {
            if (logical.GetLength(0) != logical.GetLength(1))
            {
                throw new ArgumentException($"Mismatch of {nameof(logical)} dimensions length.");
            }
            if (detailed.GetLength(0) != detailed.GetLength(1))
            {
                throw new ArgumentException($"Mismatch of {nameof(detailed)} dimensions length.");
            }

            _logicalResolution = logical.GetLength(0);
            Logical = logical;
            Detailed = detailed;
        }

        public Color[] this[int index]
        {
            get
            {
                var result = new Color[_logicalResolution];

                switch (index)
                {
                    case 0:
                        for (int i = 0; i < _logicalResolution; i++)
                        {
                            result[i] = Logical[i, 0];
                        }

                        break;
                    case 1:
                        for (int i = 0; i < _logicalResolution; i++)
                        {
                            result[i] = Logical[0, i];
                        }

                        break;
                    case 2:
                        for (int i = 0; i < _logicalResolution; i++)
                        {
                            result[i] = Logical[i, _logicalResolution - 1];
                        }

                        break;
                    case 3:
                        for (int i = 0; i < _logicalResolution; i++)
                        {
                            result[i] = Logical[_logicalResolution - 1, i];
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
