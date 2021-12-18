using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Core
{
    /// <summary>
    /// Class implement game object. Game object is container for components.
    /// Components declare properties and behaviour of game object.
    /// </summary>
    public class GameObject
    {
        private readonly Dictionary<Type, Component> _componentsMap = new();

        /// <summary>
        /// The <see cref="Core.Engine"/> this <see cref="GameObject"/> belong to.
        /// </summary>
        public readonly Engine Engine;

        /// <summary>
        /// World rotation.
        /// </summary>
        public float Rotation { get; set; } = 0;

        /// <summary>
        /// World scale.
        /// </summary>
        public Vector2 Scale { get; set; } = new(1);

        /// <summary>
        /// World position.
        /// </summary>
        public Vector2 Position { get; set; } = new(0);

        /// <summary>
        /// Event, which fires when new <see cref="Component"/> added to <see cref="GameObject"/>.
        /// </summary>
        public event Action<GameObject, Component>? ComponentAdded;

        /// <summary>
        /// Event, which fires when <see cref="Component"/> removed from <see cref="GameObject"/>.
        /// </summary>
        public event Action<GameObject, Component>? ComponentRemoved;

        internal GameObject(Engine engine)
        {
            Engine = engine;
        }

        /// <summary>
        /// Method add new <see cref="Component"/> to <see cref="GameObject"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Component"/>.</typeparam>
        /// <returns><see cref="Component"/>, added to <see cref="GameObject"/>.</returns>
        public T Add<T>() 
            where T : Component, new() 
            => (T) Add(typeof(T));

        /// <summary>
        /// Method add new <see cref="Component"/> to <see cref="GameObject"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Component"/>.</typeparam>
        /// <param name="factory"><see cref="Component"/> factory.</param>
        /// <returns><see cref="Component"/>, added to <see cref="GameObject"/>.</returns>
        public T Add<T>(Func<T> factory) where T : Component
            => (T) Add(() => factory() as Component);

        /// <summary>
        /// Method add new <see cref="Component"/> to <see cref="GameObject"/>.
        /// </summary>
        /// <param name="componentType">Type of <see cref="Component"/>.</param>
        /// <returns><see cref="Component"/>, added to <see cref="GameObject"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public Component Add(Type componentType)
        {
            if (Activator.CreateInstance(componentType) is not Component instance)
            {
                throw new ArgumentException(
                    $"Not valid type. It should inherit from {typeof(Component)} and have default constructor.");
            }

            if (_componentsMap.TryGetValue(componentType, out var component))
            {
                Remove(component.GetType());
            }
            
            _componentsMap[instance.GetType()] = instance;
            instance.GameObject = this;

            if (Engine.IsRun)
            {
                instance.Start();
            }

            ComponentAdded?.Invoke(this, instance);
            return instance;
        }

        /// <summary>
        /// Method add new <see cref="Component"/> to <see cref="GameObject"/>.
        /// </summary>
        /// <param name="factory"><see cref="Component"/> factory.</param>
        /// <returns><see cref="Component"/>, added to <see cref="GameObject"/>.</returns>
        public Component Add(Func<Component> factory)
        {
            var instance = factory();

            if (_componentsMap.TryGetValue(instance.GetType(), out var component))
            {
                Remove(component.GetType());
            }

            _componentsMap[instance.GetType()] = instance;
            instance.GameObject = this;

            if (Engine.IsRun)
            {
                instance.Start();
            }

            ComponentAdded?.Invoke(this, instance);
            return instance;
        }

        /// <summary>
        /// Method return <see cref="Component"/> of specified type.
        /// </summary>
        /// <typeparam name="T"><see cref="Component"/> type.</typeparam>
        /// <returns><see cref="Component"/> if it's exist, or null.</returns>
        public T? Get<T>() where T : Component
            => (T?) Get(typeof(T));

        /// <summary>
        /// Method return <see cref="Component"/> of specified type.
        /// </summary>
        /// <param name="componentType"><see cref="Component"/> type.</param>
        /// <returns><see cref="Component"/> if it's exist, or null.</returns>
        public Component? Get(Type componentType)
            => _componentsMap.TryGetValue(componentType, out var component) ? component : null;

        /// <summary>
        /// Remove <see cref="Component"/> of specified type.
        /// </summary>
        /// <typeparam name="T"><see cref="Component"/> type.</typeparam>
        /// <returns><see cref="Component"/> if it was removed and null in other case.</returns>
        public T? Remove<T>() where T : Component
            => (T?) Remove(typeof(T));

        /// <summary>
        /// Remove <see cref="Component"/> of specified type.
        /// </summary>
        /// <param name="componentType"><see cref="Component"/> type.</param>
        /// <returns><see cref="Component"/> if it was removed and null in other case.</returns>
        public Component? Remove(Type componentType)
        {
            if (_componentsMap.TryGetValue(componentType, out var component))
            {
                _componentsMap.Remove(componentType);

                if (Engine.IsRun)
                {
                    component.Stop();
                }

                if (Engine.IsRun)
                {
                    component.Stop();
                }
                
                component.GameObject = null;
                ComponentRemoved?.Invoke(this, component);
                return component;
            }

            return null;
        }

        public void Start()
        {
            foreach (var component in _componentsMap)
            {
                component.Value.Start();
            }
        }

        public void GameUpdate(FrameEventArgs args)
        {
            foreach (var component in _componentsMap)
            {
                component.Value.GameUpdate(args);
            }
        }

        public void RenderUpdate(FrameEventArgs args)
        {
            foreach (var component in _componentsMap)
            {
                component.Value.RenderUpdate(args);
            }
        }

        public void Stop()
        {
            foreach (var component in _componentsMap)
            {
                component.Value.Stop();
            }
        }
    }
}
