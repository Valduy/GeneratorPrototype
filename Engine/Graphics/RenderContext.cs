namespace GameEngine.Graphics
{
    public struct RenderContext
    {
        public readonly int VertexArrayObject;
        public readonly int VertexBufferObject;
        public readonly int Count;

        public RenderContext(int vertexArrayObject, int vertexBufferObject, int count)
        {
            VertexArrayObject = vertexArrayObject;
            VertexBufferObject = vertexBufferObject;
            Count = count;
        }
    }
}
