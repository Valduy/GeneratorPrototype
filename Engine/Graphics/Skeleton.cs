using System.Collections;

namespace GameEngine.Graphics
{
    public class Skeleton : IReadOnlyCollection<Bone>
    {
        private Bone _root;
        private int _count;

        public Bone Root => _root;
        public int Count => _count;

        public Skeleton(Bone root)
        {
            _root = root;
            _count = this.Count();
        }

        public Bone this[string index]
        {
            get
            {
                foreach (var bone in this)
                {
                    if (bone.Name == index)
                    {
                        return bone;
                    }
                }

                throw new ArgumentOutOfRangeException($"Skeleton does not contains bone with name: {index}.");
            }
        }

        public Bone this[int index]
        {
            get
            {
                foreach (var bone in this)
                {
                    if (bone.Id == index)
                    {
                        return bone;
                    }
                }

                throw new ArgumentOutOfRangeException($"Skeleton does not contains bone with index: {index}.");
            }
        }

        public IEnumerator<Bone> GetEnumerator()
        {
            var stack = new Stack<Bone>();
            stack.Push(Root);

            while (stack.Any())
            {
                var top = stack.Pop();
                
                foreach (var child in top.Children)
                {
                    stack.Push(child);
                }

                yield return top;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
