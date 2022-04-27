using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Core
{
    public class Engine : IDisposable
    {
        private readonly HashSet<GameObject> _gameObjects = new();

        public const int WindowWidth = 800;
        public const int WindowHeight = 600;
        public const string WindowTitle = "";

        public readonly Window Window;
        public readonly Camera Camera;
        public readonly Light Light;

        public bool IsRun { get; private set; }

        public IEnumerable<GameObject> GameObjects => _gameObjects;

        public Engine()
        {
            Window = new Window(WindowWidth, WindowHeight, WindowTitle);
            Camera = new Camera(Window, Vector3.Zero);
            Light = new Light();

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

        /// <summary>
        /// Method create new <see cref="GameObject"/> in engine.
        /// </summary>
        /// <returns>New <see cref="GameObject"/>.</returns>
        public GameObject CreateGameObject()
        {
            var go = new GameObject(this);
            _gameObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Method remove <see cref="GameObject"/> from engine.
        /// </summary>
        /// <returns>true, if <see cref="GameObject"/> was removed and false in other case.</returns>
        public bool RemoveGameObject(GameObject go)
        {
            return _gameObjects.Remove(go);
        }

        public void Run()
        {
            Window.Run();
        }

        private void OnWindowLoaded()
        {
            GL.ClearColor(0, 0, 0, 1);
            GL.Enable(EnableCap.DepthTest);
            Window.CursorGrabbed = true;

            foreach (var go in _gameObjects.ToList())
            {
                go.Start();
            }

            IsRun = true;
        }

        private void OnWindowUpdateFrame(FrameEventArgs args)
        {
            foreach (var go in _gameObjects.ToList())
            {
                go.GameUpdate(args);
            }
        }

        private void OnWindowRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (var go in _gameObjects.ToList())
            {
                go.RenderUpdate(args);
            }

            Window.SwapBuffers();
        }

        private void OnWindowUnloaded()
        {
            foreach (var go in _gameObjects.ToList())
            {
                go.Stop();
            }

            IsRun = false;
        }

        private void OnWindowResized(ResizeEventArgs args)
        {
            GL.Viewport(0, 0, Window.Size.X, Window.Size.Y);
        }
    }
}
