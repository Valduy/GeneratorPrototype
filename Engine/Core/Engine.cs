using System.Collections;
using OpenTK.Windowing.Common;

namespace GameEngine.Core
{
    /// <summary>
    /// Class implement engine logic. It manage <see cref="GameObject"/>'s and implement engine tick methods.
    /// </summary>
    public class Engine : IEnumerable<GameObject>
    {
        private readonly HashSet<GameObject> _gameObjects = new();

        public bool IsRun { get; private set; }

        public IEnumerator<GameObject> GetEnumerator() 
            => _gameObjects.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

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

        public void Start()
        {
            foreach (var go in _gameObjects.ToList())
            {
                go.Start();
            }

            IsRun = true;
        }

        public void GameUpdate(FrameEventArgs args)
        {
            foreach (var go in _gameObjects.ToList())
            {
                go.GameUpdate(args);
            }
        }

        public void RenderUpdate(FrameEventArgs args)
        {
            foreach (var go in _gameObjects.ToList())
            {
                go.RenderUpdate(args);
            }
        }

        public void Stop()
        {
            foreach (var go in _gameObjects.ToList())
            {
                go.Stop();
            }

            IsRun = false;
        }
    }
}