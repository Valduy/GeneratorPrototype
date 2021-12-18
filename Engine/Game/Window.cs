using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GameEngine.Game
{
    public class Window : GameWindow
    {
        public readonly Renderer Renderer;
        

        public Window(int width, int height, string title = "")
            : base(GameWindowSettings.Default, CreateWindowSettings(width, height, title))
        {
            Renderer = new Renderer(this);
        }
        
        private static NativeWindowSettings CreateWindowSettings(int width, int height, string title) => new()
        {
            Size = new Vector2i(width, height),
            Title = title,
        };

        protected override void OnLoad()
        {
            base.OnLoad();
            Renderer.Initialize();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            Renderer.StartRender();
            base.OnRenderFrame(args);
            Renderer.StopRender();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            Renderer.Terminate();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }
    }
}
