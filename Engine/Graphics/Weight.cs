namespace GameEngine.Graphics
{
    public struct VertexWeights
    {
        public const int MaxBoneInfluence = 4;

        private List<float> _weights;
        private List<int> _bones;

        public readonly IReadOnlyList<float> Weights => _weights;
        public readonly IReadOnlyList<int> Bones => _bones;

        public VertexWeights(IEnumerable<float> weights, IEnumerable<int> bones)
        {
            _weights = weights.ToList();
            _bones = bones.ToList();

            if (Weights.Count > MaxBoneInfluence)
            {
                throw new ArgumentException($"{nameof(weights)} count > {MaxBoneInfluence}.");
            }
            if (Bones.Count > MaxBoneInfluence)
            {
                throw new ArgumentException($"{nameof(bones)} count > {MaxBoneInfluence}.");
            }
            if (Weights.Count != Bones.Count)
            {
                throw new ArgumentException($"{nameof(weights)} count != {nameof(bones)} count.");
            }

            while(Weights.Count < MaxBoneInfluence)
            {
                _weights.Add(0.0f);
                _bones.Add(-1);
            }
        }
    }
}
