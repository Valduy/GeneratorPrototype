﻿using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Core
{
    /// <summary>
    /// Class implement engine object. Engine object is container for components.
    /// Components declare properties and behaviour of engine object.
    /// </summary>
    public class GameObject
    {
        private readonly List<GameObject> _children = new();
        private readonly Dictionary<Type, Component> _componentsMap = new();

        /// <summary>
        /// The <see cref="Core.Engine"/> this <see cref="GameObject"/> belong to.
        /// </summary>
        public readonly Engine Engine;

        /// <summary>
        /// Parent <see cref="GameObject"/>.
        /// </summary>
        public GameObject? Parent { get; private set; }

        /// <summary>
        /// Children <see cref="GameObject"/>'s.
        /// </summary>
        public IReadOnlyList<GameObject> Children => _children;

        /// <summary>
        /// Local rotation in degrees relative to <see cref="Parent"/>.
        /// </summary>
        public Vector3 LocalRotation { get; set; } = new(0);

        /// <summary>
        /// Local scale relative to <see cref="Parent"/>.
        /// </summary>
        public Vector3 LocalScale { get; set; } = new(1);

        /// <summary>
        /// Local position relative to <see cref="Parent"/>.
        /// </summary>
        public Vector3 LocalPosition { get; set; } = new(0);

        /// <summary>
        /// World rotation in degrees.
        /// </summary>
        public Vector3 Rotation
        {
            get => Parent != null 
                ? Parent.Rotation + LocalRotation 
                : LocalRotation;
            set => LocalRotation = Parent != null 
                ? Parent.Rotation - value 
                : value;
        }

        /// <summary>
        /// World scale.
        /// </summary>
        public Vector3 Scale
        {
            get => Parent != null 
                ? Parent.Scale * LocalScale 
                : LocalScale;
            set => LocalScale = Parent != null 
                ? new Vector3(value.X / Parent.Scale.X, value.Y / Parent.Scale.Y, value.Z / Parent.Scale.Z)
                : value;
        }

        /// <summary>
        /// World position.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (Parent == null) return LocalPosition;
                var offset = new Vector4(LocalPosition, 1);
                var rotation = new Quaternion(Rotation * MathHelper.Pi / 180);
                offset *= Matrix4.CreateFromQuaternion(rotation);
                offset *= Matrix4.CreateTranslation(Parent.Position);
                return offset.Xyz;

            }
            set
            {
                if (Parent != null)
                {
                    var offset = new Vector4(value, 1);
                    offset *= Matrix4.CreateTranslation(-Parent.Position);
                    offset *= Matrix4.CreateFromQuaternion(new Quaternion(-Rotation * MathHelper.Pi / 180));
                    LocalPosition = new Vector3(offset);
                }
                else
                {
                    LocalPosition = value;
                }
            }
        }

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

        public void AddChild(GameObject go)
        {
            if (go.Parent == this) return;
            go.Parent?.RemoveChild(go);
            go.Parent = this;
            _children.Add(go);
        }

        public void RemoveChild(GameObject go)
        {
            if (go.Parent == this)
            {
                go.Parent = null;
                _children.Remove(go);
            }
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
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.Start();
                }
            }
        }

        public void GameUpdate(FrameEventArgs args)
        {
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.GameUpdate(args);
                }
            }
        }

        public void RenderUpdate(FrameEventArgs args)
        {
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.RenderUpdate(args);
                }
            }
        }

        public void Stop()
        {
            foreach (var component in _componentsMap.ToList())
            {
                if (component.Value.GameObject == this)
                {
                    component.Value.Stop();
                }
            }
        }
    }
}
