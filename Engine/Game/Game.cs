using GameEngine.Core;
using OpenTK.Windowing.Common;

namespace GameEngine.Game
{
    public class Game : IDisposable
    {
        public const int WindowWidth = 800;
        public const int WindowHeight = 600;
        public const string WindowTitle = "";
        
        public readonly Engine Engine = new();
        public readonly Window Window = new(WindowWidth, WindowHeight, WindowTitle);
        
        public Game()
        {
            Window.Load += OnWindowLoaded;
            Window.UpdateFrame += OnWindowUpdateFrame;
            Window.RenderFrame += OnWindowRenderFrame;
            Window.Unload += OnWindowUnloaded;
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
            Engine.Start();
        }

        private void OnWindowUpdateFrame(FrameEventArgs args)
        {
            Engine.GameUpdate(args);
        }

        private void OnWindowRenderFrame(FrameEventArgs args)
        {
            Engine.RenderUpdate(args);
        }

        private void OnWindowUnloaded()
        {
            Engine.Stop();
        }
    }
}
