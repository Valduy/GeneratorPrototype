using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace GameEngine.Graphics
{
    // A helper class, much like Shader, meant to simplify loading textures.
    public class Texture
    {
        public readonly int Handle;

        public static readonly Texture Default = LoadFromFile("Content/default.png");

        public static Texture LoadFromFile(string path, bool isShouldFlip = false)
        {
            using Stream stream = File.OpenRead(path);
            StbImage.stbi_set_flip_vertically_on_load(isShouldFlip ? 1 : 0);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            return GenerateTexture(image.Data, image.Width, image.Height);
        }

        public static Texture LoadFromMemory(byte[] data, int width, int height)
        {
            return GenerateTexture(data, width, height);
        }

        public Texture(int glHandle)
        {
            Handle = glHandle;
        }

        public void Use(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        private static Texture GenerateTexture(byte[] data, int width, int height)
        {
            int handle = GL.GenTexture();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, handle);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return new Texture(handle);
        }
    }
}
