using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GameEngine.Game
{
    public class Game : IDisposable
    {
        public const int WindowWidth = 800;
        public const int WindowHeight = 600;
        public const string WindowTitle = "";
        
        public readonly Engine Engine;
        public readonly Window Window;
        public readonly Camera Camera;

        public Game()
        {
            Engine = new Engine();
            Window = new Window(WindowWidth, WindowHeight, WindowTitle);
            Camera = new Camera(Window, Vector3.Zero);

            Window.Load += OnWindowLoaded;
            Window.UpdateFrame += OnWindowUpdateFrame;
            Window.RenderFrame += OnWindowRenderFrame;
            Window.Unload += OnWindowUnloaded;
            Window.Resize += OnWindowResized;
        }

        public void Dispose()
        {
            Window.Dispose();
        }

        public void Run()
        {
            Window.Run();
        }

        private void OnWindowLoaded()
        {
            GL.ClearColor(0, 0, 0, 1);
            GL.Enable(EnableCap.DepthTest);
            Engine.Start();
        }

        private void OnWindowUpdateFrame(FrameEventArgs args)
        {
            Engine.GameUpdate(args);
        }

        private void OnWindowRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Engine.RenderUpdate(args);
            Window.SwapBuffers();
        }

        private void OnWindowUnloaded()
        {
            Engine.Stop();
        }

        private void OnWindowResized(ResizeEventArgs args)
        {
            GL.Viewport(0, 0, Window.Size.X, Window.Size.Y);
        }
    }
}
