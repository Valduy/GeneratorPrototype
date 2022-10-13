namespace GameEngine.Graphics
{
    // Crutch for shader distribution
    public static class ShaderManager
    {
        private static readonly Dictionary<string, Shader> Shaders = new();

        public static Shader GetShader(string vertexShader, string fragmentShader)
        {
            var key = vertexShader + fragmentShader;

            if (Shaders.TryGetValue(key, out var shader))
            {
                return shader;
            }

            shader = new Shader(vertexShader, fragmentShader);
            Shaders[key] = shader;
            return shader;
        }
    }
}
