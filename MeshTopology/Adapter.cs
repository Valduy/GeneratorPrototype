using GameEngine.Helpers;
using GameEngine.Graphics;
using System.Collections;

namespace MeshTopology
{
    public class Adapter : IReadOnlyList<Vertex>
    {
        private int _index;

        public readonly TopologyNode Pivot;
        public readonly TopologyNode Neighbour;

        public int Count => Neighbour.Face.Count;

        public Vertex this[int index]
        {
            get
            {
                if (index < 0 || index >= 4)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return Neighbour.Face.GetCircular(_index + index);
            }
        }

        public Adapter(TopologyNode pivot, TopologyNode neighbour)
        {
            if (!pivot.Neighbours.Contains(neighbour))
            {
                throw new ArgumentException();
            }

            Pivot = pivot;
            Neighbour = neighbour;

            var sharedEdge = Neighbour.Face.GetSharedEdge(Pivot.Face);
            var pivotSharedIndex = Pivot.Face.GetEdgeIndex(e => e.HasSamePositions(sharedEdge));
            var neighbourSharedIndex = Neighbour.Face.GetEdgeIndex(e => e.HasSamePositions(sharedEdge));

            _index = Neighbour.Face.IndexOf(v => v.Position == sharedEdge.A.Position);

            Console.WriteLine($"Neighbour: {pivotSharedIndex}");

            switch (pivotSharedIndex)
            {
                case 0:
                    {
                        _index -= 2;
                        
                        Console.WriteLine($"{this[1].Position}\t---\t{this[0].Position}");
                        Console.WriteLine($"{this[2].Position}\t---\t{this[3].Position}");
                        Console.WriteLine($"{Pivot.Face[1].Position}\t---\t{Pivot.Face[0].Position}");
                        Console.WriteLine($"{Pivot.Face[2].Position}\t---\t{Pivot.Face[3].Position}");
                        break;
                    }
                case 1:
                    {
                        _index += 1;

                        Console.WriteLine($"{this[1].Position}\t---\t{this[0].Position}{Pivot.Face[1].Position}\t---\t{Pivot.Face[0].Position}");
                        Console.WriteLine($"{this[2].Position}\t---\t{this[3].Position}{Pivot.Face[2].Position}\t---\t{Pivot.Face[3].Position}");
                        break;
                    }
                case 2:
                    {

                        Console.WriteLine($"{Pivot.Face[1].Position}\t---\t{Pivot.Face[0].Position}");
                        Console.WriteLine($"{Pivot.Face[2].Position}\t---\t{Pivot.Face[3].Position}");
                        Console.WriteLine($"{this[1].Position}\t---\t{this[0].Position}");
                        Console.WriteLine($"{this[2].Position}\t---\t{this[3].Position}");                        
                        break;
                    }
                case 3:
                    {
                        _index -= 1;

                        Console.WriteLine($"{Pivot.Face[1].Position}\t---\t{Pivot.Face[0].Position}{this[1].Position}\t---\t{this[0].Position}");
                        Console.WriteLine($"{Pivot.Face[2].Position}\t---\t{Pivot.Face[3].Position}{this[2].Position}\t---\t{this[3].Position}");
                        break;
                    }
                default:
                    {
                        throw new ArgumentException();
                    }                
            }

            Console.WriteLine();
        }

        public IEnumerator<Vertex> GetEnumerator()
        {
            return Neighbour.Face.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
